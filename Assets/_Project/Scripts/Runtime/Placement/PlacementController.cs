using CaseGame.Buildings;
using CaseGame.Grid;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CaseGame.Placement
{
    /// <summary>
    /// Controller: drives building placement. Tracks the mouse over the grid, moves a ghost
    /// preview (<see cref="BuildingGhostView"/>) and tints it green/valid or red/invalid by
    /// querying <see cref="GridModel"/> each frame — no coupling to specific building types
    /// (GI-3) — and commits (marks grid cells occupied, reveals the real sprite) or cancels
    /// (returns the pooled instance) on input.
    ///
    /// The <c>cell</c> passed to <see cref="UpdateGhostAt"/>/<see cref="TryCommitAt"/> is the
    /// cell under the cursor, treated as the *center* of the footprint, not its bottom-left
    /// corner — the building's visual art is centered on its own GameObject origin, so
    /// positioning by corner instead of center would visibly misalign the sprite from the cells
    /// it actually occupies (see ARCHITECTURE.md decisions log).
    ///
    /// The actual decision logic (<see cref="BeginPlacement"/>/<see cref="UpdateGhostAt"/>/
    /// <see cref="TryCommitAt"/>/<see cref="CancelPlacement"/>) takes an explicit cell and is
    /// callable directly, independent of <see cref="Update"/>'s mouse reading — humble
    /// MonoBehaviour, testable logic, per CONVENTIONS.md.
    /// </summary>
    public class PlacementController : MonoBehaviour
    {
        [SerializeField] private Camera gameCamera;
        [SerializeField] private BuildingCatalogEntryEventChannel produceRequestedChannel;
        [SerializeField] private BuildingRemovalRequestedEventChannel removalRequestedChannel;

        private GridModel _grid;
        private BuildingFactory _factory;
        private BuildingDefinition _currentDefinition;
        private BuildingBase _ghostInstance;
        private BuildingGhostView _ghostView;

        public bool IsPlacing => _ghostInstance != null;

        /// <summary>The instance currently being placed, or null. Exposed for external observers (and tests) — PlacementController owns it while placing, but doesn't hide its existence.</summary>
        public BuildingBase CurrentGhost => _ghostInstance;

        /// <summary>Explicit initialization (not Awake-wired) — Awake order between this and whatever owns the GridModel/Factory isn't guaranteed, so the scene's bootstrap calls this once both exist.</summary>
        public void Initialize(GridModel grid, BuildingFactory factory)
        {
            _grid = grid;
            _factory = factory;
        }

        private void OnEnable()
        {
            if (produceRequestedChannel != null)
            {
                produceRequestedChannel.Subscribe(HandleProduceRequested);
            }

            if (removalRequestedChannel != null)
            {
                removalRequestedChannel.Subscribe(HandleRemovalRequested);
            }
        }

        private void OnDisable()
        {
            if (produceRequestedChannel != null)
            {
                produceRequestedChannel.Unsubscribe(HandleProduceRequested);
            }

            if (removalRequestedChannel != null)
            {
                removalRequestedChannel.Unsubscribe(HandleRemovalRequested);
            }
        }

        /// <summary>Responds to the Production Menu's "produce" click (<see cref="BuildingCatalogEntryEventChannel"/>) by starting placement of that entry — UI.Production and Placement stay decoupled, connected only via the shared channel asset.</summary>
        private void HandleProduceRequested(BuildingCatalogEntry entry)
        {
            BeginPlacement(entry.Definition, entry.Prefab);
        }

        /// <summary>Responds to the Information Panel's "remove" click (<see cref="BuildingRemovalRequestedEventChannel"/>) — Placement owns "commit to grid," so it also owns "uncommit from grid," symmetric with <see cref="HandleProduceRequested"/>.</summary>
        private void HandleRemovalRequested(BuildingBase building)
        {
            RemoveBuilding(building);
        }

        public void BeginPlacement(BuildingDefinition definition, BuildingBase prefab)
        {
            CancelPlacement();

            _currentDefinition = definition;
            _ghostInstance = _factory.Create(definition, prefab);
            _ghostView = _ghostInstance.GetComponent<BuildingGhostView>();
            _ghostView.ShowGhost();
        }

        public void CancelPlacement()
        {
            if (_ghostInstance == null)
            {
                return;
            }

            _factory.Release(_ghostInstance);
            _ghostInstance = null;
            _ghostView = null;
        }

        /// <summary>Frees the building's grid footprint and returns its instance to the pool — entirely independent of Health/<c>ApplyDamage</c>, unlike combat destruction (see ARCHITECTURE.md decisions log).</summary>
        public void RemoveBuilding(BuildingBase building)
        {
            if (building == null)
            {
                return;
            }

            building.ReleaseFootprint();
            _factory.Release(building);
        }

        /// <summary>Moves the ghost to the given cell and updates its valid/invalid tint. No-op if not currently placing.</summary>
        public void UpdateGhostAt(Vector2Int cell)
        {
            if (_ghostInstance == null)
            {
                return;
            }

            var origin = ComputeFootprintOrigin(cell);
            _ghostInstance.transform.position = _grid.FootprintCenterToWorld(origin, _currentDefinition.Footprint);
            _ghostView.SetValid(_grid.IsAreaFree(origin, _currentDefinition.Footprint));
        }

        /// <summary>Attempts to commit the building at the given cell. Returns false (no-op) if the area isn't free.</summary>
        public bool TryCommitAt(Vector2Int cell)
        {
            if (_ghostInstance == null || !IsCellValid(cell))
            {
                return false;
            }

            var origin = ComputeFootprintOrigin(cell);
            _grid.SetAreaOccupied(origin, _currentDefinition.Footprint, true);
            _ghostInstance.transform.position = _grid.FootprintCenterToWorld(origin, _currentDefinition.Footprint);
            _ghostInstance.SetPlacement(_grid, origin);
            _ghostView.Commit();

            _ghostInstance = null;
            _ghostView = null;
            return true;
        }

        private bool IsCellValid(Vector2Int cell)
        {
            var origin = ComputeFootprintOrigin(cell);
            return _grid.IsAreaFree(origin, _currentDefinition.Footprint);
        }

        /// <summary>Converts the cursor's cell (footprint center) to the footprint's bottom-left cell (the shape <see cref="GridModel.IsAreaFree"/>/<see cref="GridModel.SetAreaOccupied"/> already expect). Integer division biases even-sized footprints slightly toward the origin corner — standard, expected behavior for centering an even-sized selection on a single cell.</summary>
        private Vector2Int ComputeFootprintOrigin(Vector2Int hoverCell)
        {
            var footprint = _currentDefinition.Footprint;
            return hoverCell - new Vector2Int(footprint.x / 2, footprint.y / 2);
        }

        private void Update()
        {
            if (!IsPlacing)
            {
                return;
            }

            var cell = _grid.WorldToCell(ScreenToWorld(Mouse.current.position.ReadValue()));
            UpdateGhostAt(cell);

            // Clicks over UI (the Production Menu/Info Panel) shouldn't also act on the world
            // underneath them — e.g. clicking a Production Menu row shouldn't simultaneously
            // commit/cancel whatever ghost happens to be at that same screen position.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                CancelPlacement();
            }
            else if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                TryCommitAt(cell);
            }
        }

        private Vector3 ScreenToWorld(Vector2 screenPosition)
        {
            var depth = -gameCamera.transform.position.z;
            return gameCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
        }
    }
}
