using System.Collections.Generic;
using CaseGame.Buildings;
using CaseGame.Units;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CaseGame.UI.Info
{
    /// <summary>
    /// Controller: the Information Panel (BRIEF.md requirement 5). Subscribes to
    /// <see cref="SelectedBuildingEventChannel"/> (raised by <c>SelectionController</c>, Report
    /// 015) and shows the selected building's image/name, plus one small icon per producible
    /// unit if it has any (requirement 6: Power Plant has none, so its row is simply empty — no
    /// per-building-type branch here, same generic-iteration discipline as the Production Menu).
    /// Hides entirely when nothing (or a soldier) is selected.
    ///
    /// The producible-unit list is always small (≤3 today) and only rebuilds on the rare
    /// "selection changed" event, not every frame — plain Instantiate/Destroy per change is
    /// simpler and just as correct as pooling here; the brief ties Object Pooling specifically
    /// to the Production Menu's *infinite* scroll view, not this fixed-size list (see
    /// ARCHITECTURE.md decisions log).
    /// </summary>
    public class InfoPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Image buildingIcon;
        [SerializeField] private TMP_Text buildingNameText;
        [SerializeField] private RectTransform producibleUnitsContainer;
        [SerializeField] private ProducibleUnitIconView producibleUnitIconPrefab;
        [SerializeField] private SelectedBuildingEventChannel selectedBuildingChannel;

        private readonly List<ProducibleUnitIconView> _producibleUnitIcons = new List<ProducibleUnitIconView>();

        public IReadOnlyList<ProducibleUnitIconView> ProducibleUnitIcons => _producibleUnitIcons;

        private void OnEnable()
        {
            if (selectedBuildingChannel != null)
            {
                selectedBuildingChannel.Subscribe(SetSelectedBuilding);
            }

            SetSelectedBuilding(null);
        }

        private void OnDisable()
        {
            if (selectedBuildingChannel != null)
            {
                selectedBuildingChannel.Unsubscribe(SetSelectedBuilding);
            }
        }

        /// <summary>Updates the panel for the given building, or hides it for null. Public and independent of the channel callback so it's directly testable, mirroring the rest of the project's "extract the testable decision" pattern.</summary>
        public void SetSelectedBuilding(BuildingBase building)
        {
            panelRoot.SetActive(building != null);
            ClearProducibleUnitIcons();

            if (building == null)
            {
                return;
            }

            buildingIcon.sprite = building.Definition.Sprite;
            buildingNameText.text = building.Definition.EntityName;

            foreach (var unitDefinition in building.Definition.ProducibleUnits)
            {
                SpawnProducibleUnitIcon(unitDefinition);
            }
        }

        private void SpawnProducibleUnitIcon(UnitDefinition unitDefinition)
        {
            var icon = Instantiate(producibleUnitIconPrefab, producibleUnitsContainer);
            icon.Bind(unitDefinition);
            _producibleUnitIcons.Add(icon);
        }

        private void ClearProducibleUnitIcons()
        {
            foreach (var icon in _producibleUnitIcons)
            {
                if (icon != null)
                {
                    DestroyView(icon.gameObject);
                }
            }

            _producibleUnitIcons.Clear();
        }

        private static void DestroyView(GameObject go)
        {
            // Destroy() is deferred to end-of-frame and is invalid outside Play Mode (same
            // reasoning as GameManager.DestroyDuplicate) — Edit Mode/EditMode tests need the
            // immediate variant so a cleared icon is actually gone before the next assertion.
            if (Application.isPlaying)
            {
                Destroy(go);
            }
            else
            {
                DestroyImmediate(go);
            }
        }
    }
}
