using System.Collections.Generic;
using CaseGame.Buildings;
using CaseGame.UI.Production;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CaseGame.Tests.EditMode.UI.Production
{
    public class ProductionMenuControllerTests
    {
        private class TestBuilding : BuildingBase
        {
        }

        private ProductionMenuItemView _itemPrefab;
        private RectTransform _content;
        private ScrollRect _scrollRect;
        private ProductionMenuController _controller;
        private BuildingBase _buildingPrefab;
        private BuildingCatalog _catalog;
        private readonly List<BuildingDefinition> _definitions = new List<BuildingDefinition>();

        [SetUp]
        public void SetUp()
        {
            _itemPrefab = CreateItemPrefab();
            _buildingPrefab = new GameObject("BuildingPrefab").AddComponent<TestBuilding>();

            var scrollRoot = new GameObject("ScrollView", typeof(RectTransform));
            _scrollRect = scrollRoot.AddComponent<ScrollRect>();
            _content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            _content.SetParent(scrollRoot.transform);
            _scrollRect.content = _content;

            _controller = new GameObject("ProductionMenuController").AddComponent<ProductionMenuController>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var view in _controller.ActiveSlots)
            {
                if (view != null)
                {
                    Object.DestroyImmediate(view.gameObject);
                }
            }

            Object.DestroyImmediate(_controller.gameObject);
            Object.DestroyImmediate(_scrollRect.gameObject);
            Object.DestroyImmediate(_itemPrefab.gameObject);
            Object.DestroyImmediate(_buildingPrefab.gameObject);

            foreach (var definition in _definitions)
            {
                Object.DestroyImmediate(definition);
            }

            if (_catalog != null)
            {
                Object.DestroyImmediate(_catalog);
            }
        }

        [Test]
        public void Initialize_ItemCountBelowPoolSize_BuildsOneSlotPerItem()
        {
            Configure(CreateCatalog(3), poolSize: 8, itemHeight: 100f);

            _controller.Initialize();

            Assert.AreEqual(3, _controller.ActiveSlots.Count);
        }

        [Test]
        public void Initialize_ItemCountAbovePoolSize_CapsSlotsAtPoolSize()
        {
            Configure(CreateCatalog(10), poolSize: 4, itemHeight: 100f);

            _controller.Initialize();

            Assert.AreEqual(4, _controller.ActiveSlots.Count);
        }

        [Test]
        public void RefreshLayout_BindsSlotsToFirstItemsAndSizesContent()
        {
            Configure(CreateCatalog(5), poolSize: 3, itemHeight: 100f);
            _controller.Initialize();

            _controller.RefreshLayout();

            Assert.AreEqual("Building0", GetSlotName(0));
            Assert.AreEqual("Building1", GetSlotName(1));
            Assert.AreEqual("Building2", GetSlotName(2));
            Assert.AreEqual(500f, _content.sizeDelta.y);
        }

        [Test]
        public void ApplyScrollOffset_RebindsSlotsToScrolledWindow()
        {
            Configure(CreateCatalog(5), poolSize: 3, itemHeight: 100f);
            _controller.Initialize();
            _controller.RefreshLayout();

            _controller.ApplyScrollOffset(200f);

            Assert.AreEqual("Building2", GetSlotName(0));
            Assert.AreEqual("Building3", GetSlotName(1));
            Assert.AreEqual("Building4", GetSlotName(2));
        }

        private string GetSlotName(int slotIndex)
        {
            return _controller.ActiveSlots[slotIndex].transform.Find("Name").GetComponent<TextMeshProUGUI>().text;
        }

        private void Configure(BuildingCatalog catalog, int poolSize, float itemHeight)
        {
            var so = new SerializedObject(_controller);
            so.FindProperty("scrollRect").objectReferenceValue = _scrollRect;
            so.FindProperty("content").objectReferenceValue = _content;
            so.FindProperty("itemPrefab").objectReferenceValue = _itemPrefab;
            so.FindProperty("catalog").objectReferenceValue = catalog;
            so.FindProperty("itemHeight").floatValue = itemHeight;
            so.FindProperty("poolSize").intValue = poolSize;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private BuildingCatalog CreateCatalog(int count)
        {
            _catalog = ScriptableObject.CreateInstance<BuildingCatalog>();
            var so = new SerializedObject(_catalog);
            var entries = so.FindProperty("entries");
            for (var i = 0; i < count; i++)
            {
                var definition = ScriptableObject.CreateInstance<BuildingDefinition>();
                var defSo = new SerializedObject(definition);
                defSo.FindProperty("entityName").stringValue = $"Building{i}";
                defSo.ApplyModifiedPropertiesWithoutUndo();
                _definitions.Add(definition);

                entries.InsertArrayElementAtIndex(i);
                var entryProperty = entries.GetArrayElementAtIndex(i);
                entryProperty.FindPropertyRelative("definition").objectReferenceValue = definition;
                entryProperty.FindPropertyRelative("prefab").objectReferenceValue = _buildingPrefab;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return _catalog;
        }

        private static ProductionMenuItemView CreateItemPrefab()
        {
            var root = new GameObject("Item", typeof(RectTransform));
            var icon = new GameObject("Icon", typeof(RectTransform)).AddComponent<Image>();
            icon.transform.SetParent(root.transform);
            var nameText = new GameObject("Name", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            nameText.transform.SetParent(root.transform);
            var button = root.AddComponent<Button>();

            var view = root.AddComponent<ProductionMenuItemView>();
            var so = new SerializedObject(view);
            so.FindProperty("iconImage").objectReferenceValue = icon;
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("produceButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();

            return view;
        }
    }
}
