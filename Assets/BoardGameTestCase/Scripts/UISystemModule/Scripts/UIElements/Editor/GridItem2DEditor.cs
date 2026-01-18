using UnityEngine;
using UnityEditor;
using UISystemModule.UIElements;

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
                if (Application.isPlaying) gridItem.PlayReloadCompleteAnimation();
                else Debug.LogWarning("Animation preview only available in Play Mode.");
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Play Placement Animation"))
            {
                if (Application.isPlaying) gridItem.PlayPlacementAnimation();
                else Debug.LogWarning("Animation preview only available in Play Mode.");
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Play Return Animation"))
            {
                if (Application.isPlaying) gridItem.ReturnToOriginalPosition();
                else Debug.LogWarning("Animation preview only available in Play Mode.");
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Play Fail Animation"))
            {
                if (Application.isPlaying) gridItem.PlayFailAnimation();
                else Debug.LogWarning("Animation preview only available in Play Mode.");
            }

            GUILayout.Space(5);
            if (GUILayout.Button("Play Discard Animation (Test)"))
            {
                if (Application.isPlaying) gridItem.PlayDiscardAnimationTest();
                else Debug.LogWarning("Animation preview only available in Play Mode.");
            }

            GUILayout.Space(10);
            GUILayout.Label("Reload Control", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Start Reload"))
            {
                if (Application.isPlaying) gridItem.StartReloadAnimation();
                else Debug.LogWarning("Animation preview only available in Play Mode.");
            }
            if (GUILayout.Button("Stop Reload"))
            {
                if (Application.isPlaying) gridItem.StopReloadAnimation();
                else Debug.LogWarning("Animation preview only available in Play Mode.");
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
