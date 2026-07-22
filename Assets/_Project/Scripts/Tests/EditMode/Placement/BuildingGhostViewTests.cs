using CaseGame.Placement;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Placement
{
    public class BuildingGhostViewTests
    {
        private GameObject _root;
        private GameObject _visuals;
        private GameObject _visualsGrayscale;
        private GameObject _hitbox;
        private BuildingGhostView _view;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Building");
            _visuals = new GameObject("Visuals");
            _visuals.transform.SetParent(_root.transform);
            _visualsGrayscale = new GameObject("VisualsGrayscale");
            _visualsGrayscale.transform.SetParent(_root.transform);
            var grayscaleRenderer = _visualsGrayscale.AddComponent<SpriteRenderer>();
            _hitbox = new GameObject("Hitbox");
            _hitbox.transform.SetParent(_root.transform);

            _view = _root.AddComponent<BuildingGhostView>();
            var so = new SerializedObject(_view);
            so.FindProperty("visuals").objectReferenceValue = _visuals;
            so.FindProperty("visualsGrayscale").objectReferenceValue = _visualsGrayscale;
            so.FindProperty("grayscaleRenderer").objectReferenceValue = grayscaleRenderer;
            so.FindProperty("hitbox").objectReferenceValue = _hitbox;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void ShowGhost_HidesVisuals_ShowsGrayscaleAndHitbox()
        {
            _visuals.SetActive(true);
            _visualsGrayscale.SetActive(false);
            _hitbox.SetActive(true);

            _view.ShowGhost();

            Assert.IsFalse(_visuals.activeSelf);
            Assert.IsTrue(_visualsGrayscale.activeSelf);
            Assert.IsFalse(_hitbox.activeSelf);
        }

        [Test]
        public void SetValid_True_TintsGrayscaleGreenish()
        {
            _view.SetValid(true);

            var color = _visualsGrayscale.GetComponent<SpriteRenderer>().color;
            Assert.Greater(color.g, color.r);
            Assert.Greater(color.g, color.b);
        }

        [Test]
        public void SetValid_False_TintsGrayscaleReddish()
        {
            _view.SetValid(false);

            var color = _visualsGrayscale.GetComponent<SpriteRenderer>().color;
            Assert.Greater(color.r, color.g);
            Assert.Greater(color.r, color.b);
        }

        [Test]
        public void Commit_ShowsVisualsAndHitbox_HidesGrayscale()
        {
            _view.ShowGhost();

            _view.Commit();

            Assert.IsTrue(_visuals.activeSelf);
            Assert.IsFalse(_visualsGrayscale.activeSelf);
            Assert.IsTrue(_hitbox.activeSelf);
        }
    }
}
