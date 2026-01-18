using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using DG.Tweening;
using GridSystemModule.Core.Interfaces;
using BoardGameTestCase.Core.ScriptableObjects;
using UISystemModule.Core.Interfaces;
using BoardGameTestCase.Core.Common;
using GameModule.Core.Interfaces;
using GameState = GameModule.Core.Interfaces.GameState;
using BoardGameTestCase.Core;

namespace UISystemModule.UIElements
{
    public class GridItem2D : MonoBehaviour, IPlaceable
    {
        // Sorting Layer names
        private const string DRAGGABLE_SORTING_LAYER = "DraggableItem";
        private const string DRAGGED_SORTING_LAYER = "DraggedItem";
        
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
        [SerializeField] private float _dragUpdateDuration = 0.03f; // Duration for smooth drag updates
        [SerializeField] private Ease _dragUpdateEase = Ease.Linear; // Easing for drag updates
        [SerializeField] private float _manualDragDuration = 0.1f; // Duration for initial click-to-position movement
        [SerializeField] private Ease _manualDragEase = Ease.OutCubic; // Easing for initial drag movement
        
        [Header("Selection Punch Effect Settings")]
        [SerializeField] private float _selectionPunchStrength = 0.3f; // Punch strength for selection effect (scale bounce)
        [SerializeField] private float _selectionPunchDuration = 0.3f; // Duration of selection punch animation
        [SerializeField] private int _selectionPunchVibrato = 10; // Vibrato (elasticity) of punch effect
        
        [Header("Placement Animation Settings")]
        [SerializeField] private float _placementMoveDuration = 0.25f; // Duration for placement movement
        [SerializeField] private Ease _placementMoveEase = Ease.OutBack; // Easing for placement movement
        [SerializeField] private float _placementScaleDuration = 0.25f; // Duration for placement scale
        [SerializeField] private Ease _placementScaleEase = Ease.OutBack; // Easing for placement scale
        [SerializeField] private float _placementPunchPositionStrength = 0.1f; // Position punch strength (world units)
        [SerializeField] private float _placementPunchScaleStrength = 0.2f; // Scale punch strength (multiplier)
        [SerializeField] private float _placementPunchDuration = 0.3f; // Punch effect duration
        [SerializeField] private float _placementPunchDelay = 0.15f; // Delay before punch effect
        
        [Header("Return Animation Settings")]
        [SerializeField] private float _returnDuration = 0.3f; // Duration for return to inventory animation
        [SerializeField] private Ease _returnEase = Ease.OutCubic; // Easing for return animation
        
        [Header("Fail Animation Settings")]
        [SerializeField] private float _failPunchStrength = 0.2f; // Movement strength of fail shake
        [SerializeField] private float _failPunchDuration = 0.4f; // Duration of fail animation
        [SerializeField] private Color _failFlashColor = Color.red; // Color to flash on failure
        [SerializeField] private float _failFlashDuration = 0.4f; // How long the color flash lasts
        
        [Header("Discard Animation Settings")]
        [SerializeField] private float _discardDuration = 0.8f;
        [SerializeField] private float _discardRotationAmount = 720f;
        [SerializeField] private float _discardBezierHeight = 3f;
        [SerializeField] private Ease _discardEase = Ease.InQuad;

        [Header("Outline Selection Animation Settings")]
        [SerializeField] private float _dropOutlineFadeDuration = 0.3f;
        [SerializeField] private float _idleOutlineWidth = 1f;
        [SerializeField] private float _placedOutlineWidth = 1.2f;
        [SerializeField] private float _dragOutlineGlowValue = 2f;
        [SerializeField] private float _dragOutlineWidthValue = 2f;
        
        [Header("Reload Animation Settings")]
        [SerializeField] private bool _enableReloadAnimation = true;
        [SerializeField] private float _reloadDuration = 3f;
        [SerializeField] private bool _loopReload = true;
        
        [Header("Reload Complete Animation Settings")]
        [SerializeField] private float _reloadCompleteScaleDuration = 0.2f;
        [SerializeField] private Ease _reloadCompleteScaleEase = Ease.OutBack;
        [SerializeField] private float _reloadCompletePunchScaleStrength = 0.2f;
        [SerializeField] private float _reloadCompletePunchDuration = 0.3f;

