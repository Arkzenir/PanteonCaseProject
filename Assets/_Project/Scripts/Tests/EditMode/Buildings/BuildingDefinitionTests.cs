using CaseGame.Buildings;
using CaseGame.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Buildings
{
    public class BuildingDefinitionTests
    {
        private static BuildingDefinition CreateDefinition()
        {
            return ScriptableObject.CreateInstance<BuildingDefinition>();
        }

        [Test]
        public void CanProduceUnits_FalseWhenProducibleListEmpty()
        {
            var definition = CreateDefinition();

            Assert.IsFalse(definition.CanProduceUnits);
            Assert.IsEmpty(definition.ProducibleUnits);
        }

        [Test]
        public void CanProduceUnits_TrueWhenProducibleListNonEmpty()
        {
            var definition = CreateDefinition();
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

        [Test]
        public void OnValidate_ClampsFootprintAndMaxHealthToAtLeastOne()
        {
            var definition = CreateDefinition();
            var so = new SerializedObject(definition);
            so.FindProperty("footprint").vector2IntValue = new Vector2Int(0, -3);
            so.FindProperty("maxHealth").intValue = -10;
            so.ApplyModifiedPropertiesWithoutUndo();

            // OnValidate only runs automatically via the Editor; invoke it directly here.
            typeof(BuildingDefinition)
                .GetMethod("OnValidate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(definition, null);

            Assert.AreEqual(new Vector2Int(1, 1), definition.Footprint);
            Assert.AreEqual(1, definition.MaxHealth);
        }
    }
}
