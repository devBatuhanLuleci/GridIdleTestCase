using System.Collections.Generic;
using UnityEngine;
using BoardGameTestCase.Core.Common;
using BoardGameTestCase.Core.ScriptableObjects;

namespace GameplayModule
{
    public class EnemyFactory : MonoBehaviour
    {
        [SerializeField] private EnemyItem2D _enemyPrefab;
        
        // Object Pooling storage
        private Dictionary<string, Queue<EnemyItem2D>> _enemyPool = new Dictionary<string, Queue<EnemyItem2D>>();

        private void Awake()
        {
            ServiceLocator.Instance.Register<EnemyFactory>(this);
        }
        
        private void OnDestroy()
        {
            ServiceLocator.Instance?.Unregister<EnemyFactory>();
        }
        
        public EnemyItem2D CreateEnemy(EnemyData enemyData, Vector3 position, Transform parent = null)
        {
            if (enemyData == null) return null;
            
            EnemyItem2D enemyInstance = null;
            string poolKey = enemyData.EnemyId;

            // Check if we have an available enemy in the pool
            if (_enemyPool.ContainsKey(poolKey) && _enemyPool[poolKey].Count > 0)
            {
                enemyInstance = _enemyPool[poolKey].Dequeue();
                enemyInstance.transform.SetParent(parent);
                enemyInstance.transform.position = position;
                enemyInstance.ResetState();
            }
            else
            {
                // Instantiate new one if pool is empty
                if (_enemyPrefab != null)
                {
                    enemyInstance = Instantiate(_enemyPrefab, position, Quaternion.identity, parent);
                }
                else
                {
                    GameObject enemyObj = new GameObject($"Enemy_{enemyData.EnemyId}");
                    enemyInstance = enemyObj.AddComponent<EnemyItem2D>();
                    enemyObj.transform.SetParent(parent);
                    enemyObj.transform.position = position;
                }
                
                // Subscribe to recycle event to return it back to pool
                enemyInstance.OnRecycle += (item) => ReturnToPool(poolKey, item);
            }
            
            enemyInstance.SetEnemyData(enemyData);
            return enemyInstance;
        }

        private void ReturnToPool(string key, EnemyItem2D enemy)
        {
            if (!_enemyPool.ContainsKey(key))
            {
                _enemyPool[key] = new Queue<EnemyItem2D>();
            }
            
            _enemyPool[key].Enqueue(enemy);
        }
        
        public void SetEnemyPrefab(EnemyItem2D prefab)
        {
            _enemyPrefab = prefab;
        }
    }
}