        private Material _instancedMaterial;
        private static readonly int UseOutlineProp = Shader.PropertyToID("_UseOutline");
        private static readonly int OutlineWidthProp = Shader.PropertyToID("_OutlineWidth");
        private static readonly int OutlineGlowProp = Shader.PropertyToID("_OutlineGlow");
        private static readonly int UseShineProp = Shader.PropertyToID("_UseShine");
        private static readonly int UseFillTileProp = Shader.PropertyToID("_UseFillTile");
        private static readonly int FillVerticalProgressProp = Shader.PropertyToID("_FillVerticalProgress");
        private static readonly int FillTexProp = Shader.PropertyToID("_FillTex");
        
        private float _initialOutlineGlow;
        private bool _initialShineState;
        private Tween _glowTween;
        private Tween _widthTween;
        private Tween _reloadTween;

        public event System.Action OnReloadComplete;
        
        private Vector3 _originalPosition;
        private Transform _originalParent;
        private Vector3 _originalScale;
        private bool _isDragging = false;
        private bool _isPlaced = false;
        private bool _isBeingDiscarded = false;
        private Vector2Int _gridPosition;
        private IGridPlacementSystem _placementSystem;
        private Camera _camera;
        private bool _wasTouchingLastFrame = false;
        private IGameFlowController _gameFlowController;
        private string _originalSortingLayerName;
        public string PlaceableId => _placeableId;
        public Vector2Int GridSize => _gridSize;
        public bool IsDragging { get => _isDragging; set => _isDragging = value; }
        public bool IsPlaced { get => _isPlaced; set => _isPlaced = value; }
        public Vector2Int GridPosition { get => _gridPosition; set => _gridPosition = value; }
        public Transform Transform => transform;
        
        private void Awake()
        {
            if (_defenceItemData != null) LoadFromDefenceItemData();
            else if (string.IsNullOrEmpty(_placeableId)) _placeableId = gameObject.name;
            _camera = Camera.main;
            FindPlacementSystem();
            // DON'T fetch GameFlowController here - initialization order issue
            // It will be fetched in Start() or lazily when needed
            _originalScale = transform.localScale;
            
            // Set default sorting layer to DraggableItem
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingLayerName = DRAGGABLE_SORTING_LAYER;
                _originalSortingLayerName = _spriteRenderer.sortingLayerName;
                
                // Eagerly create material instance
                _instancedMaterial = _spriteRenderer.material;
                _initialOutlineGlow = _instancedMaterial.GetFloat(OutlineGlowProp);
                _initialShineState = _instancedMaterial.GetFloat(UseShineProp) > 0.5f;
                UpdateShineState();
            }
            
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

