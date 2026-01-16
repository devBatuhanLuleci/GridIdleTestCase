using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DG.Tweening;
using GridSystemModule.Core.Interfaces;
using BoardGameTestCase.Core.ScriptableObjects;
using UISystemModule.Core.Interfaces;
using BoardGameTestCase.Core.Common;
using GameModule.Core.Interfaces;
using GameState = GameModule.Core.Interfaces.GameState;

namespace UISystemModule.UIElements
{
    public class GridItem2D : MonoBehaviour, IPlaceable
    {
        // Static flag to ensure only one item can be dragged at a time
        private static GridItem2D _currentlyDraggingItem = null;
        
        [SerializeField] private DefenceItemData _defenceItemData;
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Collider2D _collider;
        [SerializeField] private MonoBehaviour _combatComponent;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _draggingColor = new Color(1f, 1f, 1f, 0.7f);
        [SerializeField] private Color _invalidColor = new Color(1f, 0.5f, 0.5f, 0.7f);
        [SerializeField] private bool _isDraggable = true;
        [SerializeField] private Vector2Int _gridSize = new Vector2Int(1, 1);
        [SerializeField] private string _placeableId;
        
        private Vector3 _originalPosition;
        private Transform _originalParent;
        private Vector3 _originalScale;
        private bool _isDragging = false;
        private bool _isPlaced = false;
        private Vector2Int _gridPosition;
        private IGridPlacementSystem _placementSystem;
        private Camera _camera;
        private bool _wasTouchingLastFrame = false;
        private IGameFlowController _gameFlowController;
        public string PlaceableId => _placeableId;
        public Vector2Int GridSize => _gridSize;
        public bool IsDragging { get => _isDragging; set => _isDragging = value; }
        public bool IsPlaced { get => _isPlaced; set => _isPlaced = value; }
        public Vector2Int GridPosition { get => _gridPosition; set => _gridPosition = value; }
        
        private void Awake()
        {
            if (_defenceItemData != null) LoadFromDefenceItemData();
            else if (string.IsNullOrEmpty(_placeableId)) _placeableId = gameObject.name;
            _camera = Camera.main;
            FindPlacementSystem();
            // DON'T fetch GameFlowController here - initialization order issue
            // It will be fetched in Start() or lazily when needed
            _originalScale = transform.localScale;
            SetupScaleFromGridParent();
            SetupVisuals();
        }
        
        private void Start()
        {
            // Fetch GameFlowController after all Awake() calls are done
            if (_gameFlowController == null)
            {
                _gameFlowController = ServiceLocator.Instance?.Get<IGameFlowController>();
            }
        }
        
        private bool IsPlacingState()
        {
            return _gameFlowController != null && _gameFlowController.CurrentGameState == GameState.Placing;
        }
        
        private void OnValidate()
        {
            if (_defenceItemData != null && !Application.isPlaying) LoadFromDefenceItemData();
        }
        private void LoadFromDefenceItemData()
        {
            if (_defenceItemData == null) return;
            _gridSize = _defenceItemData.GridSize;
            _placeableId = _defenceItemData.ItemId;
            if (_defenceItemData.Sprite != null && _spriteRenderer != null)
            {
                _spriteRenderer.sprite = _defenceItemData.Sprite;
            }
            
            // Setup visual scale based on grid size
            UpdateVisualScaleFromGridSize();
        }
        
        private void UpdateVisualScaleFromGridSize()
        {
            if (_spriteRenderer == null) return;
            
            // Grid cell size is assumed to be 1 unit
            // Scale sprite based on grid dimensions
            // For example: 2x2 item should be 2 units wide and 2 units tall
            
            float spriteWidth = _spriteRenderer.sprite != null ? _spriteRenderer.sprite.bounds.size.x : 1f;
            float spriteHeight = _spriteRenderer.sprite != null ? _spriteRenderer.sprite.bounds.size.y : 1f;
            
            // Calculate scale to fit grid size
            float targetWidth = _gridSize.x;
            float targetHeight = _gridSize.y;
            
            float scaleX = spriteWidth > 0 ? targetWidth / spriteWidth : 1f;
            float scaleY = spriteHeight > 0 ? targetHeight / spriteHeight : 1f;
            
            // Use the average scale to maintain aspect ratio
            float avgScale = (scaleX + scaleY) / 2f;
            
            transform.localScale = new Vector3(avgScale, avgScale, 1f);
        }
        
