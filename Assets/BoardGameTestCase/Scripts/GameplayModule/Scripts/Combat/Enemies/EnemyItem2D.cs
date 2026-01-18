using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using GridSystemModule.Core.Interfaces;
using BoardGameTestCase.Core.ScriptableObjects;
using GameModule.Core.Interfaces;
using BoardGameTestCase.Core.Common;
using BoardGameTestCase.Core;

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
        private bool _isBeingDiscarded = false;
        
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

        public void PlayFailAnimation()
        {
            // Position shake (horizontal)
            transform.DOPunchPosition(Vector3.right * 0.1f, 0.4f, 20, 0.5f);
            
            // Color flash (Kırmızı)
            if (_spriteRenderer != null)
            {
                _spriteRenderer.DOKill();
                Sequence colorSeq = DOTween.Sequence();
                colorSeq.Append(DOTween.To(() => _spriteRenderer.color, x => _spriteRenderer.color = x, Color.red, 0.15f));
                colorSeq.Append(DOTween.To(() => _spriteRenderer.color, x => _spriteRenderer.color = x, Color.white, 0.25f));
            }
        }

        public void PlayDiscardAnimation(Vector3 trashPosition, System.Action onComplete = null)
        {
            if (_isBeingDiscarded) return;
            _isBeingDiscarded = true;
            
            // Kill any active tweens
            transform.DOKill();

            Vector3 startPos = transform.position;
            // Use a default height since this class doesn't have serialized discard settings yet
            float discardBezierHeight = 3f;
            float discardDuration = 0.8f;
            float discardRotationAmount = 720f;
            
            Vector3 controlPoint = BezierUtils.GetAutomaticControlPoint(startPos, trashPosition, discardBezierHeight, Vector3.up);

            // 1. Move along Bezier Curve
            DOVirtual.Float(0f, 1f, discardDuration, t =>
            {
                if (this == null) return;
                transform.position = BezierUtils.GetPoint(startPos, controlPoint, trashPosition, t);
            }).SetEase(DG.Tweening.Ease.InQuad);

            // 2. Rotate along Z axis
            transform.DORotate(new Vector3(0, 0, discardRotationAmount), discardDuration, RotateMode.FastBeyond360)
                .SetEase(DG.Tweening.Ease.InQuad);

            // 3. Shrink scale
            transform.DOScale(Vector3.zero, discardDuration)
                .SetEase(DG.Tweening.Ease.InQuad);

            // 4. Fade out transparency (Alpha)
            if (_spriteRenderer != null)
            {
                Color startColor = _spriteRenderer.color;
                DOTween.To(() => _spriteRenderer.color.a, x => 
                {
                    if (_spriteRenderer != null)
                    {
                        Color c = _spriteRenderer.color;
                        c.a = x;
                        _spriteRenderer.color = c;
                    }
                }, 0f, discardDuration).SetEase(DG.Tweening.Ease.InQuad);
            }

            // 5. Cleanup on complete
            DOVirtual.DelayedCall(discardDuration, () =>
            {
                onComplete?.Invoke();
                if (this != null && gameObject != null)
                    Destroy(gameObject);
            });
        }
    }
}

