using System.Collections.Generic;
using UnityEngine;
using BoardGameTestCase.Core.Common;
using BoardGameTestCase.Core.ScriptableObjects;
using GameModule.Core.Interfaces;
using GameplayModule;
using InventoryModule;
using GridSystemModule.Core.Interfaces;
using GridSystemModule.Core.Models;
using GridSystemModule.Managers;
using EnemyItem2D = GameplayModule.EnemyItem2D;
using EnemyMovementController = GameplayModule.EnemyMovementController;

namespace CombatModule
{
    public class EnemySpawner : MonoBehaviour, IInitializable, IEnemySpawner
    {
        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;
        
        private EnemyItemInventoryManager _enemyInventoryManager;
        private GameplayModule.EnemyFactory _enemyFactory;
        private ILevelDataProvider _levelDataProvider;
        private IGridPlacementSystem _placementSystem;
        private IGameFlowController _gameFlowController;
        
        [SerializeField] private List<Transform> _spawnPoints = new List<Transform>();
        [SerializeField] private float _respawnDelay = 1.5f;
        private List<EnemyItem2D> _spawnedEnemies = new List<EnemyItem2D>();
        
        public IReadOnlyList<IEnemy> SpawnedEnemies
        {
            get
            {
                var result = new List<IEnemy>();
                foreach (var enemy in _spawnedEnemies)
                {
                    if (enemy != null) result.Add(enemy as IEnemy);
                }
                return result;
            }
        }
        
        public int ActiveEnemyCount => _spawnedEnemies.Count;
        
        private void Awake()
        {
            ServiceLocator.Instance.Register<EnemySpawner>(this);
            ServiceLocator.Instance.Register<IEnemySpawner>(this);
        }
        
        private void OnDestroy()
        {
            ServiceLocator.Instance?.Unregister<EnemySpawner>();
            ServiceLocator.Instance?.Unregister<IEnemySpawner>();
        }
        
        public void Initialize()
        {
            if (_isInitialized) return;
            _enemyInventoryManager = ServiceLocator.Instance.Get<EnemyItemInventoryManager>();
            _enemyFactory = ServiceLocator.Instance.Get<GameplayModule.EnemyFactory>();
            _levelDataProvider = ServiceLocator.Instance.Get<ILevelDataProvider>();
            _gameFlowController = ServiceLocator.Instance.Get<IGameFlowController>();
            
            var placementManager = ServiceLocator.Instance.Get<PlacementManager>();
            if (placementManager != null)
            {
                _placementSystem = placementManager.GridPlacementSystem;
            }
            
            _isInitialized = true;
            
            // Spawn enemies at game start as requested
            SpawnAllEnemies();
        }
        
        public void SpawnAllEnemies()
        {
            if (_levelDataProvider == null || _levelDataProvider.CurrentLevel == null) return;
            
            ClearAllEnemies();
            
            var currentLevel = _levelDataProvider.CurrentLevel;
            foreach (var enemyEntry in currentLevel.Enemies)
            {
                if (enemyEntry.EnemyData == null) continue;
                
                for (int i = 0; i < enemyEntry.Quantity; i++)
                {
                    SpawnEnemy(enemyEntry.EnemyData);
                }
            }
        }
        
        public IEnemy SpawnEnemy(EnemyData enemyData, UnityEngine.Transform manualParent = null)
        {
            if (enemyData == null || _enemyFactory == null) return null;
            
            Transform spawnParent = manualParent;
            Vector3 spawnWorldPos = manualParent != null ? manualParent.position : Vector3.zero;

            if (manualParent == null)
            {
                if (_spawnPoints != null && _spawnPoints.Count > 0)
                {
                    // Simple distribution: current index % spawn point count
                    int spawnIndex = _spawnedEnemies.Count % _spawnPoints.Count;
                    Transform spawnPoint = _spawnPoints[spawnIndex];
                    spawnWorldPos = spawnPoint.position;
                    spawnParent = spawnPoint; // Spawn inside the spawn point itself
                }
                else if (_placementSystem != null)
                {
                    // Fallback to old grid logic if no points assigned
                    Vector2Int gridDimensions = _placementSystem.GridDimensions;
                    int randomX = Random.Range(0, gridDimensions.x);
                    Vector2Int spawnGridPos = FindAvailableSpawnPosition(randomX, gridDimensions);
                    spawnWorldPos = _placementSystem.GridToWorld(spawnGridPos);
                }
            }
            
            EnemyItem2D enemy = _enemyFactory.CreateEnemy(enemyData, spawnWorldPos, spawnParent);
            if (enemy == null) return null;
            
            enemy.OnDeath += OnEnemyDeath;
            _spawnedEnemies.Add(enemy);
            
            return enemy as IEnemy;
        }
        
