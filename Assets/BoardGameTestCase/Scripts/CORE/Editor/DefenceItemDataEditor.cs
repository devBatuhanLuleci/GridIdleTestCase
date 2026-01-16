using UnityEngine;
using UnityEditor;
using BoardGameTestCase.Core.ScriptableObjects;

namespace BoardGameTestCase.Core.Editor
{
    [CustomEditor(typeof(DefenceItemData))]
    public class DefenceItemDataEditor : UnityEditor.Editor
    {
        private const float PreviewPadding = 20f;
        private const float PreviewHeight = 260f;
        private static readonly Color Border = new Color(0.2f, 0.2f, 0.2f, 1f);
        private static readonly Color Footprint = new Color(0.1f, 0.7f, 1f, 0.35f);
        private static readonly Color FootprintBorder = new Color(0.1f, 0.6f, 1f, 0.9f);

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var data = (DefenceItemData)target;
            DrawPreview(data);
        }

        private void DrawPreview(DefenceItemData data)
        {
            if (data == null) return;
            var sprite = data.Sprite;
            var gridSize = data.GridSize;
            if (gridSize.x <= 0 || gridSize.y <= 0) return;

            Object settingsObj = data.GridPreviewSettingsObject;

            GetGridSettingsValues(settingsObj, out int gridWidth, out int gridHeight, out Vector2 cellSize, out Vector2 cellSpacing, out Sprite normalSprite, out Sprite darkSprite, out Color normalColor, out Color darkColor, gridSize);

            Rect previewRect = EditorGUILayout.GetControlRect(GUILayout.Height(PreviewHeight));
            EditorGUI.DrawRect(previewRect, new Color(0.12f, 0.12f, 0.12f, 1f));

            // Border
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.y, previewRect.width, 1), Border);
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.y + previewRect.height - 1, previewRect.width, 1), Border);
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.y, 1, previewRect.height), Border);
            EditorGUI.DrawRect(new Rect(previewRect.x + previewRect.width - 1, previewRect.y, 1, previewRect.height), Border);

            float totalWidth = gridWidth * (cellSize.x + cellSpacing.x) - cellSpacing.x;
            float totalHeight = gridHeight * (cellSize.y + cellSpacing.y) - cellSpacing.y;

            float scaleX = (previewRect.width - PreviewPadding) / Mathf.Max(totalWidth, 0.0001f);
            float scaleY = (previewRect.height - PreviewPadding) / Mathf.Max(totalHeight, 0.0001f);
            float scale = Mathf.Min(scaleX, scaleY);

            Vector2 centerOffset = new Vector2(previewRect.width / 2f, previewRect.height / 2f);
            Vector2 gridCenter = new Vector2(totalWidth / 2f, totalHeight / 2f) * scale;

            // Draw tiles
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    float posX = x * (cellSize.x + cellSpacing.x) * scale;
                    float posY = y * (cellSize.y + cellSpacing.y) * scale;

                    float rectX = previewRect.x + centerOffset.x - gridCenter.x + posX;
                    float rectY = previewRect.y + centerOffset.y - gridCenter.y + posY;
                    float rectWidth = cellSize.x * scale;
                    float rectHeight = cellSize.y * scale;

                    bool isNormal = (x + y) % 2 == 0;
                    Sprite tileSprite = isNormal ? normalSprite : darkSprite;
                    Color tileColor = isNormal ? normalColor : darkColor;

                    if (tileSprite != null)
                    {
                        Color prev = GUI.color;
                        GUI.color = tileColor;
                        GUI.DrawTextureWithTexCoords(
                            new Rect(rectX, rectY, rectWidth, rectHeight),
                            tileSprite.texture,
                            GetSpriteTexCoords(tileSprite),
                            true
                        );
                        GUI.color = prev;
                    }
                    else
                    {
                        EditorGUI.DrawRect(new Rect(rectX, rectY, rectWidth, rectHeight), tileColor);
                    }

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

            // Footprint offset to center within grid
            float itemWidthUnits = gridSize.x * (cellSize.x + cellSpacing.x) - cellSpacing.x;
            float itemHeightUnits = gridSize.y * (cellSize.y + cellSpacing.y) - cellSpacing.y;
            float offsetUnitsX = (totalWidth - itemWidthUnits) * 0.5f;
            float offsetUnitsY = (totalHeight - itemHeightUnits) * 0.5f;

            // Draw footprint highlight
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    float posX = offsetUnitsX + x * (cellSize.x + cellSpacing.x);
                    float posY = offsetUnitsY + y * (cellSize.y + cellSpacing.y);

                    float rectX = previewRect.x + centerOffset.x - gridCenter.x + posX * scale;
                    float rectY = previewRect.y + centerOffset.y - gridCenter.y + posY * scale;
                    float rectWidth = cellSize.x * scale;
                    float rectHeight = cellSize.y * scale;

                    EditorGUI.DrawRect(new Rect(rectX, rectY, rectWidth, rectHeight), Footprint);
                    Handles.color = FootprintBorder;
                    Handles.DrawPolyLine(
                        new Vector3(rectX, rectY, 0),
                        new Vector3(rectX + rectWidth, rectY, 0),
                        new Vector3(rectX + rectWidth, rectY + rectHeight, 0),
                        new Vector3(rectX, rectY + rectHeight, 0),
                        new Vector3(rectX, rectY, 0)
                    );
                }
            }

            // Draw sprite centered on footprint
            if (sprite != null && sprite.texture != null)
            {
                Rect spriteRect = sprite.textureRect;
                Rect uv = new Rect(spriteRect.x / sprite.texture.width, spriteRect.y / sprite.texture.height, spriteRect.width / sprite.texture.width, spriteRect.height / sprite.texture.height);

                float drawWidthUnits = itemWidthUnits;
                float drawHeightUnits = itemHeightUnits;
                float spriteAspect = spriteRect.width / spriteRect.height;
                float targetAspect = drawWidthUnits / drawHeightUnits;
                if (spriteAspect > targetAspect)
                {
                    drawHeightUnits = drawWidthUnits / spriteAspect;
                }
                else
                {
                    drawWidthUnits = drawHeightUnits * spriteAspect;
                }

                float drawWidth = drawWidthUnits * scale;
                float drawHeight = drawHeightUnits * scale;
                float baseX = previewRect.x + centerOffset.x - gridCenter.x + offsetUnitsX * scale + (itemWidthUnits * scale - drawWidth) * 0.5f;
                float baseY = previewRect.y + centerOffset.y - gridCenter.y + offsetUnitsY * scale + (itemHeightUnits * scale - drawHeight) * 0.5f;
                Rect drawRect = new Rect(baseX, baseY, drawWidth, drawHeight);
                GUI.DrawTextureWithTexCoords(drawRect, sprite.texture, uv, true);
            }
        }

        private static Sprite GetPrefabSprite(Object tileObj)
        {
            if (tileObj == null) return null;
            var go = tileObj as GameObject;
            if (go != null)
            {
                var sr = go.GetComponent<SpriteRenderer>();
                return sr != null ? sr.sprite : null;
            }
            var sr2 = tileObj as SpriteRenderer;
            return sr2 != null ? sr2.sprite : null;
        }

        private static Color GetPrefabColor(Object tileObj)
        {
            if (tileObj == null) return new Color(0.9f, 0.9f, 0.9f, 1f);
            var go = tileObj as GameObject;
            if (go != null)
            {
                var sr = go.GetComponent<SpriteRenderer>();
                return sr != null ? sr.color : new Color(0.9f, 0.9f, 0.9f, 1f);
            }
            var sr2 = tileObj as SpriteRenderer;
            return sr2 != null ? sr2.color : new Color(0.9f, 0.9f, 0.9f, 1f);
        }

        private static Rect GetSpriteTexCoords(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null) return new Rect(0, 0, 1, 1);
            Rect r = sprite.textureRect;
            return new Rect(r.x / sprite.texture.width, r.y / sprite.texture.height, r.width / sprite.texture.width, r.height / sprite.texture.height);
        }

        private static void GetGridSettingsValues(Object settingsObj, out int gridWidth, out int gridHeight, out Vector2 cellSize, out Vector2 cellSpacing, out Sprite normalSprite, out Sprite darkSprite, out Color normalColor, out Color darkColor, Vector2Int fallbackGridSize)
        {
            // defaults
            gridWidth = Mathf.Max(fallbackGridSize.x + 2, 4);
            gridHeight = Mathf.Max(fallbackGridSize.y + 2, 4);
            cellSize = Vector2.one;
            cellSpacing = Vector2.zero;
            normalSprite = null;
            darkSprite = null;
            normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            darkColor = new Color(0.82f, 0.82f, 0.82f, 1f);

            if (settingsObj == null) return;

            SerializedObject so = new SerializedObject(settingsObj);
            gridWidth = GetInt(so, "_width", gridWidth);
            gridHeight = GetInt(so, "_height", gridHeight);
            cellSize = GetVector2(so, "_cellSize", cellSize);
            cellSpacing = GetVector2(so, "_cellSpacing", cellSpacing);

            Object normalTile = GetObject(so, "_normalGrassTilePrefab");
            Object darkTile = GetObject(so, "_darkGrassTilePrefab");
            normalSprite = GetPrefabSprite(normalTile);
            darkSprite = GetPrefabSprite(darkTile);
            normalColor = GetPrefabColor(normalTile);
            darkColor = GetPrefabColor(darkTile);
        }

        private static int GetInt(SerializedObject so, string name, int fallback)
        {
            var p = so.FindProperty(name);
            return p != null ? p.intValue : fallback;
        }

        private static Vector2 GetVector2(SerializedObject so, string name, Vector2 fallback)
        {
            var p = so.FindProperty(name);
            return p != null ? p.vector2Value : fallback;
        }

        private static Object GetObject(SerializedObject so, string name)
        {
            var p = so.FindProperty(name);
            return p != null ? p.objectReferenceValue : null;
        }
    }
}
