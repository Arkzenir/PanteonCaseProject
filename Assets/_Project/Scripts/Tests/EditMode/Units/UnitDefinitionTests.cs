using CaseGame.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Units
{
    public class UnitDefinitionTests
    {
        private static void InvokeOnValidate(UnitDefinition definition)
        {
            // OnValidate only runs automatically via the Editor; invoke it directly here.
            typeof(UnitDefinition)
                .GetMethod("OnValidate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(definition, null);
        }

        [Test]
        public void OnValidate_ClampsAttackDamageToAtLeastZero()
        {
            var definition = ScriptableObject.CreateInstance<UnitDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("attackDamage").intValue = -5;
            so.ApplyModifiedPropertiesWithoutUndo();

            InvokeOnValidate(definition);

            Assert.AreEqual(0, definition.AttackDamage);
        }

        [Test]
        public void OnValidate_ClampsMoveSpeedToAtLeastMinimum()
        {
            var definition = ScriptableObject.CreateInstance<UnitDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("moveSpeed").floatValue = -1f;
            so.ApplyModifiedPropertiesWithoutUndo();

            InvokeOnValidate(definition);

            Assert.Greater(definition.MoveSpeed, 0f);
        }
    }
}
