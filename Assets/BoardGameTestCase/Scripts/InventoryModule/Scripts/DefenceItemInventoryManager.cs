using System;
using System.Collections.Generic;
using UnityEngine;
using BoardGameTestCase.Core.Common;
using GameModule.Core.Interfaces;
using BoardGameTestCase.Core.ScriptableObjects;

namespace InventoryModule
{
    public class DefenceItemInventoryManager : MonoBehaviour, IInitializable, IInventoryManager
    {
        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;
        private Dictionary<DefenceItemData, int> _availableQuantities = new Dictionary<DefenceItemData, int>();
        private ILevelDataProvider _levelDataProvider;
        
        public event Action<DefenceItemData, int> OnQuantityChanged;
        public event Action OnLevelChanged;
        
        public int GetAvailableQuantity(DefenceItemData itemData)
        {
            if (itemData == null) return 0;
            return _availableQuantities.TryGetValue(itemData, out int quantity) ? quantity : 0;
        }
        
        public bool IsItemAvailable(DefenceItemData itemData) => GetAvailableQuantity(itemData) > 0;
        
        private void Awake()
        {
            ServiceLocator.Instance.Register<DefenceItemInventoryManager>(this);
            ServiceLocator.Instance.Register<IInventoryManager>(this);
        }
        
        private void OnDestroy()
        {
            ServiceLocator.Instance?.Unregister<DefenceItemInventoryManager>();
            ServiceLocator.Instance?.Unregister<IInventoryManager>();
        }
        
        public void Initialize()
        {
            if (_isInitialized) return;
            _levelDataProvider = ServiceLocator.Instance.Get<ILevelDataProvider>();
            if (_levelDataProvider == null) return;
            RefreshInventoryFromLevel();
            _isInitialized = true;
        }
        
        public void RefreshInventoryFromLevel()
        {
            if (_levelDataProvider == null || _levelDataProvider.CurrentLevel == null)
            {
                _availableQuantities.Clear();
                return;
            }
            
            _availableQuantities.Clear();
            var currentLevel = _levelDataProvider.CurrentLevel;
            foreach (var entry in currentLevel.DefenceItems)
            {
                if (entry.DefenceItemData != null) _availableQuantities[entry.DefenceItemData] = entry.Quantity;
            }
            OnLevelChanged?.Invoke();
        }
        
        public void ConsumeItem(DefenceItemData itemData)
        {
            if (itemData == null) return;
            if (!_availableQuantities.ContainsKey(itemData)) return;
            int currentQuantity = _availableQuantities[itemData];
            if (currentQuantity <= 0) return;
            _availableQuantities[itemData] = currentQuantity - 1;
            OnQuantityChanged?.Invoke(itemData, _availableQuantities[itemData]);
        }
        
        public void ReturnItem(DefenceItemData itemData)
        {
            if (itemData == null) return;
            if (!_availableQuantities.ContainsKey(itemData)) _availableQuantities[itemData] = 1;
            else _availableQuantities[itemData] = _availableQuantities[itemData] + 1;
            OnQuantityChanged?.Invoke(itemData, _availableQuantities[itemData]);
        }
        
        public Dictionary<DefenceItemData, int> GetAllAvailableItems() => new Dictionary<DefenceItemData, int>(_availableQuantities);
    }
}

