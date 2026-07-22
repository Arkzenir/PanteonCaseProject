using System.Collections;
using System.Collections.Generic;
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
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int AttackHash = Animator.StringToHash("Attack");

        [SerializeField] private Animator animator;

        public new UnitDefinition Definition => (UnitDefinition)base.Definition;

        private Coroutine _actionCoroutine;
        private GameEntityBase _pendingRangedTarget;
        private int _pendingRangedDamage;
        private ProjectileFactory _pendingProjectileFactory;

        /// <summary>Whether a move or attack coroutine is currently running — exposed for observers/tests, mirroring <c>PlacementController.IsPlacing</c>'s pattern.</summary>
        public bool IsActing => _actionCoroutine != null;

        /// <summary>Requests the shortest path to <paramref name="targetCell"/> and walks it, routing around buildings (GI-7/8). Cancels any in-progress attack — a move order always takes over, per the human's cancellation rule. No-ops (but still cancels) if unreachable or already there.</summary>
        public void MoveTo(Vector2Int targetCell, GridModel grid)
        {
            CancelAction();

            var path = AStarPathfinder.FindPath(grid, grid.WorldToCell(transform.position), targetCell);
            if (path == null || path.Count <= 1)
            {
                return;
            }

            _actionCoroutine = StartCoroutine(MoveRoutine(grid, path));
        }

        /// <summary>
        /// Commands this soldier to attack <paramref name="target"/> (GI-10/11): walks into
        /// <see cref="UnitDefinition.AttackRange"/> first if not already there, then attacks once
        /// every <c>1 / AttackSpeed</c> seconds until the target leaves range or dies. Cancels
        /// (and replaces) any in-progress move or attack — calling this again with a different
        /// target switches onto it immediately, no "attack lock" to clear first, matching normal
        /// RTS convention (human-confirmed). No-ops on a null or already-dead target.
        /// </summary>
        public void Attack(GameEntityBase target, GridModel grid, ProjectileFactory projectileFactory)
        {
            if (target == null || target.IsDead)
            {
                return;
            }

            CancelAction();
            _actionCoroutine = StartCoroutine(AttackRoutine(target, grid, projectileFactory));
        }

        /// <summary>Cancels whichever move/attack coroutine is currently running. Safe to call when nothing is active. Also resets the "moving" animation state — <c>StopCoroutine</c> abandons <see cref="FollowPath"/> wherever it was suspended, so without this an interrupted move (e.g. a new attack order mid-stride) would leave the run animation stuck on.</summary>
        public void CancelAction()
        {
            if (_actionCoroutine != null)
            {
                StopCoroutine(_actionCoroutine);
                _actionCoroutine = null;
            }

            if (animator != null)
            {
                animator.SetBool(IsMovingHash, false);
            }
        }

        /// <summary>Seconds a single grid step should take at the given cells-per-second move speed — every step, orthogonal or diagonal, counts as exactly 1 cell (GI-7/8's "shortest path" is measured in steps, not world distance), so this depends only on <paramref name="moveSpeed"/>, never on the step's actual world distance. Pure so this specific contract is directly testable without a live coroutine/Update loop.</summary>
        public static float StepDuration(float moveSpeed) => 1f / moveSpeed;

        /// <summary>Seconds between attacks at the given attacks-per-second rate. Pure, mirrors <see cref="StepDuration"/>.</summary>
        public static float AttackInterval(float attackSpeed) => 1f / attackSpeed;

        /// <summary>Grid (Chebyshev) distance between two cells — matches the 8-directional movement this project already uses, so "in range" means "within N steps," not raw world distance. Pure and testable.</summary>
        public static int ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        /// <summary>Whether a unit at <paramref name="attackerCell"/> is within <paramref name="range"/> cells of <paramref name="targetCell"/>. Pure and testable, independent of any live target/transform.</summary>
        public static bool IsInRange(Vector2Int attackerCell, Vector2Int targetCell, int range)
        {
            return ChebyshevDistance(attackerCell, targetCell) <= range;
        }

        /// <summary>Position along a single step at a given elapsed time — clamped to <paramref name="end"/> once <paramref name="elapsed"/> reaches <paramref name="duration"/>. Pure so the interpolation itself is testable independent of <see cref="Time.deltaTime"/>.</summary>
        public static Vector3 InterpolateStep(Vector3 start, Vector3 end, float elapsed, float duration)
        {
            return Vector3.Lerp(start, end, duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f);
        }

        /// <summary>Whether facing from X <paramref name="fromX"/> toward X <paramref name="toX"/> means facing left (the art's default, unflipped orientation faces right) — used for both movement-direction and attack-target-direction sprite flipping. Callers should only act on this when the two X values actually differ; a purely-vertical move/attack has no defined horizontal facing and should just keep whatever facing was already set. Pure and testable.</summary>
        public static bool FacesLeft(float fromX, float toX) => toX < fromX;

        /// <summary>Called via Animation Event (relayed from the <c>Visuals</c> child's <see cref="Animator"/> — see <see cref="SoldierAnimationEvents"/>) at the moment a ranged attack's projectile should actually launch, e.g. when the bow visibly releases the arrow, rather than the instant the attack tick started (human-requested, Report 034). No-ops if the pending target died, or was cleared by a newer attack overwriting it, before the event fired.</summary>
        public void ReleaseAttack()
        {
            if (_pendingRangedTarget == null || _pendingRangedTarget.IsDead)
            {
                _pendingRangedTarget = null;
                return;
            }

            _pendingProjectileFactory.Launch(transform.position, _pendingRangedTarget, _pendingRangedDamage);
            _pendingRangedTarget = null;
        }

        private IEnumerator MoveRoutine(GridModel grid, List<Vector2Int> path)
        {
            yield return FollowPath(grid, path);
            _actionCoroutine = null;
        }

        private IEnumerator AttackRoutine(GameEntityBase target, GridModel grid, ProjectileFactory projectileFactory)
        {
            var attackerCell = grid.WorldToCell(transform.position);
            var nearestTargetCell = target.GetNearestOccupiedCell(grid, attackerCell);

            if (!IsInRange(attackerCell, nearestTargetCell, Definition.AttackRange))
            {
                var approachCell = AStarPathfinder.FindApproachCell(grid, attackerCell, nearestTargetCell, Definition.AttackRange);

                if (approachCell.HasValue)
                {
                    var path = AStarPathfinder.FindPath(grid, attackerCell, approachCell.Value);
                    if (path != null && path.Count > 1)
                    {
                        yield return FollowPath(grid, path);
                    }
                }
            }

            var attackInterval = AttackInterval(Definition.AttackSpeed);

            while (target != null && !target.IsDead &&
                   IsInRange(grid.WorldToCell(transform.position), target.GetNearestOccupiedCell(grid, grid.WorldToCell(transform.position)), Definition.AttackRange))
            {
                PerformAttack(target, projectileFactory);

                var elapsed = 0f;
                while (elapsed < attackInterval)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
            }

            _actionCoroutine = null;
        }

        /// <summary>
        /// Melee attacks apply damage instantly (unchanged brief-minimum behavior). Ranged
        /// attacks launch a tracked <see cref="Projectile"/> instead — but only immediately if
        /// there's no <see cref="animator"/> to synchronize with; when one exists, the launch is
        /// deferred to <see cref="ReleaseAttack"/> (an Animation Event partway through the Shoot
        /// clip, human-requested so the arrow visibly leaves the bow instead of the moment the
        /// attack tick started). Fires the <c>Attack</c> animation trigger once per call, the
        /// single point every actual hit/shot passes through, and faces this soldier toward the
        /// target first.
        /// </summary>
        private void PerformAttack(GameEntityBase target, ProjectileFactory projectileFactory)
        {
            var deltaX = target.transform.position.x - transform.position.x;
            if (!Mathf.Approximately(deltaX, 0f))
            {
                SetFlippedHorizontally(FacesLeft(0f, deltaX));
            }

            if (animator != null)
            {
                animator.SetTrigger(AttackHash);
            }

            if (Definition.Ranged && projectileFactory != null)
            {
                if (animator != null)
                {
                    _pendingRangedTarget = target;
                    _pendingRangedDamage = Definition.AttackDamage;
                    _pendingProjectileFactory = projectileFactory;
                }
                else
                {
                    projectileFactory.Launch(transform.position, target, Definition.AttackDamage);
                }
            }
            else
            {
                target.ApplyDamage(Definition.AttackDamage);
            }
        }

        /// <summary>Walks <paramref name="path"/> one cell at a time. Toggles the <c>IsMoving</c> animation bool on for the duration — used by both <see cref="MoveRoutine"/> and <see cref="AttackRoutine"/>'s walk-into-range phase, so either context shows the run animation.</summary>
        private IEnumerator FollowPath(GridModel grid, List<Vector2Int> path)
        {
            if (animator != null)
            {
                animator.SetBool(IsMovingHash, true);
            }

            var stepDuration = StepDuration(Definition.MoveSpeed);

            for (var i = 1; i < path.Count; i++)
            {
                var startPosition = transform.position;
                var targetPosition = (Vector3)grid.CellCenterToWorld(path[i]);
                var elapsed = 0f;

                var deltaX = targetPosition.x - startPosition.x;
                if (!Mathf.Approximately(deltaX, 0f))
                {
                    SetFlippedHorizontally(FacesLeft(0f, deltaX));
                }

                while (elapsed < stepDuration)
                {
                    elapsed += Time.deltaTime;
                    transform.position = InterpolateStep(startPosition, targetPosition, elapsed, stepDuration);
                    yield return null;
                }

                transform.position = targetPosition;
            }

            if (animator != null)
            {
                animator.SetBool(IsMovingHash, false);
            }
        }
    }
}
