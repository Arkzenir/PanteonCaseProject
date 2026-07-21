using CaseGame.Buildings;
using CaseGame.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Buildings
{
    public class BuildingDefinitionTests
    {
        // Footprint/max-health clamping is shared GameEntityDefinition behavior, covered once
        // in Tests/EditMode/Entities/GameEntityDefinitionTests.cs — not duplicated here.

        [Test]
        public void CanProduceUnits_FalseWhenProducibleListEmpty()
        {
            var definition = ScriptableObject.CreateInstance<BuildingDefinition>();

            Assert.IsFalse(definition.CanProduceUnits);
            Assert.IsEmpty(definition.ProducibleUnits);
        }

        [Test]
        public void CanProduceUnits_TrueWhenProducibleListNonEmpty()
        {
            var definition = ScriptableObject.CreateInstance<BuildingDefinition>();
            var unitDefinition = ScriptableObject.CreateInstance<UnitDefinition>();
            var so = new SerializedObject(definition);
            var listProperty = so.FindProperty("producibleUnits");
            listProperty.InsertArrayElementAtIndex(0);
            listProperty.GetArrayElementAtIndex(0).objectReferenceValue = unitDefinition;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsTrue(definition.CanProduceUnits);
            Assert.AreEqual(1, definition.ProducibleUnits.Count);

            Object.DestroyImmediate(unitDefinition);
        }
    }
}
