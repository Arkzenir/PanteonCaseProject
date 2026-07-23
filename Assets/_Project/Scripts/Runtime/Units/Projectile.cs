using CaseGame.Entities;
using UnityEngine;

namespace CaseGame.Units
{
    /// <summary>
    /// Pooled, purely-visual ranged-attack projectile. Tracks its target's current position every
    /// frame rather than a fixed trajectory, so it visibly follows a moving target. Has no
    /// <c>Collider</c>/<c>Rigidbody</c> — it applies damage directly once it reaches the target's
    /// position instead of colliding with anything along the way. A plain pooled MonoBehaviour
    /// rather than a <c>ParticleSystem</c>: reproducing per-target homing and an arrival trigger
    /// with particles would mean manually driving individual particles every frame anyway, with
    /// no real benefit.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float arrivalDistance = 0.05f;
        [SerializeField] private float spriteForwardOffsetDegrees;

        private GameEntityBase _target;
        private int _damage;
        private System.Action<Projectile> _onArrived;

        /// <summary>Starts (or restarts, if pooled and reused) this projectile toward <paramref name="target"/>. <paramref name="onArrived"/> is called exactly once, whether the projectile actually lands a hit or fizzles because the target is already gone — that's the factory's cue to return it to the pool.</summary>
        public void Launch(GameEntityBase target, int damage, System.Action<Projectile> onArrived)
        {
            _target = target;
            _damage = damage;
            _onArrived = onArrived;
        }

        /// <summary>Position after moving at <paramref name="speed"/> for <paramref name="deltaTime"/> seconds toward <paramref name="targetPosition"/>. Pure wrapper around <see cref="Vector3.MoveTowards"/> so the step math is directly testable independent of a live <c>Update</c> loop.</summary>
        public static Vector3 Step(Vector3 currentPosition, Vector3 targetPosition, float speed, float deltaTime)
        {
            return Vector3.MoveTowards(currentPosition, targetPosition, speed * deltaTime);
        }

        /// <summary>Whether <paramref name="currentPosition"/> is close enough to <paramref name="targetPosition"/> to count as arrived. Pure so the arrival threshold is directly testable.</summary>
        public static bool HasArrived(Vector3 currentPosition, Vector3 targetPosition, float arrivalDistance)
        {
            return Vector3.Distance(currentPosition, targetPosition) <= arrivalDistance;
        }

        /// <summary>Rotation that points this projectile from <paramref name="currentPosition"/> toward <paramref name="targetPosition"/>, so it visibly faces its travel direction. <paramref name="forwardOffsetDegrees"/> corrects for the sprite's own drawn orientation — the art's "forward" isn't guaranteed to be +X, so this is inspector-tunable rather than hardcoded. Identity when the two positions coincide, which only happens at the exact arrival instant. Pure so the angle math is directly testable independent of a live <c>Update</c> loop.</summary>
        public static Quaternion FacingRotation(Vector3 currentPosition, Vector3 targetPosition, float forwardOffsetDegrees)
        {
            var direction = targetPosition - currentPosition;
            if (direction.sqrMagnitude < 0.0000001f)
            {
                return Quaternion.identity;
            }

            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            return Quaternion.AngleAxis(angle + forwardOffsetDegrees, Vector3.forward);
        }

        private void Update()
        {
            // Pooled-reuse hazard: the target reference can go stale (destroyed and reused for
            // something else) between frames. IsDead guards against this — a freshly-reused
            // instance resets IsDead to false via its own Initialize, so worst case is a narrow
            // window where a dying target gets replaced mid-flight.
            if (_target == null || _target.IsDead)
            {
                Arrive(applyDamage: false);
                return;
            }

            var targetPosition = _target.transform.position;
            transform.rotation = FacingRotation(transform.position, targetPosition, spriteForwardOffsetDegrees);
            transform.position = Step(transform.position, targetPosition, speed, Time.deltaTime);

            if (HasArrived(transform.position, targetPosition, arrivalDistance))
            {
                Arrive(applyDamage: true);
            }
        }

        private void Arrive(bool applyDamage)
        {
            if (applyDamage && _target != null && !_target.IsDead)
            {
                _target.ApplyDamage(_damage);
            }

            _target = null;
            var callback = _onArrived;
            _onArrived = null;
            callback?.Invoke(this);
        }
    }
}
