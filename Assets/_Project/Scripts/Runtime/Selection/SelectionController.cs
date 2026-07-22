using System.Collections.Generic;
using CaseGame.Buildings;
using CaseGame.Combat;
using CaseGame.Entities;
using CaseGame.Grid;
using CaseGame.Units;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CaseGame.Selection
{
    /// <summary>
    /// Controller: left-click select / right-click move-or-attack (GI-7/8/10/11). A plain click
    /// replaces the current selection; shift-click adds/removes a soldier (the brief's "unit(s)"
    /// wording implies multi-select; shift-click is the minimal mechanism that doesn't require a
    /// new drag-box visual system — see ARCHITECTURE.md decisions log). Selecting a building and
    /// selecting soldiers are mutually exclusive "modes" — selecting one clears the other, since
    /// only soldiers take move/attack commands and only buildings are shown on the (future)
    /// Information Panel.
    ///
    /// Visual selection feedback is <see cref="GameEntityBase.SetSelected"/> — no new prefab
    /// wiring needed, unlike Placement's ghost. Right-click attack calls
    /// <see cref="SoldierBase.TryAttack"/> directly (instant, no range/approach — the brief
    /// doesn't require walking into range, and <c>TryAttack</c>'s own doc comment already
    /// anticipated Selection wiring it up exactly like this).
    ///
    /// The actual decisions (<see cref="HandleLeftClick"/>/<see cref="HandleRightClick"/>) take
    /// explicit, already-resolved inputs and are callable directly, independent of
    /// <see cref="Update"/>'s mouse/hit-test reading — the same "extract the testable decision,
    /// keep the MonoBehaviour thin" pattern used by <c>PlacementController</c>/
    /// <c>ProductionMenuController</c>.
    /// </summary>
    public class SelectionController : MonoBehaviour
    {
        [SerializeField] private Camera gameCamera;
        [SerializeField] private SelectedBuildingEventChannel selectedBuildingChannel;
        [SerializeField] private BuildingRemovalRequestedEventChannel removalRequestedChannel;

        private GridModel _grid;
        private BuildingBase _selectedBuilding;
        private readonly List<SoldierBase> _selectedSoldiers = new List<SoldierBase>();

        public BuildingBase SelectedBuilding => _selectedBuilding;
        public IReadOnlyList<SoldierBase> SelectedSoldiers => _selectedSoldiers;

        /// <summary>Explicit initialization (not Awake-wired), mirroring <c>PlacementController.Initialize</c> — the scene's bootstrap calls this once a <see cref="GridModel"/> exists.</summary>
        public void Initialize(GridModel grid)
        {
            _grid = grid;
        }

        private void OnEnable()
        {
            if (removalRequestedChannel != null)
            {
                removalRequestedChannel.Subscribe(HandleBuildingRemoved);
            }
        }

        private void OnDisable()
        {
            if (removalRequestedChannel != null)
            {
                removalRequestedChannel.Unsubscribe(HandleBuildingRemoved);
            }
        }

        /// <summary>Reacts to a building being removed elsewhere (<see cref="BuildingRemovalRequestedEventChannel"/>) by clearing the selection if it was the one selected. Removal doesn't touch Health/<c>IsDead</c> the way combat death does, so this proactive subscription is what keeps a removed building from lingering as "selected" — mirrors <see cref="SetSelectedBuilding"/>'s existing <c>IsDead</c> guard, which still covers the combat-death case.</summary>
        public void HandleBuildingRemoved(BuildingBase building)
        {
            if (_selectedBuilding == building)
            {
                SetSelectedBuilding(null);
            }
        }

        /// <param name="hitEntity">Whatever was under the cursor, or null for empty ground.</param>
        /// <param name="additive">True if the multi-select modifier (shift) was held.</param>
        public void HandleLeftClick(GameEntityBase hitEntity, bool additive)
        {
            switch (hitEntity)
            {
                case BuildingBase building:
                    SelectBuilding(building);
                    break;
                case SoldierBase soldier:
                    SelectSoldier(soldier, additive);
                    break;
                default:
                    if (!additive)
                    {
                        ClearSelection();
                    }
                    break;
            }
        }

        /// <param name="cell">The grid cell under the cursor — used as the move destination when there's no attack target.</param>
        /// <param name="hitTarget">Whatever damageable was under the cursor, or null. Non-null takes priority over movement (GI-10/11).</param>
        public void HandleRightClick(Vector2Int cell, IDamageable hitTarget)
        {
            _selectedSoldiers.RemoveAll(soldier => soldier == null || soldier.IsDead);

            foreach (var soldier in _selectedSoldiers)
            {
                if (hitTarget != null)
                {
                    soldier.TryAttack(hitTarget);
                }
                else
                {
                    soldier.MoveTo(cell, _grid);
                }
            }
        }

        public void ClearSelection()
        {
            DeselectAllSoldiers();
            SetSelectedBuilding(null);
        }

        private void SelectBuilding(BuildingBase building)
        {
            DeselectAllSoldiers();
            SetSelectedBuilding(building);
        }

        private void SelectSoldier(SoldierBase soldier, bool additive)
        {
            SetSelectedBuilding(null);

            if (additive)
            {
                ToggleSoldier(soldier);
            }
            else
            {
                ReplaceSoldierSelection(soldier);
            }
        }

        private void ToggleSoldier(SoldierBase soldier)
        {
            if (_selectedSoldiers.Remove(soldier))
            {
                soldier.SetSelected(false);
            }
            else
            {
                _selectedSoldiers.Add(soldier);
                soldier.SetSelected(true);
            }
        }

        private void ReplaceSoldierSelection(SoldierBase soldier)
        {
            foreach (var existing in _selectedSoldiers)
            {
                if (existing != null && existing != soldier)
                {
                    existing.SetSelected(false);
                }
            }

            _selectedSoldiers.Clear();
            _selectedSoldiers.Add(soldier);
            soldier.SetSelected(true);
        }

        private void DeselectAllSoldiers()
        {
            foreach (var soldier in _selectedSoldiers)
            {
                if (soldier != null)
                {
                    soldier.SetSelected(false);
                }
            }

            _selectedSoldiers.Clear();
        }

        private void SetSelectedBuilding(BuildingBase building)
        {
            if (_selectedBuilding != null && _selectedBuilding.IsDead)
            {
                // The previously-selected building died in combat elsewhere, without going
                // through this controller — don't let a stale reference (possibly since reused
                // by pooling for an unrelated building) short-circuit the equality check below.
                // Mirrors the soldier-pruning pattern in HandleRightClick (decisions log #39).
                // Manual removal is a separate, non-Health trigger — handled proactively by
                // HandleBuildingRemoved instead, since removal never sets IsDead.
                _selectedBuilding = null;
            }

            if (_selectedBuilding == building)
            {
                return;
            }

            if (_selectedBuilding != null)
            {
                _selectedBuilding.SetSelected(false);
            }

            _selectedBuilding = building;

            if (_selectedBuilding != null)
            {
                _selectedBuilding.SetSelected(true);
            }

            if (selectedBuildingChannel != null)
            {
                selectedBuildingChannel.Raise(_selectedBuilding);
            }
        }

        private void Update()
        {
            // Clicks over UI (the Production Menu/Info Panel) shouldn't also select/command
            // whatever happens to be in the world underneath them.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                var worldPosition = ScreenToWorld(Mouse.current.position.ReadValue());
                var additive = Keyboard.current != null &&
                    (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
                HandleLeftClick(HitTestEntity(worldPosition), additive);
            }
            else if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                var worldPosition = ScreenToWorld(Mouse.current.position.ReadValue());
                var hitEntity = HitTestEntity(worldPosition);
                HandleRightClick(_grid.WorldToCell(worldPosition), hitEntity as IDamageable);
            }
        }

        private static GameEntityBase HitTestEntity(Vector2 worldPosition)
        {
            var collider = Physics2D.OverlapPoint(worldPosition);
            return collider != null ? collider.GetComponentInParent<GameEntityBase>() : null;
        }

        private Vector3 ScreenToWorld(Vector2 screenPosition)
        {
            var depth = -gameCamera.transform.position.z;
            return gameCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
        }
    }
}