        private void OnDestroy()
        {
            // Kill any active tweens on this object to prevent errors when destroyed
            _glowTween?.Kill();
            _widthTween?.Kill();
            _reloadTween?.Kill();
            transform.DOKill();
            if (_spriteRenderer != null) _spriteRenderer.DOKill();

            // Clean up instanced material
            if (_instancedMaterial != null)
            {
                if (Application.isPlaying) Destroy(_instancedMaterial);
                else DestroyImmediate(_instancedMaterial);
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
            _reloadDuration = _defenceItemData.ReloadDuration;
            ApplySprite(_defenceItemData.Sprite);
        }

        private void ApplySprite(Sprite sprite)
        {
            if (_spriteRenderer == null)
            {
                _spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (_spriteRenderer == null)
            {
                return;
            }

            _spriteRenderer.sprite = sprite;

            if (_spriteRenderer.sprite == null)
            {
                CreateColoredSprite();
            }

            _spriteRenderer.enabled = true;
            _spriteRenderer.color = _normalColor;
            _spriteRenderer.sortingOrder = 1;

            // Scale to grid footprint, then fit collider to the assigned sprite
            UpdateVisualScaleFromGridSize();
            EnsureCollider2D();
        }
        
        private void UpdateVisualScaleFromGridSize()
        {
            if (_spriteRenderer == null) return;
            
            // Fetch grid cell size from placement system by sampling GridToWorld; fallback to 1x1 if unavailable
            Vector2 cellSize = Vector2.one;
            if (_placementSystem != null)
            {
                Vector3 origin = _placementSystem.GridToWorld(Vector2Int.zero);
                Vector3 right = _placementSystem.GridToWorld(new Vector2Int(1, 0));
                Vector3 up = _placementSystem.GridToWorld(new Vector2Int(0, 1));

                float cellWidth = Mathf.Abs((right - origin).x);
                float cellHeight = Mathf.Abs((up - origin).y);

                if (cellWidth > 0.0001f) cellSize.x = cellWidth;
                if (cellHeight > 0.0001f) cellSize.y = cellHeight;
            }
            
            float spriteWidth = _spriteRenderer.sprite != null ? _spriteRenderer.sprite.bounds.size.x : 1f;
            float spriteHeight = _spriteRenderer.sprite != null ? _spriteRenderer.sprite.bounds.size.y : 1f;
            
            // Calculate target footprint in world units using grid cell size
            float targetWidth = _gridSize.x * cellSize.x;
            float targetHeight = _gridSize.y * cellSize.y;
            
            float scaleX = spriteWidth > 0 ? targetWidth / spriteWidth : 1f;
            float scaleY = spriteHeight > 0 ? targetHeight / spriteHeight : 1f;
            
            // Use the smaller scale to preserve aspect ratio and fit inside footprint
            float uniformScale = Mathf.Min(scaleX, scaleY);
            
            transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
            
            // IMPORTANT: Update _originalScale after calculating the correct scale
            // This ensures animations return to the correct size
            _originalScale = transform.localScale;
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
            
            // Save original sorting layer and set to DraggedItem
            if (_spriteRenderer != null)
            {
                _originalSortingLayerName = _spriteRenderer.sortingLayerName;
                _spriteRenderer.sortingLayerName = DRAGGED_SORTING_LAYER;
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
                
                // Correctly handle the visual position during drag
                transform.DOKill(false);
                transform.DOMove(worldPosition, _dragUpdateDuration).SetEase(_dragUpdateEase);
                
                // Do NOT update _originalPosition here. It must stay at the value set in StartDragging()
                // so that we can revert to it if placement is invalid.
                
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
            
            // IMPORTANT: Kill any ongoing animations to prevent scale/position issues
            // The placement system (OnDrop/ReturnToOriginalPosition) will handle scale animation
            transform.DOKill();
            StopOutlineAnimation();
            
            // Clear the global dragging flag
            if (_currentlyDraggingItem == this)
            {
                _currentlyDraggingItem = null;
            }
            
            // Restore sorting layer to DraggableItem
            if (_spriteRenderer != null)
            {
                _spriteRenderer.sortingLayerName = DRAGGABLE_SORTING_LAYER;
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
            if (_isBeingDiscarded) return;
            if (!IsPlacingState()) return;
            if (!_isDraggable && !_isPlaced) return;
            
            StopReloadAnimation();
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
            _isDragging = false;
            StopOutlineAnimation();
            
            Vector3 worldPosition = _camera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, _camera.nearClipPlane));
            worldPosition.z = transform.position.z;
            if (_placementSystem != null) _placementSystem.EndDragging(worldPosition);
            else ReturnToOriginalPosition();
        }
        
        public void ReturnToOriginalPosition()
        {
            if (_originalParent == null && _originalPosition == Vector3.zero && !_isPlaced)
            {
                Destroy(gameObject);
                return;
            }
            
            transform.SetParent(_originalParent, true);
            
            // Animate return to EXACT original WORLD position
            transform.DOMove(_originalPosition, _returnDuration).SetEase(_returnEase);
            transform.DOScale(_originalScale, _returnDuration).SetEase(_returnEase);
            
            SetColor(_normalColor);
            StopOutlineAnimation();
        }
        
        private void SetColor(Color color)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = color;
            }
        }

