using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BoardGameTestCase.Core.Common;
using BoardGameTestCase.Core.ScriptableObjects;
using UISystemModule.UIElements;
using UISystemModule.Core.Interfaces;
using UISystemModule.Core;
using GridSystemModule.Core.Interfaces;
using GameModule.Core.Interfaces;
using GameModule.Core;
using GameState = GameModule.Core.Interfaces.GameState;

namespace UISystemModule.UIElements
{
    public class DefenceItemPanel : BaseUIPanel
    {        [SerializeField] private GameObject _dragHandlerPrefab;
        [SerializeField] private Transform _itemsContainer;
        [SerializeField] private BaseUIButton _autoPlacementButton;
        
        private GameModule.Core.Interfaces.ILevelDataProvider _levelDataProvider;
        private GameModule.Core.Interfaces.IInventoryManager _inventoryManager;
        private GridSystemModule.Core.Interfaces.IGridPlacementSystem _placementSystem;
        private Dictionary<DefenceItemData, UIGridItemDragHandler> _itemHandlers = new Dictionary<DefenceItemData, UIGridItemDragHandler>();
        private GameModule.Core.Interfaces.IStateController _stateController;
        private CompositeDisposable _disposables = new CompositeDisposable();
        
        protected override void Awake()
        {
            base.Awake();
            if (_itemsContainer == null) _itemsContainer = _contentParent;
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            _levelDataProvider = ServiceLocator.Instance.Get<GameModule.Core.Interfaces.ILevelDataProvider>();
            _inventoryManager = ServiceLocator.Instance.Get<GameModule.Core.Interfaces.IInventoryManager>();
            _stateController = ServiceLocator.Instance.Get<GameModule.Core.Interfaces.IStateController>();
            
            _placementSystem = ServiceLocator.Instance?.Get<IGridPlacementSystem>();
            
            if (_inventoryManager != null)
            {
                _inventoryManager.OnQuantityChanged += OnItemQuantityChanged;
                _inventoryManager.OnLevelChanged += OnLevelChanged;
            }
            
            if (_autoPlacementButton != null)
            {
                _autoPlacementButton.OnButtonClicked += OnAutoPlacementButtonClicked;
            }
            
            SubscribeToGameStateEvents();
            UpdateAutoPlacementButtonState();
            
            RefreshUI();
        }
        
        private void SubscribeToGameStateEvents()
        {
            var stateChangedSubscription = EventBus.Instance.Subscribe<GameModule.Core.GameStateChangedEvent>(OnGameStateChanged);
            _disposables.Add(stateChangedSubscription);
        }
        
        private void OnGameStateChanged(GameModule.Core.GameStateChangedEvent evt)
        {
            UpdateAutoPlacementButtonState();
        }
        
        private void UpdateAutoPlacementButtonState()
        {
            if (_autoPlacementButton == null) return;
            
            bool isPlacingState = _stateController != null && _stateController.CurrentState == GameState.Placing;
            
            if (isPlacingState)
            {
                _autoPlacementButton.Show();
                _autoPlacementButton.SetInteractable(true);
            }
            else
            {
                _autoPlacementButton.SetInteractable(false);
                _autoPlacementButton.Hide();
            }
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_inventoryManager != null)
            {
                _inventoryManager.OnQuantityChanged -= OnItemQuantityChanged;
                _inventoryManager.OnLevelChanged -= OnLevelChanged;
            }
            
            if (_autoPlacementButton != null)
            {
                _autoPlacementButton.OnButtonClicked -= OnAutoPlacementButtonClicked;
            }
            
            _disposables?.Dispose();
        }
        
        public void RefreshUI()
        {
            if (_levelDataProvider == null || _levelDataProvider.CurrentLevel == null) return;
            
            var currentLevel = _levelDataProvider.CurrentLevel;
            
            foreach (var entry in currentLevel.DefenceItems)
            {
                if (entry.DefenceItemData == null) continue;
                
                if (!_itemHandlers.ContainsKey(entry.DefenceItemData))
                {
                    int currentQuantity = _inventoryManager != null ? _inventoryManager.GetAvailableQuantity(entry.DefenceItemData) : entry.Quantity;
                    CreateItemUI(entry.DefenceItemData, currentQuantity);
                }
                else
                {
                    int currentQuantity = _inventoryManager != null ? _inventoryManager.GetAvailableQuantity(entry.DefenceItemData) : entry.Quantity;
                    UpdateItemVisualState(entry.DefenceItemData, currentQuantity);
                }
            }
            
            var keysToRemove = new List<DefenceItemData>();
            foreach (var kvp in _itemHandlers)
            {
                bool stillExists = false;
                foreach (var entry in currentLevel.DefenceItems)
                {
                    if (entry.DefenceItemData == kvp.Key)
                    {
                        stillExists = true;
                        break;
                    }
                }
                if (!stillExists)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                if (_itemHandlers.TryGetValue(key, out var handler) && handler != null)
                {
                    Destroy(handler.gameObject);
                }
                _itemHandlers.Remove(key);
            }
        }
        
