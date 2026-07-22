using CaseGame.Buildings;
using CaseGame.Combat;
using CaseGame.Grid;
using CaseGame.Selection;
using CaseGame.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Selection
{
    public class SelectionControllerTests
    {
        private class TestBuilding : BuildingBase
        {
        }

        private class FakeDamageable : IDamageable
        {
            public int MaxHealth => 10;
            public int CurrentHealth => 10;
            public bool IsDead => false;
            public int TotalDamageApplied { get; private set; }

            public void ApplyDamage(int amount)
            {
                TotalDamageApplied += amount;
            }
        }

        private GridModel _grid;
        private SelectionController _controller;
        private SelectedBuildingEventChannel _channel;

        [SetUp]
        public void SetUp()
        {
            _grid = CreateGrid(10, 10);

            _channel = ScriptableObject.CreateInstance<SelectedBuildingEventChannel>();
            _controller = new GameObject("SelectionController").AddComponent<SelectionController>();
            var so = new SerializedObject(_controller);
            so.FindProperty("selectedBuildingChannel").objectReferenceValue = _channel;
            so.ApplyModifiedPropertiesWithoutUndo();
            _controller.Initialize(_grid);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_controller.gameObject);
            Object.DestroyImmediate(_channel);
        }

        [Test]
        public void HandleLeftClick_Building_SelectsIt()
        {
            var building = CreateBuilding();

            _controller.HandleLeftClick(building, additive: false);

            Assert.AreSame(building, _controller.SelectedBuilding);

            DestroyBuilding(building);
        }

        [Test]
        public void HandleLeftClick_Building_RaisesChannelWithBuilding()
        {
            var building = CreateBuilding();
            BuildingBase received = null;
            _channel.Subscribe(b => received = b);

            _controller.HandleLeftClick(building, additive: false);

            Assert.AreSame(building, received);

            DestroyBuilding(building);
        }

        [Test]
        public void HandleLeftClick_Soldier_NonAdditive_ReplacesSelection()
        {
            var first = CreateSoldier();
            var second = CreateSoldier();

            _controller.HandleLeftClick(first, additive: false);
            _controller.HandleLeftClick(second, additive: false);

            Assert.AreEqual(1, _controller.SelectedSoldiers.Count);
            Assert.AreSame(second, _controller.SelectedSoldiers[0]);

            DestroySoldier(first);
            DestroySoldier(second);
        }

        [Test]
        public void HandleLeftClick_Soldier_Additive_AddsToSelection()
        {
            var first = CreateSoldier();
            var second = CreateSoldier();

            _controller.HandleLeftClick(first, additive: false);
            _controller.HandleLeftClick(second, additive: true);

            Assert.AreEqual(2, _controller.SelectedSoldiers.Count);
            CollectionAssert.Contains(_controller.SelectedSoldiers, first);
            CollectionAssert.Contains(_controller.SelectedSoldiers, second);

            DestroySoldier(first);
            DestroySoldier(second);
        }

        [Test]
        public void HandleLeftClick_Soldier_AdditiveOnAlreadySelected_RemovesFromSelection()
        {
            var soldier = CreateSoldier();
            _controller.HandleLeftClick(soldier, additive: false);

            _controller.HandleLeftClick(soldier, additive: true);

            Assert.AreEqual(0, _controller.SelectedSoldiers.Count);

            DestroySoldier(soldier);
        }

        [Test]
        public void HandleLeftClick_Soldier_ClearsPreviouslySelectedBuilding()
        {
            var building = CreateBuilding();
            var soldier = CreateSoldier();
            _controller.HandleLeftClick(building, additive: false);

            _controller.HandleLeftClick(soldier, additive: false);

            Assert.IsNull(_controller.SelectedBuilding);
            Assert.AreEqual(1, _controller.SelectedSoldiers.Count);

            DestroyBuilding(building);
            DestroySoldier(soldier);
        }

        [Test]
        public void HandleLeftClick_Building_ClearsPreviouslySelectedSoldiers()
        {
            var soldier = CreateSoldier();
            var building = CreateBuilding();
            _controller.HandleLeftClick(soldier, additive: false);

            _controller.HandleLeftClick(building, additive: false);

            Assert.AreEqual(0, _controller.SelectedSoldiers.Count);
            Assert.AreSame(building, _controller.SelectedBuilding);

            DestroySoldier(soldier);
            DestroyBuilding(building);
        }

        [Test]
        public void HandleLeftClick_EmptyGround_NonAdditive_ClearsSelection()
        {
            var soldier = CreateSoldier();
            _controller.HandleLeftClick(soldier, additive: false);

            _controller.HandleLeftClick(null, additive: false);

            Assert.AreEqual(0, _controller.SelectedSoldiers.Count);

            DestroySoldier(soldier);
        }

        [Test]
        public void HandleLeftClick_EmptyGround_Additive_DoesNotClearSelection()
        {
            var soldier = CreateSoldier();
            _controller.HandleLeftClick(soldier, additive: false);

            _controller.HandleLeftClick(null, additive: true);

            Assert.AreEqual(1, _controller.SelectedSoldiers.Count);

            DestroySoldier(soldier);
        }

        [Test]
        public void HandleRightClick_WithTarget_AppliesDamageFromEachSelectedSoldier()
        {
            var first = CreateSoldier(attackDamage: 5);
            var second = CreateSoldier(attackDamage: 3);
            _controller.HandleLeftClick(first, additive: false);
            _controller.HandleLeftClick(second, additive: true);
            var target = new FakeDamageable();

            _controller.HandleRightClick(Vector2Int.zero, target);

            Assert.AreEqual(8, target.TotalDamageApplied);

            DestroySoldier(first);
            DestroySoldier(second);
        }

        [Test]
        public void HandleRightClick_NoSelection_DoesNotThrow()
        {
            var target = new FakeDamageable();

            Assert.DoesNotThrow(() => _controller.HandleRightClick(Vector2Int.zero, target));
            Assert.AreEqual(0, target.TotalDamageApplied);
        }

        [Test]
        public void HandleRightClick_DeadSelectedSoldier_IsPrunedAndDoesNotAttack()
        {
            var alive = CreateSoldier(attackDamage: 5);
            var dead = CreateSoldier(attackDamage: 5);
            _controller.HandleLeftClick(alive, additive: false);
            _controller.HandleLeftClick(dead, additive: true);
            dead.ApplyDamage(10); // kills it (10 HP from CreateSoldier's default)
            var target = new FakeDamageable();

            _controller.HandleRightClick(Vector2Int.zero, target);

            Assert.AreEqual(5, target.TotalDamageApplied);
            Assert.AreEqual(1, _controller.SelectedSoldiers.Count);

            DestroySoldier(alive);
            DestroySoldier(dead);
        }

        [Test]
        public void HandleRightClick_NoTarget_SoldierAlreadyAtCell_DoesNotThrow()
        {
            var soldier = CreateSoldier();
            soldier.transform.position = _grid.CellCenterToWorld(new Vector2Int(2, 2));
            _controller.HandleLeftClick(soldier, additive: false);

            Assert.DoesNotThrow(() => _controller.HandleRightClick(new Vector2Int(2, 2), null));

            DestroySoldier(soldier);
        }

        [Test]
        public void ClearSelection_DeselectsSoldiersAndBuildingAndRaisesNull()
        {
            var building = CreateBuilding();
            _controller.HandleLeftClick(building, additive: false);
            BuildingBase received = building; // overwritten by ClearSelection's Raise below if it fires correctly
            _channel.Subscribe(b => received = b);

            _controller.ClearSelection();

            Assert.IsNull(_controller.SelectedBuilding);
            Assert.IsNull(received);

            DestroyBuilding(building);
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

        private static TestBuilding CreateBuilding()
        {
            var definition = ScriptableObject.CreateInstance<BuildingDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("maxHealth").intValue = 100;
            so.ApplyModifiedPropertiesWithoutUndo();

            var building = new GameObject("Building").AddComponent<TestBuilding>();
            building.Initialize(definition);
            return building;
        }

        private static void DestroyBuilding(TestBuilding building)
        {
            var definition = building.Definition;
            Object.DestroyImmediate(building.gameObject);
            Object.DestroyImmediate(definition);
        }

        private static Soldier CreateSoldier(int attackDamage = 1)
        {
            var definition = ScriptableObject.CreateInstance<UnitDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("maxHealth").intValue = 10;
            so.FindProperty("attackDamage").intValue = attackDamage;
            so.ApplyModifiedPropertiesWithoutUndo();

            var soldier = new GameObject("Soldier").AddComponent<Soldier>();
            soldier.Initialize(definition);
            return soldier;
        }

        private static void DestroySoldier(Soldier soldier)
        {
            var definition = soldier.Definition;
            Object.DestroyImmediate(soldier.gameObject);
            Object.DestroyImmediate(definition);
        }
    }
}
