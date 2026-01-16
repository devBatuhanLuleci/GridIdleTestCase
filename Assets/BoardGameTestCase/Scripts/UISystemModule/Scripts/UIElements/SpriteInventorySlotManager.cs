using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

namespace UISystemModule.UIElements
{
    /// <summary>
    /// Manages horizontal slot-based layout and reordering of sprite inventory items.
    /// Allows drag-drop swapping similar to board game tiles (like Okey).
    /// </summary>
    public class SpriteInventorySlotManager : MonoBehaviour
    {
        [Header("Layout Settings")]
        [SerializeField] private Vector3 _slotStartPosition = new Vector3(-2f, 0f, 0f);
        [SerializeField] private Vector3 _slotStep = new Vector3(2f, 0f, 0f);
        
        [Header("Animation")]
        [SerializeField] private float _repositionDuration = 0.3f;
        [SerializeField] private Ease _repositionEase = Ease.OutCubic;
        
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
        
        private void Update()
        {
            // Update method left empty - reordering handled in OnItemDragEnded
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
                
                Vector3 targetPos = slot.Position;
                Vector3 currentPos = slot.Item.transform.position;
                
                Debug.Log($"[RefreshPositions] {slot.Item.name}: current={currentPos}, target={targetPos}");
                
                if (animate && Application.isPlaying)
                {
                    slot.Item.transform.DOMove(targetPos, _repositionDuration)
                        .SetEase(_repositionEase);
                }
                else
                {
                    slot.Item.transform.position = targetPos;
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
        
        public void SetLayoutSettings(Vector3 startPosition, Vector3 step)
        {
            _slotStartPosition = startPosition;
            _slotStep = step;
            RebuildLayout();
        }
        
        private Vector3 CalculateSlotPosition(int slotIndex)
        {
            return _slotStartPosition + _slotStep * slotIndex;
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
            
            Debug.Log($"[SlotManager] Drag started for item at slot {_originalSlotIndex}");
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
                Debug.Log("[SlotManager] Item not in slot system, ignoring");
                _currentlyDraggingItem = null;
                return;
            }
            
            Debug.Log($"[SlotManager] Drag ended, current position: {item.transform.position}, IsPlaced: {item.IsPlaced}");
            
            // Check if item was placed on grid by PlacementSystem
            // If IsPlaced=true, PlacementSystem handled it and we skip reordering
            // If IsPlaced=false, item stays in inventory and we reorder
            if (!item.IsPlaced)
            {
                Debug.Log($"[SlotManager] Item not placed on grid, reordering inventory");
                ReorderItemsByXPosition();
            }
            else
            {
                Debug.Log($"[SlotManager] Item placed on grid, unregistering from inventory");
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
                    itemsWithPositions.Add((_slots[i].Item, _slots[i].Item.transform.position, i));
                }
            }
            
            // Sort by X position (left to right)
            itemsWithPositions.Sort((a, b) => a.position.x.CompareTo(b.position.x));
            
            Debug.Log($"[SlotManager] BEFORE SORT - Inventory items (not placed):");
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].Item != null && !_slots[i].Item.IsPlaced)
                {
                    Debug.Log($"  Slot{i}: {_slots[i].Item.name} (id={_slots[i].Item.GetInstanceID()}) @ x={_slots[i].Item.transform.position.x:F2}");
                }
            }
            
            Debug.Log($"[SlotManager] AFTER SORT - Sorted by X:");
            for (int i = 0; i < itemsWithPositions.Count; i++)
            {
                var (item, pos, oldIdx) = itemsWithPositions[i];
                Debug.Log($"  NewSlot{i}: {item.name} (id={item.GetInstanceID()}) @ x={pos.x:F2}, was at slot{oldIdx}");
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
                    Debug.LogWarning("[SlotManager] Not enough slots for inventory items!");
                    break;
                }
                
                if (i != oldSlotIndex)
                {
                    Debug.Log($"[SlotManager] Moving {item.name} from slot {oldSlotIndex} to slot {i}, target position: {_slots[i].Position}");
                    OnItemSwapped?.Invoke(item, oldSlotIndex, i);
                }
                else
                {
                    Debug.Log($"[SlotManager] {item.name} staying at slot {i}");
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
