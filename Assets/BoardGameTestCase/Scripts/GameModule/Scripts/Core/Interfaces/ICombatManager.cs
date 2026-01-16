using System.Collections.Generic;

namespace GameModule.Core.Interfaces
{
    public interface ICombatManager
    {
        IReadOnlyList<IEnemy> ActiveEnemies { get; }
        int ActiveEnemyCount { get; }
        void StartCombat();
        void StopCombat();
        void RegisterEnemy(IEnemy enemy);
        void UnregisterEnemy(IEnemy enemy);
    }
}

