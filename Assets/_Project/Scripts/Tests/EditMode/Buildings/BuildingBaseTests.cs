using CaseGame.Buildings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Buildings
{
    public class BuildingBaseTests
    {
        // Health/IDamageable/death-callback behavior is shared GameEntityBase behavior,
        // covered once in Tests/EditMode/Entities/GameEntityBaseTests.cs — not duplicated here.

        private class TestBuilding : BuildingBase
        {
        }

        [Test]
        public void Definition_ReturnsStronglyTypedBuildingDefinition()
        {
            var go = new GameObject("Building");
            var building = go.AddComponent<TestBuilding>();
            var definition = ScriptableObject.CreateInstance<BuildingDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("maxHealth").intValue = 10;
            so.ApplyModifiedPropertiesWithoutUndo();

            building.Initialize(definition);

            Assert.AreSame(definition, building.Definition);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }
    }
}
