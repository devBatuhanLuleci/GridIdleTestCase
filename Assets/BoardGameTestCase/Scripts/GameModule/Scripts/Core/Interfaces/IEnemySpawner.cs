using System.Collections.Generic;
using BoardGameTestCase.Core.ScriptableObjects;

namespace GameModule.Core.Interfaces
{
    public interface IEnemySpawner
    {
        IReadOnlyList<IEnemy> SpawnedEnemies { get; }
        int ActiveEnemyCount { get; }
        void SpawnAllEnemies();
        IEnemy SpawnEnemy(EnemyData enemyData, UnityEngine.Transform manualParent = null);
        void ClearAllEnemies();
    }
}

