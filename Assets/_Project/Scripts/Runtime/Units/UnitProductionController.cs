using CaseGame.Grid;
using UnityEngine;

namespace CaseGame.Units
{
    /// <summary>
    /// Controller: responds to a clicked producible-unit icon on the Information Panel by
    /// spawning that unit at the requested position — production is free and instant, spawning
    /// at the producing building's spawn point. Mirrors <c>PlacementController</c>'s
    /// produce-request subscription but for units instead of buildings — units don't go through
    /// a ghost/placement flow, they just appear.
    ///
    /// The requested spawn position is snapped to the grid cell it falls in/nearest to (not the
    /// raw prefab position), and production is blocked outright if that cell is already occupied
    /// — by a building (<see cref="GridModel.IsOccupied"/>) or another unit (a live scan of
    /// <see cref="UnitFactory.ActiveUnits"/>). Units don't have a static footprint on the grid the
    /// way buildings do, so this is a point-in-time check at spawn time, not a persisted
    /// occupancy grid — no continuous unit-collision/pathfinding-around-units is implied.
    /// </summary>
    public class UnitProductionController : MonoBehaviour
    {
        [SerializeField] private UnitProductionRequestEventChannel produceRequestedChannel;

        private UnitFactory _factory;
        private GridModel _grid;

        /// <summary>Explicit initialization (not Awake-wired), mirroring the rest of the project's scene-bootstrap-calls-Initialize pattern — <see cref="UnitFactory"/> needs a real container Transform that only exists once the scene's bootstrap runs.</summary>
        public void Initialize(UnitFactory factory, GridModel grid)
        {
            _factory = factory;
            _grid = grid;
        }

        private void OnEnable()
        {
            if (produceRequestedChannel != null)
            {
                produceRequestedChannel.Subscribe(HandleProduceRequested);
            }
        }

        private void OnDisable()
        {
            if (produceRequestedChannel != null)
            {
                produceRequestedChannel.Unsubscribe(HandleProduceRequested);
            }
        }

        /// <summary>Public and independent of the channel callback so it's directly testable, same pattern as every other controller in the project.</summary>
        public void HandleProduceRequested(UnitProductionRequest request)
        {
            var spawnCell = _grid.WorldToCell(request.SpawnPosition);
            if (IsCellBlocked(spawnCell))
            {
                return;
            }

            var instance = _factory.Create(request.Entry.Definition, request.Entry.Prefab);
            instance.transform.position = _grid.CellCenterToWorld(spawnCell);
        }

        private bool IsCellBlocked(Vector2Int cell)
        {
            if (_grid.IsOccupied(cell))
            {
                return true;
            }

            foreach (var unit in _factory.ActiveUnits)
            {
                if (unit != null && _grid.WorldToCell(unit.transform.position) == cell)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
