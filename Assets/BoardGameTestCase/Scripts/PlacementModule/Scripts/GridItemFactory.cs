using UnityEngine;
using BoardGameTestCase.Core.ScriptableObjects;
using GridSystemModule.Core.Interfaces;
using BoardGameTestCase.Core.Common;
using UISystemModule.UIElements;

namespace PlacementModule
{
    public class GridItemFactory : MonoBehaviour, IGridItemFactory
    {
        [SerializeField] private GameObject _gridItem2DPrefab;

        private void Awake()
        {
            ServiceLocator.Instance.Register<IGridItemFactory>(this);
            ServiceLocator.Instance.Register<GridItemFactory>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Instance?.Unregister<IGridItemFactory>();
            ServiceLocator.Instance?.Unregister<GridItemFactory>();
        }

        public GameObject CreateGridItem(ScriptableObject itemData, Vector3 position, bool isGhost = false, float ghostAlpha = 1.0f)
        {
            var defenceItemData = itemData as DefenceItemData;
            if (defenceItemData == null) return null;
            return CreateGridItem(defenceItemData, position, isGhost, ghostAlpha);
        }
        
        private GameObject CreateGridItem(DefenceItemData itemData, Vector3 position, bool isGhost, float ghostAlpha)
        {
            if (itemData == null) return null;
            GameObject itemObject = null;
            if (_gridItem2DPrefab != null) itemObject = Instantiate(_gridItem2DPrefab, position, Quaternion.identity);
            else
            {
                itemObject = new GameObject(isGhost ? "GridItem2D_RuntimeGhost" : "GridItem2D");
                itemObject.transform.position = position;
            }

            var gridItem = GetOrAddGridItem2D(itemObject);
            var spriteRenderer = GetOrAddSpriteRenderer(itemObject);

            gridItem.SetSpriteRenderer(spriteRenderer);
            gridItem.SetDefenceItemData(itemData);
            
            if (itemData.Sprite != null && spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = itemData.Sprite;
            }

            if (isGhost)
            {
                gridItem.SetDraggable(false);
                itemObject.name = $"{itemData.DisplayName}_Ghost";

                if (!string.IsNullOrEmpty(itemData.GhostSortingLayerName))
                {
                    spriteRenderer.sortingLayerName = itemData.GhostSortingLayerName;
                }
                spriteRenderer.sortingOrder = itemData.GhostSortingOrder;
                
                var color = spriteRenderer.color;
                color.a = ghostAlpha;
                spriteRenderer.color = color;
            }
            return itemObject;
        }

        public IPlaceable CreateGridItem2D(DefenceItemData itemData, Vector3 position, bool isGhost = false)
        {
            if (itemData == null) return null;
            
            GameObject itemObject = null;
            if (_gridItem2DPrefab != null) itemObject = Instantiate(_gridItem2DPrefab, position, Quaternion.identity);
            else
            {
                itemObject = new GameObject(isGhost ? "GridItem2D_RuntimeGhost" : "GridItem2D");
                itemObject.transform.position = position;
            }

            var gridItem = GetOrAddGridItem2D(itemObject);
            if (gridItem == null) return null;
            
            var spriteRenderer = GetOrAddSpriteRenderer(itemObject);
            if (spriteRenderer == null) return null;

            gridItem.SetSpriteRenderer(spriteRenderer);
            gridItem.SetDefenceItemData(itemData);
            
            if (itemData.Sprite != null && spriteRenderer.sprite == null)
            {
                spriteRenderer.sprite = itemData.Sprite;
            }

            if (isGhost)
            {
                gridItem.SetDraggable(false);
                itemObject.name = $"{itemData.DisplayName}_Ghost";

                if (!string.IsNullOrEmpty(itemData.GhostSortingLayerName))
                {
                    spriteRenderer.sortingLayerName = itemData.GhostSortingLayerName;
                }
                spriteRenderer.sortingOrder = itemData.GhostSortingOrder;
            }
            
            return gridItem;
        }
        
        private GridItem2D GetOrAddGridItem2D(GameObject obj)
        {
            if (obj == null) return null;
            
            var existing = obj.GetComponent<GridItem2D>();
            if (existing != null) return existing;
            
            return obj.AddComponent<GridItem2D>();
        }

        private SpriteRenderer GetOrAddSpriteRenderer(GameObject obj)
        {
            if (obj == null) return null;
            
            var existing = obj.GetComponent<SpriteRenderer>();
            if (existing != null) return existing;
            
            return obj.AddComponent<SpriteRenderer>();
        }

        public void SetPrefab(GameObject prefab)
        {
            _gridItem2DPrefab = prefab;
        }
    }
}

