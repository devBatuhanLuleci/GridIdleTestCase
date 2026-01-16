using System;
using System.Collections.Generic;
using UnityEngine;
using BoardGameTestCase.Core.Common;
using BoardGameTestCase.Core.ScriptableObjects;
using GameModule.Core.Interfaces;

namespace InventoryModule
{
    public class EnemyItemInventoryManager : MonoBehaviour, IInitializable
    {
        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;
        private Dictionary<EnemyData, int> _availableQuantities = new Dictionary<EnemyData, int>();
        private ILevelDataProvider _levelDataProvider;
        
        public event Action<EnemyData, int> OnEnemyQuantityChanged;
        public event Action OnLevelChanged;
        
        public int GetAvailableQuantity(EnemyData enemyData)
        {
            if (enemyData == null) return 0;
            return _availableQuantities.TryGetValue(enemyData, out int quantity) ? quantity : 0;
        }
        
        public bool IsItemAvailable(EnemyData enemyData) => GetAvailableQuantity(enemyData) > 0;
        
        private void Awake()
        {
            ServiceLocator.Instance.Register<EnemyItemInventoryManager>(this);
        }
        
        private void OnDestroy()
        {
            ServiceLocator.Instance?.Unregister<EnemyItemInventoryManager>();
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
            foreach (var entry in currentLevel.Enemies)
            {
                if (entry.EnemyData != null) _availableQuantities[entry.EnemyData] = entry.Quantity;
            }
            OnLevelChanged?.Invoke();
        }
        
        public void ConsumeItem(EnemyData enemyData)
        {
            if (enemyData == null) return;
            if (!_availableQuantities.ContainsKey(enemyData)) return;
            int currentQuantity = _availableQuantities[enemyData];
            if (currentQuantity <= 0) return;
            _availableQuantities[enemyData] = currentQuantity - 1;
            OnEnemyQuantityChanged?.Invoke(enemyData, _availableQuantities[enemyData]);
        }
        
        public Dictionary<EnemyData, int> GetAllAvailableItems() => new Dictionary<EnemyData, int>(_availableQuantities);
    }
}

