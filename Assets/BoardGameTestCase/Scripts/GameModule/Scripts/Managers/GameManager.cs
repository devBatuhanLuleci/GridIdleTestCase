using UnityEngine;
using BoardGameTestCase.Core.Common;
using GameModule.Core.Interfaces;
using InventoryModule;
using BoardGameTestCase.Core.ScriptableObjects;
using Sirenix.OdinInspector;

namespace GameModule.Managers
{
    public class GameManager : MonoBehaviour, IInitializable
    {
        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;        [SerializeField] private StateManager _stateManager;        [ReadOnly, ShowInInspector]
        private GameState CurrentGameStateDisplay => CurrentGameState;
        
        public GameState CurrentGameState => _gameFlowService?.CurrentGameState ?? GameState.Placing;
        
        private LevelManager _levelManager;
        private DefenceItemInventoryManager _inventoryManager;
        private IItemDataProvider _itemDataProviderService;
        private IGameFlowController _gameFlowService;
        
        public LevelManager LevelManager => _levelManager;
        public DefenceItemInventoryManager InventoryManager => _inventoryManager;
        public IItemDataProvider ItemDataProvider => _itemDataProviderService;
        public IGameFlowController GameFlowController => _gameFlowService;
        
        private void Awake()
        {
            ServiceLocator.Instance.Register<GameManager>(this);
        }
        
        private void OnDestroy()
        {
            ServiceLocator.Instance?.Unregister<GameManager>();
        }
        
        public void Initialize()
        {
            if (_isInitialized) return;
            _levelManager = ServiceLocator.Instance.Get<LevelManager>();
            _inventoryManager = ServiceLocator.Instance.Get<DefenceItemInventoryManager>();
            
            _itemDataProviderService = ServiceLocator.Instance.Get<IItemDataProvider>();
            _gameFlowService = ServiceLocator.Instance.Get<IGameFlowController>();
            
            if (_itemDataProviderService is IInitializable itemDataInit) itemDataInit.Initialize();
            if (_gameFlowService is IInitializable gameFlowInit) gameFlowInit.Initialize();
            RefreshInventoryFromLevel();
            _isInitialized = true;
        }
        
        public void RefreshInventoryFromLevel()
        {
            _inventoryManager?.RefreshInventoryFromLevel();
        }
        
        public DefenceItemData GetItemDataById(string itemId) => _itemDataProviderService?.GetItemDataById(itemId);
        
        public int GetItemQuantityById(string itemId) => _itemDataProviderService?.GetItemQuantityById(itemId) ?? 0;
        
        public Sprite GetItemSpriteById(string itemId) => _itemDataProviderService?.GetItemSpriteById(itemId);
        
        public void StartGame() => _gameFlowService?.StartGame();
        public void StartFight() => _gameFlowService?.StartFight();
        public void EndFight() => _gameFlowService?.EndFight();
        public void SetWin() => _gameFlowService?.SetWin();
        public void SetLose() => _gameFlowService?.SetLose();
        public void RestartGame() => _gameFlowService?.RestartGame();
    }
}

