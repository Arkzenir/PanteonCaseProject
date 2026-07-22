using CaseGame.Grid;
using CaseGame.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Units
{
    public class UnitProductionControllerTests
    {
        private GridModel _grid;
        private UnitFactory _factory;
        private UnitProductionController _controller;
        private Soldier _prefab;
        private UnitDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _grid = CreateGrid(10, 10);
            _factory = new UnitFactory();
            _controller = new GameObject("UnitProductionController").AddComponent<UnitProductionController>();
            _controller.Initialize(_factory, _grid);

            _prefab = new GameObject("Prefab").AddComponent<Soldier>();
            _definition = ScriptableObject.CreateInstance<UnitDefinition>();
            var so = new SerializedObject(_definition);
            so.FindProperty("maxHealth").intValue = 10;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var unit in _factory.ActiveUnits)
            {
                if (unit != null)
                {
                    Object.DestroyImmediate(unit.gameObject);
                }
            }

            Object.DestroyImmediate(_controller.gameObject);
            Object.DestroyImmediate(_prefab.gameObject);
            Object.DestroyImmediate(_definition);
        }

        [Test]
        public void HandleProduceRequested_SpawnsAtCenterOfSpawnPositionsCell()
        {
            var entry = new UnitCatalogEntry(_definition, _prefab);
            var spawnPosition = new Vector3(7.3f, 2.8f, 0f); // falls in cell (7, 2)

            _controller.HandleProduceRequested(new UnitProductionRequest(entry, spawnPosition));

            var instance = FindActiveInstance();
            Assert.IsNotNull(instance);
            Assert.AreEqual((Vector3)_grid.CellCenterToWorld(new Vector2Int(7, 2)), instance.transform.position);
        }

        [Test]
        public void HandleProduceRequested_InstanceIsInitializedWithDefinition()
        {
            var entry = new UnitCatalogEntry(_definition, _prefab);

            _controller.HandleProduceRequested(new UnitProductionRequest(entry, Vector3.zero));

            var instance = FindActiveInstance();
            Assert.AreSame(_definition, instance.Definition);
        }

        [Test]
        public void HandleProduceRequested_CellOccupiedByBuilding_DoesNotSpawn()
        {
            var entry = new UnitCatalogEntry(_definition, _prefab);
            _grid.SetAreaOccupied(new Vector2Int(2, 2), Vector2Int.one, true);

            _controller.HandleProduceRequested(new UnitProductionRequest(entry, _grid.CellCenterToWorld(new Vector2Int(2, 2))));

            Assert.IsNull(FindActiveInstance());
        }

        [Test]
        public void HandleProduceRequested_CellOccupiedByAnotherUnit_DoesNotSpawn()
        {
            var entry = new UnitCatalogEntry(_definition, _prefab);
            var spawnPosition = _grid.CellCenterToWorld(new Vector2Int(4, 4));
            _controller.HandleProduceRequested(new UnitProductionRequest(entry, spawnPosition));
            Assert.AreEqual(1, _factory.ActiveUnits.Count); // sanity: first spawn succeeded

            _controller.HandleProduceRequested(new UnitProductionRequest(entry, spawnPosition));

            Assert.AreEqual(1, _factory.ActiveUnits.Count);
        }

        [Test]
        public void HandleProduceRequested_FreeCell_Spawns()
        {
            var entry = new UnitCatalogEntry(_definition, _prefab);

            _controller.HandleProduceRequested(new UnitProductionRequest(entry, _grid.CellCenterToWorld(new Vector2Int(4, 4))));

            Assert.AreEqual(1, _factory.ActiveUnits.Count);
        }

        private SoldierBase FindActiveInstance()
        {
            foreach (var soldier in _factory.ActiveUnits)
            {
                return soldier;
            }

            return null;
        }

        private static GridModel CreateGrid(int columns, int rows)
        {
            var gridDefinition = ScriptableObject.CreateInstance<GridDefinition>();
            var so = new SerializedObject(gridDefinition);
            so.FindProperty("cellSize").floatValue = 1f;
            so.FindProperty("columns").intValue = columns;
            so.FindProperty("rows").intValue = rows;
            so.FindProperty("originWorldPosition").vector2Value = Vector2.zero;
            so.ApplyModifiedPropertiesWithoutUndo();
            return new GridModel(gridDefinition);
        }
    }
}
