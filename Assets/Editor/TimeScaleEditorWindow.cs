using UnityEditor;
using UnityEngine;

public class TimeScaleEditorWindow : EditorWindow
{
    [MenuItem("Window/TimeScale Controller")]
    public static void ShowWindow()
    {
        GetWindow<TimeScaleEditorWindow>("TimeScale");
    }

    private void OnGUI()
    {
        GUILayout.Label("Time Scale Controller", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        // Slider for time scale
        Time.timeScale = EditorGUILayout.Slider("Time Scale", Time.timeScale, 0f, 1f);
        
        GUILayout.Space(10);
        
        // Display current value
        GUILayout.Label($"Current: {Time.timeScale:F2}", EditorStyles.miniLabel);
        
        GUILayout.Space(10);
        
        // Quick buttons
        GUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Pause (0)", GUILayout.Height(30)))
        {
            Time.timeScale = 0f;
        }
        
        if (GUILayout.Button("0.5x", GUILayout.Height(30)))
        {
            Time.timeScale = 0.5f;
        }
        
        if (GUILayout.Button("Normal (1)", GUILayout.Height(30)))
        {
            Time.timeScale = 1f;
        }
        
        GUILayout.EndHorizontal();
    }
}
