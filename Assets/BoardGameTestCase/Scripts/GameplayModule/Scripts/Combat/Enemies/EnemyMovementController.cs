using System;
using UnityEngine;
using DG.Tweening;
using GridSystemModule.Core.Interfaces;
using BoardGameTestCase.Core.Common;

namespace GameplayModule
{
    public class EnemyMovementController : MonoBehaviour
    {
        [SerializeField] private EnemyItem2D _enemyItem;
        [SerializeField] private Vector3 _basePosition;
        
        private Sequence _movementSequence;
        private IGridPlacementSystem _placementSystem;
        private Action<EnemyItem2D> _onReachBase;
        private bool _isMoving = false;
        
        public bool IsMoving => _isMoving;
        
        private void Awake()
        {
            if (_enemyItem == null) _enemyItem = GetComponent<EnemyItem2D>();
            FindPlacementSystem();
        }
        
        private void FindPlacementSystem()
        {
            _placementSystem = ServiceLocator.Instance?.Get<IGridPlacementSystem>();
        }
        
        private void OnDestroy()
        {
            if (_movementSequence != null)
            {
                _movementSequence.Kill();
            }
        }
        
        public void Initialize(EnemyItem2D enemyItem, Vector3 basePosition, Action<EnemyItem2D> onReachBase = null)
        {
            _enemyItem = enemyItem;
            _basePosition = basePosition;
            _onReachBase = onReachBase;
            
            if (_placementSystem == null)
            {
                FindPlacementSystem();
            }
        }
        
        public void StartMovement()
        {
            if (_enemyItem == null || !_enemyItem.IsAlive) return;
            if (_isMoving) return;
            
            if (_placementSystem == null)
            {
                FindPlacementSystem();
            }
            
            Vector3 startPosition = transform.position;
            float distance = Vector3.Distance(startPosition, _basePosition);
            
            if (_enemyItem.Speed <= 0) return;
            
            float duration = distance / _enemyItem.Speed;
            
            _movementSequence = DOTween.Sequence();
            _movementSequence.Append(transform.DOMove(_basePosition, duration).SetEase(Ease.Linear));
            
            _movementSequence.OnUpdate(() =>
            {
                CheckPosition();
            });
            
            _movementSequence.OnComplete(() =>
            {
                OnReachBase();
            });
            
            _isMoving = true;
        }
        
        public void StopMovement()
        {
            if (_movementSequence != null)
            {
                _movementSequence.Kill();
                _movementSequence = null;
            }
            _isMoving = false;
        }
        
        private void CheckPosition()
        {
            if (_enemyItem == null || !_enemyItem.IsAlive) return;
            
            float distanceToBase = Vector3.Distance(transform.position, _basePosition);
            float threshold = 0.1f;
            
            if (distanceToBase <= threshold)
            {
                transform.position = _basePosition;
                OnReachBase();
            }
        }
        
        private Vector2Int GetCurrentGridPosition()
        {
            if (_placementSystem != null)
            {
                return _placementSystem.WorldToGrid(transform.position);
            }
            
            return new Vector2Int(
                Mathf.RoundToInt(transform.position.x),
                Mathf.RoundToInt(transform.position.y)
            );
        }
        
        private void OnReachBase()
        {
            if (!_isMoving) return;
            _isMoving = false;
            
            if (_movementSequence != null)
            {
                _movementSequence.Kill();
                _movementSequence = null;
            }
            
            _onReachBase?.Invoke(_enemyItem);
        }
        
        public void SetBasePosition(Vector3 basePosition)
        {
            _basePosition = basePosition;
        }
    }
}

