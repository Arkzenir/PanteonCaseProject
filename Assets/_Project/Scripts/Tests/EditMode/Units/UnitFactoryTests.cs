using CaseGame.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Units
{
    public class UnitFactoryTests
    {
        private static UnitDefinition CreateDefinition(int maxHealth)
        {
            var definition = ScriptableObject.CreateInstance<UnitDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("maxHealth").intValue = maxHealth;
            so.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        [Test]
        public void Create_ReturnsInstanceInitializedWithDefinition()
        {
            var prefab = new GameObject("Prefab").AddComponent<Soldier>();
            var definition = CreateDefinition(10);
            var factory = new UnitFactory();

            var instance = factory.Create(definition, prefab);

            Assert.IsNotNull(instance);
            Assert.AreSame(definition, instance.Definition);
            Assert.AreEqual(10, instance.CurrentHealth);

            Object.DestroyImmediate(prefab.gameObject);
            Object.DestroyImmediate(instance.gameObject);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Create_AfterInstanceDies_ReusesPooledInstance()
        {
            var prefab = new GameObject("Prefab").AddComponent<Soldier>();
            var definition = CreateDefinition(10);
            var factory = new UnitFactory();

            var first = factory.Create(definition, prefab);
            first.ApplyDamage(10); // kills it, releasing it back to the pool via the factory's callback

            var second = factory.Create(definition, prefab);

            Assert.AreSame(first, second);
            Assert.IsFalse(second.IsDead); // Initialize() built a fresh Health

            Object.DestroyImmediate(prefab.gameObject);
            Object.DestroyImmediate(second.gameObject);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Create_DifferentPrefabs_ReturnInstancesOfEachDistinctPrefab()
        {
            var prefabA = new GameObject("PrefabA").AddComponent<Soldier>();
            var prefabB = new GameObject("PrefabB").AddComponent<Soldier>();
            var definition = CreateDefinition(10);
            var factory = new UnitFactory();

            var instanceA = factory.Create(definition, prefabA);
            var instanceB = factory.Create(definition, prefabB);

            Assert.AreNotSame(instanceA, instanceB);

            Object.DestroyImmediate(prefabA.gameObject);
            Object.DestroyImmediate(prefabB.gameObject);
            Object.DestroyImmediate(instanceA.gameObject);
            Object.DestroyImmediate(instanceB.gameObject);
            Object.DestroyImmediate(definition);
        }
    }
}
