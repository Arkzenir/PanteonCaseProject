using CaseGame.Entities;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Entities
{
    public class GameEntityDefinitionTests
    {
        private class TestDefinition : GameEntityDefinition
        {
        }

        private static TestDefinition CreateDefinition()
        {
            return ScriptableObject.CreateInstance<TestDefinition>();
        }

        private static void InvokeOnValidate(GameEntityDefinition definition)
        {
            // OnValidate only runs automatically via the Editor; invoke it directly here.
            typeof(GameEntityDefinition)
                .GetMethod("OnValidate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(definition, null);
        }

        [Test]
        public void OnValidate_ClampsFootprintToAtLeastOneByOne()
        {
            var definition = CreateDefinition();
            var so = new SerializedObject(definition);
            so.FindProperty("footprint").vector2IntValue = new Vector2Int(0, -3);
            so.ApplyModifiedPropertiesWithoutUndo();

            InvokeOnValidate(definition);

            Assert.AreEqual(new Vector2Int(1, 1), definition.Footprint);
        }

        [Test]
        public void OnValidate_ClampsMaxHealthToAtLeastOne()
        {
            var definition = CreateDefinition();
            var so = new SerializedObject(definition);
            so.FindProperty("maxHealth").intValue = -10;
            so.ApplyModifiedPropertiesWithoutUndo();

            InvokeOnValidate(definition);

            Assert.AreEqual(1, definition.MaxHealth);
        }
    }
}
