using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace CaseGame.Core
{
    /// <summary>
    /// Inspector-friendly reference to a Scene asset: shows an asset-picker field in the
    /// Editor (via the <c>SceneReferenceDrawer</c> property drawer) instead of a typo-prone
    /// raw string, while only ever serializing the scene name — the only part available (or
    /// needed) at runtime in a Player build, where <c>SceneAsset</c> doesn't exist.
    /// </summary>
    [Serializable]
    public class SceneReference : ISerializationCallbackReceiver
    {
#if UNITY_EDITOR
        [SerializeField] private SceneAsset sceneAsset;
#endif
        [SerializeField] private string sceneName;

        public string SceneName => sceneName;

        public bool IsSet => !string.IsNullOrEmpty(sceneName);

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
#if UNITY_EDITOR
            sceneName = sceneAsset != null ? sceneAsset.name : string.Empty;
#endif
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
        }
    }
}
