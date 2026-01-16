#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using GridSystemModule.Services;
using GridSystemModule.Core.Models;

namespace GridSystemModule.Editor
{
    [CustomEditor(typeof(GridSystemSettings))]
    public class GridSystemSettingsEditor : UnityEditor.Editor
    {
        private bool _showGridPreview = true;
        private const float PREVIEW_HEIGHT = 300f;

        public override void OnInspectorGUI()
        {
            // Draw default inspector properties
            DrawDefaultInspector();

            EditorGUILayout.Space();
            
            // Grid Preview Section
            EditorGUILayout.LabelField("Grid Preview", EditorStyles.boldLabel);
            _showGridPreview = EditorGUILayout.Foldout(_showGridPreview, "Visual Grid Preview");
            
            if (_showGridPreview)
            {
                var settings = target as GridSystemSettings;
                if (settings != null)
                {
                    DrawGridPreview(settings);
                }
                EditorGUILayout.Space();
            }

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

        private void DrawGridPreview(GridSystemSettings settings)
        {
            Rect previewRect = EditorGUILayout.GetControlRect(GUILayout.Height(PREVIEW_HEIGHT));
            EditorGUI.DrawRect(previewRect, new Color(0.15f, 0.15f, 0.15f, 1f));
            
            // Draw border
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.y, previewRect.width, 1), Color.white);
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.y + previewRect.height - 1, previewRect.width, 1), Color.white);
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.y, 1, previewRect.height), Color.white);
            EditorGUI.DrawRect(new Rect(previewRect.x + previewRect.width - 1, previewRect.y, 1, previewRect.height), Color.white);

            // Calculate grid dimensions
            int width = settings.Width;
            int height = settings.Height;
            Vector2 cellSize = settings.CellSize;
            Vector2 cellSpacing = settings.CellSpacing;

            // Get actual sprites and colors from the tile prefabs
            Sprite normalSprite = GetPrefabSprite(settings.NormalGrassTilePrefab);
            Sprite darkSprite = GetPrefabSprite(settings.DarkGrassTilePrefab);
            Color normalColor = GetPrefabColor(settings.NormalGrassTilePrefab);
            Color darkColor = GetPrefabColor(settings.DarkGrassTilePrefab);

            // Calculate total grid size in world units
            float totalWidth = width * (cellSize.x + cellSpacing.x) - cellSpacing.x;
            float totalHeight = height * (cellSize.y + cellSpacing.y) - cellSpacing.y;

            // Calculate scaling to fit in preview rect
            float scaleX = (previewRect.width - 20) / (totalWidth > 0 ? totalWidth : 1);
            float scaleY = (previewRect.height - 20) / (totalHeight > 0 ? totalHeight : 1);
            float scale = Mathf.Min(scaleX, scaleY);

            Vector2 centerOffset = new Vector2(previewRect.width / 2, previewRect.height / 2);
            Vector2 gridCenter = new Vector2(totalWidth / 2, totalHeight / 2) * scale;

            // Draw grid
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float posX = x * (cellSize.x + cellSpacing.x) * scale;
                    float posY = y * (cellSize.y + cellSpacing.y) * scale;
                    
                    float rectX = previewRect.x + centerOffset.x - gridCenter.x + posX;
                    float rectY = previewRect.y + centerOffset.y - gridCenter.y + posY;
                    float rectWidth = cellSize.x * scale;
                    float rectHeight = cellSize.y * scale;

                    // Determine which sprite and color to use
                    bool isNormal = (x + y) % 2 == 0;
                    Sprite sprite = isNormal ? normalSprite : darkSprite;
                    Color tileColor = isNormal ? normalColor : darkColor;
                    
                    // Draw sprite or fallback to colored rectangle
                    if (sprite != null)
                    {
                        // Apply the exact tile color to the sprite
                        Color prevColor = GUI.color;
                        GUI.color = tileColor;
                        GUI.DrawTextureWithTexCoords(
                            new Rect(rectX, rectY, rectWidth, rectHeight),
                            sprite.texture,
                            GetSpriteTexCoords(sprite),
                            true
                        );
                        GUI.color = prevColor;
                    }
                    else
                    {
                        // Fallback: draw colored rectangle if no sprite
                        EditorGUI.DrawRect(new Rect(rectX, rectY, rectWidth, rectHeight), tileColor);
                    }
                    
                    // Draw tile border
                    Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                    Handles.DrawPolyLine(
                        new Vector3(rectX, rectY, 0),
                        new Vector3(rectX + rectWidth, rectY, 0),
                        new Vector3(rectX + rectWidth, rectY + rectHeight, 0),
                        new Vector3(rectX, rectY + rectHeight, 0),
                        new Vector3(rectX, rectY, 0)
                    );
                }
            }

            // Draw info text
            string infoText = $"Grid: {width}x{height} | Cell Size: {cellSize.x}x{cellSize.y} | Spacing: {cellSpacing.x}x{cellSpacing.y}";
            GUI.Label(new Rect(previewRect.x + 5, previewRect.y + 5, previewRect.width - 10, 20), infoText, EditorStyles.whiteLabel);
        }

        private Sprite GetPrefabSprite(BaseTile tilePrefab)
        {
            if (tilePrefab == null)
                return null;

            var spriteRenderer = tilePrefab.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                return spriteRenderer.sprite;
            }

            return null;
        }

        private Rect GetSpriteTexCoords(Sprite sprite)
        {
            if (sprite == null)
                return new Rect(0, 0, 1, 1);

            // Get the normalized texture coordinates for the sprite
            Rect rect = sprite.rect;
            Texture2D texture = sprite.texture;
            
            return new Rect(
                rect.x / texture.width,
                rect.y / texture.height,
                rect.width / texture.width,
                rect.height / texture.height
            );
        }

        private Color GetPrefabColor(BaseTile tilePrefab)
        {
            if (tilePrefab == null)
                return new Color(0.3f, 0.5f, 0.3f, 0.8f); // Default light green

            // Get SpriteRenderer from the variant tile prefab
            var spriteRenderer = tilePrefab.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                // Get the rendered color (color * material color)
                Color renderedColor = spriteRenderer.color;
                
                // If sharedMaterial exists, also apply material tint
                if (spriteRenderer.sharedMaterial != null)
                {
                    // Get color property from sharedMaterial
                    if (spriteRenderer.sharedMaterial.HasProperty("_Color"))
                    {
                        Color materialColor = spriteRenderer.sharedMaterial.GetColor("_Color");
                        renderedColor *= materialColor;
                    }
                }
                
                return renderedColor;
            }

            return new Color(0.3f, 0.5f, 0.3f, 0.8f); // Default light green
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
