using CaseGame.Grid;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CaseGame.Environment
{
    /// <summary>
    /// Humble view rebuilding the island's grass/cliff Tilemaps (Report 031's hand-painted
    /// reference, now generated for any grid size, human-requested) from
    /// <see cref="IslandTilemapLayout"/> whenever <see cref="gridDefinition"/>'s
    /// <c>Columns</c>/<c>Rows</c> — or <see cref="tileSet"/> itself — change.
    /// <c>[ExecuteAlways]</c> plus a cheap per-Update signature check mirrors
    /// <see cref="GridView"/>'s exact Report 029 pattern: editing the grid size in the Inspector
    /// previews the regenerated island immediately, without entering Play Mode.
    /// </summary>
    [ExecuteAlways]
    public class IslandTerrainView : MonoBehaviour
    {
        [SerializeField] private GridDefinition gridDefinition;
        [SerializeField] private IslandTileSet tileSet;
        [SerializeField] private Tilemap grassTilemap;
        [SerializeField] private Tilemap cliffTilemap;

        private GridDefinition _lastBuiltDefinition;
        private int _lastBuiltColumns;
        private int _lastBuiltRows;
        private IslandTileSet _lastBuiltTileSet;

        private void Awake()
        {
            RebuildTerrain();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            // Deferred: OnValidate runs mid-serialization, and Tilemap.SetTile internally uses
            // SendMessage, which Unity forbids in that window (harmless "SendMessage cannot be
            // called during Awake, CheckConsistency, or OnValidate" warning otherwise).
            UnityEditor.EditorApplication.delayCall += DeferredRebuildTerrain;
#endif
        }

#if UNITY_EDITOR
        private void DeferredRebuildTerrain()
        {
            UnityEditor.EditorApplication.delayCall -= DeferredRebuildTerrain;

            if (this == null)
            {
                return;
            }

            RebuildTerrain();
        }
#endif

        private void Update()
        {
            if (!Application.isPlaying)
            {
                RebuildIfChanged();
            }
        }

        private void RebuildIfChanged()
        {
            if (gridDefinition == null)
            {
                return;
            }

            var unchanged = _lastBuiltDefinition == gridDefinition &&
                             _lastBuiltColumns == gridDefinition.Columns &&
                             _lastBuiltRows == gridDefinition.Rows &&
                             _lastBuiltTileSet == tileSet;

            if (!unchanged)
            {
                RebuildTerrain();
            }
        }

        private void RebuildTerrain()
        {
            if (gridDefinition == null || tileSet == null || grassTilemap == null || cliffTilemap == null)
            {
                return;
            }

            grassTilemap.ClearAllTiles();
            cliffTilemap.ClearAllTiles();

            foreach (var placement in IslandTilemapLayout.BuildGrass(gridDefinition.Columns, gridDefinition.Rows, tileSet))
            {
                grassTilemap.SetTile(placement.Cell, placement.Tile);
            }

            foreach (var placement in IslandTilemapLayout.BuildCliff(gridDefinition.Columns, tileSet))
            {
                cliffTilemap.SetTile(placement.Cell, placement.Tile);
            }

            _lastBuiltDefinition = gridDefinition;
            _lastBuiltColumns = gridDefinition.Columns;
            _lastBuiltRows = gridDefinition.Rows;
            _lastBuiltTileSet = tileSet;
        }
    }
}
