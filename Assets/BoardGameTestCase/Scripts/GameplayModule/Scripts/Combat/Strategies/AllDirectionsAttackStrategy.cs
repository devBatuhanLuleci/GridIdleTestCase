using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameplayModule.Strategies
{
    public class AllDirectionsAttackStrategy : IAttackStrategy
    {
        public void Attack(DefenceItemCombat attacker, List<EnemyItem2D> enemiesInRange)
        {
            if (attacker == null || enemiesInRange == null || enemiesInRange.Count == 0) return;
            
            Vector2Int attackerPosition = attacker.GetAttackerGridPosition();
            
            var validEnemies = enemiesInRange
                .Where(e => e != null && e.IsAlive && e.GridPosition.y >= attackerPosition.y)
                .OrderBy(e => e.GridPosition.y)
                .ToList();
            
            if (validEnemies.Count > 0)
            {
                EnemyItem2D target = validEnemies.FirstOrDefault();
                if (target != null)
                {
                    attacker.CreateProjectile(target);
                }
            }
        }
    }
}