        private void UpdateShineState()
        {
            if (_instancedMaterial == null) return;

            // Shine is ON only if: 
            // 1. It was ON initially
            // 2. It is NOT currently placed on the grid
            // 3. It is NOT currently being dragged
            bool shouldShowShine = _initialShineState && !_isPlaced && !_isDragging;

            _instancedMaterial.SetFloat(UseShineProp, shouldShowShine ? 1f : 0f);
            if (shouldShowShine)
                _instancedMaterial.EnableKeyword("_SHINE_ON");
            else
                _instancedMaterial.DisableKeyword("_SHINE_ON");
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
            ApplySprite(sprite);
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
            if (_spriteRenderer != null)
            {
                ApplySprite(_defenceItemData != null ? _defenceItemData.Sprite : _spriteRenderer.sprite);
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
        public string DisplayName => _defenceItemData != null ? _defenceItemData.DisplayName : gameObject.name;
        
        public void OnDragStart()
        {
            _isDragging = true;
            SetColor(_draggingColor);
            
            // Punch effect on selection - like snapping into a slot
            transform.DOPunchScale(Vector3.one * _selectionPunchStrength, _selectionPunchDuration, _selectionPunchVibrato, 1);

            StartOutlineAnimation();
        }

        private void StartOutlineAnimation()
        {
            if (_instancedMaterial != null)
            {
                _instancedMaterial.SetFloat(UseOutlineProp, 1f);
                _instancedMaterial.EnableKeyword("_OUTLINE_ON");

                // Sync shine state (will turn off since _isDragging is true)
                UpdateShineState();

                _glowTween?.Kill();
                _widthTween?.Kill();

                // Animate Glow to target value
                _glowTween = _instancedMaterial.DOFloat(_dragOutlineGlowValue, OutlineGlowProp, _selectionPunchDuration)
                    .SetEase(Ease.OutSine);
                
                // Animate Width to target value
                _widthTween = _instancedMaterial.DOFloat(_dragOutlineWidthValue, OutlineWidthProp, _selectionPunchDuration)
                    .SetEase(Ease.OutSine);
            }
        }

        private void StopOutlineAnimation()
        {
            if (_instancedMaterial != null)
            {
                _glowTween?.Kill();
                _widthTween?.Kill();

                // Animate Glow back to initial value
                _glowTween = _instancedMaterial.DOFloat(_initialOutlineGlow, OutlineGlowProp, _dropOutlineFadeDuration)
                    .SetEase(Ease.InSine)
                    .OnComplete(() =>
                    {
                        if (_instancedMaterial != null && !_isPlaced)
                        {
                            _instancedMaterial.SetFloat(UseOutlineProp, 0f);
                            _instancedMaterial.DisableKeyword("_OUTLINE_ON");
                        }
                    });

                // Animate Width back to target value
                float targetWidth = _isPlaced ? _placedOutlineWidth : _idleOutlineWidth;
                _widthTween = _instancedMaterial.DOFloat(targetWidth, OutlineWidthProp, _dropOutlineFadeDuration)
                    .SetEase(Ease.InSine);

                // Sync shine state (will restore if not placed)
                UpdateShineState();
            }
        }
        
        public void OnDrag(Vector3 worldPosition)
        {
        }

        [ContextMenu("Test Drag Start")]
        public void TestDragStart()
        {
            OnDragStart();
        }

        [ContextMenu("Test Drop")]
        public void TestDrop()
        {
            OnDrop(_gridPosition, true);
        }

        [ContextMenu("Test Return")]
        public void TestReturn()
        {
            ReturnToOriginalPosition();
        }

        [ContextMenu("Test Fail Animation")]
        public void TestFailAnimation()
        {
            PlayFailAnimation();
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
                    Vector3 targetWorldPosition = GetCorrectPlacedWorldPosition(gridPosition);
                    
                    // Kill any ongoing drag tweens to ensure the placement animation wins
                    transform.DOKill();
                    
                    // Smooth placement animation to the centered world position
                    transform.DOMove(targetWorldPosition, _placementMoveDuration).SetEase(_placementMoveEase);
                    // Scale back to original
                    transform.DOScale(_originalScale, _placementScaleDuration).SetEase(_placementScaleEase);
                    
                    // Dual Punch effect (Position + Scale) for maximum "snap" feel
                    transform.DOPunchPosition(Vector3.down * _placementPunchPositionStrength, _placementPunchDuration, 10, 1)
                             .SetDelay(_placementPunchDelay);
                    transform.DOPunchScale(Vector3.one * _placementPunchScaleStrength, _placementPunchDuration, 10, 1)
                             .SetDelay(_placementPunchDelay);
                }
                
                StopOutlineAnimation();
                AttachCombatComponentIfNeeded();
            }
            else
            {
                // If it's invalid, the GridPlacementSystem already calls RevertDraggedToStartPosition()
                // which moves it back to _dragStartWorldPos.
                // We only need to call ReturnToOriginalPosition() if we are NOT on a grid or grid failed logic.
                // But for safety and consistency, let's let the GridPlacementSystem handle the world revert,
                // and we'll handle the UI/State revert here.
                
                if (_placementSystem == null || !IsPlacingState())
                {
                    ReturnToOriginalPosition();
                }
                else
                {
                    // Ensure state is reset even if GridPlacementSystem handles movement
                    if (_spriteRenderer != null && !string.IsNullOrEmpty(_originalSortingLayerName))
                    {
                        _spriteRenderer.sortingLayerName = _originalSortingLayerName;
                    }
                    SetColor(_normalColor);
                    StopOutlineAnimation();
                }
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
                Vector3 targetWorldPosition = GetCorrectPlacedWorldPosition(gridPosition);
                transform.position = targetWorldPosition;
                // Ensure scale is correct for placed items
                transform.localScale = _originalScale;
            }
            
            AttachCombatComponentIfNeeded();
            UpdateShineState();

            // Ensure outline is enabled for placed items
            if (_instancedMaterial != null)
            {
                _instancedMaterial.SetFloat(UseOutlineProp, 1f);
                _instancedMaterial.EnableKeyword("_OUTLINE_ON");
                // Ensure placed width is set
                _instancedMaterial.SetFloat(OutlineWidthProp, _placedOutlineWidth);
                
                StartReloadAnimation();
            }
        }
        
