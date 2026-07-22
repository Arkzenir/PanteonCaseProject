using CaseGame.Grid;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Grid
{
    public class GridDefinitionTests
    {
        private static GridDefinition CreateDefinition()
        {
            return ScriptableObject.CreateInstance<GridDefinition>();
        }

        [Test]
        public void OnValidate_ClampsLineThicknessToAtLeastMinimum()
        {
            var definition = CreateDefinition();
            var so = new SerializedObject(definition);
            so.FindProperty("lineThickness").floatValue = -1f;
            so.ApplyModifiedPropertiesWithoutUndo();

            InvokeOnValidate(definition);

            Assert.AreEqual(0.001f, definition.LineThickness, 0.0001f);
        }

        [Test]
        public void LineColor_DefaultsToTranslucentWhite()
        {
            var definition = CreateDefinition();

            Assert.AreEqual(new Color(1f, 1f, 1f, 0.35f), definition.LineColor);
        }

        [Test]
        public void OnValidate_ClampsTerrainMarginToAtLeastZero()
        {
            var definition = CreateDefinition();
            var so = new SerializedObject(definition);
            so.FindProperty("terrainMargin").floatValue = -5f;
            so.ApplyModifiedPropertiesWithoutUndo();

            InvokeOnValidate(definition);

            Assert.AreEqual(0f, definition.TerrainMargin, 0.0001f);
        }

        private static void InvokeOnValidate(GridDefinition definition)
        {
            typeof(GridDefinition)
                .GetMethod("OnValidate", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Invoke(definition, null);
        }
    }
}
