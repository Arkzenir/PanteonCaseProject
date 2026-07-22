using CaseGame.Buildings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Buildings
{
    public class BuildingCatalogTests
    {
        [Test]
        public void Entries_ReflectsSerializedList()
        {
            var definition = ScriptableObject.CreateInstance<BuildingDefinition>();
            var prefab = new GameObject("Prefab").AddComponent<TestBuilding>();
            var catalog = ScriptableObject.CreateInstance<BuildingCatalog>();

            var so = new SerializedObject(catalog);
            var entries = so.FindProperty("entries");
            entries.InsertArrayElementAtIndex(0);
            var entryProperty = entries.GetArrayElementAtIndex(0);
            entryProperty.FindPropertyRelative("definition").objectReferenceValue = definition;
            entryProperty.FindPropertyRelative("prefab").objectReferenceValue = prefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(1, catalog.Entries.Count);
            Assert.AreSame(definition, catalog.Entries[0].Definition);
            Assert.AreSame(prefab, catalog.Entries[0].Prefab);

            Object.DestroyImmediate(prefab.gameObject);
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(catalog);
        }

        private class TestBuilding : BuildingBase
        {
        }
    }
}
