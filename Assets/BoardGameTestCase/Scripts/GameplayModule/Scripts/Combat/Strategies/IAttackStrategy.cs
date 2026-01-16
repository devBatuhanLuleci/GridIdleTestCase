using System.Collections.Generic;

namespace GameplayModule.Strategies
{
    public interface IAttackStrategy
    {
        void Attack(DefenceItemCombat attacker, List<EnemyItem2D> enemiesInRange);
    }
}

