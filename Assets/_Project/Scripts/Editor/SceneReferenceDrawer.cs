using CaseGame.Core;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Editor
{
    /// <summary>
    /// Draws <see cref="SceneReference"/> fields as a single SceneAsset picker instead of
    /// Unity's default layout, which would otherwise expose the internal sceneAsset/sceneName
    /// fields as separate rows.
    /// </summary>
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var sceneAssetProperty = property.FindPropertyRelative("sceneAsset");

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.PropertyField(position, sceneAssetProperty, label);
            EditorGUI.EndProperty();
        }
    }
}
