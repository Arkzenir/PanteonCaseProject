using UnityEngine;

namespace CaseGame.Placement
{
    /// <summary>
    /// View: toggles a building instance between a desaturated "ghost" silhouette (tinted
    /// green/red by placement validity, via <c>Art/Shaders/SpriteGrayscaleGhost.shader</c>)
    /// and its real sprite — on the same pooled instance, so there's one object with one
    /// lifecycle instead of a separate temporary preview object plus the real building.
    /// </summary>
    public class BuildingGhostView : MonoBehaviour
    {
        private static readonly Color ValidColor = new Color(0.4f, 1f, 0.4f, 0.65f);
        private static readonly Color InvalidColor = new Color(1f, 0.35f, 0.35f, 0.65f);

        [SerializeField] private GameObject visuals;
        [SerializeField] private GameObject visualsGrayscale;
        [SerializeField] private SpriteRenderer grayscaleRenderer;
        [SerializeField] private GameObject hitbox;

        /// <summary>Switches to the grayscale ghost silhouette and hides the real sprite/hitbox.</summary>
        public void ShowGhost()
        {
            visuals.SetActive(false);
            visualsGrayscale.SetActive(true);

            if (hitbox != null)
            {
                hitbox.SetActive(false);
            }
        }

        /// <summary>Tints the ghost silhouette green (valid) or red (invalid).</summary>
        public void SetValid(bool isValid)
        {
            grayscaleRenderer.color = isValid ? ValidColor : InvalidColor;
        }

        /// <summary>Switches to the real sprite/hitbox and hides the ghost silhouette.</summary>
        public void Commit()
        {
            visualsGrayscale.SetActive(false);
            visuals.SetActive(true);

            if (hitbox != null)
            {
                hitbox.SetActive(true);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Debug: Show Valid Ghost")]
        private void DebugShowValidGhost()
        {
            ShowGhost();
            SetValid(true);
        }

        [ContextMenu("Debug: Show Invalid Ghost")]
        private void DebugShowInvalidGhost()
        {
            ShowGhost();
            SetValid(false);
        }

        [ContextMenu("Debug: Commit")]
        private void DebugCommit()
        {
            Commit();
        }
#endif
    }
}
