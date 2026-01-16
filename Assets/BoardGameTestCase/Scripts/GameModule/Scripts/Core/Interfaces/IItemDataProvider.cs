using UnityEngine;
using BoardGameTestCase.Core.ScriptableObjects;

namespace GameModule.Core.Interfaces
{
    public interface IItemDataProvider
    {
        DefenceItemData GetItemDataById(string itemId);
        int GetItemQuantityById(string itemId);
        Sprite GetItemSpriteById(string itemId);
    }
}

