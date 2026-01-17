using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using GridSystemModule.Core.Interfaces;
using BoardGameTestCase.Core.ScriptableObjects;
using GameModule.Core.Interfaces;
using BoardGameTestCase.Core.Common;

namespace GameplayModule
{
    public class EnemyItem2D : MonoBehaviour, IPlaceable, IEnemy
    {
        [SerializeField] private EnemyData _enemyData;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Collider2D _collider;
        [SerializeField] private Vector2Int _gridSize = new Vector2Int(1, 1);
        [SerializeField] private string _placeableId;
        [SerializeField] private Image _healthBarFillImage;
        [SerializeField] private EnemyMovementController _movementController;
        
        private int _currentHealth;
        private Vector2Int _gridPosition;
        private bool _isDragging = false;
        private bool _isPlaced = false;
        private Tween _healthBarTween;
        private Vector3 _originalScale;
        
        public string PlaceableId => _placeableId;
        public Vector2Int GridSize => _gridSize;
        public bool IsDragging { get => _isDragging; set => _isDragging = value; }
        public bool IsPlaced { get => _isPlaced; set => _isPlaced = value; }
        public Vector2Int GridPosition { get => _gridPosition; set => _gridPosition = value; }
        public EnemyData EnemyData => _enemyData;
        public int CurrentHealth => _currentHealth;
        public bool IsAlive => _currentHealth > 0;
        public Transform Transform => transform;
        
        public event Action<EnemyItem2D> OnDeath;
        public event Action<EnemyItem2D, int> OnHealthChanged;
        
        event Action<IEnemy> IEnemy.OnDeath
        {
            add
            {
                OnDeath += (enemy) => value?.Invoke(enemy as IEnemy);
            }
            remove
            {
                OnDeath -= (enemy) => value?.Invoke(enemy as IEnemy);
            }
        }
        
        private void Awake()
        {
            if (_enemyData != null) LoadFromEnemyData();
            else if (string.IsNullOrEmpty(_placeableId)) _placeableId = gameObject.name;
            _originalScale = transform.localScale;
            SetupScaleFromGridParent();
            SetupVisuals();
            InitializeHealthBar();
        }
        
        private void OnValidate()
        {
            if (_enemyData != null && !Application.isPlaying) LoadFromEnemyData();
        }
        
        private void OnDestroy()
        {
            if (_healthBarTween != null && _healthBarTween.IsActive())
            {
                _healthBarTween.Kill();
            }
            
            if (_movementController != null)
            {
                _movementController.StopMovement();
            }
        }
        
        private void LoadFromEnemyData()
        {
            if (_enemyData == null) return;
            _placeableId = _enemyData.EnemyId;
            _currentHealth = _enemyData.Health;
            if (_enemyData.Sprite != null && _spriteRenderer != null)
            {
                _spriteRenderer.sprite = _enemyData.Sprite;
            }
            
            if (_healthBarFillImage != null && MaxHealth > 0)
            {
                _healthBarFillImage.fillAmount = (float)_currentHealth / MaxHealth;
            }
        }
        
        private void SetupScaleFromGridParent()
        {
        }
        
        private void SetupVisuals()
        {
            if (_spriteRenderer == null) return;
            
            if (_spriteRenderer.sprite == null && _enemyData != null && _enemyData.Sprite != null)
            {
                _spriteRenderer.sprite = _enemyData.Sprite;
            }
            
            _spriteRenderer.color = Color.white;
            _spriteRenderer.enabled = true;
            _spriteRenderer.sortingOrder = 2;
            
            Vector3 pos = transform.position;
            pos.z = 0;
            transform.position = pos;
            
            EnsureCollider2D();
        }
        
        private void EnsureCollider2D()
        {
            if (_collider == null && _spriteRenderer != null && _spriteRenderer.sprite != null)
            {
                var boxCollider = gameObject.AddComponent<BoxCollider2D>();
                boxCollider.size = _spriteRenderer.sprite.bounds.size;
                boxCollider.offset = _spriteRenderer.sprite.bounds.center;
                boxCollider.isTrigger = true;
                _collider = boxCollider;
            }
            else if (_collider != null)
            {
                if (_collider is BoxCollider2D boxCollider)
                {
                    boxCollider.isTrigger = true;
                }
            }
        }
        
        private void InitializeHealthBar()
        {
            if (_healthBarFillImage != null)
            {
                _healthBarFillImage.type = Image.Type.Filled;
                _healthBarFillImage.fillAmount = 1f;
            }
        }
        
        public void SetEnemyData(EnemyData data)
        {
            _enemyData = data;
            if (_enemyData != null)
            {
                LoadFromEnemyData();
            }
        }
        
        public void TakeDamage(int damage)
        {
            if (!IsAlive) return;
            
            int previousHealth = _currentHealth;
            _currentHealth = Mathf.Max(0, _currentHealth - damage);
            OnHealthChanged?.Invoke(this, _currentHealth);
            
            UpdateHealthBar(previousHealth);
            
            if (_currentHealth <= 0)
            {
                OnDeath?.Invoke(this);
                Destroy(gameObject);
            }
        }
        
        private void UpdateHealthBar(int previousHealth)
        {
            if (_healthBarFillImage == null) return;
            if (MaxHealth <= 0) return;
            
            float targetFillAmount = (float)_currentHealth / MaxHealth;
            
            if (_healthBarTween != null && _healthBarTween.IsActive())
            {
                _healthBarTween.Kill();
            }
            
            float animationDuration = 0.05f;
            _healthBarTween = DOTween.To(
                () => _healthBarFillImage.fillAmount,
                x => _healthBarFillImage.fillAmount = x,
                targetFillAmount,
                animationDuration
            ).SetEase(DG.Tweening.Ease.OutQuad);
        }
        
        public void OnDragStart()
        {
        }
        
        public void OnDrag(Vector3 worldPosition)
        {
        }
        
        public void OnDrop(Vector2Int gridPosition, bool isValid)
        {
            if (isValid)
            {
                _gridPosition = gridPosition;
                _isPlaced = true;
            }
        }
        
        public void OnPlaced(Vector2Int gridPosition)
        {
            _gridPosition = gridPosition;
            _isPlaced = true;
        }
        
        public void OnRemoved()
        {
            _isPlaced = false;
        }
        
        public Vector3? GetOriginalScale()
        {
            return _originalScale;
        }
        
        public float Speed => _enemyData != null ? _enemyData.Speed : 1.0f;
        public int MaxHealth => _enemyData != null ? _enemyData.Health : 0;
        
        public void SetMovementController(EnemyMovementController movementController)
        {
            _movementController = movementController;
        }
    }
}

