using CaseGame.Grid;
using NUnit.Framework;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Grid
{
    public class TerrainBoundsTests
    {
        private static GridDefinition CreateDefinition(float cellSize, int columns, int rows, Vector2 origin)
        {
            var definition = ScriptableObject.CreateInstance<GridDefinition>();
            var serializedObject = new UnityEditor.SerializedObject(definition);
            serializedObject.FindProperty("cellSize").floatValue = cellSize;
            serializedObject.FindProperty("columns").intValue = columns;
            serializedObject.FindProperty("rows").intValue = rows;
            serializedObject.FindProperty("originWorldPosition").vector2Value = origin;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        [Test]
        public void Compute_ExpandsGridBoundsByMarginOnEverySide()
        {
            var model = new GridModel(CreateDefinition(1f, 20, 12, Vector2.zero));

            var (min, max) = TerrainBounds.Compute(model, 30f);

            Assert.AreEqual(new Vector2(-30f, -30f), min);
            Assert.AreEqual(new Vector2(50f, 42f), max);
        }

        [Test]
        public void Compute_RespectsNonZeroOriginAndCellSize()
        {
            var model = new GridModel(CreateDefinition(2f, 10, 10, new Vector2(5f, 5f)));

            var (min, max) = TerrainBounds.Compute(model, 4f);

            Assert.AreEqual(new Vector2(1f, 1f), min);
            Assert.AreEqual(new Vector2(29f, 29f), max);
        }

        [Test]
        public void Compute_ZeroMargin_MatchesGridBoundsExactly()
        {
            var model = new GridModel(CreateDefinition(1f, 8, 6, Vector2.zero));

            var (min, max) = TerrainBounds.Compute(model, 0f);

            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(new Vector2(8f, 6f), max);
        }
    }
}
