using System.Collections.Generic;
using CaseGame.Buildings;
using CaseGame.UI.Info;
using CaseGame.Units;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CaseGame.Tests.EditMode.UI.Info
{
    public class InfoPanelControllerTests
    {
        private class TestBuilding : BuildingBase
        {
        }

        private GameObject _panelRoot;
        private Image _buildingIcon;
        private TextMeshProUGUI _buildingNameText;
        private RectTransform _producibleUnitsContainer;
        private ProducibleUnitIconView _iconPrefab;
        private Soldier _soldierPrefab;
        private InfoPanelController _controller;
        private readonly List<Object> _toDestroy = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _panelRoot = new GameObject("Panel");
            _buildingIcon = new GameObject("BuildingIcon", typeof(RectTransform)).AddComponent<Image>();
            _buildingIcon.transform.SetParent(_panelRoot.transform);
            _buildingNameText = new GameObject("BuildingName", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _buildingNameText.transform.SetParent(_panelRoot.transform);
            _producibleUnitsContainer = new GameObject("ProducibleUnits", typeof(RectTransform)).GetComponent<RectTransform>();
            _producibleUnitsContainer.SetParent(_panelRoot.transform);

            _soldierPrefab = new GameObject("SoldierPrefab").AddComponent<Soldier>();
            _iconPrefab = CreateIconPrefab();

            _controller = new GameObject("InfoPanelController").AddComponent<InfoPanelController>();
            var so = new SerializedObject(_controller);
            so.FindProperty("panelRoot").objectReferenceValue = _panelRoot;
            so.FindProperty("buildingIcon").objectReferenceValue = _buildingIcon;
            so.FindProperty("buildingNameText").objectReferenceValue = _buildingNameText;
            so.FindProperty("producibleUnitsContainer").objectReferenceValue = _producibleUnitsContainer;
            so.FindProperty("producibleUnitIconPrefab").objectReferenceValue = _iconPrefab;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var icon in _controller.ProducibleUnitIcons)
            {
                if (icon != null)
                {
                    Object.DestroyImmediate(icon.gameObject);
                }
            }

            Object.DestroyImmediate(_controller.gameObject);
            Object.DestroyImmediate(_panelRoot);
            Object.DestroyImmediate(_iconPrefab.gameObject);
            Object.DestroyImmediate(_soldierPrefab.gameObject);

            foreach (var asset in _toDestroy)
            {
                if (asset != null)
                {
                    Object.DestroyImmediate(asset);
                }
            }
        }

        [Test]
        public void SetSelectedBuilding_NonNull_ActivatesPanelAndSetsIconAndName()
        {
            var building = CreateBuilding("Barracks");

            _controller.SetSelectedBuilding(building);

            Assert.IsTrue(_panelRoot.activeSelf);
            Assert.AreEqual("Barracks", _buildingNameText.text);
        }

        [Test]
        public void SetSelectedBuilding_Null_DeactivatesPanel()
        {
            var building = CreateBuilding("Barracks");
            _controller.SetSelectedBuilding(building);

            _controller.SetSelectedBuilding(null);

            Assert.IsFalse(_panelRoot.activeSelf);
        }

        [Test]
        public void SetSelectedBuilding_NoProducibleUnits_SpawnsNoIcons()
        {
            var building = CreateBuilding("Power Plant");

            _controller.SetSelectedBuilding(building);

            Assert.AreEqual(0, _controller.ProducibleUnitIcons.Count);
        }

        [Test]
        public void SetSelectedBuilding_WithProducibleUnits_SpawnsOneIconPerUnit()
        {
            var soldier1 = CreateUnitCatalogEntry("Soldier 1");
            var soldier2 = CreateUnitCatalogEntry("Soldier 2");
            var building = CreateBuilding("Barracks", soldier1, soldier2);

            _controller.SetSelectedBuilding(building);

            Assert.AreEqual(2, _controller.ProducibleUnitIcons.Count);
            CollectionAssert.AreEquivalent(
                new[] { "Soldier 1", "Soldier 2" },
                new[] { GetIconName(0), GetIconName(1) });
        }

        [Test]
        public void SetSelectedBuilding_ChangingBuilding_ReplacesOldIcons()
        {
            var soldier1 = CreateUnitCatalogEntry("Soldier 1");
            var barracks = CreateBuilding("Barracks", soldier1);
            var powerPlant = CreateBuilding("Power Plant");
            _controller.SetSelectedBuilding(barracks);

            _controller.SetSelectedBuilding(powerPlant);

            Assert.AreEqual(0, _controller.ProducibleUnitIcons.Count);
            Assert.AreEqual(0, _producibleUnitsContainer.childCount);
        }

        private string GetIconName(int index)
        {
            return _controller.ProducibleUnitIcons[index].transform.Find("Name").GetComponent<TextMeshProUGUI>().text;
        }

        private TestBuilding CreateBuilding(string name, params UnitCatalogEntry[] producibleUnits)
        {
            var definition = ScriptableObject.CreateInstance<BuildingDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("entityName").stringValue = name;
            var listProperty = so.FindProperty("producibleUnits");
            for (var i = 0; i < producibleUnits.Length; i++)
            {
                listProperty.InsertArrayElementAtIndex(i);
                var entryProperty = listProperty.GetArrayElementAtIndex(i);
                entryProperty.FindPropertyRelative("definition").objectReferenceValue = producibleUnits[i].Definition;
                entryProperty.FindPropertyRelative("prefab").objectReferenceValue = producibleUnits[i].Prefab;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            _toDestroy.Add(definition);

            var building = new GameObject("Building").AddComponent<TestBuilding>();
            building.Initialize(definition);
            _toDestroy.Add(building.gameObject);
            return building;
        }

        private UnitCatalogEntry CreateUnitCatalogEntry(string name)
        {
            var definition = ScriptableObject.CreateInstance<UnitDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("entityName").stringValue = name;
            so.ApplyModifiedPropertiesWithoutUndo();
            _toDestroy.Add(definition);
            return new UnitCatalogEntry(definition, _soldierPrefab);
        }

        private static ProducibleUnitIconView CreateIconPrefab()
        {
            var root = new GameObject("ProducibleUnitIcon", typeof(RectTransform));
            var icon = new GameObject("Image", typeof(RectTransform)).AddComponent<Image>();
            icon.transform.SetParent(root.transform, false);
            var nameText = new GameObject("Name", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            nameText.transform.SetParent(root.transform, false);
            var button = root.AddComponent<Button>();

            var view = root.AddComponent<ProducibleUnitIconView>();
            var so = new SerializedObject(view);
            so.FindProperty("iconImage").objectReferenceValue = icon;
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("produceButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }
    }
}
