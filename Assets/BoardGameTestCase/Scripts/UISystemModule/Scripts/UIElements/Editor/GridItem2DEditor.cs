using UnityEditor;
using UnityEngine;
using UISystemModule.UIElements;

namespace UISystemModule.UIElements.Editor
{
    [CustomEditor(typeof(GridItem2D))]
    public class GridItem2DEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GridItem2D item = (GridItem2D)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation Testing", EditorStyles.boldLabel);

            if (GUILayout.Button("Test Drag Start (Punch)"))
            {
                item.TestDragStart();
            }

            if (GUILayout.Button("Test Placement Animation"))
            {
                item.TestDrop();
            }

            if (GUILayout.Button("Test Return Animation"))
            {
                item.TestReturn();
            }
        }
    }
}
