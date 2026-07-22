using CaseGame.Buildings;
using CaseGame.Grid;
using UnityEngine;
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
    /// The actual decision logic (<see cref="BeginPlacement"/>/<see cref="UpdateGhostAt"/>/
    /// <see cref="TryCommitAt"/>/<see cref="CancelPlacement"/>) takes an explicit cell and is
    /// callable directly, independent of <see cref="Update"/>'s mouse reading — humble
    /// MonoBehaviour, testable logic, per CONVENTIONS.md.
    /// </summary>
    public class PlacementController : MonoBehaviour
    {
        [SerializeField] private Camera gameCamera;

        private GridModel _grid;
        private BuildingFactory _factory;
        private BuildingDefinition _currentDefinition;
        private BuildingBase _currentPrefab;
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

        public void BeginPlacement(BuildingDefinition definition, BuildingBase prefab)
        {
            CancelPlacement();

            _currentDefinition = definition;
            _currentPrefab = prefab;
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

            _factory.Release(_currentPrefab, _ghostInstance);
            _ghostInstance = null;
            _ghostView = null;
        }

        /// <summary>Moves the ghost to the given cell and updates its valid/invalid tint. No-op if not currently placing.</summary>
        public void UpdateGhostAt(Vector2Int cell)
        {
            if (_ghostInstance == null)
            {
                return;
            }

            _ghostInstance.transform.position = _grid.CellToWorld(cell);
            _ghostView.SetValid(IsCellValid(cell));
        }

        /// <summary>Attempts to commit the building at the given cell. Returns false (no-op) if the area isn't free.</summary>
        public bool TryCommitAt(Vector2Int cell)
        {
            if (_ghostInstance == null || !IsCellValid(cell))
            {
                return false;
            }

            _grid.SetAreaOccupied(cell, _currentDefinition.Footprint, true);
            _ghostInstance.transform.position = _grid.CellToWorld(cell);
            _ghostView.Commit();

            _ghostInstance = null;
            _ghostView = null;
            return true;
        }

        private bool IsCellValid(Vector2Int cell)
        {
            return _grid.IsAreaFree(cell, _currentDefinition.Footprint);
        }

        private void Update()
        {
            if (!IsPlacing)
            {
                return;
            }

            var cell = _grid.WorldToCell(ScreenToWorld(Mouse.current.position.ReadValue()));
            UpdateGhostAt(cell);

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
