using UnityEngine;
using UnityEditor;
using DebugModule.Utils;

namespace DebugModule.Editor
{
    /// <summary>
    /// Editor window for configuring DebugLogger settings
    /// </summary>
    public class DebugLoggerSettingsWindow : EditorWindow
    {
        private bool _enableLogging;
        private bool _enablePlacementLogs;
        private bool _enableDragLogs;
        private bool _enableTileLogs;
        
        private Vector2 _scrollPosition;
        
        [MenuItem("Tools/Debug Logger/Settings", false, 1)]
        public static void ShowWindow()
        {
            DebugLoggerSettingsWindow window = GetWindow<DebugLoggerSettingsWindow>("Debug Logger Settings");
            window.minSize = new Vector2(300, 200);
            window.maxSize = new Vector2(400, 300);
            window.Show();
        }
        
        private void OnEnable()
        {
            LoadSettings();
        }
        
        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Debug Logger Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            // Main logging toggle
            EditorGUILayout.BeginHorizontal();
            _enableLogging = EditorGUILayout.Toggle("Enable All Logging", _enableLogging);
            if (GUI.changed)
            {
                DebugLogger.EnableLogging = _enableLogging;
                EditorUtility.SetDirty(this);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Category toggles
            EditorGUI.BeginDisabledGroup(!_enableLogging);
            
            EditorGUILayout.LabelField("Log Categories:", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);
            
            // Placement logs
            EditorGUILayout.BeginHorizontal();
            _enablePlacementLogs = EditorGUILayout.Toggle("Placement Logs", _enablePlacementLogs);
            if (GUI.changed)
            {
                DebugLogger.EnablePlacementLogs = _enablePlacementLogs;
                EditorUtility.SetDirty(this);
            }
            EditorGUILayout.EndHorizontal();
            
            // Drag logs
            EditorGUILayout.BeginHorizontal();
            _enableDragLogs = EditorGUILayout.Toggle("Drag Logs", _enableDragLogs);
            if (GUI.changed)
            {
                DebugLogger.EnableDragLogs = _enableDragLogs;
                EditorUtility.SetDirty(this);
            }
            EditorGUILayout.EndHorizontal();
            
            // Tile logs
            EditorGUILayout.BeginHorizontal();
            _enableTileLogs = EditorGUILayout.Toggle("Tile Logs", _enableTileLogs);
            if (GUI.changed)
            {
                DebugLogger.EnableTileLogs = _enableTileLogs;
                EditorUtility.SetDirty(this);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.Space(15);
            
            // Action buttons
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Reset to Defaults"))
            {
                ResetToDefaults();
            }
            
            if (GUILayout.Button("Test All Logs"))
            {
                TestAllLogs();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Status information
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Current Status:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"All Logging: {(_enableLogging ? "Enabled" : "Disabled")}");
            EditorGUILayout.LabelField($"Placement: {(_enablePlacementLogs ? "Enabled" : "Disabled")}");
            EditorGUILayout.LabelField($"Drag: {(_enableDragLogs ? "Enabled" : "Disabled")}");
            EditorGUILayout.LabelField($"Tile: {(_enableTileLogs ? "Enabled" : "Disabled")}");
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        
        private void LoadSettings()
        {
            _enableLogging = DebugLogger.EnableLogging;
            _enablePlacementLogs = DebugLogger.EnablePlacementLogs;
            _enableDragLogs = DebugLogger.EnableDragLogs;
            _enableTileLogs = DebugLogger.EnableTileLogs;
        }
        
        private void ResetToDefaults()
        {
            _enableLogging = true;
            _enablePlacementLogs = true;
            _enableDragLogs = true;
            _enableTileLogs = true;
            
            DebugLogger.EnableLogging = _enableLogging;
            DebugLogger.EnablePlacementLogs = _enablePlacementLogs;
            DebugLogger.EnableDragLogs = _enableDragLogs;
            DebugLogger.EnableTileLogs = _enableTileLogs;
            
            EditorUtility.SetDirty(this);
            Repaint();
        }
        
        private void TestAllLogs()
        {
            DebugLogger.Log("Test general log message");
            DebugLogger.LogPlacement("Test placement log message");
            DebugLogger.LogDrag("Test drag log message");
            DebugLogger.LogTile("Test tile log message");
            DebugLogger.LogWarning("Test warning message");
            DebugLogger.LogError("Test error message");
            DebugLogger.LogCoordinates("Test", Vector3.zero, Vector2Int.zero, Vector2.zero);
            DebugLogger.LogPlacementValidation("Test", true, "Test validation");
        }
    }
}
