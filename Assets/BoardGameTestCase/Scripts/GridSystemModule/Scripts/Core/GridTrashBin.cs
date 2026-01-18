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
            if (_spriteRenderer != null)
            {
                Color targetColor = active ? _highlightColor : _normalColor;
                targetColor.a = _spriteRenderer.color.a; // Preserve current alpha
                _spriteRenderer.color = targetColor;
            }
        }

        public void Show(bool active)
        {
            if (_spriteRenderer == null) return;
            
            float targetAlpha = active ? 1f : 0f;
            _spriteRenderer.DOKill();
            DOTween.To(() => _spriteRenderer.color.a, x => 
            {
                Color c = _spriteRenderer.color;
                c.a = x;
                _spriteRenderer.color = c;
            }, targetAlpha, 0.3f);
        }
    }
}