        private Vector2Int FindAvailableSpawnPosition(int x, Vector2Int gridDimensions)
        {
            const int MAX_BACK_SPAWN_OFFSET = 10;
            const int SPAWN_START_OFFSET = 1;
            int startY = gridDimensions.y + SPAWN_START_OFFSET;
            
            for (int offset = 0; offset <= MAX_BACK_SPAWN_OFFSET; offset++)
            {
                int checkY = startY + offset;
                Vector2Int checkPos = new Vector2Int(x, checkY);
                
                if (!IsPositionOccupied(checkPos))
                {
                    return checkPos;
                }
            }
            
            return new Vector2Int(x, startY + MAX_BACK_SPAWN_OFFSET);
        }
        
        private bool IsPositionOccupied(Vector2Int gridPos)
        {
            foreach (var enemy in _spawnedEnemies)
            {
                if (enemy == null || !enemy.IsAlive) continue;
                
                Vector2Int enemyPos = enemy.GridPosition;
                if (enemyPos.x == gridPos.x && enemyPos.y == gridPos.y)
                {
                    return true;
                }
            }
            
            return false;
        }
        
        private void OnEnemyDeath(EnemyItem2D enemy)
        {
            if (enemy != null && _spawnedEnemies.Contains(enemy))
            {
                Transform dyingParent = enemy.transform.parent;
                _spawnedEnemies.Remove(enemy);
                StartCoroutine(RespawnWithDelay(dyingParent));
            }
        }

        private System.Collections.IEnumerator RespawnWithDelay(Transform parent)
        {
            yield return new WaitForSeconds(_respawnDelay);

            // Spawn a new one when one dies as requested
            if (_levelDataProvider != null && _levelDataProvider.CurrentLevel != null)
            {
                var enemies = _levelDataProvider.CurrentLevel.Enemies;
                if (enemies.Count > 0)
                {
                    // Spawn a random enemy type from the current level data
                    int randomIndex = Random.Range(0, enemies.Count);
                    SpawnEnemy(enemies[randomIndex].EnemyData, parent);
                }
            }
        }
        
        private void OnEnemyReachBase(EnemyItem2D enemy)
        {
            if (_gameFlowController != null)
            {
                _gameFlowController.SetLose();
            }
        }
        
        public void ClearAllEnemies()
        {
            var enemiesToDestroy = new List<EnemyItem2D>(_spawnedEnemies);
            _spawnedEnemies.Clear();
            
            foreach (var enemy in enemiesToDestroy)
            {
                if (enemy != null)
                {
                    enemy.Recycle();
                }
            }
        }
        
        private Vector3 GetTileCenterPosition(Vector2Int gridPos)
        {
            if (_placementSystem == null) return Vector3.zero;
            
            var gridManager = ServiceLocator.Instance?.Get<GridManager>();
            if (gridManager != null)
            {
                var tile = gridManager.GetTileAtPosition(new Vector2(gridPos.x, gridPos.y));
                if (tile != null)
                {
                    var baseTile = tile as BaseTile;
                    if (baseTile != null)
                    {
                        var collider = baseTile.GetComponent<Collider2D>();
                        if (collider != null)
                        {
                            return collider.bounds.center;
                        }
                        return baseTile.transform.position;
                    }
                }
            }
            
            return _placementSystem.GridToWorld(gridPos);
        }
    }
}

