
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using GridSystemModule.Core.Interfaces;
using BoardGameTestCase.Core.ScriptableObjects;

namespace UISystemModule.UIElements
{
    /// <summary>
    /// Manages horizontal slot-based layout and reordering of sprite inventory items.
    /// Allows drag-drop swapping similar to board game tiles (like Okey).
    /// </summary>

    public class SpriteInventorySlotManager : MonoBehaviour
    {
        [Header("Layout Settings")]
        [SerializeField] private Vector3 _slotStep = new Vector3(2f, 0f, 0f);

        [Header("Animation")]
        [SerializeField] private float _repositionDuration = 0.3f;
        [SerializeField] private Ease _repositionEase = Ease.OutCubic;


        [Header("Grid Placement Integration")]
        [SerializeField] private MonoBehaviour _gridPlacementSystemObject; // Inspector'dan atanacak
        [SerializeField] private GameObject _gridItem2DPrefab;
        [SerializeField] private Transform _inventoryParent;

        private IGridPlacementSystem _gridPlacementSystem;
        // Event dinleme için yardımcı interface (GridPlacementSystem'da public event ile uygulanmalı)


        private void OnEnable()
        {
            if (_gridPlacementSystemObject != null)
            {
                _gridPlacementSystem = _gridPlacementSystemObject as IGridPlacementSystem;
                if (_gridPlacementSystem != null)
                {
                    _gridPlacementSystem.OnItemPlaced += HandleGridItemPlaced;
                }
            }
        }

        private void OnDisable()
        {
            if (_gridPlacementSystem != null)
            {
                _gridPlacementSystem.OnItemPlaced -= HandleGridItemPlaced;
            }
        }

        // Grid'e item yerleştirildiğinde çağrılır (event handler)
        private void HandleGridItemPlaced(IPlaceable placeable)
        {
            _placedItemCount++;
            if (_placedItemCount % 3 == 0)
            {
                AddRandomItemsToInventory(3);
            }
        }

        private int _placedItemCount = 0;

        private List<SlotData> _slots = new List<SlotData>();
        private GridItem2D _currentlyDraggingItem = null;
        private int _originalSlotIndex = -1;
        private bool _swapHandled = false;

        public event Action<GridItem2D, int, int> OnItemSwapped; // item, oldIndex, newIndex

        public bool IsItemInSlot(GridItem2D item) => GetSlotIndex(item) != -1;
        
        private class SlotData
        {
            public int Index;
            public GridItem2D Item;
            public Vector3 Position;
            public SlotData(int index, Vector3 position)
            {
                Index = index;
                Position = position;
            }
        }

        private void AddRandomItemsToInventory(int count)
        {
            Debug.Log("SLOTSTEP: " + _slotStep + ", parent: " + (_inventoryParent != null ? _inventoryParent.name : "null"));
            for (int i = 0; i < count; i++)
            {
               // Pass -1 to automatically find the next empty slot
               CreateAndRegisterItem(null, -1);
            }
        }

        /// <summary>
        /// Ortak item instantiate methodu. Hem DefenceItemPanel hem de buradan erişilebilir.
        /// </summary>
        /// <summary>
        /// slotPosition: item'ın slot'a göre localPosition'ı (örn: slotStep * slotIndex)
        /// </summary>
        public GridItem2D CreateAndRegisterItem(BoardGameTestCase.Core.ScriptableObjects.DefenceItemData itemData = null, int specificSlotIndex = -1)
        {
            if (_gridItem2DPrefab == null || _inventoryParent == null) return null;

            // 1. Determine Slot Index and Position
            int targetIndex = specificSlotIndex != -1 ? specificSlotIndex : GetFirstEmptySlotIndex();
            Vector3 slotPos = CalculateSlotPosition(targetIndex);

            // 2. Instantiate Item
            var itemObject = Instantiate(_gridItem2DPrefab, _inventoryParent);
            itemObject.transform.localPosition = slotPos;
            
            // 3. Get or Add GridItem2D
            if (!itemObject.TryGetComponent<GridItem2D>(out var gridItem))
            {
                gridItem = itemObject.AddComponent<GridItem2D>();
            }
            
            // 4. Setup based on data
            if (itemData != null)
            {
                gridItem.SetDefenceItemData(itemData);
                
                // Drag Handler
                if (!itemObject.TryGetComponent<SpriteGridItemDragHandler>(out var handler))
                {
                    handler = itemObject.AddComponent<SpriteGridItemDragHandler>();
                }
                handler.SetDefenceItemData(itemData);

                // Sprite Renderer
                var sr = gridItem.GetSpriteRenderer();
                if (sr == null)
                {
                   if (!itemObject.TryGetComponent(out sr))
                   {
                       sr = itemObject.AddComponent<SpriteRenderer>();
                   }
                   gridItem.SetSpriteRenderer(sr);
                }
                
                if (sr != null)
                {
                    if (itemData.Sprite != null) sr.sprite = itemData.Sprite;
                    sr.sortingLayerName = itemData.GhostSortingLayerName;
                    sr.sortingOrder = itemData.GhostSortingOrder; 
                    sr.color = Color.white;
                }
            }
            else
            {
                 if (!itemObject.TryGetComponent<SpriteGridItemDragHandler>(out _))
                 {
                     itemObject.AddComponent<SpriteGridItemDragHandler>();
                 }
            }

            // 5. Common Configuration
            gridItem.SetDraggable(true);
            gridItem.IsPlaced = false;
            gridItem.SetPlacementSystem(null);
            itemObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            // 6. Drag Notification
            if (!itemObject.TryGetComponent<InventoryDragNotifier>(out var notifier))
            {
                notifier = itemObject.AddComponent<InventoryDragNotifier>();
            }
             notifier.Initialize(gridItem, this, _gridPlacementSystem);

            // 7. Registration
            RegisterItem(gridItem, targetIndex);
            
            return gridItem;
        }

