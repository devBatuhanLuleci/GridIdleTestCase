#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace BoardGameTestCase.Core.ScriptableObjects
{
    [CustomPropertyDrawer(typeof(SortingLayerAttribute))]
    public class SortingLayerPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var sortingLayers = SortingLayer.layers;
            string[] layerNames = new string[sortingLayers.Length];
            int[] layerIds = new int[sortingLayers.Length];
            
            for (int i = 0; i < sortingLayers.Length; i++)
            {
                layerNames[i] = sortingLayers[i].name;
                layerIds[i] = sortingLayers[i].id;
            }
            
            string currentValue = property.stringValue;
            int currentIndex = 0;
            
            for (int i = 0; i < layerNames.Length; i++)
            {
                if (layerNames[i] == currentValue)
                {
                    currentIndex = i;
                    break;
                }
            }
            
            EditorGUI.BeginProperty(position, label, property);
            int newIndex = EditorGUI.Popup(position, label.text, currentIndex, layerNames);
            EditorGUI.EndProperty();
            
            if (newIndex != currentIndex && newIndex >= 0 && newIndex < layerNames.Length)
            {
                property.stringValue = layerNames[newIndex];
            }
        }
    }
}
#endif

