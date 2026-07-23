using UnityEngine;
using UnityEngine.Pool;

namespace CaseGame.Pooling
{
    /// <summary>
    /// Reusable prefab-instance pool for any Component type, built on Unity's built-in
    /// <see cref="ObjectPool{T}"/>. Callers <see cref="Get"/>/<see cref="Release"/> instances
    /// instead of calling Instantiate/Destroy directly, so any concrete type that needs pooling
    /// (list items, soldiers, buildings) can reuse this instead of a bespoke pool each time.
    /// </summary>
    public class PrefabPool<T> where T : Component
    {
        private readonly ObjectPool<T> _pool;
        private readonly T _prefab;
        private readonly Transform _parent;

        public PrefabPool(T prefab, Transform parent = null, int defaultCapacity = 8, int maxSize = 128)
        {
            _prefab = prefab;
            _parent = parent;
            _pool = new ObjectPool<T>(
                CreateInstance,
                instance => instance.gameObject.SetActive(true),
                instance => instance.gameObject.SetActive(false),
                instance => Object.Destroy(instance.gameObject),
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        public int CountActive => _pool.CountActive;
        public int CountInactive => _pool.CountInactive;

        public T Get()
        {
            return _pool.Get();
        }

        public void Release(T instance)
        {
            _pool.Release(instance);
        }

        private T CreateInstance()
        {
            return Object.Instantiate(_prefab, _parent);
        }
    }
}
