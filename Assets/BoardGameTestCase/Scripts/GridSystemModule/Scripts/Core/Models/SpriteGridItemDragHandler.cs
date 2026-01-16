using UnityEngine;
using GridSystemModule.Core.Interfaces;
using BoardGameTestCase.Core.Common;

namespace GridSystemModule.Core.Models
{
    /// <summary>
    /// Drag handler component for world-space sprite objects.
    /// Provides grid placement drag and drop functionality for 2D sprites.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class SpriteGridItemDragHandler : MonoBehaviour, IPlaceable
    {
        [SerializeField] private Vector2Int _gridSize = new Vector2Int(1, 1);
        [SerializeField] private string _placeableId;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _draggingColor = new Color(1f, 1f, 1f, 0.7f);
        [SerializeField] private Color _invalidColor = Color.red;
        
        private SpriteRenderer _spriteRenderer;
        private Collider2D _collider;
        private Color _originalColor;
        private Vector3 _originalPosition;
        private Vector3 _originalScale;
        private IGridPlacementSystem _placementSystem;
        private Camera _camera;
        
        private bool _isDragging = false;
        private bool _isPlaced = false;
        private Vector2Int _gridPosition;
        private IPlaceable _ghostObject;

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
            _collider = GetComponent<Collider2D>();
            
            if (_spriteRenderer != null)
                _originalColor = _spriteRenderer.color;
            
            _originalScale = transform.localScale;
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            if (_placementSystem == null)
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
                    OnBeginDrag();
                }
            }

            // Check for mouse drag
            if (Input.GetMouseButton(0) && _isDragging)
            {
                Vector3 worldPos = GetWorldPositionFromMouse();
                OnDrag(worldPos);
            }

            // Check for mouse up to end drag
            if (Input.GetMouseButtonUp(0) && _isDragging)
            {
                Vector3 worldPos = GetWorldPositionFromMouse();
                OnEndDrag(worldPos);
            }
        }

        private bool IsMouseOverSprite()
        {
            if (_collider == null || _camera == null)
                return false;

            Vector3 mouseWorldPos = GetWorldPositionFromMouse();
            Vector2 mousePos2D = new Vector2(mouseWorldPos.x, mouseWorldPos.y);
            
            // Raycast to check if mouse is over this collider
            RaycastHit2D hit = Physics2D.Raycast(mousePos2D, Vector2.zero);
            return hit.collider != null && hit.collider.gameObject == gameObject;
        }

        private Vector3 GetWorldPositionFromMouse()
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = 10f; // Distance from camera
            Vector3 worldPos = _camera.ScreenToWorldPoint(mouseScreenPos);
            worldPos.z = 0; // Keep z at 0 for 2D
            return worldPos;
        }

        private void OnBeginDrag()
        {
            if (_placementSystem == null)
                _placementSystem = ServiceLocator.Instance?.Get<IGridPlacementSystem>();

            if (_placementSystem == null)
                return;

            _isDragging = true;
            
            // Create a ghost object for dragging
            CreateGhostObject();
            
            // Update placement system
            _placementSystem.StartDragging(_ghostObject ?? this);
        }

        private void OnDrag(Vector3 worldPosition)
        {
            if (!_isDragging || _ghostObject == null)
                return;

            // Update ghost position
            var ghostMB = _ghostObject as MonoBehaviour;
            if (ghostMB != null)
            {
                ghostMB.transform.position = worldPosition;
            }

            // Update placement system
            _placementSystem?.UpdateDrag(worldPosition);
        }

        private void OnEndDrag(Vector3 worldPosition)
        {
            if (!_isDragging)
                return;

            _isDragging = false;

            // Finalize placement
            if (_placementSystem != null && _ghostObject != null)
            {
                _placementSystem.EndDragging(worldPosition);
                
                // If placement was successful, destroy this object
                if (_ghostObject.IsPlaced)
                {
                    Destroy(gameObject);
                    return;
                }
            }

            // Reset to original position if placement failed
            transform.position = _originalPosition;
            if (_spriteRenderer != null)
                _spriteRenderer.color = _originalColor;

            // Cleanup ghost
            DestroyGhostObject();
        }

        private void CreateGhostObject()
        {
            // Create a ghost GameObject for dragging
            GameObject ghostGO = new GameObject($"{gameObject.name}_Ghost");
            ghostGO.transform.position = transform.position;
            ghostGO.transform.localScale = transform.localScale;

            // Copy SpriteRenderer
            var ghostRenderer = ghostGO.AddComponent<SpriteRenderer>();
            if (_spriteRenderer != null)
            {
                ghostRenderer.sprite = _spriteRenderer.sprite;
                ghostRenderer.color = _draggingColor;
                ghostRenderer.sortingOrder = _spriteRenderer.sortingOrder + 1;
            }

            // Create a simple IPlaceable wrapper for the ghost
            var ghostHandler = ghostGO.AddComponent<SpriteGridItemDragHandler>();
            ghostHandler._gridSize = _gridSize;
            ghostHandler._placeableId = _placeableId;
            ghostHandler._normalColor = _normalColor;
            ghostHandler._draggingColor = _draggingColor;
            ghostHandler._invalidColor = _invalidColor;
            ghostHandler._spriteRenderer = ghostRenderer;

            _ghostObject = ghostHandler;
        }

        private void DestroyGhostObject()
        {
            if (_ghostObject is MonoBehaviour mb)
            {
                Destroy(mb.gameObject);
            }
            _ghostObject = null;
        }

        public void OnDragStart()
        {
            // Handled in OnBeginDrag
        }

        public void OnDrop(Vector2Int gridPosition, bool isValid)
        {
            if (_ghostObject is MonoBehaviour ghostMB && ghostMB.TryGetComponent<SpriteRenderer>(out var ghostRenderer))
            {
                ghostRenderer.color = isValid ? _normalColor : _invalidColor;
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
                _spriteRenderer.color = _originalColor;
        }

        public Vector3? GetOriginalScale()
        {
            return _originalScale;
        }
    }
}
