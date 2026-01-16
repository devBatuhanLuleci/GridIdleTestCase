using UnityEngine;
using GridSystemModule.Core.Interfaces;
using BoardGameTestCase.Core.Common;

namespace GridSystemModule.Core.Models
{
    /// <summary>
    /// Draggable sprite component for world-space 2D objects that can be placed on the grid.
    /// Handles drag input through physics raycasting.
    /// </summary>
    public class DraggableSprite : MonoBehaviour, IPlaceable
    {
        [SerializeField] private Vector2Int _gridSize = new Vector2Int(1, 1);
        [SerializeField] private string _placeableId;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _draggingColor = new Color(1f, 1f, 1f, 0.7f);
        [SerializeField] private Color _invalidColor = Color.red;
        
        private SpriteRenderer _spriteRenderer;
        private Color _originalColor;
        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private IGridPlacementSystem _placementSystem;
        private bool _isDragging = false;
        private bool _isPlaced = false;
        private Vector2Int _gridPosition;
        private Camera _camera;

        public string PlaceableId => _placeableId;
        public Vector2Int GridSize => _gridSize;
        public bool IsDragging { get => _isDragging; set => _isDragging = value; }
        public bool IsPlaced { get => _isPlaced; set => _isPlaced = value; }
        public Vector2Int GridPosition { get => _gridPosition; set => _gridPosition = value; }

        private void Awake()
        {
            if (string.IsNullOrEmpty(_placeableId))
                _placeableId = gameObject.name;

            _spriteRenderer = GetComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
                _originalColor = _spriteRenderer.color;
            
            _originalScale = transform.localScale;
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            _placementSystem = ServiceLocator.Instance?.Get<IGridPlacementSystem>();
        }

        private void Update()
        {
            if (_camera == null)
                _camera = Camera.main;

            // Check for mouse down to start drag
            if (Input.GetMouseButtonDown(0) && !_isDragging)
            {
                if (IsMouseOverSprite())
                {
                    _originalPosition = transform.position;
                    _placementSystem?.StartDragging(this);
                    OnDragStart();
                }
            }

            // Check for mouse drag
            if (Input.GetMouseButton(0) && _isDragging)
            {
                Vector3 worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
                worldPos.z = 0; // Keep z at 0 for 2D
                transform.position = worldPos;
                _placementSystem?.UpdateDrag(worldPos);
                OnDrag(worldPos);
            }

            // Check for mouse up to end drag
            if (Input.GetMouseButtonUp(0) && _isDragging)
            {
                Vector3 worldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
                worldPos.z = 0;
                _placementSystem?.EndDragging(worldPos);
                OnEndDrag();
            }
        }

        private bool IsMouseOverSprite()
        {
            if (_spriteRenderer == null)
                return false;

            Vector3 mouseWorldPos = _camera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;

            // Use bounds check for quick hit detection
            return _spriteRenderer.bounds.Contains(mouseWorldPos);
        }

        public void OnDragStart()
        {
            if (_spriteRenderer != null)
                _spriteRenderer.color = _draggingColor;
        }

        public void OnDrag(Vector3 worldPosition)
        {
            // Position update is handled in Update()
        }

        public void OnDrop(Vector2Int gridPosition, bool isValid)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = isValid ? _normalColor : _invalidColor;
            }
        }

        public void OnPlaced(Vector2Int gridPosition)
        {
            _isPlaced = true;
            _gridPosition = gridPosition;
            if (_spriteRenderer != null)
                _spriteRenderer.color = _normalColor;
        }

        public void OnRemoved()
        {
            _isPlaced = false;
            if (_spriteRenderer != null)
                _spriteRenderer.color = _normalColor;
        }

        public Vector3? GetOriginalScale()
        {
            return _originalScale;
        }

        private void OnEndDrag()
        {
            _isDragging = false;
        }
    }
}
