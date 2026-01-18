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
    {
        private enum DisplayMode
        {
            CanvasUI,
            WorldSprites
        }

        [Header("Display Mode")]
        [SerializeField] private DisplayMode _displayMode = DisplayMode.CanvasUI;

        [Header("Canvas UI Mode")]
        [SerializeField] private GameObject _dragHandlerPrefab;
        [SerializeField] private Transform _itemsContainer;
        [SerializeField] private BaseUIButton _autoPlacementButton;

        [Header("Sprite Mode")]
        [SerializeField] private int _maxSpritesPerItem = 0; // 0 means no cap
        [SerializeField] private SpriteInventorySlotManager _slotManager;
        
        private GameModule.Core.Interfaces.ILevelDataProvider _levelDataProvider;
        private GameModule.Core.Interfaces.IInventoryManager _inventoryManager;
        private GridSystemModule.Core.Interfaces.IGridPlacementSystem _placementSystem;
        private Dictionary<DefenceItemData, UIGridItemDragHandler> _itemHandlers = new Dictionary<DefenceItemData, UIGridItemDragHandler>();
        private Dictionary<DefenceItemData, List<SpriteGridItemDragHandler>> _spriteItemHandlers = new Dictionary<DefenceItemData, List<SpriteGridItemDragHandler>>();
        private List<GridItem2D> _allSpriteItems = new List<GridItem2D>();
        private GameModule.Core.Interfaces.IStateController _stateController;
        private CompositeDisposable _disposables = new CompositeDisposable();
        
        protected override void Awake()
        {
            base.Awake();
            if (_itemsContainer == null) _itemsContainer = _contentParent;

            
            if (_slotManager == null)
            {
                _slotManager = GetComponent<SpriteInventorySlotManager>();
                if (_slotManager == null)
                {
                    _slotManager = (SpriteInventorySlotManager)gameObject.AddComponent(typeof(SpriteInventorySlotManager));
                }
            }
            
            // Layout handled solely by SlotManager using its slot step
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
            
            if (_displayMode == DisplayMode.WorldSprites)
            {
                RefreshSpriteItems(forceFullReset: true);
                return;
            }
            RefreshCanvasItems();
        }
        
        private void RefreshCanvasItems()
        {
            var currentLevel = _levelDataProvider.CurrentLevel;
            foreach (var entry in currentLevel.DefenceItems)
            {
                if (entry.DefenceItemData == null) continue;
                int currentQuantity = _inventoryManager != null ? _inventoryManager.GetAvailableQuantity(entry.DefenceItemData) : entry.Quantity;
                if (!_itemHandlers.ContainsKey(entry.DefenceItemData))
                {
                    CreateItemUI(entry.DefenceItemData, currentQuantity);
                }
                else
                {
                    UpdateItemVisualState(entry.DefenceItemData, currentQuantity);
                }
            }
            RemoveStaleCanvasItems(currentLevel);
        }

        private void RefreshSpriteItems(bool forceFullReset = false)
        {
            var currentLevel = _levelDataProvider.CurrentLevel;
            
            // Removed redundant unregistration loop that was causing SlotManager to lose track of items.
            // SyncSpriteItems will handle adding/removing items as needed based on inventory quantity.
            
            int spawnCursor = 0;
            for (int itemIndex = 0; itemIndex < currentLevel.DefenceItems.Count; itemIndex++)
            {
                var entry = currentLevel.DefenceItems[itemIndex];
                if (entry == null || entry.DefenceItemData == null) continue;
                int currentQuantity = _inventoryManager != null ? _inventoryManager.GetAvailableQuantity(entry.DefenceItemData) : entry.Quantity;
                spawnCursor = SyncSpriteItems(entry.DefenceItemData, currentQuantity, spawnCursor);
            }
            RemoveStaleSpriteItems(currentLevel);
            // Finalize positioning after all items are synced
            _slotManager?.RefreshAllPositions(false);
        }

        private void CreateItemUI(DefenceItemData itemData, int initialQuantity)
        {
            if (itemData == null || _itemsContainer == null) return;
            GameObject itemObject;
            if (_dragHandlerPrefab != null)
            {
                // Prefabdan instantiate
                int slotIndex = _itemHandlers.Count;
                // If using slot manager logic for UI items, we need to replicate the step logic locally or expose it.
                // But for now, fixing the compilation error by instantiating directly.
                var go = Instantiate(_dragHandlerPrefab, _itemsContainer);
                Vector3 slotStep = Vector3.right * 100f; // Default UI step if manager not available
                if (_slotManager != null)
                {
                     // Try to get step via reflection or assume default
                     // We can't access private _slotStep easily without reflection, and the user's reflection code was messy.
                     // It's better to just place them simply for now to fix the build.
                     // Or, assuming UI layout handles positioning (horizontal layout group)?
                     // If _itemsContainer has a LayoutGroup, we don't need to set index.
                     // The user's code manually set anchoredPosition. 
                }
                
                // Basic positioning for UI
                // go.transform.localPosition = ... 
                
                // Check if we can get GridItem2D
                var gridItem = go.GetComponent<GridItem2D>();
                itemObject = go;
            }
            else
            {
                itemObject = new GameObject($"Item_{itemData.DisplayName}");
                itemObject.transform.SetParent(_itemsContainer, false);
                var image = itemObject.AddComponent<Image>();
                if (itemData.Sprite != null) image.sprite = itemData.Sprite;
                var rectTransform = itemObject.GetComponent<RectTransform>() ?? itemObject.AddComponent<RectTransform>();
                rectTransform.sizeDelta = new Vector2(100, 100);
            }
            if (itemObject == null) return;
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

        private int SyncSpriteItems(DefenceItemData itemData, int quantity, int startIndex)
        {
            if (itemData == null) return startIndex;
            if (!_spriteItemHandlers.TryGetValue(itemData, out var handlers))
            {
                handlers = new List<SpriteGridItemDragHandler>();
                _spriteItemHandlers[itemData] = handlers;
            }

            int targetQuantity = Mathf.Max(0, quantity);
            if (_maxSpritesPerItem > 0)
            {
                targetQuantity = Mathf.Min(targetQuantity, _maxSpritesPerItem);
            }

            for (int i = handlers.Count - 1; i >= targetQuantity; i--)
            {
                if (handlers[i] != null)
                {
                    var gridItem = handlers[i].GetComponent<GridItem2D>();
                    if (gridItem != null)
                    {
                        _slotManager?.UnregisterItem(gridItem);
                        _allSpriteItems.Remove(gridItem);
                    }
                    Destroy(handlers[i].gameObject);
                }
                handlers.RemoveAt(i);
            }

            for (int i = handlers.Count; i < targetQuantity; i++)
            {
                // Refactored: Use SlotManager to create item
                if (_slotManager != null)
                {
                    int slotIndex = -1; // Let manager find the slot
                    var gridItem = _slotManager.CreateAndRegisterItem(itemData, slotIndex);
                    if (gridItem != null)
                    {
                        var handler = gridItem.GetComponent<SpriteGridItemDragHandler>();
                        if (handler != null)
                        {
                            handlers.Add(handler);
                        }
                        _allSpriteItems.Add(gridItem);
                    }
                }
            }
            // Handlers are now registered in CreateAndRegisterItem, no need for loop here
            // But we might want to ensure they are at correct indices if this was a refresh only?
            // Actually CreateAndRegisterItem handles slot registration.
            
            return startIndex + handlers.Count;
        }

        /// <summary>



        // Positioning is handled by SpriteInventorySlotManager; no local calculation needed

        private void RemoveStaleCanvasItems(LevelData currentLevel)
        {
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
                if (!stillExists) keysToRemove.Add(kvp.Key);
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

        private void RemoveStaleSpriteItems(LevelData currentLevel)
        {
            var keysToRemove = new List<DefenceItemData>();
            foreach (var kvp in _spriteItemHandlers)
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
                if (!stillExists) keysToRemove.Add(kvp.Key);
            }

            foreach (var key in keysToRemove)
            {
                if (_spriteItemHandlers.TryGetValue(key, out var handlers))
                {
                    foreach (var handler in handlers)
                    {
                        if (handler != null)
                        {
                            var gridItem = handler.GetComponent<GridItem2D>();
                            if (gridItem != null)
                            {
                                _slotManager?.UnregisterItem(gridItem);
                                _allSpriteItems.Remove(gridItem);
                            }
                            Destroy(handler.gameObject);
                        }
                    }
                }
                _spriteItemHandlers.Remove(key);
            }
        }
        
        private void ClearItems()
        {
            foreach (var handler in _itemHandlers.Values)
            {
                if (handler != null) Destroy(handler.gameObject);
            }
            _itemHandlers.Clear();

            foreach (var handlerList in _spriteItemHandlers.Values)
            {
                foreach (var handler in handlerList)
                {
                    if (handler != null)
                    {
                        var gridItem = handler.GetComponent<GridItem2D>();
                        if (gridItem != null)
                        {
                            _slotManager?.UnregisterItem(gridItem);
                        }
                        Destroy(handler.gameObject);
                    }
                }
            }
            _spriteItemHandlers.Clear();
            _allSpriteItems.Clear();
            _slotManager?.Clear();
        }

        private void OnItemQuantityChanged(DefenceItemData itemData, int newQuantity) => UpdateItemVisualState(itemData, newQuantity);
        private void OnLevelChanged() => RefreshUI();
        
        private void UpdateItemVisualState(DefenceItemData itemData, int quantity)
        {
            if (_displayMode == DisplayMode.WorldSprites)
            {
                RefreshUI();
                return;
            }

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

        public void UseSpriteDisplay(bool useSpriteMode) => _displayMode = useSpriteMode ? DisplayMode.WorldSprites : DisplayMode.CanvasUI;
        // Layout configuration is managed via SlotManager's slot step only; remove sprite start/step
        
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
            }
        }
        

    }
    
    /// <summary>
    /// Helper component to notify SlotManager about drag state changes
    /// </summary>
    public class InventoryDragNotifier : MonoBehaviour
    {
        private GridItem2D _gridItem;
        private SpriteInventorySlotManager _slotManager;
        private IGridPlacementSystem _placementSystem;
        private bool _wasDraggingLastFrame = false;
        
        public void Initialize(GridItem2D gridItem, SpriteInventorySlotManager slotManager, IGridPlacementSystem placementSystem = null)
        {
            _gridItem = gridItem;
            _slotManager = slotManager;
            _placementSystem = placementSystem;
        }
        
        private void Update()
        {
            if (_gridItem == null || _slotManager == null) return;
            
            bool isDragging = _gridItem.IsDragging;
            bool isInSlot = _slotManager.IsItemInSlot(_gridItem);
            
            // Detect drag START
            if (isDragging && !_wasDraggingLastFrame)
            {
                if (isInSlot)
                {
                    // Enable PlacementSystem for this drag so item can be placed on tiles
                    if (_placementSystem != null)
                    {
                        _gridItem.SetPlacementSystem(_placementSystem);
                    }
                    
                    _slotManager.NotifyDragStart(_gridItem);
                }
            }
            // IMPORTANT: Check drag END BEFORE GridItem2D.EndDragging() changes IsDragging state
            // This ensures we reorder items based on their dragged positions, not reverted positions
            else if (isDragging && _wasDraggingLastFrame)
            {
                // Still dragging, nothing to do
            }
            else if (!isDragging && _wasDraggingLastFrame && isInSlot)
            {
                // Drag ended - notify IMMEDIATELY before any other systems process the drag end
                // IMPORTANT: Call NotifyDragEnd BEFORE disabling PlacementSystem
                // This allows PlacementSystem.EndDragging() to complete placement if needed
                // If item was placed (IsPlaced=true), PlacementSystem handled it
                // If item wasn't placed (IsPlaced=false), SlotManager will reorder inventory
                _slotManager.NotifyDragEnd(_gridItem);
                
                // Now disable PlacementSystem after notification
                // Next drag will re-enable it again
                _gridItem.SetPlacementSystem(null);
            }
            
            _wasDraggingLastFrame = isDragging;
        }
    }
}
