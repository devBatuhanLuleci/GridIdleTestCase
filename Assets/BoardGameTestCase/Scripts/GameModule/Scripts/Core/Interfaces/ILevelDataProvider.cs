using BoardGameTestCase.Core.ScriptableObjects;

namespace GameModule.Core.Interfaces
{
    public interface ILevelDataProvider
    {
        LevelData CurrentLevel { get; }
        int CurrentLevelNumber { get; }
        bool HasNextLevel { get; }
        bool NextLevel();
    }
}

