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
        
        public IEnemy SpawnEnemy(EnemyData enemyData)
        {
            if (enemyData == null || _enemyFactory == null || _placementSystem == null) return null;
            
            if (_gameFlowController != null && _gameFlowController.CurrentGameState != GameState.Fight)
            {
                return null;
            }
            
            Vector2Int gridDimensions = _placementSystem.GridDimensions;
            int randomX = Random.Range(0, gridDimensions.x);
            
            Vector2Int spawnGridPos = FindAvailableSpawnPosition(randomX, gridDimensions);
            if (spawnGridPos.y < 0)
            {
                return null;
            }
            
            Vector3 spawnWorldPos = _placementSystem.GridToWorld(spawnGridPos);
            
            EnemyItem2D enemy = _enemyFactory.CreateEnemy(enemyData, spawnWorldPos);
            if (enemy == null) return null;
            
            Vector2Int baseGridPos = new Vector2Int(spawnGridPos.x, 0);
            Vector3 baseWorldPos = GetTileCenterPosition(baseGridPos);
            
            var movementController = enemy.gameObject.AddComponent<EnemyMovementController>();
            movementController.Initialize(enemy, baseWorldPos, OnEnemyReachBase);
            enemy.SetMovementController(movementController);
            
            enemy.OnPlaced(spawnGridPos);
            enemy.OnDeath += OnEnemyDeath;
            
            _spawnedEnemies.Add(enemy);
            movementController.StartMovement();
            
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
                _spawnedEnemies.Remove(enemy);
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
                if (enemy != null && enemy.gameObject != null)
                {
                    Destroy(enemy.gameObject);
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

