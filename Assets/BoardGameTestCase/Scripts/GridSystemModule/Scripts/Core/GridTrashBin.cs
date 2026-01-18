using UnityEngine;
using BoardGameTestCase.Core.Common;
using DG.Tweening;
using GridSystemModule.Core.Interfaces;

namespace GridSystemModule.Core
{
    public class GridTrashBin : MonoBehaviour, IGridTrashBin
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Color _highlightColor = Color.red;
        
        [Header("Animation Settings")]
        [SerializeField] private float _fadeDuration = 0.3f;
        [SerializeField] private float _activationDelay = 0.1f;
        [SerializeField] private float _deactivationDelay = 0.1f;
        
        private Color _normalColor;
        private bool _isHighlighted = false;
        private Tweener _currentFadeTween;
        private Tween _delayedCallTween;
        
        public Transform Transform => transform;

        private void Awake()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _normalColor = _spriteRenderer.color;
                
                // Start invisible
                Color c = _spriteRenderer.color;
                c.a = 0f;
                _spriteRenderer.color = c;
            }

            // Register to service locator
            if (ServiceLocator.Instance != null)
            {
                ServiceLocator.Instance.Register<IGridTrashBin>(this);
            }
        }

        private void OnDestroy()
        {
            if (ServiceLocator.Instance != null)
            {
                ServiceLocator.Instance.Unregister<IGridTrashBin>();
            }
            
            // Kill all tweens
            _currentFadeTween?.Kill();
            _delayedCallTween?.Kill();
            if (_spriteRenderer != null) _spriteRenderer.DOKill();
        }

        public bool IsPointOver(Vector3 worldPoint)
        {
            var collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                return collider.OverlapPoint(worldPoint);
            }
            
            // Fallback to simple distance if no collider
            return Vector3.Distance(transform.position, worldPoint) < 1f;
        }

        public void SetHighlight(bool active)
        {
            // Avoid unnecessary color updates that might fight with the alpha tween
            if (_isHighlighted == active) return;
            _isHighlighted = active;

            if (_spriteRenderer != null)
            {
                Color targetBase = active ? _highlightColor : _normalColor;
                Color currentColor = _spriteRenderer.color;
                
                // Preserve the CURRENT alpha which is being animated by Show()
                targetBase.a = currentColor.a;
                _spriteRenderer.color = targetBase;
            }
        }

        public void Show(bool active)
        {
            if (_spriteRenderer == null) return;
            
            float targetAlpha = active ? 1f : 0f;
            float delay = active ? _activationDelay : _deactivationDelay;
            
            // Kill any previous tweens AND delayed calls to avoid conflicts
            _currentFadeTween?.Kill();
            _delayedCallTween?.Kill();
            _spriteRenderer.DOKill();
            
            // Use DOVirtual.DelayedCall for the delay, then start the fade
            _delayedCallTween = DOVirtual.DelayedCall(delay, () =>
            {
                if (_spriteRenderer == null) return;
                
                _currentFadeTween = DOTween.To(() => _spriteRenderer.color.a, x => 
                {
                    if (_spriteRenderer == null) return;
                    Color c = _spriteRenderer.color;
                    c.a = x;
                    _spriteRenderer.color = c;
                }, targetAlpha, _fadeDuration)
                .SetTarget(_spriteRenderer)
                .SetUpdate(true);
            });
        }
    }
}
