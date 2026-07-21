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

        private IEnumerator FollowPath(GridModel grid, List<Vector2Int> path)
        {
            for (var i = 1; i < path.Count; i++)
            {
                var targetPosition = (Vector3)grid.CellCenterToWorld(path[i]);
                while ((transform.position - targetPosition).sqrMagnitude > 0.0001f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPosition, Definition.MoveSpeed * Time.deltaTime);
                    yield return null;
                }

                transform.position = targetPosition;
            }

            _moveCoroutine = null;
        }
    }
}