        public void PlayPlacementAnimation()
        {
            // Reset to original scale first
            transform.localScale = _originalScale;
            
            // Placement Animation Sequence
            transform.DOScale(_originalScale * 1.1f, _placementScaleDuration).SetEase(_placementScaleEase);
            
            // Dual Punch effect (Position + Scale) for maximum "snap" feel
            transform.DOPunchPosition(Vector3.down * _placementPunchPositionStrength, _placementPunchDuration, 10, 1)
                     .SetDelay(_placementPunchDelay);
            transform.DOPunchScale(Vector3.one * _placementPunchScaleStrength, _placementPunchDuration, 10, 1)
                     .SetDelay(_placementPunchDelay);
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
            
            StopReloadAnimation();
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
            
            // Smooth movement to initial click position
            transform.DOKill(false);
            transform.DOMove(mousePosition, _manualDragDuration).SetEase(_manualDragEase);
            
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
            if (_spriteRenderer == null || _spriteRenderer.sprite == null) return;

            if (_collider == null)
            {
                _collider = gameObject.AddComponent<BoxCollider2D>();
            }

            if (_collider is BoxCollider2D box)
            {
                var bounds = _spriteRenderer.sprite.bounds;
                box.size = bounds.size;
                box.offset = bounds.center;
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
                StopOutlineAnimation();
                Vector3 mousePos = _camera.ScreenToWorldPoint(Input.mousePosition);
                mousePos.z = transform.position.z;
                EndManualDrag(mousePos);
            }
        }
        
        public void OnRemoved()
        {
            _isPlaced = false;
            StopReloadAnimation();
            
            if (_combatComponent != null)
            {
                var combatInitializable = _combatComponent as ICombatInitializable;
                combatInitializable?.StopCombat();
            }
            
            if (!_isDragging)
            {
                ReturnToOriginalPosition();
            }
            UpdateShineState();
        }

