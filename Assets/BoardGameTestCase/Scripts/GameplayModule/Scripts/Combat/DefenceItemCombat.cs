using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BoardGameTestCase.Core.ScriptableObjects;
using GameplayModule.Strategies;
using GridSystemModule.Core.Interfaces;
using BoardGameTestCase.Core.Common;
using GameModule.Core.Interfaces;
using GameState = GameModule.Core.Interfaces.GameState;

namespace GameplayModule
{
    public class DefenceItemCombat : MonoBehaviour, ICombatInitializable
    {
        private IPlaceable _placeable;
        
        private DefenceItemData _defenceItemData;
        private IAttackStrategy _attackStrategy;
        private CombatTargetFinder _targetFinder;
        private IProjectileFactory _projectileFactory;
        private IEnemySpawner _enemySpawner;
        private IGameFlowController _gameFlowController;
        private Coroutine _attackCoroutine;
        private bool _isCombatActive = false;
        private Vector2Int _cachedAttackerPosition;
        private bool _attackerPositionCached = false;
        
        private void Awake()
        {
            _targetFinder = new CombatTargetFinder();
            ServiceLocator.Instance.Register<ICombatInitializable>(this);
        }
        
        private void OnDestroy()
        {
            ServiceLocator.Instance?.Unregister<ICombatInitializable>(this);
            StopAttacking();
        }
        
        private void Start()
        {
            _projectileFactory = ServiceLocator.Instance?.Get<IProjectileFactory>();
            _enemySpawner = ServiceLocator.Instance?.Get<IEnemySpawner>();
            _gameFlowController = ServiceLocator.Instance?.Get<IGameFlowController>();
            
            InitializeAttackStrategy();
            
            if (_gameFlowController != null && _gameFlowController.CurrentGameState == GameState.Fight)
            {
                StartCombat();
            }
        }
        
        
        private void InitializeAttackStrategy()
        {
            if (_defenceItemData == null) return;
            
            // Default to Forward since AttackDirection was removed from data
            AttackDirection direction = AttackDirection.Forward; 
            switch (direction)
            {
                case AttackDirection.Forward:
                    _attackStrategy = new ForwardAttackStrategy();
                    break;
                case AttackDirection.All:
                    _attackStrategy = new AllDirectionsAttackStrategy();
                    break;
                default:
                    _attackStrategy = new ForwardAttackStrategy();
                    break;
            }
        }
        
        public void StartCombat()
        {
            if (_isCombatActive) return;
            
            if (_placeable == null)
            {
                return;
            }
            
            if (_defenceItemData == null)
            {
                return;
            }
            
            _isCombatActive = true;
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
            }
            
            _attackCoroutine = StartCoroutine(AttackLoop());
        }
        
        public void StopCombat()
        {
            _isCombatActive = false;
            StopAttacking();
        }
        
        private void StopAttacking()
        {
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                _attackCoroutine = null;
            }
        }
        
        private IEnumerator AttackLoop()
        {
            while (_isCombatActive)
            {
                if (_gameFlowController != null && _gameFlowController.CurrentGameState != GameState.Fight)
                {
                    _isCombatActive = false;
                    yield break;
                }
                
                if (_placeable == null)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }
                
                if (!_placeable.IsPlaced)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }
                
                if (_defenceItemData == null)
                {
                    yield return new WaitForSeconds(1f);
                    continue;
                }
                
                if (_enemySpawner == null)
                {
                    _enemySpawner = ServiceLocator.Instance?.Get<IEnemySpawner>();
                    if (_enemySpawner == null)
                    {
                        yield return new WaitForSeconds(1f);
                        continue;
                    }
                }
                
                if (_placeable != null && _placeable.IsPlaced)
                {
                    Vector2Int currentPos = _placeable.GridPosition;
                    if (!_attackerPositionCached || currentPos != _cachedAttackerPosition)
                    {
                        _cachedAttackerPosition = currentPos;
                        _attackerPositionCached = true;
                    }
                }
                
                if (!_attackerPositionCached)
                {
                    yield return new WaitForSeconds(0.1f);
                    continue;
                }
                
                List<EnemyItem2D> enemiesInRange = _targetFinder.FindEnemiesInRange(
                    _cachedAttackerPosition, 
                    2.0f, // Default Range
                    _enemySpawner
                );
                
                if (enemiesInRange != null && enemiesInRange.Count > 0)
                {
                    if (_attackStrategy == null)
                    {
                        InitializeAttackStrategy();
                    }
                    _attackStrategy?.Attack(this, enemiesInRange);
                }
                
                float attackInterval = 1.0f; // Default Attack Interval
                yield return new WaitForSeconds(attackInterval);
            }
        }
        
        public void CreateProjectile(EnemyItem2D target)
        {
            if (_defenceItemData == null || target == null || !target.IsAlive) return;
            if (_placeable == null || !_placeable.IsPlaced) return;
            
            if (_projectileFactory == null)
            {
                _projectileFactory = ServiceLocator.Instance?.Get<IProjectileFactory>();
                if (_projectileFactory == null)
                {
                    Debug.LogWarning("DefenceItemCombat: IProjectileFactory not found in ServiceLocator.");
                    return;
                }
            }
            
            Vector3 spawnPosition = transform.position;
            int damage = _defenceItemData.Damage;
            float speed = 15f;
            
            var projectile = _projectileFactory.CreateProjectile(spawnPosition, damage, speed, target as IEnemy);
            if (projectile == null)
            {
                Debug.LogWarning($"DefenceItemCombat: Failed to create projectile at {spawnPosition} for target {target.name}");
            }
        }
        
        public void SetDefenceItemData(DefenceItemData data)
        {
            _defenceItemData = data;
            InitializeAttackStrategy();
        }
        
        public void SetPlaceable(IPlaceable placeable)
        {
            _placeable = placeable;
        }
        
        public Vector2Int GetAttackerGridPosition()
        {
            return _placeable != null && _placeable.IsPlaced ? _placeable.GridPosition : Vector2Int.zero;
        }
    }
}

