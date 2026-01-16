using UnityEngine;
using BoardGameTestCase.Core.ScriptableObjects;
using BoardGameTestCase.Core.Common;
using GameModule.Core.Interfaces;

namespace GameModule.Services
{
    public class ItemDataProvider : MonoBehaviour, IItemDataProvider, IInitializable
    {
        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;
        private ILevelDataProvider _levelDataProvider;
        private IInventoryManager _inventoryManager;

        private void Awake()
        {
            ServiceLocator.Instance.Register<IItemDataProvider>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Instance?.Unregister<IItemDataProvider>();
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            _levelDataProvider = ServiceLocator.Instance.Get<ILevelDataProvider>();
            _inventoryManager = ServiceLocator.Instance.Get<IInventoryManager>();
            _isInitialized = true;
        }

        public DefenceItemData GetItemDataById(string itemId)
        {
            if (string.IsNullOrEmpty(itemId) || _levelDataProvider?.CurrentLevel == null) return null;
            foreach (var entry in _levelDataProvider.CurrentLevel.DefenceItems)
            {
                if (entry.DefenceItemData?.ItemId == itemId) return entry.DefenceItemData;
            }
            return null;
        }

        public int GetItemQuantityById(string itemId)
        {
            var data = GetItemDataById(itemId);
            return data != null && _inventoryManager != null ? _inventoryManager.GetAvailableQuantity(data) : 0;
        }

        public Sprite GetItemSpriteById(string itemId)
        {
            var data = GetItemDataById(itemId);
            return data?.Sprite;
        }
    }
}

