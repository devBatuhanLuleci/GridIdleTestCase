using System.Collections.Generic;
using UnityEngine;
using BoardGameTestCase.Core.Common;
using GameModule.Core.Interfaces;
using GameModule.Services;
using GameplayModule;
using UISystemModule.UIElements;
using UISystemModule.Managers;
using InventoryModule;
using CombatModule;
using GridSystemModule.Managers;
using GridSystemModule.Core.Interfaces;
using PlacementModule.Interfaces;

namespace GameModule.Managers
{
    public class BootstrapManager : MonoBehaviour
    {        [SerializeField] private StateManager _stateManager;
        [SerializeField] private GameManager _gameManager;
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private UIManager _uiManager;
        [SerializeField] private PlacementManager _placementManager;
        [SerializeField] private MenuManager _menuManager;
        [SerializeField] private LevelManager _levelManager;
        [SerializeField] private DefenceItemInventoryManager _inventoryManager;
        [SerializeField] private EnemyItemInventoryManager _enemyInventoryManager;
        [SerializeField] private GameplayModule.EnemyFactory _enemyFactory;
        [SerializeField] private EnemySpawner _enemySpawner;
        [SerializeField] private CombatManager _combatManager;
        [SerializeField] private ProjectileFactory _projectileFactory;        private IItemDataProvider _itemDataProvider;
        private IGameFlowController _gameFlowController;
        private IGhostObjectCreator _ghostObjectCreator;
        private IInitializable _itemDataProviderInit;
        private IInitializable _gameFlowControllerInit;
        private IInitializable _ghostObjectCreatorInit;        private IGridItemFactory _gridItemFactory;        [SerializeField] private bool _autoInitializeOnStart = true;
        [SerializeField] private bool _initializeGridOnStart = true;
        
        private bool _isInitialized = false;
        private static BootstrapManager _instance;
        public static BootstrapManager Instance => _instance;
        public bool IsInitialized => _isInitialized;
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                if (transform.parent == null) DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }
        
        private void Start()
        {
            if (_autoInitializeOnStart) InitializeAllSystems();
        }
        
        public void InitializeAllSystems()
        {
            if (_isInitialized) return;
            
            var initializationOrder = GetInitializationOrder();
            
            foreach (var initializable in initializationOrder)
            {
                if (initializable != null && !initializable.IsInitialized)
                {
                    initializable.Initialize();
                }
            }
            
            if (_gridManager != null && _initializeGridOnStart)
            {
                _gridManager.GenerateGrid();
            }
            
            _isInitialized = true;
        }
        
        private List<IInitializable> GetInitializationOrder()
        {
            var order = new List<IInitializable>();
            
            EnsureManager(ref _stateManager, ServiceLocator.Instance.Get<StateManager>);
            if (_stateManager is IInitializable stateInit) order.Add(stateInit);
            
            EnsureManager(ref _levelManager, ServiceLocator.Instance.Get<LevelManager>);
            if (_levelManager is IInitializable levelInit) order.Add(levelInit);
            
            EnsureManager(ref _inventoryManager, ServiceLocator.Instance.Get<DefenceItemInventoryManager>);
            if (_inventoryManager is IInitializable inventoryInit) order.Add(inventoryInit);
            
            EnsureManager(ref _enemyInventoryManager, ServiceLocator.Instance.Get<EnemyItemInventoryManager>);
            if (_enemyInventoryManager is IInitializable enemyInventoryInit) order.Add(enemyInventoryInit);
            
            EnsureManager(ref _enemyFactory, ServiceLocator.Instance.Get<GameplayModule.EnemyFactory>);
            
            EnsureManager(ref _projectileFactory, ServiceLocator.Instance.Get<ProjectileFactory>);
            
            EnsureManager(ref _enemySpawner, ServiceLocator.Instance.Get<EnemySpawner>);
            if (_enemySpawner is IInitializable enemySpawnerInit) order.Add(enemySpawnerInit);
            
            EnsureManager(ref _combatManager, ServiceLocator.Instance.Get<CombatManager>);
            if (_combatManager is IInitializable combatManagerInit) order.Add(combatManagerInit);
            
            var combatComponentAttacher = ServiceLocator.Instance.TryGet<ICombatComponentAttacher>() as MonoBehaviour;
            if (combatComponentAttacher != null && combatComponentAttacher is IInitializable combatAttacherInit)
            {
                order.Add(combatAttacherInit);
            }
            
            _gridItemFactory = ServiceLocator.Instance.TryGet<IGridItemFactory>();
            
            _itemDataProvider = ServiceLocator.Instance.TryGet<IItemDataProvider>();
            _itemDataProviderInit = _itemDataProvider as IInitializable;
            if (_itemDataProviderInit != null) order.Add(_itemDataProviderInit);
            
            _ghostObjectCreator = ServiceLocator.Instance.TryGet<IGhostObjectCreator>();
            _ghostObjectCreatorInit = _ghostObjectCreator as IInitializable;
            if (_ghostObjectCreatorInit != null) order.Add(_ghostObjectCreatorInit);
            
            _gameFlowController = ServiceLocator.Instance.TryGet<IGameFlowController>();
            _gameFlowControllerInit = _gameFlowController as IInitializable;
            if (_gameFlowControllerInit != null) order.Add(_gameFlowControllerInit);
            
            EnsureManager(ref _gridManager, ServiceLocator.Instance.Get<GridManager>);
            if (_gridManager is IInitializable gridInit) order.Add(gridInit);
            
            EnsureManager(ref _placementManager, ServiceLocator.Instance.Get<PlacementManager>);
            if (_placementManager is IInitializable placementInit) order.Add(placementInit);
            
            EnsureManager(ref _gameManager, ServiceLocator.Instance.Get<GameManager>);
            if (_gameManager is IInitializable gameInit) order.Add(gameInit);
            
            EnsureManager(ref _uiManager, ServiceLocator.Instance.Get<UIManager>);
            if (_uiManager is IInitializable uiInit) order.Add(uiInit);
            
            EnsureManager(ref _menuManager, ServiceLocator.Instance.Get<MenuManager>);
            if (_menuManager is IInitializable menuInit) order.Add(menuInit);
            
            return order;
        }
        
        private void EnsureManager<T>(ref T manager, System.Func<T> getFromServiceLocator) where T : MonoBehaviour
        {
            if (manager == null)
            {
                manager = getFromServiceLocator?.Invoke();
            }
        }
        
        public T GetManager<T>() where T : class => ServiceLocator.Instance?.Get<T>();
        
        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
    }
}

