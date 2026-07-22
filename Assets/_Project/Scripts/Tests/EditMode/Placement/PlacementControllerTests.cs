using CaseGame.Buildings;
using CaseGame.Grid;
using CaseGame.Placement;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Placement
{
    public class PlacementControllerTests
    {
        private class TestBuilding : BuildingBase
        {
        }

        private GridModel _grid;
        private BuildingFactory _factory;
        private BuildingDefinition _definition;
        private BuildingBase _prefab;
        private PlacementController _controller;

        [SetUp]
        public void SetUp()
        {
            var gridDefinition = ScriptableObject.CreateInstance<GridDefinition>();
            var gridSo = new SerializedObject(gridDefinition);
            gridSo.FindProperty("cellSize").floatValue = 1f;
            gridSo.FindProperty("columns").intValue = 10;
            gridSo.FindProperty("rows").intValue = 10;
            gridSo.FindProperty("originWorldPosition").vector2Value = Vector2.zero;
            gridSo.ApplyModifiedPropertiesWithoutUndo();
            _grid = new GridModel(gridDefinition);

            _definition = ScriptableObject.CreateInstance<BuildingDefinition>();
            var defSo = new SerializedObject(_definition);
            defSo.FindProperty("maxHealth").intValue = 100;
            defSo.FindProperty("footprint").vector2IntValue = new Vector2Int(2, 2);
            defSo.ApplyModifiedPropertiesWithoutUndo();

            _prefab = CreatePrefab("Prefab");
            _factory = new BuildingFactory();

            _controller = new GameObject("PlacementController").AddComponent<PlacementController>();
            _controller.Initialize(_grid, _factory);
        }

        [TearDown]
        public void TearDown()
        {
            if (_controller.CurrentGhost != null)
            {
                Object.DestroyImmediate(_controller.CurrentGhost.gameObject);
            }

            Object.DestroyImmediate(_prefab.gameObject);
            Object.DestroyImmediate(_controller.gameObject);
            Object.DestroyImmediate(_definition);
        }

        private static BuildingBase CreatePrefab(string name)
        {
            var root = new GameObject(name);
            var visuals = new GameObject("Visuals");
            visuals.transform.SetParent(root.transform);
            var visualsGrayscale = new GameObject("VisualsGrayscale");
            visualsGrayscale.transform.SetParent(root.transform);
            var grayscaleRenderer = visualsGrayscale.AddComponent<SpriteRenderer>();

            var building = root.AddComponent<TestBuilding>();
            var ghostView = root.AddComponent<BuildingGhostView>();
            var so = new SerializedObject(ghostView);
            so.FindProperty("visuals").objectReferenceValue = visuals;
            so.FindProperty("visualsGrayscale").objectReferenceValue = visualsGrayscale;
            so.FindProperty("grayscaleRenderer").objectReferenceValue = grayscaleRenderer;
            so.ApplyModifiedPropertiesWithoutUndo();

            return building;
        }

        [Test]
        public void BeginPlacement_StartsPlacingAndShowsGhost()
        {
            _controller.BeginPlacement(_definition, _prefab);

            Assert.IsTrue(_controller.IsPlacing);
        }

        [Test]
        public void CancelPlacement_StopsPlacing()
        {
            _controller.BeginPlacement(_definition, _prefab);

            _controller.CancelPlacement();

            Assert.IsFalse(_controller.IsPlacing);
        }

        [Test]
        public void UpdateGhostAt_FreeCell_TintsGhostValid()
        {
            _controller.BeginPlacement(_definition, _prefab);

            _controller.UpdateGhostAt(new Vector2Int(5, 5));

            var color = GetGhostGrayscaleColor();
            Assert.Greater(color.g, color.r);
        }

        [Test]
        public void UpdateGhostAt_OccupiedCell_TintsGhostInvalid()
        {
            _grid.SetAreaOccupied(new Vector2Int(5, 5), Vector2Int.one, true);
            _controller.BeginPlacement(_definition, _prefab);

            _controller.UpdateGhostAt(new Vector2Int(5, 5));

            var color = GetGhostGrayscaleColor();
            Assert.Greater(color.r, color.g);
        }

        [Test]
        public void TryCommitAt_FreeCell_MarksGridOccupiedAndReturnsTrue()
        {
            _controller.BeginPlacement(_definition, _prefab);

            var committed = _controller.TryCommitAt(new Vector2Int(2, 2));

            Assert.IsTrue(committed);
            // Hover cell (2,2) is the footprint's *center*; a 2x2 footprint centers there as
            // origin (1,1), covering (1,1)-(2,2) — not (2,2)-(3,3).
            Assert.IsFalse(_grid.IsAreaFree(new Vector2Int(1, 1), new Vector2Int(2, 2)));
            Assert.IsFalse(_controller.IsPlacing);
        }

        [Test]
        public void UpdateGhostAt_PositionsInstanceAtTrueFootprintCenter()
        {
            _controller.BeginPlacement(_definition, _prefab);

            _controller.UpdateGhostAt(new Vector2Int(5, 5));

            // hover (5,5), 2x2 footprint -> origin (4,4) -> true center (5,5).
            Assert.AreEqual(new Vector3(5f, 5f, 0f), _controller.CurrentGhost.transform.position);
        }

        [Test]
        public void TryCommitAt_PositionsInstanceAtTrueFootprintCenter()
        {
            _controller.BeginPlacement(_definition, _prefab);
            var committedInstance = _controller.CurrentGhost;

            _controller.TryCommitAt(new Vector2Int(2, 2));

            // hover (2,2), 2x2 footprint -> origin (1,1) -> true center (2,2).
            Assert.AreEqual(new Vector3(2f, 2f, 0f), committedInstance.transform.position);
        }

        [Test]
        public void TryCommitAt_RecordsFootprintOriginOnTheInstance()
        {
            _controller.BeginPlacement(_definition, _prefab);
            var committedInstance = _controller.CurrentGhost;

            _controller.TryCommitAt(new Vector2Int(2, 2));

            Assert.AreEqual(new Vector2Int(1, 1), committedInstance.FootprintOrigin);
        }

        [Test]
        public void RemoveBuilding_PlacedBuilding_FreesTheGridWithoutKillingTheInstance()
        {
            _controller.BeginPlacement(_definition, _prefab);
            var committedInstance = _controller.CurrentGhost;
            _controller.TryCommitAt(new Vector2Int(2, 2));

            _controller.RemoveBuilding(committedInstance);

            Assert.IsTrue(_grid.IsAreaFree(new Vector2Int(1, 1), new Vector2Int(2, 2)));
            Assert.IsFalse(committedInstance.IsDead);
        }

        [Test]
        public void RemoveBuilding_Null_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _controller.RemoveBuilding(null));
        }

        [Test]
        public void TryCommitAt_OccupiedCell_ReturnsFalseAndKeepsPlacing()
        {
            _grid.SetAreaOccupied(new Vector2Int(2, 2), Vector2Int.one, true);
            _controller.BeginPlacement(_definition, _prefab);

            var committed = _controller.TryCommitAt(new Vector2Int(2, 2));

            Assert.IsFalse(committed);
            Assert.IsTrue(_controller.IsPlacing);
        }

        private Color GetGhostGrayscaleColor()
        {
            return _controller.CurrentGhost.transform.Find("VisualsGrayscale").GetComponent<SpriteRenderer>().color;
        }
    }
}
