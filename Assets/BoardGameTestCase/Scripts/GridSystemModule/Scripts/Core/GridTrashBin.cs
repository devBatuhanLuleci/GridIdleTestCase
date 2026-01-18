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
        private Color _normalColor;
        public Transform Transform => transform;

        private float _currentAlpha = 0f;
        private Color _currentBaseColor;

        private void Awake()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                _normalColor = _spriteRenderer.color;
                _currentBaseColor = _normalColor;
                
                // Start invisible
                Color c = _spriteRenderer.color;
                c.a = 0f;
                _spriteRenderer.color = c;
                _currentAlpha = 0f;
            }

            // Register to service locator
            if (ServiceLocator.Instance != null)
            {
                ServiceLocator.Instance.Register<IGridTrashBin>(this);
            }
        }
        
        private void Update()
        {
            if (_spriteRenderer != null)
            {
                Color finalColor = _currentBaseColor;
                finalColor.a = _currentAlpha;
                _spriteRenderer.color = finalColor;
            }
        }

        private void OnDestroy()
        {
            if (ServiceLocator.Instance != null)
            {
                ServiceLocator.Instance.Unregister<IGridTrashBin>();
            }
            _spriteRenderer.DOKill();
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
            // Just update the target base color
            _currentBaseColor = active ? _highlightColor : _normalColor;
        }

        public void Show(bool active)
        {
            if (_spriteRenderer == null) return;
            
            float targetAlpha = active ? 1f : 0f;
            
            // Kill any alpha tweens on this object (or specifically on the field if we stored the Tweener)
            DOTween.Kill(this, "AlphaTween");

            DOTween.To(() => _currentAlpha, x => _currentAlpha = x, targetAlpha, 0.3f)
                   .SetTarget(this)
                   .SetId("AlphaTween");
        }
    }
}
