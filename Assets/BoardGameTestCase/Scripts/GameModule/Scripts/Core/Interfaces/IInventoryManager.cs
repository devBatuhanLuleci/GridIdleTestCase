using System;
using BoardGameTestCase.Core.ScriptableObjects;

namespace GameModule.Core.Interfaces
{
    public interface IInventoryManager
    {
        event Action<DefenceItemData, int> OnQuantityChanged;
        event Action OnLevelChanged;
        
        int GetAvailableQuantity(DefenceItemData itemData);
        bool IsItemAvailable(DefenceItemData itemData);
        void RefreshInventoryFromLevel();
        void ConsumeItem(DefenceItemData itemData);
    }
}

