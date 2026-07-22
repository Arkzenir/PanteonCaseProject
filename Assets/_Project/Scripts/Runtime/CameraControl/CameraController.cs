using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace CaseGame.CameraControl
{
    /// <summary>
    /// Controller: middle-mouse-drag pans the camera, scroll wheel zooms it (human-requested
    /// quality-of-life, not a brief requirement). Both are skipped while the pointer is over UI
    /// (the Production Menu/Info Panel) — same `IsPointerOverGameObject` guard Placement/
    /// Selection already use (decisions log #47) — so scrolling the Production Menu's own list
    /// doesn't also zoom the camera underneath it.
    ///
    /// <see cref="Pan"/>/<see cref="Zoom"/> take explicit inputs and are callable directly,
    /// independent of <see cref="Update"/>'s device reading — the same "extract the testable
    /// decision, keep the MonoBehaviour thin" pattern used by every other controller here.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField] private float zoomSpeed = 2f;
        [SerializeField] private float minOrthographicSize = 4f;
        [SerializeField] private float maxOrthographicSize = 16f;

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
        }

        /// <summary>Positive <paramref name="scrollDelta"/> (scroll up/forward) zooms in (shrinks orthographic size); clamped to [minOrthographicSize, maxOrthographicSize].</summary>
        public void Zoom(float scrollDelta)
        {
            var newSize = targetCamera.orthographicSize - scrollDelta * zoomSpeed;
            targetCamera.orthographicSize = Mathf.Clamp(newSize, minOrthographicSize, maxOrthographicSize);
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
                Zoom(scroll);
            }
        }
    }
}
