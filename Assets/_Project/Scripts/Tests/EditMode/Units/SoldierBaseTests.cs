using CaseGame.Combat;
using CaseGame.Grid;
using CaseGame.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Units
{
    public class SoldierBaseTests
    {
        private class FakeDamageable : IDamageable
        {
            public int MaxHealth => 10;
            public int CurrentHealth => 10;
            public bool IsDead => false;
            public int LastDamageApplied { get; private set; } = -1;

            public void ApplyDamage(int amount)
            {
                LastDamageApplied = amount;
            }
        }

        private static UnitDefinition CreateDefinition(int maxHealth, int attackDamage)
        {
            var definition = ScriptableObject.CreateInstance<UnitDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("maxHealth").intValue = maxHealth;
            so.FindProperty("attackDamage").intValue = attackDamage;
            so.ApplyModifiedPropertiesWithoutUndo();
            return definition;
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

        [Test]
        public void Definition_ReturnsStronglyTypedUnitDefinition()
        {
            var go = new GameObject("Soldier");
            var soldier = go.AddComponent<Soldier>();
            var definition = CreateDefinition(10, 5);

            soldier.Initialize(definition);

            Assert.AreSame(definition, soldier.Definition);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void TryAttack_NullTarget_DoesNotThrow()
        {
            var go = new GameObject("Soldier");
            var soldier = go.AddComponent<Soldier>();
            var definition = CreateDefinition(10, 5);
            soldier.Initialize(definition);

            Assert.DoesNotThrow(() => soldier.TryAttack(null));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void TryAttack_ValidTarget_AppliesDefinitionAttackDamage()
        {
            var go = new GameObject("Soldier");
            var soldier = go.AddComponent<Soldier>();
            var definition = CreateDefinition(10, 5);
            soldier.Initialize(definition);
            var target = new FakeDamageable();

            soldier.TryAttack(target);

            Assert.AreEqual(5, target.LastDamageApplied);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void MoveTo_SameCellAsCurrentPosition_DoesNotThrow()
        {
            var go = new GameObject("Soldier");
            var soldier = go.AddComponent<Soldier>();
            var definition = CreateDefinition(10, 5);
            soldier.Initialize(definition);
            var grid = CreateGrid(5, 5);
            soldier.transform.position = grid.CellCenterToWorld(new Vector2Int(2, 2));

            Assert.DoesNotThrow(() => soldier.MoveTo(new Vector2Int(2, 2), grid));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void MoveTo_UnreachableGoal_DoesNotThrow()
        {
            var go = new GameObject("Soldier");
            var soldier = go.AddComponent<Soldier>();
            var definition = CreateDefinition(10, 5);
            soldier.Initialize(definition);
            var grid = CreateGrid(5, 5);
            grid.SetAreaOccupied(new Vector2Int(3, 3), Vector2Int.one, true);

            Assert.DoesNotThrow(() => soldier.MoveTo(new Vector2Int(3, 3), grid));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }
    }
}
