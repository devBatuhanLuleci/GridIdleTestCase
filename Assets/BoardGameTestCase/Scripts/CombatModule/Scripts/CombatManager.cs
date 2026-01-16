using System.Collections.Generic;
using UnityEngine;
using BoardGameTestCase.Core.Common;
using GameModule.Core.Interfaces;

namespace CombatModule
{
    public class CombatManager : MonoBehaviour, IInitializable, ICombatManager
    {
        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;
        
        private IEnemySpawner _enemySpawner;
        private IGameFlowController _gameFlowController;
        private IStateController _stateController;
        private List<IEnemy> _activeEnemies = new List<IEnemy>();
        private bool _combatActive = false;
        
        public IReadOnlyList<IEnemy> ActiveEnemies => _activeEnemies;
        public int ActiveEnemyCount => _activeEnemies.Count;
        
        private void Awake()
        {
            ServiceLocator.Instance.Register<CombatManager>(this);
            ServiceLocator.Instance.Register<ICombatManager>(this);
        }
        
        private void OnDestroy()
        {
            if (_stateController != null) _stateController.OnStateChanged -= OnStateChanged;
            ServiceLocator.Instance?.Unregister<CombatManager>();
            ServiceLocator.Instance?.Unregister<ICombatManager>();
        }
        
        public void Initialize()
        {
            if (_isInitialized) return;
            _enemySpawner = ServiceLocator.Instance.Get<IEnemySpawner>();
            _gameFlowController = ServiceLocator.Instance.Get<IGameFlowController>();
            _stateController = ServiceLocator.Instance.Get<IStateController>();
            
            if (_stateController != null)
            {
                _stateController.OnStateChanged += OnStateChanged;
            }
            
            if (_enemySpawner != null)
            {
                foreach (var enemy in _enemySpawner.SpawnedEnemies)
                {
                    if (enemy != null && enemy.IsAlive)
                    {
                        RegisterEnemy(enemy);
                    }
                }
            }
            
            _isInitialized = true;
        }
        
        private void OnStateChanged(GameState newState)
        {
            if (newState == GameState.Fight)
            {
                StartCombat();
            }
            else
            {
                StopCombat();
            }
        }
        
        public void StartCombat()
        {
            if (_combatActive) return;
            _combatActive = true;
            
            if (_enemySpawner != null)
            {
                _enemySpawner.SpawnAllEnemies();
                UpdateActiveEnemiesList();
            }
            
            InitializeCombatForDefenceItems();
        }
        
        private void InitializeCombatForDefenceItems()
        {
            var combatInitializables = ServiceLocator.Instance.GetAll<ICombatInitializable>();
            if (combatInitializables != null)
            {
                foreach (var combatInit in combatInitializables)
                {
                    if (combatInit != null)
                    {
                        combatInit.StartCombat();
                    }
                }
            }
        }
        
        public void StopCombat()
        {
            _combatActive = false;
        }
        
        public void RegisterEnemy(IEnemy enemy)
        {
            if (enemy == null || _activeEnemies.Contains(enemy)) return;
            
            _activeEnemies.Add(enemy);
            enemy.OnDeath += OnEnemyDeath;
        }
        
        public void UnregisterEnemy(IEnemy enemy)
        {
            if (enemy == null || !_activeEnemies.Contains(enemy)) return;
            
            enemy.OnDeath -= OnEnemyDeath;
            _activeEnemies.Remove(enemy);
            
            CheckWinCondition();
        }
        
        private void OnEnemyDeath(IEnemy enemy)
        {
            UnregisterEnemy(enemy);
        }
        
        private void UpdateActiveEnemiesList()
        {
            _activeEnemies.Clear();
            if (_enemySpawner != null)
            {
                foreach (var enemy in _enemySpawner.SpawnedEnemies)
                {
                    if (enemy != null && enemy.IsAlive)
                    {
                        RegisterEnemy(enemy);
                    }
                }
            }
        }
        
        private void CheckWinCondition()
        {
            if (!_combatActive) return;
            if (_activeEnemies.Count == 0)
            {
                if (_gameFlowController != null)
                {
                    _gameFlowController.SetWin();
                }
            }
        }
        
        private void Update()
        {
            if (_combatActive)
            {
                for (int i = _activeEnemies.Count - 1; i >= 0; i--)
                {
                    if (_activeEnemies[i] == null || !_activeEnemies[i].IsAlive)
                    {
                        _activeEnemies.RemoveAt(i);
                    }
                }
                
                if (_activeEnemies.Count == 0 && _combatActive)
                {
                    CheckWinCondition();
                }
            }
        }
    }
}

