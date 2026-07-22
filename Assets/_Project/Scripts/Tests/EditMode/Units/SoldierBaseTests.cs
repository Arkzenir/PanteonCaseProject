using CaseGame.Grid;
using CaseGame.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Units
{
    public class SoldierBaseTests
    {
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
        public void Attack_NullTarget_DoesNotThrowAndDoesNotStartActing()
        {
            var go = new GameObject("Soldier");
            var soldier = go.AddComponent<Soldier>();
            var definition = CreateDefinition(10, 5);
            soldier.Initialize(definition);
            var grid = CreateGrid(5, 5);

            Assert.DoesNotThrow(() => soldier.Attack(null, grid, null));
            Assert.IsFalse(soldier.IsActing);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Attack_DeadTarget_DoesNotThrowAndDoesNotStartActing()
        {
            var go = new GameObject("Soldier");
            var soldier = go.AddComponent<Soldier>();
            var definition = CreateDefinition(10, 5);
            soldier.Initialize(definition);
            var grid = CreateGrid(5, 5);

            var targetGo = new GameObject("Target");
            var target = targetGo.AddComponent<Soldier>();
            var targetDefinition = CreateDefinition(10, 0);
            target.Initialize(targetDefinition);
            target.ApplyDamage(10); // kills it

            Assert.DoesNotThrow(() => soldier.Attack(target, grid, null));
            Assert.IsFalse(soldier.IsActing);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(targetGo);
            Object.DestroyImmediate(targetDefinition);
        }

        [Test]
        public void ChebyshevDistance_DiagonalAndOrthogonalOffsets_ReturnsMaxOfAbsoluteDeltas()
        {
            Assert.AreEqual(3, SoldierBase.ChebyshevDistance(new Vector2Int(0, 0), new Vector2Int(3, 1)));
            Assert.AreEqual(2, SoldierBase.ChebyshevDistance(new Vector2Int(0, 0), new Vector2Int(2, 2)));
            Assert.AreEqual(0, SoldierBase.ChebyshevDistance(new Vector2Int(4, 4), new Vector2Int(4, 4)));
        }

        [Test]
        public void IsInRange_WithinRange_ReturnsTrue()
        {
            Assert.IsTrue(SoldierBase.IsInRange(new Vector2Int(0, 0), new Vector2Int(1, 1), range: 1));
        }

        [Test]
        public void IsInRange_BeyondRange_ReturnsFalse()
        {
            Assert.IsFalse(SoldierBase.IsInRange(new Vector2Int(0, 0), new Vector2Int(2, 0), range: 1));
        }

        [Test]
        public void AttackInterval_ReturnsReciprocalOfAttackSpeed()
        {
            Assert.AreEqual(0.5f, SoldierBase.AttackInterval(2f), 0.0001f);
        }

        [Test]
        public void FacesLeft_TargetXLessThanCurrentX_ReturnsTrue()
        {
            Assert.IsTrue(SoldierBase.FacesLeft(fromX: 5f, toX: 0f));
        }

        [Test]
        public void FacesLeft_TargetXGreaterThanCurrentX_ReturnsFalse()
        {
            Assert.IsFalse(SoldierBase.FacesLeft(fromX: 0f, toX: 5f));
        }

        [Test]
        public void FacesLeft_SameX_ReturnsFalse()
        {
            // Ties default to "right" (unflipped) — matches the art's own default-right orientation.
            Assert.IsFalse(SoldierBase.FacesLeft(fromX: 3f, toX: 3f));
        }

        [Test]
        public void ReleaseAttack_NoPendingTarget_DoesNotThrow()
        {
            var go = new GameObject("Soldier");
            var soldier = go.AddComponent<Soldier>();
            var definition = CreateDefinition(10, 5);
            soldier.Initialize(definition);

            Assert.DoesNotThrow(() => soldier.ReleaseAttack());

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void MoveTo_TowardCellToTheLeft_FlipsSpriteHorizontally()
        {
            var (soldier, spriteRenderer) = CreateSoldierWithSpriteRenderer();
            var definition = CreateDefinition(10, 5);
            soldier.Initialize(definition);
            var grid = CreateGrid(5, 5);
            soldier.transform.position = grid.CellCenterToWorld(new Vector2Int(3, 2));

            soldier.MoveTo(new Vector2Int(0, 2), grid); // strictly to the left

            Assert.IsTrue(spriteRenderer.flipX);

            Object.DestroyImmediate(soldier.gameObject);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void MoveTo_TowardCellToTheRight_DoesNotFlipSprite()
        {
            var (soldier, spriteRenderer) = CreateSoldierWithSpriteRenderer();
            var definition = CreateDefinition(10, 5);
            soldier.Initialize(definition);
            var grid = CreateGrid(5, 5);
            soldier.transform.position = grid.CellCenterToWorld(new Vector2Int(0, 2));

            soldier.MoveTo(new Vector2Int(4, 2), grid); // strictly to the right

            Assert.IsFalse(spriteRenderer.flipX);

            Object.DestroyImmediate(soldier.gameObject);
            Object.DestroyImmediate(definition);
        }

        private static (Soldier soldier, SpriteRenderer spriteRenderer) CreateSoldierWithSpriteRenderer()
        {
            var go = new GameObject("Soldier");
            var spriteRenderer = new GameObject("Visuals").AddComponent<SpriteRenderer>();
            spriteRenderer.transform.SetParent(go.transform);
            var soldier = go.AddComponent<Soldier>();
            var so = new SerializedObject(soldier);
            so.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            so.ApplyModifiedPropertiesWithoutUndo();
            return (soldier, spriteRenderer);
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