        private void FindPlacementSystem()
        {
            _placementSystem = ServiceLocator.Instance?.Get<IGridPlacementSystem>();
        }
        
        private void SetupScaleFromGridParent()
        {
        }
        
        private void SetupVisuals()
        {
            if (_spriteRenderer == null) return;
            
            if (_isPlaced)
            {
                EnsureCollider2D();
            }
            
            if (_spriteRenderer.sprite == null)
            {
                CreateColoredSprite();
            }
            
            _spriteRenderer.color = _normalColor;
            _spriteRenderer.enabled = true;
            _spriteRenderer.sortingOrder = 1;
            
            Vector3 pos = transform.position;
            pos.z = 0;
            transform.position = pos;
        }
        
        private void Update()
        {
            if (!_isDragging)
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    CheckForMouseClick();
                }
                
                if (Touchscreen.current != null)
                {
                    CheckForNewInputSystemTouch();
                }
            }
            else
            {
                bool isTouchingNow = false;
                if (Touchscreen.current != null)
                {
                    var touches = Touchscreen.current.touches;
                    isTouchingNow = touches.Count > 0;
                }
                
                UpdateDragPosition();
                CheckForDragEnd(isTouchingNow);
                
                _wasTouchingLastFrame = isTouchingNow;
            }
        }
        
        private void CheckForMouseClick()
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            
            if (_camera != null)
            {
                Vector3 worldPosition = _camera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, _camera.nearClipPlane));
                worldPosition.z = 0f;
                
                if (_collider != null && _collider.OverlapPoint(worldPosition))
                {
                    StartDragging();
                }
            }
        }
        
        private void CheckForNewInputSystemTouch()
        {
            var touches = Touchscreen.current.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch.press.wasPressedThisFrame)
                {
                    CheckForTouchClick(touch.position.ReadValue());
                }
            }
        }
        
        private void CheckForTouchClick(Vector2 touchPosition)
        {
            if (_camera != null)
            {
                Vector3 worldPosition = _camera.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, _camera.nearClipPlane));
                worldPosition.z = 0f;
                
                if (_collider != null && _collider.OverlapPoint(worldPosition)) StartDragging();
            }
        }
        
        private void StartDragging()
        {
            // IMPORTANT: Only allow one item to be dragged at a time
            if (_currentlyDraggingItem != null && _currentlyDraggingItem != this)
            {
                Debug.Log($"[GridItem2D] Cannot drag {name} - another item is already being dragged: {_currentlyDraggingItem.name}");
                return;
            }
            
            // IMPORTANT: Cancel any ongoing DOTween animations on this item's transform
            // This prevents scale stacking and other animation conflicts
            transform.DOKill();
            
            // Ensure GameFlowController is initialized (lazy loading)
            if (_gameFlowController == null)
            {
                _gameFlowController = ServiceLocator.Instance?.Get<IGameFlowController>();
            }
            
            // Check if we're in placing state
            if (!IsPlacingState())
            {
                return;
            }
            
            if (!_isDraggable && !_isPlaced) 
            {
                return;
            }
            
            if (_placementSystem == null)
            {
                FindPlacementSystem();
            }
            
            if (_collider != null) _collider.enabled = true;
            _originalPosition = transform.position;
            _originalParent = transform.parent;
            
            if (Touchscreen.current != null)
            {
                _wasTouchingLastFrame = Touchscreen.current.touches.Count > 0;
            }
            
            _isDragging = true;
            _currentlyDraggingItem = this; // Mark this item as currently dragging
            _placementSystem?.StartDragging(this);
            
            if (_isPlaced)
            {
                _isPlaced = false;
            }
            
            SetColor(_draggingColor);        }
        
        private void UpdateDragPosition()
        {
            Vector2 inputPosition = Vector2.zero;
            bool hasInput = false;
            
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                inputPosition = Mouse.current.position.ReadValue();
                hasInput = true;
            }
            else if (Touchscreen.current != null)
            {
                var touches = Touchscreen.current.touches;
                if (touches.Count > 0)
                {
                    inputPosition = touches[0].position.ReadValue();
                    hasInput = true;
                }
            }
            
            if (hasInput && _camera != null)
            {
                Vector3 worldPosition = _camera.ScreenToWorldPoint(new Vector3(inputPosition.x, inputPosition.y, _camera.nearClipPlane));
                worldPosition.z = transform.position.z;
                
                transform.position = worldPosition;
                
                // IMPORTANT: Update _originalPosition during drag so that if ReturnToOriginalPosition() 
                // is called, it returns to the current dragged position, not the initial position.
                // This is crucial for inventory items that should stay where they're dragged to.
                // Update X, Y from dragged world position, and Z from actual transform (camera-independent)
                _originalPosition.x = worldPosition.x;
                _originalPosition.y = worldPosition.y;
                _originalPosition.z = transform.position.z;  // Always use actual transform Z, not camera calculation
                
                if (_placementSystem != null)
                {
                    _placementSystem.UpdateDrag(worldPosition);
                }
            }
        }
        
        private void EndDragging()
        {
            if (!_isDragging) return;
            _isDragging = false;
            
            // Clear the global dragging flag
            if (_currentlyDraggingItem == this)
            {
                _currentlyDraggingItem = null;
            }
            
            if (_placementSystem != null)
            {
                Vector3 worldPosition = transform.position;
                _placementSystem.EndDragging(worldPosition);
            }
            else ReturnToOriginalPosition();
        }
        
        private void CheckForDragEnd(bool isTouchingNow)
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame)
            {
                EndDragging();
                return;
            }
            
            if (_wasTouchingLastFrame && !isTouchingNow)
            {
                EndDragging();
                return;
            }
            
            if (Touchscreen.current != null)
            {
                var touches = Touchscreen.current.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    var touch = touches[i];
                    if (touch.press.wasReleasedThisFrame)
                    {
                        EndDragging();
                        break;
                    }
                }
            }
        }
        
        private void CreateColoredSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            _spriteRenderer.sprite = sprite;
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!IsPlacingState()) return;
            if (!_isDraggable && !_isPlaced) return;
            
            _originalPosition = transform.position;
            _originalParent = transform.parent;
            _isDragging = true;
            _placementSystem?.StartDragging(this);
            
            if (_isPlaced)
            {
                _isPlaced = false;
            }
            
            OnDragStart();
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (!IsPlacingState())
            {
                if (_isDragging) EndDragging();
                return;
            }
            if (!_isDragging) return;
            
            Vector3 worldPosition = _camera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, _camera.nearClipPlane));
            worldPosition.z = transform.position.z;
            
            transform.position = worldPosition;
            
            if (_placementSystem != null)
            {
                _placementSystem.UpdateDrag(worldPosition);
            }
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsPlacingState())
            {
                if (_isDragging) EndDragging();
                return;
            }
            if (!_isDragging) return;
            _isDragging = false;
            Vector3 worldPosition = _camera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, _camera.nearClipPlane));
            worldPosition.z = transform.position.z;
            if (_placementSystem != null) _placementSystem.EndDragging(worldPosition);
            else ReturnToOriginalPosition();
        }
        
        private void ReturnToOriginalPosition()
        {
            if (_originalParent == null && _originalPosition == Vector3.zero && !_isPlaced)
            {
                Destroy(gameObject);
                return;
            }
            
            transform.SetParent(_originalParent, true);
            transform.position = _originalPosition;
            
            SetColor(_normalColor);
        }
        
        private void SetColor(Color color)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = color;
            }
        }
        
        public void SetDraggable(bool draggable)
        {
            _isDraggable = draggable;
        }
        
        public void SetGridSize(Vector2Int gridSize)
        {
            _gridSize = gridSize;
        }
        
        public Vector2Int GetGridSize()
        {
            return _gridSize;
        }
        
        public void SetPlaceableId(string placeableId)
        {
            _placeableId = placeableId;
        }
        
        public void SetSprite(Sprite sprite)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sprite = sprite;
            }
        }
        
        public void SetDefenceItemData(DefenceItemData data)
        {
            _defenceItemData = data;
            if (_defenceItemData != null)
            {
                LoadFromDefenceItemData();
            }
        }
        
        public void SetPlacementSystem(IGridPlacementSystem placementSystem)
        {
            _placementSystem = placementSystem;
        }
        
        public SpriteRenderer GetSpriteRenderer() => _spriteRenderer;
        
        public void SetSpriteRenderer(SpriteRenderer spriteRenderer)
        {
            _spriteRenderer = spriteRenderer;
            if (_spriteRenderer != null && _defenceItemData != null && _defenceItemData.Sprite != null)
            {
                _spriteRenderer.sprite = _defenceItemData.Sprite;
            }
        }
        
        public DefenceItemData GetDefenceItemData()
        {
            return _defenceItemData;
        }
        
        public MonoBehaviour GetCombatComponent()
        {
            return _combatComponent;
        }
        
        public int Damage => _defenceItemData != null ? _defenceItemData.Damage : 0;
        public float Range => _defenceItemData != null ? _defenceItemData.Range : 0f;
        public float AttackInterval => _defenceItemData != null ? _defenceItemData.AttackInterval : 0f;
        public AttackDirection AttackDirection => _defenceItemData != null ? _defenceItemData.AttackDirection : AttackDirection.Forward;
        public int Health => _defenceItemData != null ? _defenceItemData.Health : 0;
        public int Cost => _defenceItemData != null ? _defenceItemData.Cost : 0;
        public string DisplayName => _defenceItemData != null ? _defenceItemData.DisplayName : gameObject.name;
        
        public void OnDragStart()
        {
            SetColor(_draggingColor);
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
                _isDraggable = true;
                EnsureCollider2D();
                SetColor(_normalColor);
                
                if (_placementSystem == null)
                {
                    FindPlacementSystem();
                }
                
                if (_placementSystem != null)
                {
                    Vector3 worldPosition = _placementSystem.GridToWorld(gridPosition);
                    transform.position = worldPosition;
                }
                
                _originalScale = transform.localScale;
                
                AttachCombatComponentIfNeeded();
            }
            else
            {
                ReturnToOriginalPosition();
            }
        }
        
        public void OnPlaced(Vector2Int gridPosition)
        {
            _gridPosition = gridPosition;
            _isPlaced = true;
            _isDraggable = true;
            EnsureCollider2D();
            SetColor(_normalColor);
            
            if (_placementSystem != null)
            {
                Vector3 worldPosition = _placementSystem.GridToWorld(gridPosition);
                transform.position = worldPosition;
            }
            
            AttachCombatComponentIfNeeded();
        }
        
        private void AttachCombatComponentIfNeeded()
        {
            if (_defenceItemData == null || _combatComponent == null) return;
            
            var combatInitializable = _combatComponent as ICombatInitializable;
            if (combatInitializable == null) return;
            
            combatInitializable.SetDefenceItemData(_defenceItemData);
            combatInitializable.SetPlaceable(this);
            
            var gameFlowController = ServiceLocator.Instance?.Get<IGameFlowController>();
            if (gameFlowController != null && gameFlowController.CurrentGameState == GameState.Fight)
            {
                combatInitializable.StartCombat();
            }
        }
        
        private void StartManualDrag(Vector3 mousePosition)
        {
            if (!_isDraggable && !_isPlaced) return;
            
            _originalPosition = transform.position;
            _originalParent = transform.parent;
            _isDragging = true;
            
            if (_placementSystem != null)
            {
                _placementSystem.StartDragging(this);
            }
            
            if (_isPlaced)
            {
                _isPlaced = false;
            }
            
            OnDragStart();
        }
        
        private void EndManualDrag(Vector3 mousePosition)
        {
            _isDragging = false;
            
            if (_placementSystem != null)
            {
                _placementSystem.EndDragging(mousePosition);
            }
            else
            {
                ReturnToOriginalPosition();
            }
        }
        
        private void EnsureCollider2D()
        {
            if (_collider == null && _spriteRenderer != null && _spriteRenderer.sprite != null)
            {
                var boxCollider = gameObject.AddComponent<BoxCollider2D>();
                boxCollider.size = _spriteRenderer.sprite.bounds.size;
                boxCollider.offset = _spriteRenderer.sprite.bounds.center;
                _collider = boxCollider;
            }
        }
        
        private void OnMouseDown()
        {
            if (!IsPlacingState()) return;
            if (_isDraggable || _isPlaced)
            {
                StartManualDrag(transform.position);
            }
        }
        
        private void OnMouseEnter() { }
        private void OnMouseExit() { }
        
        private void OnMouseUp()
        {
            if (_isDragging)
            {
                Vector3 mousePos = _camera.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = transform.position.z;
                EndManualDrag(mousePos);
            }
        }
        
        public void OnRemoved()
        {
            _isPlaced = false;
            
            if (_combatComponent != null)
            {
                var combatInitializable = _combatComponent as ICombatInitializable;
                combatInitializable?.StopCombat();
            }
            
            if (!_isDragging)
            {
                ReturnToOriginalPosition();
            }
        }
        
        public Vector3? GetOriginalScale()
        {
            return _originalScale;
        }
    }
}
