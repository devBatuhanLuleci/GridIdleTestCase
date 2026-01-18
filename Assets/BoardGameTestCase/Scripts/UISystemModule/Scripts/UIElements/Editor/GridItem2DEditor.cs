using UnityEngine;
using UnityEditor;

namespace BoardGameTestCase.UI.Elements.Editor
{
    [CustomEditor(typeof(GridItem2D))]
    public class GridItem2DEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var gridItem = (GridItem2D)target;

            GUILayout.Space(10);
            GUILayout.Label("Debug Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Play Reload Complete Animation"))
            {
                if (Application.isPlaying)
                {
                    gridItem.PlayReloadCompleteAnimation();
                }
                else
                {
                    Debug.LogWarning("Animation preview only available in Play Mode.");
                }
            }
        }
    }
}
