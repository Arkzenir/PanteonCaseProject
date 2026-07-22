using CaseGame.Entities;
using CaseGame.Grid;
using UnityEngine;

namespace CaseGame.Buildings
{
    /// <summary>
    /// Building-specific base on top of <see cref="GameEntityBase"/>. All the actual behavior
    /// (Health, IDamageable, sprite, death callback) lives on the shared base; this just
    /// re-exposes <see cref="Definition"/> as the strongly-typed <see cref="BuildingDefinition"/>
    /// for consumers (e.g. a future Info Panel reading <c>ProducibleUnits</c>).
    /// </summary>
    public abstract class BuildingBase : GameEntityBase
    {
        public new BuildingDefinition Definition => (BuildingDefinition)base.Definition;

        /// <summary>Where this building's produced units appear (GI-7). Defaults to the building's own position; <see cref="Barracks"/> overrides it with a dedicated spawn point. Virtual so callers (e.g. Info Panel unit production) never need to type-check for "is this a Barracks" — any building capable of producing units can be asked this generically (requirement 2's modularity mandate).</summary>
        public virtual Vector3 SpawnPosition => transform.position;

        private GridModel _grid;
        private Vector2Int _footprintOrigin;
        private bool _isPlacedOnGrid;

        /// <summary>The grid cells this building currently occupies (bottom-left of its footprint), or null if it isn't placed on a grid right now. Set by <see cref="Placement.PlacementController"/> once a placement is actually committed — not at ghost-creation time.</summary>
        public Vector2Int? FootprintOrigin => _isPlacedOnGrid ? _footprintOrigin : (Vector2Int?)null;

        /// <summary>Records where this building sits on the grid, so it can release those cells itself when it goes away — combat death (<see cref="OnEntityDied"/>) and manual removal (<see cref="ReleaseFootprint"/> called directly) both end up freeing the same footprint, through two independent triggers.</summary>
        public void SetPlacement(GridModel grid, Vector2Int footprintOrigin)
        {
            _grid = grid;
            _footprintOrigin = footprintOrigin;
            _isPlacedOnGrid = true;
        }

        /// <summary>Combat death's only extra cleanup beyond the pooling callback: free the grid footprint. Reached only via <see cref="GameEntityBase.ApplyDamage"/> → <c>Health</c> → this hook — manual removal never calls <c>ApplyDamage</c> at all, see <see cref="ReleaseFootprint"/>.</summary>
        protected override void OnEntityDied()
        {
            ReleaseFootprint();
        }

        /// <summary>Releases this building's occupied footprint back to the grid. Called from <see cref="OnEntityDied"/> for combat destruction, and directly by <c>PlacementController</c> for manual removal — the same cleanup, reached by two independent, non-overlapping triggers (removal never raises <c>ApplyDamage</c>/Health, and combat death never calls this method directly).</summary>
        public void ReleaseFootprint()
        {
            if (_isPlacedOnGrid && _grid != null)
            {
                _grid.SetAreaOccupied(_footprintOrigin, Definition.Footprint, false);
                _isPlacedOnGrid = false;
            }
        }
    }
}
