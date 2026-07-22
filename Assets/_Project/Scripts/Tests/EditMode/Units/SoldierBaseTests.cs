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

        [Test]
        public void StepDuration_ReturnsReciprocalOfMoveSpeed()
        {
            Assert.AreEqual(0.25f, SoldierBase.StepDuration(4f), 0.0001f);
        }

        [Test]
        public void StepDuration_MatchesWorkedExample_EightStepsAtSpeedFourTakeTwoSeconds()
        {
            var stepDuration = SoldierBase.StepDuration(4f);
            var totalDuration = 8 * stepDuration; // 5 orthogonal + 3 diagonal steps — every step counts as 1 cell

            Assert.AreEqual(2f, totalDuration, 0.0001f);
        }

        [Test]
        public void InterpolateStep_AtZeroElapsed_ReturnsStart()
        {
            var result = SoldierBase.InterpolateStep(Vector3.zero, new Vector3(5f, 0f, 0f), 0f, 1f);

            Assert.AreEqual(Vector3.zero, result);
        }

        [Test]
        public void InterpolateStep_AtHalfDuration_ReturnsMidpoint()
        {
            var result = SoldierBase.InterpolateStep(Vector3.zero, new Vector3(10f, 0f, 0f), 0.5f, 1f);

            Assert.AreEqual(new Vector3(5f, 0f, 0f), result);
        }

        [Test]
        public void InterpolateStep_ElapsedAtOrPastDuration_ClampsToEnd()
        {
            var end = new Vector3(3f, 4f, 0f);

            Assert.AreEqual(end, SoldierBase.InterpolateStep(Vector3.zero, end, 1f, 1f));
            Assert.AreEqual(end, SoldierBase.InterpolateStep(Vector3.zero, end, 5f, 1f)); // overshoot still clamps
        }

        [Test]
        public void InterpolateStep_DiagonalStep_SameElapsedFractionOfEqualDuration_ReachesSameProgressFractionAsOrthogonal()
        {
            // A diagonal step covers more world distance than an orthogonal one, but at the same
            // elapsed fraction of the same (fixed) duration both must be at the same *progress
            // fraction* — this is what makes the interpolation time-based, not distance-based,
            // fixing the bug where diagonal steps used to take longer at the same nominal speed.
            var orthogonalEnd = new Vector3(1f, 0f, 0f);
            var diagonalEnd = new Vector3(1f, 1f, 0f);

            var orthogonalResult = SoldierBase.InterpolateStep(Vector3.zero, orthogonalEnd, 0.5f, 1f);
            var diagonalResult = SoldierBase.InterpolateStep(Vector3.zero, diagonalEnd, 0.5f, 1f);

            Assert.AreEqual(0.5f, orthogonalResult.x, 0.0001f);
            Assert.AreEqual(0.5f, diagonalResult.x, 0.0001f);
            Assert.AreEqual(0.5f, diagonalResult.y, 0.0001f);
        }
    }
}