        private void CreateItemUI(DefenceItemData itemData, int initialQuantity)
        {
            if (itemData == null || _itemsContainer == null) return;
            GameObject itemObject;
            if (_dragHandlerPrefab != null) itemObject = Instantiate(_dragHandlerPrefab, _itemsContainer);
            else
            {
                itemObject = new GameObject($"Item_{itemData.DisplayName}");
                itemObject.transform.SetParent(_itemsContainer, false);
                
                var image = itemObject.AddComponent<Image>();
                if (itemData.Sprite != null) image.sprite = itemData.Sprite;
                
                var rectTransform = itemObject.GetComponent<RectTransform>() ?? itemObject.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(100, 100);
            }
            
            var dragHandler = itemObject.GetComponent<UIGridItemDragHandler>() ?? itemObject.AddComponent<UIGridItemDragHandler>();
            SetupDragHandlerReferences(dragHandler, itemObject);
            dragHandler.SetDefenceItemData(itemData);
            _itemHandlers[itemData] = dragHandler;
            UpdateItemVisualState(itemData, initialQuantity);
        }
        
        private void SetupDragHandlerReferences(UIGridItemDragHandler dragHandler, GameObject itemObject)
        {
            if (dragHandler == null || itemObject == null) return;
            
            var quantityText = itemObject.GetComponentInChildren<TextMeshProUGUI>();
            if (quantityText != null) dragHandler.SetItemUIQuantityText(quantityText);
            
            var canvas = itemObject.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                dragHandler.SetCanvas(canvas);
                var worldCamera = canvas.worldCamera ?? (canvas.renderMode == RenderMode.ScreenSpaceOverlay ? Camera.main : null);
                if (worldCamera != null) dragHandler.SetWorldCamera(worldCamera);
            }
            
            var placementSystem = ServiceLocator.Instance?.Get<IGridPlacementSystem>();
            if (placementSystem != null) dragHandler.SetPlacementSystem(placementSystem);
        }
        
        private void ClearItems()
        {
            foreach (var handler in _itemHandlers.Values)
            {
                if (handler != null) Destroy(handler.gameObject);
            }
            _itemHandlers.Clear();
        }
        
        private void OnItemQuantityChanged(DefenceItemData itemData, int newQuantity) => UpdateItemVisualState(itemData, newQuantity);
        private void OnLevelChanged() => RefreshUI();
        
        private void UpdateItemVisualState(DefenceItemData itemData, int quantity)
        {
            if (itemData == null || !_itemHandlers.TryGetValue(itemData, out var handler) || handler == null) return;
            
            if (handler.gameObject != null)
            {
                var quantityText = handler.gameObject.GetComponentInChildren<TextMeshProUGUI>();
                if (quantityText != null)
                {
                    handler.SetItemUIQuantityText(quantityText);
                }
            }
            
            handler.SetItemUIQuantity(quantity);
            
            var canvasGroup = handler.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = quantity > 0 ? 1.0f : 0.5f;
                canvasGroup.interactable = quantity > 0;
            }
        }
        
        public void SetDragHandlerPrefab(GameObject prefab) => _dragHandlerPrefab = prefab;
        public void SetItemsContainer(Transform container) => _itemsContainer = container;
        
        private void OnAutoPlacementButtonClicked(UISystemModule.Core.Interfaces.IUIButton button)
        {
            if (_placementSystem == null)
            {
                _placementSystem = ServiceLocator.Instance?.Get<IGridPlacementSystem>();
            }
            
            if (_placementSystem != null)
            {
                _placementSystem.AutoPlaceAllItemsFromInventory();
                RefreshUI();
            }
            else
            {
                Debug.LogWarning("DefenceItemPanel: PlacementSystem not found for auto-placement.");
            }
        }
    }
}
