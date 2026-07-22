using CaseGame.UI.Info;
using CaseGame.Units;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CaseGame.Tests.EditMode.UI.Info
{
    public class ProducibleUnitIconViewTests
    {
        private GameObject _root;
        private Image _icon;
        private TextMeshProUGUI _nameText;
        private ProducibleUnitIconView _view;
        private UnitDefinition _definition;
        private Sprite _sprite;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Icon", typeof(RectTransform));
            _icon = new GameObject("Image", typeof(RectTransform)).AddComponent<Image>();
            _icon.transform.SetParent(_root.transform);
            _nameText = new GameObject("Name", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _nameText.transform.SetParent(_root.transform);

            _view = _root.AddComponent<ProducibleUnitIconView>();
            var so = new SerializedObject(_view);
            so.FindProperty("iconImage").objectReferenceValue = _icon;
            so.FindProperty("nameText").objectReferenceValue = _nameText;
            so.ApplyModifiedPropertiesWithoutUndo();

            _sprite = Sprite.Create(new Texture2D(1, 1), new Rect(0, 0, 1, 1), Vector2.zero);
            _definition = ScriptableObject.CreateInstance<UnitDefinition>();
            var defSo = new SerializedObject(_definition);
            defSo.FindProperty("entityName").stringValue = "Soldier 1";
            defSo.FindProperty("sprite").objectReferenceValue = _sprite;
            defSo.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_definition);
            Object.DestroyImmediate(_sprite);
        }

        [Test]
        public void Bind_SetsIconAndNameFromDefinition()
        {
            _view.Bind(_definition);

            Assert.AreSame(_sprite, _icon.sprite);
            Assert.AreEqual("Soldier 1", _nameText.text);
        }

        [Test]
        public void Bind_NoNameTextAssigned_DoesNotThrow()
        {
            var so = new SerializedObject(_view);
            so.FindProperty("nameText").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.DoesNotThrow(() => _view.Bind(_definition));
            Assert.AreSame(_sprite, _icon.sprite);
        }
    }
}
