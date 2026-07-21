using System;
using CaseGame.Pooling;
using NUnit.Framework;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Pooling
{
    public class PrefabPoolTests
    {
        private GameObject _template;
        private Transform _parent;
        private PrefabPool<Transform> _pool;

        [SetUp]
        public void SetUp()
        {
            _template = new GameObject("Template");
            _parent = new GameObject("Parent").transform;
            _pool = new PrefabPool<Transform>(_template.transform, _parent);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(_template);
            UnityEngine.Object.DestroyImmediate(_parent.gameObject);
        }

        [Test]
        public void Get_CreatesActiveInstance_WhenPoolEmpty()
        {
            var instance = _pool.Get();

            Assert.IsNotNull(instance);
            Assert.IsTrue(instance.gameObject.activeSelf);
        }

        [Test]
        public void Get_ParentsInstanceUnderProvidedTransform()
        {
            var instance = _pool.Get();

            Assert.AreEqual(_parent, instance.parent);
        }

        [Test]
        public void Get_Twice_WithoutRelease_ReturnsDistinctInstances()
        {
            var first = _pool.Get();
            var second = _pool.Get();

            Assert.AreNotSame(first, second);
        }

        [Test]
        public void Release_DeactivatesInstance()
        {
            var instance = _pool.Get();

            _pool.Release(instance);

            Assert.IsFalse(instance.gameObject.activeSelf);
        }

        [Test]
        public void Get_AfterRelease_ReusesSameInstance()
        {
            var first = _pool.Get();
            _pool.Release(first);

            var second = _pool.Get();

            Assert.AreSame(first, second);
        }

        [Test]
        public void CountActive_And_CountInactive_TrackCorrectly()
        {
            var first = _pool.Get();
            _pool.Get();
            _pool.Release(first);

            Assert.AreEqual(1, _pool.CountActive);
            Assert.AreEqual(1, _pool.CountInactive);
        }

        [Test]
        public void Release_SameInstanceTwice_Throws()
        {
            var instance = _pool.Get();
            _pool.Release(instance);

            Assert.Throws<InvalidOperationException>(() => _pool.Release(instance));
        }
    }
}
