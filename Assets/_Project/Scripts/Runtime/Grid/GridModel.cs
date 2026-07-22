using UnityEngine;

namespace CaseGame.Grid
{
    /// <summary>
    /// Plain C# grid data model: world/cell coordinate conversion and cell occupancy.
    /// Engine-independent aside from Vector2/Vector2Int, so it is unit-testable without
    /// entering play mode. Buildings/Placement/Pathfinding are the intended consumers.
    /// </summary>
    public class GridModel
    {
        private readonly GridDefinition _definition;
        private readonly bool[,] _occupied;

        public GridModel(GridDefinition definition)
        {
            _definition = definition;
            _occupied = new bool[definition.Columns, definition.Rows];
        }

        public int Columns => _definition.Columns;
        public int Rows => _definition.Rows;
        public float CellSize => _definition.CellSize;

        public bool IsInBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Columns && cell.y >= 0 && cell.y < Rows;
        }

        public Vector2Int WorldToCell(Vector2 worldPosition)
        {
            var local = worldPosition - _definition.OriginWorldPosition;
            return new Vector2Int(Mathf.FloorToInt(local.x / CellSize), Mathf.FloorToInt(local.y / CellSize));
        }

        /// <summary>World position of the cell's bottom-left corner.</summary>
        public Vector2 CellToWorld(Vector2Int cell)
        {
            return _definition.OriginWorldPosition + new Vector2(cell.x * CellSize, cell.y * CellSize);
        }

        /// <summary>World position of the cell's center.</summary>
        public Vector2 CellCenterToWorld(Vector2Int cell)
        {
            return CellToWorld(cell) + new Vector2(CellSize, CellSize) * 0.5f;
        }

        /// <summary>World position of the geometric center of a whole footprint rectangle (origin = its bottom-left cell) — correct regardless of whether the footprint's width/height is odd or even, unlike picking a single "center cell" (which doesn't exist for an even-sized footprint).</summary>
        public Vector2 FootprintCenterToWorld(Vector2Int origin, Vector2Int footprint)
        {
            return CellToWorld(origin) + new Vector2(footprint.x, footprint.y) * CellSize * 0.5f;
        }

        /// <summary>Out-of-bounds cells report as occupied, since they are never legal to place/path through.</summary>
        public bool IsOccupied(Vector2Int cell)
        {
            return !IsInBounds(cell) || _occupied[cell.x, cell.y];
        }

        public bool IsAreaFree(Vector2Int origin, Vector2Int footprint)
        {
            for (var x = 0; x < footprint.x; x++)
            {
                for (var y = 0; y < footprint.y; y++)
                {
                    if (IsOccupied(new Vector2Int(origin.x + x, origin.y + y)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public void SetAreaOccupied(Vector2Int origin, Vector2Int footprint, bool occupied)
        {
            for (var x = 0; x < footprint.x; x++)
            {
                for (var y = 0; y < footprint.y; y++)
                {
                    var cell = new Vector2Int(origin.x + x, origin.y + y);
                    if (IsInBounds(cell))
                    {
                        _occupied[cell.x, cell.y] = occupied;
                    }
                }
            }
        }
    }
}
