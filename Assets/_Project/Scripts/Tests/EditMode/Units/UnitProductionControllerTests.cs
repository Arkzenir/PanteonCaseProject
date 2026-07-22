using CaseGame.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Units
{
    public class UnitProductionControllerTests
    {
        private UnitFactory _factory;
        private UnitProductionController _controller;
        private Soldier _prefab;
        private UnitDefinition _definition;

        [SetUp]
        public void SetUp()
        {
            _factory = new UnitFactory();
            _controller = new GameObject("UnitProductionController").AddComponent<UnitProductionController>();
            _controller.Initialize(_factory);

            _prefab = new GameObject("Prefab").AddComponent<Soldier>();
            _definition = ScriptableObject.CreateInstance<UnitDefinition>();
            var so = new SerializedObject(_definition);
            so.FindProperty("maxHealth").intValue = 10;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_controller.gameObject);
            Object.DestroyImmediate(_prefab.gameObject);
            Object.DestroyImmediate(_definition);
        }

        [Test]
        public void HandleProduceRequested_CreatesInstanceAtSpawnPosition()
        {
            var entry = new UnitCatalogEntry(_definition, _prefab);
            var spawnPosition = new Vector3(7f, 2f, 0f);

            _controller.HandleProduceRequested(new UnitProductionRequest(entry, spawnPosition));

            var pool = FindActiveInstance();
            Assert.IsNotNull(pool);
            Assert.AreEqual(spawnPosition, pool.transform.position);

            Object.DestroyImmediate(pool.gameObject);
        }

        [Test]
        public void HandleProduceRequested_InstanceIsInitializedWithDefinition()
        {
            var entry = new UnitCatalogEntry(_definition, _prefab);

            _controller.HandleProduceRequested(new UnitProductionRequest(entry, Vector3.zero));

            var instance = FindActiveInstance();
            Assert.AreSame(_definition, instance.Definition);

            Object.DestroyImmediate(instance.gameObject);
        }

        private Soldier FindActiveInstance()
        {
            foreach (var soldier in Object.FindObjectsOfType<Soldier>())
            {
                if (soldier.gameObject != _prefab.gameObject)
                {
                    return soldier;
                }
            }

            return null;
        }
    }
}
