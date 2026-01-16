using System.Collections.Generic;
using UnityEngine;
using BoardGameTestCase.Core.Common;
using GameModule.Core.Interfaces;
using GameModule.Core;
using GridSystemModule.Managers;

namespace GameModule.Services
{
    public class GameFlowController : MonoBehaviour, IGameFlowController, IInitializable
    {
        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;        [SerializeField] private bool _autoStartGame = false;

        private IStateController _stateController;
        private IInventoryManager _inventoryManager;
        private ICombatManager _combatManager;

        public GameState CurrentGameState => _stateController?.CurrentState ?? GameState.Placing;

        private void Awake()
        {
            ServiceLocator.Instance.Register<IGameFlowController>(this);
        }

        private void OnDestroy()
        {
            if (_stateController != null) _stateController.OnStateChanged -= OnStateChanged;
            ServiceLocator.Instance?.Unregister<IGameFlowController>();
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            if (_stateController == null) _stateController = ServiceLocator.Instance.Get<IStateController>();
            _inventoryManager = ServiceLocator.Instance.Get<IInventoryManager>();
            _combatManager = ServiceLocator.Instance.Get<ICombatManager>();
            if (_stateController != null) _stateController.OnStateChanged += OnStateChanged;
            _isInitialized = true;
        }

        private void Start()
        {
            if (_autoStartGame) StartGame();
        }

        public void StartGame()
        {
            _stateController?.SetPlacing();
            _inventoryManager?.RefreshInventoryFromLevel();
        }

        public void StartFight()
        {
            if (_stateController == null || CurrentGameState != GameState.Placing) return;
            _stateController.SetFight();
        }

        public void EndFight()
        {
            if (_stateController == null || CurrentGameState != GameState.Fight) return;
            _stateController.SetPlacing();
        }

        public void SetWin()
        {
            _stateController?.SetWin();
            EventBus.Instance.Publish(new GameEndedEvent(true));
        }

        public void SetLose()
        {
            _stateController?.SetLose();
            EventBus.Instance.Publish(new GameEndedEvent(false));
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;
            
            var placementManager = ServiceLocator.Instance?.Get<PlacementManager>();
            if (placementManager != null)
            {
                placementManager.ClearAll();
            }
            
            var enemySpawner = ServiceLocator.Instance?.Get<IEnemySpawner>();
            if (enemySpawner != null)
            {
                enemySpawner.ClearAllEnemies();
            }
            
            ClearAllSceneItems();
            
            var gridManager = ServiceLocator.Instance?.Get<GridManager>();
            if (gridManager != null)
            {
                gridManager.ClearAndRegenerateGrid();
            }
            
            _combatManager?.StopCombat();
            
            StartGame();
        }
        
        private void ClearAllSceneItems()
        {
            var gridManager = ServiceLocator.Instance?.Get<GridManager>();
            if (gridManager == null || gridManager.TilesParent == null) return;
            
            var allTiles = gridManager.GetAllTiles();
            if (allTiles == null) return;
            
            var itemsToDestroy = new List<GameObject>();
            
            foreach (var tile in allTiles.Values)
            {
                if (tile == null || tile.transform == null) continue;
                
                CollectTileChildren(tile.transform, itemsToDestroy);
            }
            
            foreach (var item in itemsToDestroy)
            {
                if (item != null)
                {
                    Object.Destroy(item);
                }
            }
        }
        
        private void CollectTileChildren(Transform tileTransform, List<GameObject> itemsToDestroy)
        {
            if (tileTransform == null) return;
            
            var childrenToRemove = new List<Transform>();
            
            for (int i = 0; i < tileTransform.childCount; i++)
            {
                Transform child = tileTransform.GetChild(i);
                if (child == null) continue;
                
                childrenToRemove.Add(child);
            }
            
            foreach (var child in childrenToRemove)
            {
                if (child != null && child.gameObject != null)
                {
                    itemsToDestroy.Add(child.gameObject);
                }
            }
        }

        private void OnStateChanged(GameState newState)
        {
        }
    }
}

