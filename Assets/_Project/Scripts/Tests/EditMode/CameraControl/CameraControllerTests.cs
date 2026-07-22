using CaseGame.CameraControl;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.CameraControl
{
    public class CameraControllerTests
    {
        private GameObject _cameraGo;
        private Camera _camera;
        private CameraController _controller;

        [SetUp]
        public void SetUp()
        {
            _cameraGo = new GameObject("Camera");
            _camera = _cameraGo.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 10f;
            _cameraGo.transform.position = new Vector3(5f, 5f, -10f);

            _controller = _cameraGo.AddComponent<CameraController>();
            var so = new SerializedObject(_controller);
            so.FindProperty("targetCamera").objectReferenceValue = _camera;
            so.FindProperty("zoomSpeed").floatValue = 2f;
            so.FindProperty("minOrthographicSize").floatValue = 4f;
            so.FindProperty("maxOrthographicSize").floatValue = 16f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cameraGo);
        }

        [Test]
        public void Pan_MovesCameraOppositeDragDirection()
        {
            // orthographicSize 10 over a 1000px-tall screen => 0.02 world units per pixel.
            _controller.Pan(new Vector2(100f, 50f), 1000f);

            var position = _cameraGo.transform.position;
            Assert.AreEqual(5f - 2f, position.x, 0.0001f);
            Assert.AreEqual(5f - 1f, position.y, 0.0001f);
        }

        [Test]
        public void Pan_DoesNotChangeZ()
        {
            _controller.Pan(new Vector2(100f, 50f), 1000f);

            Assert.AreEqual(-10f, _cameraGo.transform.position.z, 0.0001f);
        }

        [Test]
        public void Pan_ZeroScreenHeight_DoesNotThrowAndDoesNotMove()
        {
            Assert.DoesNotThrow(() => _controller.Pan(new Vector2(100f, 50f), 0f));

            Assert.AreEqual(new Vector3(5f, 5f, -10f), _cameraGo.transform.position);
        }

        [Test]
        public void Zoom_PositiveScroll_DecreasesOrthographicSize()
        {
            _controller.Zoom(1f);

            Assert.AreEqual(8f, _camera.orthographicSize, 0.0001f);
        }

        [Test]
        public void Zoom_NegativeScroll_IncreasesOrthographicSize()
        {
            _controller.Zoom(-1f);

            Assert.AreEqual(12f, _camera.orthographicSize, 0.0001f);
        }

        [Test]
        public void Zoom_BelowMin_ClampsToMin()
        {
            _controller.Zoom(10f);

            Assert.AreEqual(4f, _camera.orthographicSize, 0.0001f);
        }

        [Test]
        public void Zoom_AboveMax_ClampsToMax()
        {
            _controller.Zoom(-10f);

            Assert.AreEqual(16f, _camera.orthographicSize, 0.0001f);
        }
    }
}
