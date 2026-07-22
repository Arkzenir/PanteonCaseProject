using CaseGame.Grid;
using NUnit.Framework;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Grid
{
    public class GridModelTests
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
        public void WorldToCell_And_CellToWorld_RoundTrip()
        {
            var model = new GridModel(CreateDefinition(2f, 10, 10, Vector2.zero));

            var cell = new Vector2Int(3, 4);
            var world = model.CellToWorld(cell);
            var roundTripped = model.WorldToCell(world);

            Assert.AreEqual(cell, roundTripped);
        }

        [Test]
        public void WorldToCell_RespectsOrigin()
        {
            var model = new GridModel(CreateDefinition(1f, 10, 10, new Vector2(5f, 5f)));

            var cell = model.WorldToCell(new Vector2(5.5f, 6.5f));

            Assert.AreEqual(new Vector2Int(0, 1), cell);
        }

        [Test]
        public void IsInBounds_ReturnsFalse_OutsideGrid()
        {
            var model = new GridModel(CreateDefinition(1f, 5, 5, Vector2.zero));

            Assert.IsFalse(model.IsInBounds(new Vector2Int(-1, 0)));
            Assert.IsFalse(model.IsInBounds(new Vector2Int(0, 5)));
            Assert.IsTrue(model.IsInBounds(new Vector2Int(4, 4)));
        }

        [Test]
        public void IsOccupied_ReturnsTrue_ForOutOfBoundsCell()
        {
            var model = new GridModel(CreateDefinition(1f, 5, 5, Vector2.zero));

            Assert.IsTrue(model.IsOccupied(new Vector2Int(-1, -1)));
        }

        [Test]
        public void SetAreaOccupied_MarksEveryCellInFootprint()
        {
            var model = new GridModel(CreateDefinition(1f, 10, 10, Vector2.zero));

            model.SetAreaOccupied(new Vector2Int(2, 2), new Vector2Int(4, 4), true);

            Assert.IsTrue(model.IsOccupied(new Vector2Int(2, 2)));
            Assert.IsTrue(model.IsOccupied(new Vector2Int(5, 5)));
            Assert.IsFalse(model.IsOccupied(new Vector2Int(6, 2)));
            Assert.IsFalse(model.IsOccupied(new Vector2Int(1, 2)));
        }

        [Test]
        public void IsAreaFree_FalseWhenAnyCellOccupied()
        {
            var model = new GridModel(CreateDefinition(1f, 10, 10, Vector2.zero));
            model.SetAreaOccupied(new Vector2Int(3, 3), new Vector2Int(1, 1), true);

            Assert.IsFalse(model.IsAreaFree(new Vector2Int(2, 2), new Vector2Int(2, 2)));
        }

        [Test]
        public void IsAreaFree_FalseWhenFootprintExtendsOutOfBounds()
        {
            var model = new GridModel(CreateDefinition(1f, 5, 5, Vector2.zero));

            Assert.IsFalse(model.IsAreaFree(new Vector2Int(4, 4), new Vector2Int(2, 2)));
        }

        [Test]
        public void IsAreaFree_TrueForUntouchedGrid()
        {
            var model = new GridModel(CreateDefinition(1f, 10, 10, Vector2.zero));

            Assert.IsTrue(model.IsAreaFree(new Vector2Int(0, 0), new Vector2Int(4, 4)));
        }

        [Test]
        public void FootprintCenterToWorld_EvenFootprint_ReturnsTrueGeometricCenter()
        {
            var model = new GridModel(CreateDefinition(1f, 10, 10, Vector2.zero));

            // A 4x4 footprint at origin (0,0) spans world [0,4]x[0,4]; true center is (2,2).
            var center = model.FootprintCenterToWorld(Vector2Int.zero, new Vector2Int(4, 4));

            Assert.AreEqual(new Vector2(2f, 2f), center);
        }

        [Test]
        public void FootprintCenterToWorld_OddFootprint_ReturnsTrueGeometricCenter()
        {
            var model = new GridModel(CreateDefinition(1f, 10, 10, Vector2.zero));

            // A 3x1 footprint at origin (0,0) spans world [0,3]x[0,1]; true center is (1.5, 0.5).
            var center = model.FootprintCenterToWorld(Vector2Int.zero, new Vector2Int(3, 1));

            Assert.AreEqual(new Vector2(1.5f, 0.5f), center);
        }

        [Test]
        public void FootprintCenterToWorld_RespectsOriginAndCellSize()
        {
            var model = new GridModel(CreateDefinition(2f, 10, 10, new Vector2(1f, 1f)));

            var center = model.FootprintCenterToWorld(new Vector2Int(1, 1), new Vector2Int(2, 2));

            // CellToWorld(1,1) = (1,1) + (2,2) = (3,3); + half of (2*2, 2*2)=(4,4) => +(2,2) => (5,5)
            Assert.AreEqual(new Vector2(5f, 5f), center);
        }
    }
}
