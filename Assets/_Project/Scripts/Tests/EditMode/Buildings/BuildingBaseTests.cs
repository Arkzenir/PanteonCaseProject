using CaseGame.Buildings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Buildings
{
    public class BuildingBaseTests
    {
        private class TestBuilding : BuildingBase
        {
        }

        private static BuildingDefinition CreateDefinition(int maxHealth)
        {
            var definition = ScriptableObject.CreateInstance<BuildingDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("maxHealth").intValue = maxHealth;
            so.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        [Test]
        public void Initialize_SetsDefinitionAndHealthFromDefinition()
        {
            var go = new GameObject("Building");
            var building = go.AddComponent<TestBuilding>();
            var definition = CreateDefinition(50);

            building.Initialize(definition);

            Assert.AreSame(definition, building.Definition);
            Assert.AreEqual(50, building.MaxHealth);
            Assert.AreEqual(50, building.CurrentHealth);
            Assert.IsFalse(building.IsDead);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void ApplyDamage_ForwardsToHealth_ReducesCurrentHealth()
        {
            var go = new GameObject("Building");
            var building = go.AddComponent<TestBuilding>();
            var definition = CreateDefinition(50);
            building.Initialize(definition);

            building.ApplyDamage(20);

            Assert.AreEqual(30, building.CurrentHealth);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void ApplyDamage_ReducingToZero_SetsIsDeadAndInvokesOnDiedCallback()
        {
            var go = new GameObject("Building");
            var building = go.AddComponent<TestBuilding>();
            var definition = CreateDefinition(10);
            var diedCalled = false;

            building.Initialize(definition, () => diedCalled = true);
            building.ApplyDamage(10);

            Assert.IsTrue(building.IsDead);
            Assert.IsTrue(diedCalled);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Initialize_WithoutOnDiedCallback_DoesNotThrowWhenKilled()
        {
            var go = new GameObject("Building");
            var building = go.AddComponent<TestBuilding>();
            var definition = CreateDefinition(10);

            building.Initialize(definition);

            Assert.DoesNotThrow(() => building.ApplyDamage(10));
            Assert.IsTrue(building.IsDead);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }
    }
}
