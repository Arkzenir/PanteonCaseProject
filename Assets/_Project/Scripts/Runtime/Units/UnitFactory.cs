using System.Collections.Generic;
using CaseGame.Pooling;
using UnityEngine;

namespace CaseGame.Units
{
    /// <summary>
    /// Factory pattern (brief-mandated): creates configured, pooled <see cref="SoldierBase"/>
    /// instances from a <see cref="UnitDefinition"/> + prefab. Mirrors <c>BuildingFactory</c>
    /// exactly — one <see cref="PrefabPool{T}"/> per distinct prefab, created lazily.
    /// </summary>
    public class UnitFactory
    {
        private readonly Dictionary<SoldierBase, PrefabPool<SoldierBase>> _pools =
            new Dictionary<SoldierBase, PrefabPool<SoldierBase>>();

        private readonly Transform _parent;

        public UnitFactory(Transform parent = null)
        {
            _parent = parent;
        }

        public SoldierBase Create(UnitDefinition definition, SoldierBase prefab)
        {
            var pool = GetOrCreatePool(prefab);
            var instance = pool.Get();
            instance.Initialize(definition, () => pool.Release(instance));
            return instance;
        }

        private PrefabPool<SoldierBase> GetOrCreatePool(SoldierBase prefab)
        {
            if (!_pools.TryGetValue(prefab, out var pool))
            {
                pool = new PrefabPool<SoldierBase>(prefab, _parent);
                _pools[prefab] = pool;
            }

            return pool;
        }
    }
}
