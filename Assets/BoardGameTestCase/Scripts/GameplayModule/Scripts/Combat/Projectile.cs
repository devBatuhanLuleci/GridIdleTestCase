using System;
using UnityEngine;
using DG.Tweening;

namespace GameplayModule
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private int _damage = 10;
        [SerializeField] private float _speed = 5f;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Collider2D _collider;
        
        private EnemyItem2D _target;
        private Tween _movementTween;
        private Action<Projectile> _onHit;
        private Action<Projectile> _onDestroy;
        private float _hitDistanceThreshold = 0.15f;
        private bool _hasHit = false;
        private float _lastTargetCheckTime = 0f;
        private const float TARGET_CHECK_INTERVAL = 0.1f;
        private float _spawnTime = 0f;
        private const float MAX_LIFETIME = 5f;
        private const float MAX_DISTANCE = 50f;
        private Vector3 _spawnPosition;
        
        public int Damage => _damage;
        
        public void SetSpriteRenderer(SpriteRenderer spriteRenderer)
        {
            _spriteRenderer = spriteRenderer;
        }
        
        private void Awake()
        {
            EnsureCollider2D();
        }
        
        private void EnsureCollider2D()
        {
            if (_collider == null)
            {
                var boxCollider = gameObject.AddComponent<BoxCollider2D>();
                boxCollider.isTrigger = true;
                boxCollider.size = new Vector2(0.5f, 0.5f);
                _collider = boxCollider;
            }
            else
            {
                if (_collider is BoxCollider2D boxCollider)
                {
                    boxCollider.isTrigger = true;
                }
            }
        }
        
        private void OnDestroy()
        {
            if (_movementTween != null)
            {
                _movementTween.Kill();
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasHit) return;
            if (_target == null || !_target.IsAlive) return;
            if (other.transform == null || _target.transform == null) return;
            
            if (other.transform == _target.transform)
            {
                HitTarget();
            }
        }
        
        public void Initialize(int damage, float speed, EnemyItem2D target, Sprite sprite = null, Action<Projectile> onHit = null, Action<Projectile> onDestroy = null)
        {
            _damage = damage;
            _speed = speed;
            _target = target;
            _onHit = onHit;
            _onDestroy = onDestroy;
            _spawnTime = Time.time;
            _spawnPosition = transform.position;
            
            if (sprite != null && _spriteRenderer != null)
            {
                _spriteRenderer.sprite = sprite;
            }
            
            if (_target == null || !_target.IsAlive)
            {
                DestroyProjectile();
                return;
            }
            
            StartMovement();
        }
        
        private void StartMovement()
        {
            if (_target == null || !_target.IsAlive)
            {
                DestroyProjectile();
                return;
            }
            
            UpdateMovement();
        }
        
        private void Update()
        {
            if (_hasHit) return;
            
            float currentTime = Time.time;
            float lifetime = currentTime - _spawnTime;
            
            if (lifetime >= MAX_LIFETIME)
            {
                DestroyProjectile();
                return;
            }
            
            float distanceFromSpawn = Vector3.Distance(transform.position, _spawnPosition);
            if (distanceFromSpawn >= MAX_DISTANCE)
            {
                DestroyProjectile();
                return;
            }
            
            if (_target == null || !_target.IsAlive)
            {
                DestroyProjectile();
                return;
            }
        }
        
        private void UpdateMovement()
        {
            if (_target == null || !_target.IsAlive)
            {
                DestroyProjectile();
                return;
            }
            
            Vector3 targetPosition = _target.transform.position;
            Vector3 myPos = transform.position;
            float distanceSquared = (targetPosition - myPos).sqrMagnitude;
            float distance = Mathf.Sqrt(distanceSquared);
            
            if (distance <= _hitDistanceThreshold)
            {
                HitTarget();
                return;
            }
            
            if (_speed <= 0)
            {
                HitTarget();
                return;
            }
            
            float duration = Mathf.Max(0.01f, distance / _speed);
            
            if (_movementTween != null && _movementTween.IsActive())
            {
                _movementTween.Kill();
            }
            
            _movementTween = transform.DOMove(targetPosition, duration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    if (!_hasHit)
                    {
                        CheckDistanceAndUpdate();
                    }
                })
                .OnUpdate(() =>
                {
                    float currentTime = Time.time;
                    if (currentTime - _lastTargetCheckTime >= TARGET_CHECK_INTERVAL)
                    {
                        _lastTargetCheckTime = currentTime;
                        CheckTargetAndDistance();
                    }
                });
        }
        
        private void CheckTargetAndDistance()
        {
            if (_hasHit) return;
            
            if (_target == null || !_target.IsAlive)
            {
                if (_movementTween != null)
                {
                    _movementTween.Kill();
                }
                DestroyProjectile();
                return;
            }
            
            Vector3 targetPos = _target.transform.position;
            Vector3 myPos = transform.position;
            float distanceSquared = (targetPos - myPos).sqrMagnitude;
            
            if (distanceSquared <= _hitDistanceThreshold * _hitDistanceThreshold)
            {
                HitTarget();
                return;
            }
            
            if (_movementTween != null && _movementTween.IsActive())
            {
                Vector3 currentDirection = (targetPos - myPos).normalized;
                Vector3 movementDirection = transform.right;
                
                float dotProduct = Vector3.Dot(currentDirection, movementDirection);
                if (dotProduct < 0.7f)
                {
                    UpdateMovement();
                }
            }
        }
        
        private void CheckDistanceAndUpdate()
        {
            if (_hasHit) return;
            
            if (_target == null || !_target.IsAlive)
            {
                DestroyProjectile();
                return;
            }
            
            Vector3 targetPos = _target.transform.position;
            Vector3 myPos = transform.position;
            float distanceSquared = (targetPos - myPos).sqrMagnitude;
            float thresholdSquared = _hitDistanceThreshold * _hitDistanceThreshold;
            
            if (distanceSquared <= thresholdSquared)
            {
                HitTarget();
            }
            else
            {
                float distance = Mathf.Sqrt(distanceSquared);
                if (distance > MAX_DISTANCE * 0.5f)
                {
                    DestroyProjectile();
                    return;
                }
                
                UpdateMovement();
            }
        }
        
        private void HitTarget()
        {
            if (_hasHit) return;
            _hasHit = true;
            
            if (_target != null && _target.IsAlive)
            {
                _target.TakeDamage(_damage);
                _onHit?.Invoke(this);
            }
            
            DestroyProjectile();
        }
        
        private void OnReachTarget()
        {
            HitTarget();
        }
        
        private void DestroyProjectile()
        {
            if (_movementTween != null)
            {
                _movementTween.Kill();
                _movementTween = null;
            }
            
            _onDestroy?.Invoke(this);
            Destroy(gameObject);
        }
    }
}

