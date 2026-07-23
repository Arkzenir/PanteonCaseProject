using UnityEngine;

namespace CaseGame.Grid
{
    /// <summary>
    /// The environment's water-backdrop rectangle: the grid's own bounds expanded by
    /// <see cref="GridDefinition.TerrainMargin"/> on every side. Single source of truth shared by
    /// <see cref="CameraControl.CameraController"/>'s pan/zoom clamp and the baked terrain quad
    /// (<c>CaseGame.Environment.TerrainCompositor</c>), so the two can never drift out of sync.
    /// </summary>
    public static class TerrainBounds
    {
        public static (Vector2 min, Vector2 max) Compute(GridModel grid, float margin)
        {
            var marginVector = new Vector2(margin, margin);
            var min = grid.CellToWorld(Vector2Int.zero) - marginVector;
            var max = grid.CellToWorld(new Vector2Int(grid.Columns, grid.Rows)) + marginVector;
            return (min, max);
        }
    }
}
