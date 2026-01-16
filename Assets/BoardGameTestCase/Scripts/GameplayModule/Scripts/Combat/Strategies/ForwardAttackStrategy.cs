using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameplayModule.Strategies
{
    public class ForwardAttackStrategy : IAttackStrategy
    {
        public void Attack(DefenceItemCombat attacker, List<EnemyItem2D> enemiesInRange)
        {
            if (attacker == null || enemiesInRange == null || enemiesInRange.Count == 0) return;
            
            Vector2Int attackerPosition = attacker.GetAttackerGridPosition();
            
            var forwardEnemies = enemiesInRange
                .Where(e => e != null && e.IsAlive && e.GridPosition.x == attackerPosition.x && e.GridPosition.y > attackerPosition.y)
                .OrderBy(e => e.GridPosition.y)
                .ToList();
            
            if (forwardEnemies.Count > 0)
            {
                EnemyItem2D target = forwardEnemies.FirstOrDefault();
                if (target != null)
                {
                    attacker.CreateProjectile(target);
                }
            }
        }
    }
}

