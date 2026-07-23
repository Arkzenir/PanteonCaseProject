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
            _camera.aspect = 1f; // deterministic for bounds-clamping math, independent of the test runner's screen/window size
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

        [Test]
        public void ClampToBounds_PositionWellInsideBounds_ReturnsUnchanged()
        {
            var result = CameraController.ClampToBounds(new Vector2(5f, 5f), orthographicSize: 2f, aspect: 1f, new Vector2(0f, 0f), new Vector2(20f, 20f));

            Assert.AreEqual(new Vector2(5f, 5f), result);
        }

        [Test]
        public void ClampToBounds_PastMaxX_ClampsSoViewportEdgeMeetsBoundsEdge()
        {
            // aspect 1, orthographicSize 2 => half-width 2; bounds max.x=20 => clamp position.x to 18.
            var result = CameraController.ClampToBounds(new Vector2(25f, 5f), orthographicSize: 2f, aspect: 1f, new Vector2(0f, 0f), new Vector2(20f, 20f));

            Assert.AreEqual(18f, result.x, 0.0001f);
        }

        [Test]
        public void ClampToBounds_BeforeMinY_ClampsSoViewportEdgeMeetsBoundsEdge()
        {
            var result = CameraController.ClampToBounds(new Vector2(5f, -5f), orthographicSize: 2f, aspect: 1f, new Vector2(0f, 0f), new Vector2(20f, 20f));

            Assert.AreEqual(2f, result.y, 0.0001f);
        }

        [Test]
        public void ClampToBounds_ViewportWiderThanBounds_CentersOnThatAxisInstead()
        {
            // orthographicSize 20 over a 10-unit-wide bounds region => can't fit, so center on (min+max)/2 = 5.
            var result = CameraController.ClampToBounds(new Vector2(2f, 5f), orthographicSize: 20f, aspect: 1f, new Vector2(0f, 0f), new Vector2(10f, 100f));

            Assert.AreEqual(5f, result.x, 0.0001f);
        }

        [Test]
        public void SetBounds_ImmediatelyClampsCurrentPosition()
        {
            _controller.SetBounds(new Vector2(-1f, -1f), new Vector2(1f, 1f));

            var position = _cameraGo.transform.position;
            Assert.LessOrEqual(position.x, 1f);
            Assert.LessOrEqual(position.y, 1f);
        }

        [Test]
        public void Pan_PastSetBounds_StopsAtTheBoundsEdge()
        {
            _controller.SetBounds(new Vector2(-1000f, -1000f), new Vector2(1000f, 1000f));

            _controller.Pan(new Vector2(-1000000f, 0f), 1000f); // huge drag, would fly far past bounds unclamped

            // orthographicSize 10, aspect 1 => half-width 10; bounds max.x=1000 => clamps to 990.
            Assert.AreEqual(990f, _cameraGo.transform.position.x, 0.0001f);
        }

        [Test]
        public void NotchesFromRawScrollDelta_OneNotch_ReturnsOne()
        {
            Assert.AreEqual(1f, CameraController.NotchesFromRawScrollDelta(120f), 0.0001f);
        }

        [Test]
        public void NotchesFromRawScrollDelta_OneNotchBackward_ReturnsMinusOne()
        {
            Assert.AreEqual(-1f, CameraController.NotchesFromRawScrollDelta(-120f), 0.0001f);
        }

        [Test]
        public void NotchesFromRawScrollDelta_HalfNotch_ReturnsHalf()
        {
            Assert.AreEqual(0.5f, CameraController.NotchesFromRawScrollDelta(60f), 0.0001f);
        }

        [Test]
        public void Zoom_OneNotchWorthOfNotches_DoesNotJumpStraightToMinOrMax()
        {
            // zoomSpeed 0.2, min 2, max 20 — one raw 120-unit notch must NOT swing the
            // full range in a single tick.
            var so = new SerializedObject(_controller);
            so.FindProperty("zoomSpeed").floatValue = 0.2f;
            so.FindProperty("minOrthographicSize").floatValue = 2f;
            so.FindProperty("maxOrthographicSize").floatValue = 20f;
            so.ApplyModifiedPropertiesWithoutUndo();
            _camera.orthographicSize = 10f;

            _controller.Zoom(CameraController.NotchesFromRawScrollDelta(120f));

            Assert.AreEqual(9.8f, _camera.orthographicSize, 0.0001f);
        }

        [Test]
        public void Zoom_PastSetBounds_ReclampsPosition()
        {
            _controller.SetBounds(new Vector2(0f, 0f), new Vector2(10f, 10f));
            _cameraGo.transform.position = new Vector3(5f, 5f, -10f);

            _controller.Zoom(-10f); // zooms out to maxOrthographicSize (16), now wider than the 10-unit bounds

            Assert.AreEqual(5f, _cameraGo.transform.position.x, 0.0001f);
            Assert.AreEqual(5f, _cameraGo.transform.position.y, 0.0001f);
        }
    }
}
