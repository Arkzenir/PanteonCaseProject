using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CaseGame.UI.Settings
{
    /// <summary>
    /// Settings screen controller: populates the resolution dropdown from the display's
    /// distinct resolutions (via <see cref="ResolutionOptionsBuilder"/>) and applies the chosen
    /// resolution and fullscreen state on demand.
    /// </summary>
    public class SettingsController : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown resolutionDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Button applyButton;

        private List<ResolutionOption> _options;

        private void OnEnable()
        {
            _options = ResolutionOptionsBuilder.BuildDistinctOptions(Screen.resolutions);
            PopulateDropdown();
            fullscreenToggle.isOn = Screen.fullScreen;

            applyButton.onClick.AddListener(OnApplyClicked);
        }

        private void OnDisable()
        {
            applyButton.onClick.RemoveListener(OnApplyClicked);
        }

        private void PopulateDropdown()
        {
            resolutionDropdown.ClearOptions();

            var labels = new List<string>(_options.Count);
            foreach (var option in _options)
            {
                labels.Add(option.Label);
            }

            resolutionDropdown.AddOptions(labels);

            var currentIndex = ResolutionOptionsBuilder.FindClosestIndex(_options, Screen.width, Screen.height);
            if (currentIndex >= 0)
            {
                resolutionDropdown.value = currentIndex;
                resolutionDropdown.RefreshShownValue();
            }
        }

        private void OnApplyClicked()
        {
            if (_options == null || _options.Count == 0 || resolutionDropdown.value >= _options.Count)
            {
                return;
            }

            var chosen = _options[resolutionDropdown.value];
            Screen.SetResolution(chosen.Width, chosen.Height, fullscreenToggle.isOn);
        }
    }
}
