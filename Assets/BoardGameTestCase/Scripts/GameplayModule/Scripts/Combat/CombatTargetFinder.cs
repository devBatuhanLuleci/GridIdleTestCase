using System.Collections.Generic;
using UnityEngine;
using BoardGameTestCase.Core.Common;
using GridSystemModule.Core.Interfaces;
using GameModule.Core.Interfaces;

namespace GameplayModule
{
    public class CombatTargetFinder
    {
        private IGridPlacementSystem _placementSystem;
        private readonly Dictionary<EnemyItem2D, Vector2Int> _enemyGridPositionCache = new Dictionary<EnemyItem2D, Vector2Int>();
        private float _lastCacheClearTime = 0f;
        private const float CACHE_CLEAR_INTERVAL = 0.5f;
        
        public CombatTargetFinder()
        {
            _placementSystem = ServiceLocator.Instance?.Get<IGridPlacementSystem>();
        }
        
        public List<EnemyItem2D> FindEnemiesInRange(Vector2Int attackerGridPosition, float range, IEnemySpawner enemySpawner)
        {
            List<EnemyItem2D> enemiesInRange = new List<EnemyItem2D>();
            
            if (enemySpawner == null || enemySpawner.SpawnedEnemies == null) return enemiesInRange;
            
            float currentTime = Time.time;
            if (currentTime - _lastCacheClearTime >= CACHE_CLEAR_INTERVAL)
            {
                _enemyGridPositionCache.Clear();
                _lastCacheClearTime = currentTime;
            }
            
            int rangeInt = Mathf.CeilToInt(range);
            int minX = attackerGridPosition.x - rangeInt;
            int maxX = attackerGridPosition.x + rangeInt;
            int minY = attackerGridPosition.y - rangeInt;
            int maxY = attackerGridPosition.y + rangeInt;
            
            foreach (var enemyInterface in enemySpawner.SpawnedEnemies)
            {
                if (enemyInterface == null || !enemyInterface.IsAlive) continue;
                
                var enemy = enemyInterface as EnemyItem2D;
                if (enemy == null) continue;
                
                Vector2Int enemyGridPos = GetEnemyGridPosition(enemy);
                
                if (enemyGridPos.x < minX || enemyGridPos.x > maxX || 
                    enemyGridPos.y < minY || enemyGridPos.y > maxY)
                {
                    continue;
                }
                
                float distance = GetGridDistance(attackerGridPosition, enemyGridPos);
                
                if (distance <= range)
                {
                    enemiesInRange.Add(enemy);
                }
            }
            
            return enemiesInRange;
        }
        
        public Vector2Int GetEnemyGridPosition(EnemyItem2D enemy)
        {
            if (enemy == null) return Vector2Int.zero;
            
            if (_enemyGridPositionCache.TryGetValue(enemy, out Vector2Int cachedPos))
            {
                return cachedPos;
            }
            
            Vector2Int gridPos;
            if (_placementSystem != null)
            {
                gridPos = _placementSystem.WorldToGrid(enemy.transform.position);
            }
            else
            {
                gridPos = enemy.GridPosition;
            }
            
            _enemyGridPositionCache[enemy] = gridPos;
            return gridPos;
        }
        
        public float GetGridDistance(Vector2Int pos1, Vector2Int pos2)
        {
            int dx = Mathf.Abs(pos2.x - pos1.x);
            int dy = Mathf.Abs(pos2.y - pos1.y);
            return Mathf.Max(dx, dy);
        }
    }
}

