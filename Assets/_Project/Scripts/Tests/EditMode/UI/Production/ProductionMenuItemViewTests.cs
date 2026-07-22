using CaseGame.Buildings;
using CaseGame.UI.Production;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CaseGame.Tests.EditMode.UI.Production
{
    public class ProductionMenuItemViewTests
    {
        private class TestBuilding : BuildingBase
        {
        }

        private GameObject _root;
        private Image _icon;
        private TextMeshProUGUI _nameText;
        private ProductionMenuItemView _view;
        private BuildingDefinition _definition;
        private BuildingBase _prefab;
        private Sprite _sprite;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Item", typeof(RectTransform));
            _icon = new GameObject("Icon", typeof(RectTransform)).AddComponent<Image>();
            _icon.transform.SetParent(_root.transform);
            _nameText = new GameObject("Name", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _nameText.transform.SetParent(_root.transform);
            var button = _root.AddComponent<Button>();

            _view = _root.AddComponent<ProductionMenuItemView>();
            var so = new SerializedObject(_view);
            so.FindProperty("iconImage").objectReferenceValue = _icon;
            so.FindProperty("nameText").objectReferenceValue = _nameText;
            so.FindProperty("produceButton").objectReferenceValue = button;
            so.ApplyModifiedPropertiesWithoutUndo();

            _sprite = Sprite.Create(new Texture2D(1, 1), new Rect(0, 0, 1, 1), Vector2.zero);
            _definition = ScriptableObject.CreateInstance<BuildingDefinition>();
            var defSo = new SerializedObject(_definition);
            defSo.FindProperty("entityName").stringValue = "Barracks";
            defSo.FindProperty("sprite").objectReferenceValue = _sprite;
            defSo.ApplyModifiedPropertiesWithoutUndo();

            _prefab = new GameObject("Prefab").AddComponent<TestBuilding>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_prefab.gameObject);
            Object.DestroyImmediate(_definition);
            Object.DestroyImmediate(_sprite);
        }

        [Test]
        public void Bind_SetsIconAndNameFromDefinition()
        {
            var entry = MakeEntry();

            _view.Bind(entry);

            Assert.AreSame(_sprite, _icon.sprite);
            Assert.AreEqual("Barracks", _nameText.text);
        }

        [Test]
        public void RequestProduce_RaisesChannelWithBoundEntry()
        {
            var channel = ScriptableObject.CreateInstance<BuildingCatalogEntryEventChannel>();
            var so = new SerializedObject(_view);
            so.FindProperty("produceRequestedChannel").objectReferenceValue = channel;
            so.ApplyModifiedPropertiesWithoutUndo();

            var entry = MakeEntry();
            _view.Bind(entry);

            BuildingCatalogEntry? received = null;
            channel.Subscribe(e => received = e);

            _view.RequestProduce();

            Assert.IsTrue(received.HasValue);
            Assert.AreSame(_definition, received.Value.Definition);
            Assert.AreSame(_prefab, received.Value.Prefab);

            Object.DestroyImmediate(channel);
        }

        [Test]
        public void RequestProduce_NoChannelAssigned_DoesNotThrow()
        {
            var entry = MakeEntry();
            _view.Bind(entry);

            Assert.DoesNotThrow(() => _view.RequestProduce());
        }

        private BuildingCatalogEntry MakeEntry()
        {
            return new BuildingCatalogEntry(_definition, _prefab);
        }
    }
}
