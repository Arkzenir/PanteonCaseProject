using CaseGame.Entities;
using UnityEngine;

namespace CaseGame.Units
{
    /// <summary>
    /// Pooled, purely-visual ranged-attack indicator (GI-10/11's ranged case, human-requested
    /// beyond the brief's minimum). Tracks its target's *current* position every frame — not a
    /// fixed straight-line trajectory toward a snapshot position — so it's visually clear which
    /// unit is shooting at which target even if the target is moving. No <c>Collider</c>/
    /// <c>Rigidbody</c>: it never collides with anything it passes over, it just applies damage
    /// once it reaches the target's actual position. A plain pooled MonoBehaviour rather than a
    /// Shuriken <c>ParticleSystem</c> — reproducing per-target homing and an exact
    /// arrived-so-apply-damage trigger with <c>ParticleSystem</c> would mean manually driving
    /// individual particles via <c>SetParticles</c>/<c>GetParticles</c> every frame anyway, no
    /// ergonomic win over a MonoBehaviour (see ARCHITECTURE.md decisions log).
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 12f;
        [SerializeField] private float arrivalDistance = 0.05f;

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

        private void Update()
        {
            // Pooled-reuse hazard (same class of issue as decisions log #39/#52/#55): the target
            // reference can go stale (destroyed-and-reused for something else) between frames.
            // IsDead is the guard — a freshly-reused instance resets IsDead to false via its own
            // Initialize, so worst case is a narrow window where a dying target gets replaced
            // mid-flight; accepted at the same tolerance this project already applies elsewhere.
            if (_target == null || _target.IsDead)
            {
                Arrive(applyDamage: false);
                return;
            }

            var targetPosition = _target.transform.position;
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
