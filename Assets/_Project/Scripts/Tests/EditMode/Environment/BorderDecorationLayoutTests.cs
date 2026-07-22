using CaseGame.Environment;
using CaseGame.Grid;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Environment
{
    public class BorderDecorationLayoutTests
    {
        private static GridModel CreateGrid(float cellSize, int columns, int rows)
        {
            var definition = ScriptableObject.CreateInstance<GridDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("cellSize").floatValue = cellSize;
            so.FindProperty("columns").intValue = columns;
            so.FindProperty("rows").intValue = rows;
            so.FindProperty("originWorldPosition").vector2Value = Vector2.zero;
            so.ApplyModifiedPropertiesWithoutUndo();
            return new GridModel(definition);
        }

        [Test]
        public void BuildRing_EvenlyDivisiblePerimeter_ReturnsExactlyPerimeterOverSpacingPlacements()
        {
            var grid = CreateGrid(1f, 4, 2); // 4x2 world bounds, perimeter = 12

            var placements = BorderDecorationLayout.BuildRing(grid, margin: 0f, spacing: 1f, variantCount: 3);

            Assert.AreEqual(12, placements.Count);
        }

        [Test]
        public void BuildRing_FirstPlacement_IsAtBottomLeftCornerOfExpandedBounds()
        {
            var grid = CreateGrid(1f, 4, 2);

            var placements = BorderDecorationLayout.BuildRing(grid, margin: 0.5f, spacing: 1f, variantCount: 3);

            Assert.AreEqual(new Vector2(-0.5f, -0.5f), placements[0].Position);
        }

        [Test]
        public void BuildRing_CyclesThroughVariantsInOrder()
        {
            var grid = CreateGrid(1f, 4, 2);

            var placements = BorderDecorationLayout.BuildRing(grid, margin: 0f, spacing: 1f, variantCount: 3);

            Assert.AreEqual(0, placements[0].VariantIndex);
            Assert.AreEqual(1, placements[1].VariantIndex);
            Assert.AreEqual(2, placements[2].VariantIndex);
            Assert.AreEqual(0, placements[3].VariantIndex);
        }

        [Test]
        public void BuildRing_WalksRightEdgeStartingAtBottomRightCorner()
        {
            var grid = CreateGrid(1f, 4, 2); // width=4 -> right edge starts at distance=4

            var placements = BorderDecorationLayout.BuildRing(grid, margin: 0f, spacing: 1f, variantCount: 1);

            Assert.AreEqual(new Vector2(4f, 0f), placements[4].Position);
        }

        [Test]
        public void BuildRing_ZeroOrNegativeSpacing_ReturnsEmpty()
        {
            var grid = CreateGrid(1f, 4, 2);

            var placements = BorderDecorationLayout.BuildRing(grid, margin: 0f, spacing: 0f, variantCount: 3);

            Assert.IsEmpty(placements);
        }

        [Test]
        public void BuildRing_ZeroVariantCount_ReturnsEmpty()
        {
            var grid = CreateGrid(1f, 4, 2);

            var placements = BorderDecorationLayout.BuildRing(grid, margin: 0f, spacing: 1f, variantCount: 0);

            Assert.IsEmpty(placements);
        }

        [Test]
        public void BuildRing_LargerMargin_ExpandsBoundsSymmetrically()
        {
            var grid = CreateGrid(1f, 4, 2);

            var placements = BorderDecorationLayout.BuildRing(grid, margin: 2f, spacing: 1f, variantCount: 1);

            Assert.AreEqual(new Vector2(-2f, -2f), placements[0].Position);
        }
    }
}
