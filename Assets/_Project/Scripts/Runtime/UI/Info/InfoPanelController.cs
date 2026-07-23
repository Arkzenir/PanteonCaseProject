using System.Collections.Generic;
using CaseGame.Buildings;
using CaseGame.Entities;
using CaseGame.Units;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CaseGame.UI.Info
{
    /// <summary>
    /// Controller for the Information Panel. Subscribes to <see cref="SelectedBuildingEventChannel"/>
    /// and shows the selected building's image/name, plus one small icon per producible unit if
    /// it has any (a building with none, like the Power Plant, just gets an empty row — no
    /// per-building-type branch here, same generic iteration as the Production Menu). Hides
    /// entirely when nothing (or a soldier) is selected.
    ///
    /// The producible-unit list is always small and only rebuilds on the rare "selection changed"
    /// event, not every frame — plain Instantiate/Destroy per change is simpler and just as
    /// correct as pooling here; pooling is reserved for the Production Menu's infinite scroll view.
    ///
    /// The panel also has a "Remove Building" button. It raises
    /// <see cref="BuildingRemovalRequestedEventChannel"/> rather than calling
    /// <see cref="GameEntityBase.ApplyDamage"/> directly — manual removal is a deliberately
    /// separate trigger from combat death, not a reuse of the Health pipeline.
    /// </summary>
    public class InfoPanelController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Image buildingIcon;
        [SerializeField] private TMP_Text buildingNameText;
        [SerializeField] private RectTransform producibleUnitsContainer;
        [SerializeField] private ProducibleUnitIconView producibleUnitIconPrefab;
        [SerializeField] private SelectedBuildingEventChannel selectedBuildingChannel;
        [SerializeField] private Button removeBuildingButton;
        [SerializeField] private BuildingRemovalRequestedEventChannel removalRequestedChannel;

        private readonly List<ProducibleUnitIconView> _producibleUnitIcons = new List<ProducibleUnitIconView>();
        private BuildingBase _currentBuilding;

        public IReadOnlyList<ProducibleUnitIconView> ProducibleUnitIcons => _producibleUnitIcons;

        private void OnEnable()
        {
            if (selectedBuildingChannel != null)
            {
                selectedBuildingChannel.Subscribe(SetSelectedBuilding);
            }

            if (removeBuildingButton != null)
            {
                removeBuildingButton.onClick.AddListener(RequestRemoveBuilding);
            }

            SetSelectedBuilding(null);
        }

        private void OnDisable()
        {
            if (selectedBuildingChannel != null)
            {
                selectedBuildingChannel.Unsubscribe(SetSelectedBuilding);
            }

            if (removeBuildingButton != null)
            {
                removeBuildingButton.onClick.RemoveListener(RequestRemoveBuilding);
            }
        }

        /// <summary>Updates the panel for the given building, or hides it for null. Public and independent of the channel callback so it's directly testable.</summary>
        public void SetSelectedBuilding(BuildingBase building)
        {
            _currentBuilding = building;
            panelRoot.SetActive(building != null);
            ClearProducibleUnitIcons();

            if (building == null)
            {
                return;
            }

            buildingIcon.sprite = building.Definition.Sprite;
            buildingNameText.text = building.Definition.EntityName;

            var spawnPosition = building.SpawnPosition;
            foreach (var entry in building.Definition.ProducibleUnits)
            {
                SpawnProducibleUnitIcon(entry, spawnPosition);
            }

            // The producible-units grid's height and the panel's stacked layout both depend on
            // the icons just spawned above. Unity's layout pass is deferred to later in the
            // frame, so without forcing it here the panel would render one stale frame before
            // catching up. Forcing it immediately keeps every selection change correct on the
            // first frame it's shown.
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)panelRoot.transform);
        }

        /// <summary>Raises a removal request for the currently-displayed building and immediately hides the panel rather than waiting for a round trip through Selection.</summary>
        public void RequestRemoveBuilding()
        {
            if (_currentBuilding == null)
            {
                return;
            }

            if (removalRequestedChannel != null)
            {
                removalRequestedChannel.Raise(_currentBuilding);
            }

            SetSelectedBuilding(null);
        }

        private void SpawnProducibleUnitIcon(UnitCatalogEntry entry, Vector3 spawnPosition)
        {
            var icon = Instantiate(producibleUnitIconPrefab, producibleUnitsContainer);
            icon.Bind(entry, spawnPosition);
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
            // DestroyImmediate, not the deferred Destroy(): a deferred destroy leaves the cleared
            // icon as a real child of producibleUnitsContainer until end of frame, which the
            // forced layout rebuild above would still measure — leaving the panel briefly too
            // tall when switching to a building with fewer producible units. Safe here since this
            // only runs over our own local list, never mid-iteration over Unity's Transform children.
            DestroyImmediate(go);
        }
    }
}
