using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using GridSystemModule.Core.Interfaces;
using UISystemModule.Core.Interfaces;
using BoardGameTestCase.Core.Common;
using BoardGameTestCase.Core.ScriptableObjects;
using GameModule.Core.Interfaces;

namespace UISystemModule.UIElements
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class UIGridItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IInitializable
    {
        [SerializeField] private string _itemId;
        [SerializeField] private Image ItemUIIcon;
        [SerializeField] private TextMeshProUGUI ItemUIQuantityText;
        [SerializeField] private float _ghostAlpha = 0.7f;
        
        private Canvas _canvas;
        private Camera _worldCamera;
        private IGridPlacementSystem _placementSystem;
        
        [SerializeField] private CanvasGroup _canvasGroup;
        private IPlaceable _ghostObject;
        private bool _isDragging = false;
        private bool _isInitialized = false;
        private IInventoryManager _inventoryManager;
        private ILevelDataProvider _levelDataProvider;
        private IItemDataProvider _itemDataProvider;
        private DefenceItemData _cachedItemData;
        
        public bool IsInitialized => _isInitialized;
        
        private void Awake()
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
            
            if (ServiceLocator.Instance != null)
            {
                _inventoryManager = ServiceLocator.Instance.TryGet<IInventoryManager>();
                _itemDataProvider = ServiceLocator.Instance.TryGet<IItemDataProvider>();
                _levelDataProvider = ServiceLocator.Instance.TryGet<ILevelDataProvider>();
            }
        }
        
        public void Initialize()
        {
            if (_isInitialized) return;
            
            var uiManager = ServiceLocator.Instance.TryGet<IUIManager>();
            if (uiManager != null)
            {
                _canvas = uiManager.MainCanvas;
                _worldCamera = uiManager.MainCamera;
            }
            
            if (!string.IsNullOrEmpty(_itemId))
            {
                var data = ResolveItemData();
                if (data != null)
                {
                    if (ItemUIIcon != null)
                    {
                        var sprite = _itemDataProvider?.GetItemSpriteById(data.ItemId) ?? data.Sprite;
                        if (sprite != null) ItemUIIcon.sprite = sprite;
                    }
                    var qty = _itemDataProvider?.GetItemQuantityById(data.ItemId) ?? _inventoryManager?.GetAvailableQuantity(data) ?? 0;
                    SetItemUIQuantity(qty);
                }
            }
            
            _isInitialized = true;
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            var data = ResolveItemData();
            if (data == null) return;
            
            if (_placementSystem == null)
            {
                _placementSystem = ServiceLocator.Instance?.Get<IGridPlacementSystem>();
            }
            
            if (_placementSystem == null) return;
            if (_inventoryManager == null || !_inventoryManager.IsItemAvailable(data)) return;
            
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null) return;
            }
            
            _isDragging = true;
            _canvasGroup.alpha = _ghostAlpha;
            _canvasGroup.blocksRaycasts = false;
            CreateGhostObject(eventData);
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging || _ghostObject == null) return;
            var worldPosition = ScreenToWorldPosition(eventData.position);
            if (_ghostObject is MonoBehaviour mb)
            {
                mb.transform.position = worldPosition;
            }
            _placementSystem?.UpdateDrag(worldPosition);
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _isDragging = false;
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1.0f;
                _canvasGroup.blocksRaycasts = true;
            }
            
            if (_placementSystem != null && _ghostObject != null)
            {
                var worldPosition = ScreenToWorldPosition(eventData.position);
                bool wasPlacedBefore = _ghostObject.IsPlaced;
                _placementSystem.EndDragging(worldPosition);
                bool wasPlacedAfter = _ghostObject != null && _ghostObject.IsPlaced;
                
                if (!wasPlacedBefore && wasPlacedAfter)
                {
                    var data = ResolveItemData();
                    if (_inventoryManager != null && data != null)
                    {
                        _inventoryManager?.ConsumeItem(data);
                        var qty = _itemDataProvider?.GetItemQuantityById(data.ItemId) ?? _inventoryManager?.GetAvailableQuantity(data) ?? 0;
                        SetItemUIQuantity(qty);
                    }
                }
            }
            
            DestroyGhostObject();
        }
        
        private void CreateGhostObject(PointerEventData eventData)
        {
            var data = ResolveItemData();
            if (data == null) return;
            
            var worldPosition = ScreenToWorldPosition(eventData.position);
            
            if (_cachedItemData == null)
            {
                _cachedItemData = data;
            }
            
            var factory = ServiceLocator.Instance?.TryGet<IGridItemFactory>();
            if (factory != null && _cachedItemData != null)
            {
                _ghostObject = factory.CreateGridItem2D(_cachedItemData, worldPosition, isGhost: true);
            }
            
            if (_ghostObject == null) return;
            
            if (_placementSystem != null)
            {
                _placementSystem.StartDragging(_ghostObject);
            }
        }
        
        private void DestroyGhostObject()
        {
            if (_ghostObject == null) return;
            
            if (!_ghostObject.IsPlaced && _ghostObject is MonoBehaviour mb)
            {
                Destroy(mb.gameObject);
            }
            
            _ghostObject = null;
        }
        
        private Vector3 ScreenToWorldPosition(Vector2 screenPosition)
        {
            if (_worldCamera == null) return new Vector3(screenPosition.x, screenPosition.y, 0);
            
            var worldPos = _worldCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, _worldCamera.nearClipPlane + 1f));
            worldPos.z = 0;
            return worldPos;
        }
        
        public void SetImage(Image image) => SetItemUIIcon(image);
        
        public void SetItemUIIcon(Image image)
        {
            ItemUIIcon = image;
            var data = ResolveItemData();
            if (data != null && ItemUIIcon != null)
            {
                var sprite = _itemDataProvider?.GetItemSpriteById(data.ItemId) ?? data.Sprite;
                if (sprite != null) ItemUIIcon.sprite = sprite;
            }
        }
        
        public void SetItemUIIconSprite(Sprite sprite)
        {
            if (ItemUIIcon == null) return;
            ItemUIIcon.sprite = sprite;
        }
        
        public void SetItemUIQuantity(int quantity)
        {
            if (ItemUIQuantityText == null) return;
            ItemUIQuantityText.text = quantity.ToString();
        }
        
        public void SetItemUIQuantityText(TextMeshProUGUI quantityText) => ItemUIQuantityText = quantityText;
        
        public void SetCanvas(Canvas canvas) => _canvas = canvas;
        public void SetPlacementSystem(IGridPlacementSystem placementSystem) => _placementSystem = placementSystem;
        public void SetWorldCamera(Camera camera) => _worldCamera = camera;
        
        public void SetDefenceItemData(DefenceItemData itemData)
        {
            _itemId = itemData?.ItemId;
            _cachedItemData = itemData;
            
            if (ItemUIIcon != null && itemData != null)
            {
                var sprite = _itemDataProvider?.GetItemSpriteById(itemData.ItemId) ?? itemData.Sprite;
                if (sprite != null) ItemUIIcon.sprite = sprite;
            }
            
            if (itemData != null)
            {
                var qty = _itemDataProvider?.GetItemQuantityById(itemData.ItemId) ?? _inventoryManager?.GetAvailableQuantity(itemData) ?? 0;
                SetItemUIQuantity(qty);
            }
        }
        
        public void SetItemId(string itemId)
        {
            _itemId = itemId;
            _cachedItemData = null;
            var data = ResolveItemData();
            if (data != null)
            {
                if (ItemUIIcon != null)
                {
                    var sprite = _itemDataProvider?.GetItemSpriteById(data.ItemId) ?? data.Sprite;
                    if (sprite != null) ItemUIIcon.sprite = sprite;
                }
                var qty = _itemDataProvider?.GetItemQuantityById(data.ItemId) ?? _inventoryManager?.GetAvailableQuantity(data) ?? 0;
                SetItemUIQuantity(qty);
            }
        }
        
        private DefenceItemData ResolveItemData()
        {
            if (_cachedItemData != null) return _cachedItemData;
            
            if (_itemDataProvider != null)
            {
                _cachedItemData = _itemDataProvider.GetItemDataById(_itemId);
                if (_cachedItemData != null) return _cachedItemData;
            }
            
            if (_levelDataProvider == null || _levelDataProvider.CurrentLevel == null || string.IsNullOrEmpty(_itemId))
                return null;
            
            foreach (var entry in _levelDataProvider.CurrentLevel.DefenceItems)
            {
                if (entry.DefenceItemData?.ItemId == _itemId)
                {
                    _cachedItemData = entry.DefenceItemData;
                    return _cachedItemData;
                }
            }
            
            return null;
        }
    }
}
