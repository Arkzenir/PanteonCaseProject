using CaseGame.Buildings;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CaseGame.UI.Production
{
    /// <summary>
    /// View: one pooled, recyclable Production Menu row. <see cref="Bind"/> re-labels it for
    /// whichever <see cref="BuildingCatalogEntry"/> it currently represents (called repeatedly
    /// as the list scrolls — this instance is never destroyed/recreated per entry). A click
    /// raises <see cref="produceRequestedChannel"/> with the currently bound entry; it doesn't
    /// know or care who's listening (Placement does, decoupled via the event channel).
    /// </summary>
    public class ProductionMenuItemView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Button produceButton;
        [SerializeField] private BuildingCatalogEntryEventChannel produceRequestedChannel;

        private BuildingCatalogEntry _entry;

        private void OnEnable()
        {
            produceButton.onClick.AddListener(RequestProduce);
        }

        private void OnDisable()
        {
            produceButton.onClick.RemoveListener(RequestProduce);
        }

        public void Bind(BuildingCatalogEntry entry)
        {
            _entry = entry;
            iconImage.sprite = entry.Definition.Sprite;
            nameText.text = entry.Definition.EntityName;
        }

        public void RequestProduce()
        {
            if (produceRequestedChannel != null)
            {
                produceRequestedChannel.Raise(_entry);
            }
        }
    }
}