        public void PlayDiscardAnimation(Vector3 trashPosition, System.Action onComplete = null)
        {
            if (_isBeingDiscarded) return;
            _isBeingDiscarded = true;
            _isDraggable = false;
            
            // Kill any active tweens
            transform.DOKill();
            _glowTween?.Kill();
            _widthTween?.Kill();

            Vector3 startPos = transform.position;
            Vector3 controlPoint = BezierUtils.GetAutomaticControlPoint(startPos, trashPosition, _discardBezierHeight, Vector3.up);

            // 1. Move along Bezier Curve
            DOVirtual.Float(0f, 1f, _discardDuration, t =>
            {
                if (this == null) return;
                transform.position = BezierUtils.GetPoint(startPos, controlPoint, trashPosition, t);
            }).SetEase(_discardEase);

            // 2. Rotate along Z axis
            transform.DORotate(new Vector3(0, 0, _discardRotationAmount), _discardDuration, RotateMode.FastBeyond360)
                .SetEase(_discardEase);

            // 3. Shrink scale
            transform.DOScale(Vector3.zero, _discardDuration)
                .SetEase(_discardEase);

            // 4. Fade out transparency (Alpha) - Safer version without needing specific DOFade extension
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
                }, 0f, _discardDuration).SetEase(_discardEase);
            }

            // 5. Cleanup on complete
            DOVirtual.DelayedCall(_discardDuration, () =>
            {
                onComplete?.Invoke();
                if (this != null && gameObject != null)
                    Destroy(gameObject);
            });
        }
        
        public void PlayDiscardAnimationTest()
        {
            // Discard to a position to the right and down
            PlayDiscardAnimation(transform.position + new Vector3(2f, -5f, 0f));
        }
        
        public Vector3? GetOriginalScale()
        {
            return _originalScale;
        }

        public void PlayFailAnimation()
        {
            // Position shake (horizontal)
            // We use DOPunchPosition which is additive and works well with existing movement
            transform.DOPunchPosition(Vector3.right * _failPunchStrength, _failPunchDuration, 20, 0.5f);
            
            // Color flash
            if (_spriteRenderer != null)
            {
                // Kill current color tweens
                _spriteRenderer.DOKill();
                
                // Sequence for color flash: normal -> red -> normal
                // Using DOTween.To to ensure compatibility with all DOTween configurations
                Sequence colorSeq = DOTween.Sequence();
                colorSeq.Append(DOTween.To(() => _spriteRenderer.color, x => _spriteRenderer.color = x, _failFlashColor, _failFlashDuration * 0.4f));
                colorSeq.Append(DOTween.To(() => _spriteRenderer.color, x => _spriteRenderer.color = x, _normalColor, _failFlashDuration * 0.6f));
            }
        }

        private Vector3 GetCorrectPlacedWorldPosition(Vector2Int gridPosition)
        {
            if (_placementSystem == null) return transform.position;
            
            var positions = new List<Vector2Int>();
            for (int x = 0; x < _gridSize.x; x++)
            {
                for (int y = 0; y < _gridSize.y; y++)
                {
                    positions.Add(new Vector2Int(gridPosition.x + x, gridPosition.y + y));
                }
            }
            return _placementSystem.MultiTileGridToWorld(positions);
        }

        public void StartReloadAnimation(float? duration = null)
        {
            if (!_enableReloadAnimation) return;
            if (duration.HasValue) _reloadDuration = duration.Value;
            
            StopReloadAnimation();
            
                if (_instancedMaterial != null)
            {
                // Ensure Fill Tile is active in the shader
                _instancedMaterial.SetFloat(UseFillTileProp, 1f);
                _instancedMaterial.EnableKeyword("_FILL_TILE_ON");
                
                // User requirement: Fill tiling texture must be same with sprite texture
                if (_spriteRenderer != null && _spriteRenderer.sprite != null)
                {
                    _instancedMaterial.SetTexture(FillTexProp, _spriteRenderer.sprite.texture);
                }
                
                // User requirement: Start at 1 (Full) then go to 0
                _instancedMaterial.SetFloat(FillVerticalProgressProp, 1f);
                
                _reloadTween = DOTween.To(() => _instancedMaterial.GetFloat(FillVerticalProgressProp), 
                    x => _instancedMaterial.SetFloat(FillVerticalProgressProp, x), 0f, _reloadDuration)
                    .SetEase(Ease.Linear)
                    .SetTarget(this)
                    .OnComplete(() => {
                        // User requirement: When it reaches 0, go value of 1 instantly
                        if (_instancedMaterial != null)
                            _instancedMaterial.SetFloat(FillVerticalProgressProp, 1f);
                            
                        OnReloadComplete?.Invoke();
                        PlayReloadCompleteAnimation();
                        
                        if (_loopReload && _isPlaced && !_isDragging && !_isBeingDiscarded)
                        {
                            StartReloadAnimation();
                        }
                    });
            }
        }
        
        // Editor accessible method
        public void PlayReloadCompleteAnimation()
        {
            if (_instancedMaterial == null) return;
            
            // Similar to placement animation: Scale up slightly and punch
            transform.DOKill(true); // Complete active tweens first
            
            // Ensure we start from original scale to avoid compounding growth in loops
            transform.localScale = _originalScale;
            
            Sequence reloadSeq = DOTween.Sequence();
            
            // 1. Scale Pulse
            reloadSeq.Append(transform.DOScale(_originalScale * 1.1f, _reloadCompleteScaleDuration).SetEase(_reloadCompleteScaleEase));
            reloadSeq.Append(transform.DOScale(_originalScale, _reloadCompleteScaleDuration).SetEase(Ease.OutCubic));
            
            // 2. Punch Scale (Overlap slightly for juicy feel)
            reloadSeq.Insert(_reloadCompleteScaleDuration * 0.8f, 
                transform.DOPunchScale(Vector3.one * _reloadCompletePunchScaleStrength, _reloadCompletePunchDuration, 10, 1));
        }

        public void StopReloadAnimation()
        {
            _reloadTween?.Kill();
            if (_instancedMaterial != null)
            {
                // Reset to 1 (Filled) as base state
                _instancedMaterial.SetFloat(FillVerticalProgressProp, 1f);
            }
        }
    }
}
