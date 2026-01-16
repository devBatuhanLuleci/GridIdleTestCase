using UnityEngine;
using BoardGameTestCase.Core.Common;
using BoardGameTestCase.Core.ScriptableObjects;

namespace GameplayModule
{
    public class EnemyFactory : MonoBehaviour
    {
        [SerializeField] private EnemyItem2D _enemyPrefab;
        
        private void Awake()
        {
            ServiceLocator.Instance.Register<EnemyFactory>(this);
        }
        
        private void OnDestroy()
        {
            ServiceLocator.Instance?.Unregister<EnemyFactory>();
        }
        
        public EnemyItem2D CreateEnemy(EnemyData enemyData, Vector3 position)
        {
            if (enemyData == null) return null;
            
            EnemyItem2D enemyInstance;
            
            if (_enemyPrefab != null)
            {
                enemyInstance = Instantiate(_enemyPrefab, position, Quaternion.identity);
            }
            else
            {
                GameObject enemyObj = new GameObject($"Enemy_{enemyData.EnemyId}");
                enemyInstance = enemyObj.AddComponent<EnemyItem2D>();
                enemyObj.transform.position = position;
            }
            
            enemyInstance.SetEnemyData(enemyData);
            
            return enemyInstance;
        }
        
        public void SetEnemyPrefab(EnemyItem2D prefab)
        {
            _enemyPrefab = prefab;
        }
    }
}

