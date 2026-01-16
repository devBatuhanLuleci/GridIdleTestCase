using BoardGameTestCase.Core.Common;
using BoardGameTestCase.Core.ScriptableObjects;
using GameModule.Core.Interfaces;

namespace GameModule.Core
{
    public struct GameStateChangedEvent : IGameEvent
    {
        public GameState NewState;
        public GameState OldState;
        
        public GameStateChangedEvent(GameState newState, GameState oldState)
        {
            NewState = newState;
            OldState = oldState;
        }
    }
    
    public struct ItemQuantityChangedEvent : IGameEvent
    {
        public DefenceItemData ItemData;
        public int NewQuantity;
        public int OldQuantity;
        
        public ItemQuantityChangedEvent(DefenceItemData itemData, int newQuantity, int oldQuantity)
        {
            ItemData = itemData;
            NewQuantity = newQuantity;
            OldQuantity = oldQuantity;
        }
    }
    
    public struct LevelChangedEvent : IGameEvent
    {
        public LevelData NewLevel;
        public LevelData OldLevel;
        
        public LevelChangedEvent(LevelData newLevel, LevelData oldLevel)
        {
            NewLevel = newLevel;
            OldLevel = oldLevel;
        }
    }
    
    public struct CurrentLevelNumberChangedEvent : IGameEvent
    {
        public int CurrentLevelNumber;
        
        public CurrentLevelNumberChangedEvent(int currentLevelNumber)
        {
            CurrentLevelNumber = currentLevelNumber;
        }
    }
    
    public struct GameStartedEvent : IGameEvent
    {
    }
    
    public struct GameEndedEvent : IGameEvent
    {
        public bool IsWin;
        
        public GameEndedEvent(bool isWin)
        {
            IsWin = isWin;
        }
    }
}

