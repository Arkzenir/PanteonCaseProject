using System.Collections.Generic;
using CaseGame.Pooling;
using UnityEngine;

namespace CaseGame.Buildings
{
    /// <summary>
    /// Factory pattern (brief-mandated): creates configured, pooled <see cref="BuildingBase"/>
    /// instances from a <see cref="BuildingDefinition"/> + prefab. One <see cref="PrefabPool{T}"/>
    /// per distinct prefab, created lazily. Doesn't touch grid occupancy or world position —
    /// that's Placement's job once it exists (ARCHITECTURE.md: Placement owns "commit-to-grid").
    /// </summary>
    public class BuildingFactory
    {
        private readonly Dictionary<BuildingBase, PrefabPool<BuildingBase>> _pools =
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
            instance.Initialize(definition, () => pool.Release(instance));
            return instance;
        }

        /// <summary>Manually returns an instance to its pool without it having "died" — e.g. Placement cancelling a ghost that was never committed.</summary>
        public void Release(BuildingBase prefab, BuildingBase instance)
        {
            GetOrCreatePool(prefab).Release(instance);
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
