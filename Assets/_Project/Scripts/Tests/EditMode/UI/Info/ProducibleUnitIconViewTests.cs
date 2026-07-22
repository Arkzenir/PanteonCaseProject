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
        private Button _button;
        private ProducibleUnitIconView _view;
        private UnitDefinition _definition;
        private Soldier _prefab;
        private Sprite _sprite;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Icon", typeof(RectTransform));
            _icon = new GameObject("Image", typeof(RectTransform)).AddComponent<Image>();
            _icon.transform.SetParent(_root.transform);
            _nameText = new GameObject("Name", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            _nameText.transform.SetParent(_root.transform);
            _button = _root.AddComponent<Button>();

            _view = _root.AddComponent<ProducibleUnitIconView>();
            var so = new SerializedObject(_view);
            so.FindProperty("iconImage").objectReferenceValue = _icon;
            so.FindProperty("nameText").objectReferenceValue = _nameText;
            so.FindProperty("produceButton").objectReferenceValue = _button;
            so.ApplyModifiedPropertiesWithoutUndo();

            _sprite = Sprite.Create(new Texture2D(1, 1), new Rect(0, 0, 1, 1), Vector2.zero);
            _definition = ScriptableObject.CreateInstance<UnitDefinition>();
            var defSo = new SerializedObject(_definition);
            defSo.FindProperty("entityName").stringValue = "Soldier 1";
            defSo.FindProperty("sprite").objectReferenceValue = _sprite;
            defSo.ApplyModifiedPropertiesWithoutUndo();

            _prefab = new GameObject("SoldierPrefab").AddComponent<Soldier>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_definition);
            Object.DestroyImmediate(_sprite);
            Object.DestroyImmediate(_prefab.gameObject);
        }

        private UnitCatalogEntry MakeEntry() => new UnitCatalogEntry(_definition, _prefab);

        [Test]
        public void Bind_SetsIconAndNameFromDefinition()
        {
            _view.Bind(MakeEntry(), Vector3.zero);

            Assert.AreSame(_sprite, _icon.sprite);
            Assert.AreEqual("Soldier 1", _nameText.text);
        }

        [Test]
        public void Bind_NoNameTextAssigned_DoesNotThrow()
        {
            var so = new SerializedObject(_view);
            so.FindProperty("nameText").objectReferenceValue = null;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.DoesNotThrow(() => _view.Bind(MakeEntry(), Vector3.zero));
            Assert.AreSame(_sprite, _icon.sprite);
        }

        [Test]
        public void RequestProduce_RaisesChannelWithBoundEntryAndSpawnPosition()
        {
            var channel = ScriptableObject.CreateInstance<UnitProductionRequestEventChannel>();
            var so = new SerializedObject(_view);
            so.FindProperty("produceRequestedChannel").objectReferenceValue = channel;
            so.ApplyModifiedPropertiesWithoutUndo();

            var spawnPosition = new Vector3(3f, 4f, 0f);
            _view.Bind(MakeEntry(), spawnPosition);

            UnitProductionRequest? received = null;
            channel.Subscribe(r => received = r);

            _view.RequestProduce();

            Assert.IsTrue(received.HasValue);
            Assert.AreSame(_definition, received.Value.Entry.Definition);
            Assert.AreSame(_prefab, received.Value.Entry.Prefab);
            Assert.AreEqual(spawnPosition, received.Value.SpawnPosition);

            Object.DestroyImmediate(channel);
        }

        [Test]
        public void RequestProduce_NoChannelAssigned_DoesNotThrow()
        {
            _view.Bind(MakeEntry(), Vector3.zero);

            Assert.DoesNotThrow(() => _view.RequestProduce());
        }
    }
}
