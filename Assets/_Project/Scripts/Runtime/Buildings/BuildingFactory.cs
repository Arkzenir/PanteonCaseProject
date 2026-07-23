using System.Collections.Generic;
using CaseGame.Pooling;
using UnityEngine;

namespace CaseGame.Buildings
{
    /// <summary>
    /// Creates configured, pooled <see cref="BuildingBase"/> instances from a
    /// <see cref="BuildingDefinition"/> + prefab. One <see cref="PrefabPool{T}"/> per distinct
    /// prefab, created lazily. Doesn't touch grid occupancy or world position — that's
    /// Placement's responsibility once a building is actually committed to the grid.
    /// </summary>
    public class BuildingFactory
    {
        private readonly Dictionary<BuildingBase, PrefabPool<BuildingBase>> _pools =
            new Dictionary<BuildingBase, PrefabPool<BuildingBase>>();

        private readonly Dictionary<BuildingBase, PrefabPool<BuildingBase>> _instancePools =
            new Dictionary<BuildingBase, PrefabPool<BuildingBase>>();

        private readonly Transform _parent;

        public BuildingFactory(Transform parent = null)
        {
            _parent = parent;
        }

        public BuildingBase Create(BuildingDefinition definition, BuildingBase prefab)
        {
            var pool = GetOrCreatePool(prefab);
            var instance = pool.Get();
            instance.Initialize(definition, () => Release(instance));
            _instancePools[instance] = pool;
            return instance;
        }

        /// <summary>Returns an instance to whichever pool it came from — used by the death pipeline's pooling callback (combat) and directly by manual removal (Placement cancelling a ghost, or removing an already-placed building) alike. Neither caller needs to know or pass back the originating prefab.</summary>
        public void Release(BuildingBase instance)
        {
            if (_instancePools.TryGetValue(instance, out var pool))
            {
                pool.Release(instance);
                _instancePools.Remove(instance);
            }
        }

        private PrefabPool<BuildingBase> GetOrCreatePool(BuildingBase prefab)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new PrefabPool<BuildingBase>(prefab, _parent);
                _pools[prefab] = pool;
            }

            return pool;
        }
    }
}