        private int GetFirstEmptySlotIndex()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Item == null)
                    return i;
            }
            return _slots.Count;
        }
        
        /// <summary>
        /// Registers an item to a specific slot index
        /// </summary>
        public void RegisterItem(GridItem2D item, int slotIndex)
        {
            if (item == null) return;
            
            // Ensure slot exists
            while (_slots.Count <= slotIndex)
            {
                int newIndex = _slots.Count;
                Vector3 pos = CalculateSlotPosition(newIndex);
                _slots.Add(new SlotData(newIndex, pos));
            }
            
            var slot = _slots[slotIndex];
            slot.Item = item;
            
            // Subscribe to drag events
            SubscribeToDragEvents(item);
        }
        
        /// <summary>
        /// Unregisters an item from the slot system
        /// </summary>
        public void UnregisterItem(GridItem2D item)
        {
            if (item == null) return;
            
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Item == item)
                {
                    _slots[i].Item = null;
                    UnsubscribeFromDragEvents(item);
                    break;
                }
            }
        }
        
        /// <summary>
        /// Clears all registered items
        /// </summary>
        public void Clear()
        {
            foreach (var slot in _slots)
            {
                if (slot.Item != null)
                {
                    UnsubscribeFromDragEvents(slot.Item);
                }
            }
            _slots.Clear();
        }
        
        /// <summary>
        /// Repositions all items to their current slot positions
        /// </summary>
        public void RefreshAllPositions(bool animate = true)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.Item == null) continue;

                var t = slot.Item.transform;
                Vector3 currentLocal = t.localPosition;
                float targetX = slot.Position.x;

                if (animate && Application.isPlaying)
                {
                    // Ensure Y stays exactly 0 before animating X
                    if (Mathf.Abs(currentLocal.y) > Mathf.Epsilon)
                    {
                        t.localPosition = new Vector3(currentLocal.x, 0f, currentLocal.z);
                    }
                    t.DOLocalMoveX(targetX, _repositionDuration)
                        .SetEase(_repositionEase);
                }
                else
                {
                    // Snap to X target and clamp Y to 0
                    t.localPosition = new Vector3(targetX, 0f, currentLocal.z);
                }
            }
        }
        
        /// <summary>
        /// Gets the slot index of an item, returns -1 if not found
        /// </summary>
        public int GetSlotIndex(GridItem2D item)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Item == item)
                {
                    return i;
                }
            }
            return -1;
        }
        
        /// <summary>
        /// Gets the item at a specific slot index
        /// </summary>
        public GridItem2D GetItemAtSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Count) return null;
            return _slots[slotIndex].Item;
        }
        
        /// <summary>
        /// Rebuilds the slot layout based on current slot step and start position
        /// </summary>
        public void RebuildLayout()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].Position = CalculateSlotPosition(i);
            }
            RefreshAllPositions(true);
        }
        
        public void SetSlotStep(Vector3 step)
        {
            _slotStep = step;
            RebuildLayout();
        }
        
        private Vector3 CalculateSlotPosition(int slotIndex)
        {
            return _slotStep * slotIndex;
        }
        
        private void SubscribeToDragEvents(GridItem2D item)
        {
            // We'll track drag state through IsDragging property in Update
            // This is simpler than event-based approach for this use case
        }
        
        private void UnsubscribeFromDragEvents(GridItem2D item)
        {
            // Cleanup if needed
        }
        
        private void OnItemDragStarted(GridItem2D item)
        {
            _currentlyDraggingItem = item;
            _originalSlotIndex = GetSlotIndex(item);
            _swapHandled = false;
            
            // Note: We do NOT set IsPlaced=true here anymore
            // UpdateDragPosition() in GridItem2D updates _originalPosition each frame,
            // so ReturnToOriginalPosition() will return to the last dragged position, not initial position
            // This allows proper reordering while preventing unwanted position resets
            
        }
        
        private void OnItemDragEnded(GridItem2D item)
        {
            if (_currentlyDraggingItem != item || _swapHandled)
            {
                return;
            }
            
            int currentSlotIndex = GetSlotIndex(item);
            if (currentSlotIndex == -1)
            {
                _currentlyDraggingItem = null;
                return;
            }
            
            
            // Check if item was placed on grid by PlacementSystem
            // If IsPlaced=true, PlacementSystem handled it and we skip reordering
            // If IsPlaced=false, item stays in inventory and we reorder
            if (!item.IsPlaced)
            {
                ReorderItemsByXPosition();
            }
            else
            {
                // IMPORTANT: Remove from slot system since it's now on grid
                UnregisterItem(item);
            }
            
            _swapHandled = true;
            
            // Reset drag state - IsDragging becomes false
            // IsPlaced stays as is (true if on grid, false if in inventory)
            item.IsDragging = false;
            
            _currentlyDraggingItem = null;
            _originalSlotIndex = -1;
        }
        
        /// <summary>
        /// Reorders all items in slots based on their current X positions (smallest X first)
        /// This mimics Okey tile reordering behavior
        /// Only reorders items that are NOT placed on grid (IsPlaced = false)
        /// </summary>
        private void ReorderItemsByXPosition()
        {
            // Collect all non-null, non-placed items with their current positions
            // Placed items (on grid) are skipped - they stay where they are
            List<(GridItem2D item, Vector3 position, int oldSlotIndex)> itemsWithPositions = new List<(GridItem2D, Vector3, int)>();
            
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Item != null && !_slots[i].Item.IsPlaced)
                {
                        itemsWithPositions.Add((_slots[i].Item, _slots[i].Item.transform.localPosition, i));
                }
            }
            
            // Sort by X position (left to right)
            itemsWithPositions.Sort((a, b) => a.position.x.CompareTo(b.position.x));
            
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Item != null && !_slots[i].Item.IsPlaced)
                {
                }
            }
            
            for (int i = 0; i < itemsWithPositions.Count; i++)
            {
                var (item, pos, oldIdx) = itemsWithPositions[i];
            }
            
            // STEP 1: Clear all inventory items from their current slots
            for (int i = 0; i < itemsWithPositions.Count; i++)
            {
                var (item, _, oldSlotIndex) = itemsWithPositions[i];
                if (oldSlotIndex >= 0 && oldSlotIndex < _slots.Count && _slots[oldSlotIndex].Item == item)
                {
                    _slots[oldSlotIndex].Item = null;
                }
            }
            
            // STEP 2: Reassign inventory items to slots sequentially from slot 0
            for (int i = 0; i < itemsWithPositions.Count; i++)
            {
                var (item, _, oldSlotIndex) = itemsWithPositions[i];
                
                if (i >= _slots.Count)
                {
                    break;
                }
                
                if (i != oldSlotIndex)
                {
                    OnItemSwapped?.Invoke(item, oldSlotIndex, i);
                }
                else
                {
                }
                
                _slots[i].Item = item;            }
            
            // Animate all items to their new slot positions
            RefreshAllPositions(true);
        }
        
        private void SwapSlots(int slotA, int slotB)
        {
            if (slotA < 0 || slotA >= _slots.Count || slotB < 0 || slotB >= _slots.Count)
            {
                return;
            }
            
            var itemA = _slots[slotA].Item;
            var itemB = _slots[slotB].Item;
            
            _slots[slotA].Item = itemB;
            _slots[slotB].Item = itemA;
            
            // Animate to new positions
            RefreshAllPositions(true);
        }
        
        /// <summary>
        /// Manually trigger drag start tracking (call from GridItem2D or drag handler)
        /// </summary>
        public void NotifyDragStart(GridItem2D item)
        {
            OnItemDragStarted(item);
        }
        
        /// <summary>
        /// Manually trigger drag end tracking (call from GridItem2D or drag handler)
        /// </summary>
        public void NotifyDragEnd(GridItem2D item)
        {
            OnItemDragEnded(item);
        }
        
        private void OnDestroy()
        {
            Clear();
        }
    }
}
