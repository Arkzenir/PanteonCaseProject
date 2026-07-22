using System.Collections;
using System.Collections.Generic;
using CaseGame.Combat;
using CaseGame.Entities;
using CaseGame.Grid;
using CaseGame.Pathfinding;
using UnityEngine;

namespace CaseGame.Units
{
    /// <summary>
    /// Unit-specific base on top of <see cref="GameEntityBase"/>: movement along a pathfound
    /// route (brief-mandated Coroutine usage, GI-7/8) and attacking (GI-10/11). Re-exposes
    /// <see cref="Definition"/> as the strongly-typed <see cref="UnitDefinition"/>, mirroring
    /// <c>BuildingBase</c>.
    ///
    /// The brief's 3 soldier types are data variants of the single concrete <see cref="Soldier"/>
    /// subclass (three different <see cref="UnitDefinition"/> assets/prefabs), not three
    /// separate classes — nothing behavioral differs between Soldier 1/2/3, only attack damage
    /// (a data value), so three empty subclasses would be an OOP checkbox with no real
    /// substance behind it. See ARCHITECTURE.md's decisions log for the full reasoning.
    /// </summary>
    public abstract class SoldierBase : GameEntityBase
    {
        public new UnitDefinition Definition => (UnitDefinition)base.Definition;

        private Coroutine _moveCoroutine;

        /// <summary>Requests the shortest path to <paramref name="targetCell"/> and walks it, routing around buildings (GI-7/8). No-ops if unreachable or already there.</summary>
        public void MoveTo(Vector2Int targetCell, GridModel grid)
        {
            var path = AStarPathfinder.FindPath(grid, grid.WorldToCell(transform.position), targetCell);
            if (path == null || path.Count <= 1)
            {
                return;
            }

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
            }

            _moveCoroutine = StartCoroutine(FollowPath(grid, path));
        }

        /// <summary>
        /// Applies this soldier's attack damage to a target (GI-10/11). Simple null-check
        /// only — full Unity destroyed-but-not-null-via-interface safety is the caller's job
        /// once Selection actually wires up attack commands and tracks target lifetime itself.
        /// </summary>
        public void TryAttack(IDamageable target)
        {
            if (target == null)
            {
                return;
            }

            target.ApplyDamage(Definition.AttackDamage);
        }

        /// <summary>Seconds a single grid step should take at the given cells-per-second move speed — every step, orthogonal or diagonal, counts as exactly 1 cell (GI-7/8's "shortest path" is measured in steps, not world distance), so this depends only on <paramref name="moveSpeed"/>, never on the step's actual world distance. Pure so this specific contract is directly testable without a live coroutine/Update loop.</summary>
        public static float StepDuration(float moveSpeed) => 1f / moveSpeed;

        /// <summary>Position along a single step at a given elapsed time — clamped to <paramref name="end"/> once <paramref name="elapsed"/> reaches <paramref name="duration"/>. Pure so the interpolation itself is testable independent of <see cref="Time.deltaTime"/>.</summary>
        public static Vector3 InterpolateStep(Vector3 start, Vector3 end, float elapsed, float duration)
        {
            return Vector3.Lerp(start, end, duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f);
        }

        private IEnumerator FollowPath(GridModel grid, List<Vector2Int> path)
        {
            var stepDuration = StepDuration(Definition.MoveSpeed);

            for (var i = 1; i < path.Count; i++)
            {
                var startPosition = transform.position;
                var targetPosition = (Vector3)grid.CellCenterToWorld(path[i]);
                var elapsed = 0f;

                while (elapsed < stepDuration)
                {
                    elapsed += Time.deltaTime;
                    transform.position = InterpolateStep(startPosition, targetPosition, elapsed, stepDuration);
                    yield return null;
                }

                transform.position = targetPosition;
            }

            _moveCoroutine = null;
        }
    }
}
