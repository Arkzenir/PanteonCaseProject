using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CaseGame.CameraControl
{
    /// <summary>
    /// Middle-mouse-drag pans the camera; scroll wheel zooms it. Both are skipped while the
    /// pointer is over UI (the Production Menu/Info Panel) — the same
    /// `IsPointerOverGameObject` guard Placement/Selection use — so scrolling the Production
    /// Menu's own list doesn't also zoom the camera underneath it.
    ///
    /// <see cref="Pan"/>/<see cref="Zoom"/> take explicit inputs and are callable directly,
    /// independent of <see cref="Update"/>'s device reading, keeping the MonoBehaviour thin
    /// and the actual pan/zoom math testable on its own.
    ///
    /// Pan/zoom are clamped to <see cref="SetBounds"/>'s rectangle (the environment's water
    /// backdrop extent) via the pure, testable <see cref="ClampToBounds"/>, so the camera can
    /// never show background past the water's own painted edge.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        /// <summary>Windows reports one physical mouse-wheel notch as ±120 on <c>Mouse.scroll</c> (the legacy WHEEL_DELTA convention), not a small ±1-ish value — dividing by this converts the raw Input System delta into "notches", so <see cref="zoomSpeed"/> means orthographic-size change per notch rather than per raw input unit. Without it, one physical notch (120 * zoomSpeed) could swing across the entire min/max range in a single tick.</summary>
        private const float RawScrollUnitsPerNotch = 120f;

        [SerializeField] private Camera targetCamera;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float minOrthographicSize = 4f;
        [SerializeField] private float maxOrthographicSize = 16f;

        private Vector2 _boundsMin = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        private Vector2 _boundsMax = new Vector2(float.PositiveInfinity, float.PositiveInfinity);

        /// <summary>Sets the world-space rectangle the camera's viewport must stay within — called once by <c>GameplayBootstrap</c> with the environment's water backdrop bounds. Unset (the default), pan/zoom are unclamped.</summary>
        public void SetBounds(Vector2 min, Vector2 max)
        {
            _boundsMin = min;
            _boundsMax = max;
            ApplyBoundsClamp();
        }

        /// <summary>Moves the camera opposite the given screen-space delta (the "grab and drag the world" convention), scaled so a drag tracks the cursor 1:1 in world space regardless of current zoom.</summary>
        public void Pan(Vector2 screenDelta, float screenHeight)
        {
            if (screenHeight <= 0f)
            {
                return;
            }

            var worldUnitsPerPixel = targetCamera.orthographicSize * 2f / screenHeight;
            var worldDelta = new Vector3(-screenDelta.x, -screenDelta.y, 0f) * worldUnitsPerPixel;
            targetCamera.transform.position += worldDelta;
            ApplyBoundsClamp();
        }

        /// <summary>Positive <paramref name="scrollDelta"/> (scroll up/forward) zooms in (shrinks orthographic size); clamped to [minOrthographicSize, maxOrthographicSize].</summary>
        public void Zoom(float scrollDelta)
        {
            var newSize = targetCamera.orthographicSize - scrollDelta * zoomSpeed;
            targetCamera.orthographicSize = Mathf.Clamp(newSize, minOrthographicSize, maxOrthographicSize);
            ApplyBoundsClamp();
        }

        /// <summary>Clamps a candidate camera center so its orthographic viewport (width <c>orthographicSize * 2 * aspect</c>, height <c>orthographicSize * 2</c>) never extends past <paramref name="boundsMin"/>/<paramref name="boundsMax"/>. An axis whose viewport is wider than the available bounds is centered instead of clamped — the area is too small to fill the view without showing past one side no matter where the camera sits. Pure so this is directly testable independent of a live <see cref="Camera"/>.</summary>
        public static Vector2 ClampToBounds(Vector2 position, float orthographicSize, float aspect, Vector2 boundsMin, Vector2 boundsMax)
        {
            return new Vector2(
                ClampAxis(position.x, boundsMin.x, boundsMax.x, orthographicSize * aspect),
                ClampAxis(position.y, boundsMin.y, boundsMax.y, orthographicSize));
        }

        private static float ClampAxis(float value, float min, float max, float halfExtent)
        {
            var low = min + halfExtent;
            var high = max - halfExtent;
            return low > high ? (min + max) * 0.5f : Mathf.Clamp(value, low, high);
        }

        /// <summary>Converts a raw Input System mouse-wheel delta into notches (see <see cref="RawScrollUnitsPerNotch"/>). Pure so the conversion is directly testable independent of a live <see cref="Mouse"/> device.</summary>
        public static float NotchesFromRawScrollDelta(float rawScrollDelta) => rawScrollDelta / RawScrollUnitsPerNotch;

        private void ApplyBoundsClamp()
        {
            var clamped = ClampToBounds(targetCamera.transform.position, targetCamera.orthographicSize, targetCamera.aspect, _boundsMin, _boundsMax);
            targetCamera.transform.position = new Vector3(clamped.x, clamped.y, targetCamera.transform.position.z);
        }

        private void Update()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (Mouse.current.middleButton.isPressed)
            {
                Pan(Mouse.current.delta.ReadValue(), Screen.height);
            }

            var scroll = Mouse.current.scroll.ReadValue().y;
            if (!Mathf.Approximately(scroll, 0f))
            {
                Zoom(NotchesFromRawScrollDelta(scroll));
            }
        }
    }
}
