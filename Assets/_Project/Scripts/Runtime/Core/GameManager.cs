using UnityEngine;
using UnityEngine.SceneManagement;

namespace CaseGame.Core
{
    /// <summary>
    /// The project's single cross-scene coordinator. Persists across scene loads via
    /// <c>DontDestroyOnLoad</c> and owns scene-transition lifecycle. Exposed to consumers
    /// through <see cref="IGameManager"/> rather than this concrete type. A second instance
    /// in the scene self-destructs, keeping the first alive.
    /// Expected to live under a <c>--- SYSTEMS ---</c> organizer object rather than at scene
    /// root, so persistence targets the whole root object (<c>transform.root</c>) —
    /// <c>DontDestroyOnLoad</c> only accepts root GameObjects.
    /// </summary>
    public class GameManager : MonoBehaviour, IGameManager
    {
        private static GameManager _instance;

        public static IGameManager Instance => _instance;

        [Tooltip("Scene to load on startup. Leave unassigned to load nothing automatically.")]
        [SerializeField] private SceneReference firstScene;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                DestroyDuplicate();
                return;
            }

            _instance = this;

            if (Application.isPlaying)
            {
                DontDestroyOnLoad(transform.root.gameObject);
            }
        }

        private void Start()
        {
            if (firstScene != null && firstScene.IsSet)
            {
                LoadScene(firstScene.SceneName);
            }
        }

        public void LoadScene(string sceneName)
        {
            SceneManager.LoadScene(sceneName);
        }

        private void DestroyDuplicate()
        {
            // Destroy() is deferred to end-of-frame and is invalid outside Play Mode, so Edit
            // Mode (including EditMode tests) needs the immediate variant instead.
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
