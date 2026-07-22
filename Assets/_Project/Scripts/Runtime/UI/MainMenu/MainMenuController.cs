using CaseGame.Core;
using UnityEngine;
using UnityEngine.UI;

namespace CaseGame.UI.MainMenu
{
    /// <summary>
    /// Main Menu navigation controller: routes Play/Settings/Back clicks to panel switching
    /// and the Gameplay scene transition. Deliberately thin — there is no tunable state here,
    /// just UI wiring and a call into the Singleton's scene-loading entry point.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject howToPlayPanel;
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button howToPlayButton;
        [SerializeField] private Button backButton;
        [SerializeField] private Button howToPlayBackButton;
        [SerializeField] private SceneReference gameplayScene;

        private void OnEnable()
        {
            playButton.onClick.AddListener(OnPlayClicked);
            settingsButton.onClick.AddListener(OnSettingsClicked);
            howToPlayButton.onClick.AddListener(OnHowToPlayClicked);
            backButton.onClick.AddListener(OnBackClicked);
            howToPlayBackButton.onClick.AddListener(OnBackClicked);
            ShowMainPanel();
        }

        private void OnDisable()
        {
            playButton.onClick.RemoveListener(OnPlayClicked);
            settingsButton.onClick.RemoveListener(OnSettingsClicked);
            howToPlayButton.onClick.RemoveListener(OnHowToPlayClicked);
            backButton.onClick.RemoveListener(OnBackClicked);
            howToPlayBackButton.onClick.RemoveListener(OnBackClicked);
        }

        private void OnPlayClicked()
        {
            if (gameplayScene != null && gameplayScene.IsSet && GameManager.Instance != null)
            {
                GameManager.Instance.LoadScene(gameplayScene.SceneName);
            }
        }

        private void OnSettingsClicked()
        {
            mainPanel.SetActive(false);
            settingsPanel.SetActive(true);
        }

        private void OnHowToPlayClicked()
        {
            mainPanel.SetActive(false);
            howToPlayPanel.SetActive(true);
        }

        private void OnBackClicked()
        {
            ShowMainPanel();
        }

        private void ShowMainPanel()
        {
            mainPanel.SetActive(true);
            settingsPanel.SetActive(false);
            howToPlayPanel.SetActive(false);
        }
    }
}
