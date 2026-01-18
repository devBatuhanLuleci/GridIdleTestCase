using UnityEngine;
using BoardGameTestCase.Core.Common;
using GridSystemModule.Core.Interfaces;

namespace GridSystemModule.Core
{
    public class GridTrashBin : MonoBehaviour, IGridTrashBin
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Color _highlightColor = Color.red;
        private Color _normalColor;

        private void Awake()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null) _normalColor = _spriteRenderer.color;

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
                _spriteRenderer.color = active ? _highlightColor : _normalColor;
            }
        }
    }
}
