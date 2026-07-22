using CaseGame.Entities;
using CaseGame.Pooling;
using UnityEngine;

namespace CaseGame.Units
{
    /// <summary>
    /// Factory pattern (brief-mandated), pooling <see cref="Projectile"/> instances in their own
    /// dedicated <see cref="PrefabPool{T}"/> — not reused from <see cref="UnitFactory"/> or
    /// <see cref="Buildings.BuildingFactory"/>, since projectiles are a materially different kind
    /// of thing (short-lived visual, no <see cref="Combat.Health"/>/definition). Only one
    /// projectile prefab is needed today (no per-soldier-type projectile variants requested), so
    /// unlike <c>UnitFactory</c>/<c>BuildingFactory</c> this wraps a single <see cref="PrefabPool{T}"/>
    /// directly rather than a per-prefab dictionary of pools.
    /// </summary>
    public class ProjectileFactory
    {
        private readonly PrefabPool<Projectile> _pool;

        public ProjectileFactory(Projectile prefab, Transform parent = null)
        {
            _pool = new PrefabPool<Projectile>(prefab, parent);
        }

        /// <summary>Spawns a projectile at <paramref name="startPosition"/> tracking <paramref name="target"/>, dealing <paramref name="damage"/> on arrival.</summary>
        public void Launch(Vector3 startPosition, GameEntityBase target, int damage)
        {
            var projectile = _pool.Get();
            projectile.transform.position = startPosition;
            projectile.Launch(target, damage, ReturnToPool);
        }

        private void ReturnToPool(Projectile projectile)
        {
            _pool.Release(projectile);
        }
    }
}
