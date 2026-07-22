using CaseGame.Units;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CaseGame.UI.Info
{
    /// <summary>
    /// View: one small icon representing a unit a selected building can produce (BRIEF.md
    /// requirement 5) — and, since production is "free, instant, unlimited" for units too
    /// (requirement 4) and the Info Panel's producible-unit row is the *only* place units are
    /// ever listed (this is GI-6's "production sub-menu"), clicking it is how a unit actually
    /// gets produced. Raises <see cref="UnitProductionRequestEventChannel"/> with the entry it's
    /// bound to plus the spawn position <see cref="InfoPanelController"/> gave it — it doesn't
    /// know or care who's listening (<c>UnitProductionController</c> does, decoupled via the
    /// channel).
    /// </summary>
    public class ProducibleUnitIconView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Button produceButton;
        [SerializeField] private UnitProductionRequestEventChannel produceRequestedChannel;

        private UnitCatalogEntry _entry;
        private Vector3 _spawnPosition;

        private void OnEnable()
        {
            produceButton.onClick.AddListener(RequestProduce);
        }

        private void OnDisable()
        {
            produceButton.onClick.RemoveListener(RequestProduce);
        }

        public void Bind(UnitCatalogEntry entry, Vector3 spawnPosition)
        {
            _entry = entry;
            _spawnPosition = spawnPosition;
            iconImage.sprite = entry.Definition.Sprite;

            if (nameText != null)
            {
                nameText.text = entry.Definition.EntityName;
            }
        }

        public void RequestProduce()
        {
            if (produceRequestedChannel != null)
            {
                produceRequestedChannel.Raise(new UnitProductionRequest(_entry, _spawnPosition));
            }
        }
    }
}
