using System.Collections.Generic;
using CaseGame.Grid;
using UnityEngine;

namespace CaseGame.Environment
{
    /// <summary>
    /// Pure geometry for the game board's decorative border ring (backlog item 17, "auto-
    /// generated border tiles around the grid's edges"): evenly-spaced placements walking the
    /// perimeter of a rectangle <paramref name="margin"/> world units outside a <see cref="GridModel"/>'s
    /// bounds, cycling through a fixed number of decoration variants so neighboring props never
    /// repeat back-to-back. Purely visual/one-time-authored (per human decision, this pass does
    /// not tie any terrain to grid occupancy) — the result is baked into the scene once by a
    /// throwaway editor script, not regenerated at runtime, so this class has no MonoBehaviour
    /// counterpart.
    /// </summary>
    public static class BorderDecorationLayout
    {
        public readonly struct Placement
        {
            public Placement(Vector2 position, int variantIndex)
            {
                Position = position;
                VariantIndex = variantIndex;
            }

            public Vector2 Position { get; }
            public int VariantIndex { get; }
        }

        /// <summary>Walks the perimeter of the grid's world bounds expanded by <paramref name="margin"/> in every direction, placing one decoration every <paramref name="spacing"/> world units, starting at the bottom-left corner and proceeding counter-clockwise (bottom edge, right edge, top edge, left edge). Empty if <paramref name="spacing"/> or <paramref name="variantCount"/> isn't positive.</summary>
        public static List<Placement> BuildRing(GridModel grid, float margin, float spacing, int variantCount)
        {
            var placements = new List<Placement>();

            if (spacing <= 0f || variantCount <= 0)
            {
                return placements;
            }

            var min = grid.CellToWorld(Vector2Int.zero) - new Vector2(margin, margin);
            var max = grid.CellToWorld(new Vector2Int(grid.Columns, grid.Rows)) + new Vector2(margin, margin);
            var width = max.x - min.x;
            var height = max.y - min.y;
            var perimeter = 2f * (width + height);

            var variantIndex = 0;
            for (var distance = 0f; distance < perimeter; distance += spacing)
            {
                placements.Add(new Placement(PointAlongPerimeter(min, width, height, distance), variantIndex % variantCount));
                variantIndex++;
            }

            return placements;
        }

        private static Vector2 PointAlongPerimeter(Vector2 min, float width, float height, float distance)
        {
            if (distance < width)
            {
                return new Vector2(min.x + distance, min.y);
            }

            distance -= width;

            if (distance < height)
            {
                return new Vector2(min.x + width, min.y + distance);
            }

            distance -= height;

            if (distance < width)
            {
                return new Vector2(min.x + width - distance, min.y + height);
            }

            distance -= width;

            return new Vector2(min.x, min.y + height - distance);
        }
    }
}
