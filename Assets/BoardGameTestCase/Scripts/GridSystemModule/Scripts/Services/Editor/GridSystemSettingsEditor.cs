#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using GridSystemModule.Services;

namespace GridSystemModule.Editor
{
    [CustomEditor(typeof(GridSystemSettings))]
    public class GridSystemSettingsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw default inspector properties
            DrawDefaultInspector();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Grid Generation", EditorStyles.boldLabel);

            // Add a big button to regenerate tiles prefab
            if (GUILayout.Button("Regenerate Tiles Prefab", GUILayout.Height(40)))
            {
                var settings = target as GridSystemSettings;
                if (settings != null)
                {
                    RegenerateTilesPrefab(settings);
                }
            }

            EditorGUILayout.HelpBox(
                "Click the button above to regenerate the Tiles prefab based on current Width and Height values.",
                MessageType.Info
            );
        }

        private void RegenerateTilesPrefab(GridSystemSettings settings)
        {
            // Create a temporary parent to hold generated tiles
            var tempRoot = new GameObject("TilesParent_Temp");
            var tempRootTransform = tempRoot.transform;
            tempRootTransform.position = Vector3.zero;
            tempRootTransform.rotation = Quaternion.identity;
            tempRootTransform.localScale = Vector3.one;

            // Build configuration using current settings
            var config = new GridConfiguration(settings.Width, settings.Height, settings.NormalGrassTilePrefab, null, tempRootTransform, settings);
            var service = new GridService(config);
            service.GenerateGrid();

            // Determine target path
            string targetPath = settings.TilesPrefab != null 
                ? AssetDatabase.GetAssetPath(settings.TilesPrefab) 
                : settings.TilesPrefabPath;
            
            if (string.IsNullOrEmpty(targetPath))
            {
                // Fallback to default path if none set
                targetPath = "Assets/BoardGameTestCase/Prefabs/GAMEPLAY/Tiles/Tiles.prefab";
            }

            // Ensure directory exists
            var dir = System.IO.Path.GetDirectoryName(targetPath);
            if (!System.IO.Directory.Exists(dir))
            {
                System.IO.Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            // Save or overwrite prefab
            var created = PrefabUtility.SaveAsPrefabAsset(tempRoot, targetPath, out bool success);
            if (success)
            {
                // Update the settings reference
                SerializedObject serializedSettings = new SerializedObject(settings);
                SerializedProperty tilesPrefabProp = serializedSettings.FindProperty("_tilesPrefab");
                if (tilesPrefabProp != null)
                {
                    tilesPrefabProp.objectReferenceValue = created;
                    serializedSettings.ApplyModifiedProperties();
                }

                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"✓ Tiles prefab regenerated at: {targetPath} (Width={settings.Width}, Height={settings.Height})");
            }
            else
            {
                Debug.LogError($"✗ Failed to regenerate tiles prefab at: {targetPath}");
            }

            // Clean up temporary object from the scene
            if (Application.isPlaying)
            {
                Object.Destroy(tempRoot);
            }
            else
            {
                Object.DestroyImmediate(tempRoot);
            }
        }
    }
}
#endif
