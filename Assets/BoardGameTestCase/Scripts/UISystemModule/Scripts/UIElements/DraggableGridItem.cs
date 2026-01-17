using UnityEngine;
using UnityEngine.EventSystems;
using GridSystemModule.Core.Interfaces;
using BoardGameTestCase.Core.Common;

namespace UISystemModule.UIElements
{
    public class DraggableGridItem : BaseUIElement, IPlaceable, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Vector2Int _gridSize = new Vector2Int(1, 1);
        [SerializeField] private string _placeableId;
        
        [SerializeField] private Canvas _canvas;
        [SerializeField] private MonoBehaviour _placementSystemReference;
        [SerializeField] private UnityEngine.UI.Image _image;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _draggingColor = new Color(1f, 1f, 1f, 0.7f);
        [SerializeField] private Color _invalidColor = Color.red;
        
        private Vector3 _originalPosition;
        private Transform _originalParent;
        private Vector3 _originalScale;
        private IGridPlacementSystem _placementSystem;
        private bool _isDragging = false;
        private bool _isPlaced = false;
        private Vector2Int _gridPosition;
        
        public string PlaceableId => _placeableId;
        public Vector2Int GridSize => _gridSize;
        public bool IsDragging { get => _isDragging; set => _isDragging = value; }
        public bool IsPlaced { get => _isPlaced; set => _isPlaced = value; }
        public Vector2Int GridPosition { get => _gridPosition; set => _gridPosition = value; }
        public Transform Transform => transform;
        
        protected override void Awake()
        {
            base.Awake();
            if (string.IsNullOrEmpty(_placeableId)) _placeableId = gameObject.name;
            if (_canvasGroup == null) _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _originalScale = transform.localScale;
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (_placementSystemReference != null) _placementSystem = _placementSystemReference as IGridPlacementSystem;
            else
            {
                _placementSystem = ServiceLocator.Instance?.Get<IGridPlacementSystem>();
            }
        }
        
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_placementSystem == null) return;
            _originalPosition = transform.position;
            _originalParent = transform.parent;
            transform.SetParent(_canvas.transform, true);
            transform.SetAsLastSibling();
            _placementSystem.StartDragging(this);
            OnDragStart();
        }
        
        public void OnDrag(PointerEventData eventData)
        {
            if (_placementSystem == null) return;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out Vector3 worldPosition))
            {
                transform.position = worldPosition;
                _placementSystem.UpdateDrag(worldPosition);
                OnDrag(worldPosition);
            }
        }
        
        public void OnEndDrag(PointerEventData eventData)
        {
            if (_placementSystem == null) return;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(_canvas.transform as RectTransform, eventData.position, eventData.pressEventCamera, out Vector3 worldPosition))
            {
                _placementSystem.EndDragging(worldPosition);
            }
            else ReturnToOriginalPosition();
        }
        
        public void OnDragStart()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0.7f;
                _canvasGroup.blocksRaycasts = false;
            }
            SetColor(_draggingColor);
        }
        
        public void OnDrag(Vector3 worldPosition) { }
        
        public void OnDrop(Vector2Int gridPosition, bool isValid)
        {
            if (isValid) SetColor(_normalColor);
            else ReturnToOriginalPosition();
        }
        
        public void OnPlaced(Vector2Int gridPosition)
        {
            _gridPosition = gridPosition;
            _isPlaced = true;
            SetColor(_normalColor);
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
            }
        }
        
        public void OnRemoved()
        {
            _isPlaced = false;
            ReturnToOriginalPosition();
        }
        
        public Vector3? GetOriginalScale()
        {
            return _originalScale;
        }
        
        private void ReturnToOriginalPosition()
        {
            transform.SetParent(_originalParent, true);
            transform.position = _originalPosition;
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.blocksRaycasts = true;
            }
            SetColor(_normalColor);
        }
        
        private void SetColor(Color color)
        {
            if (_image != null) _image.color = color;
        }
        
        public void SetGridSize(Vector2Int newSize) => _gridSize = newSize;
        public void SetPlaceableId(string id) => _placeableId = id;

        public void PlayFailAnimation()
        {
            // Simple visual feedback for UI items (could be expanded)
            if (_image != null)
            {
                _image.color = _invalidColor;
                // Using a simple delayed call to return to normal color or DRAGGING color if still dragging
                BoardGameTestCase.Core.Common.ServiceLocator.Instance?.Get<MonoBehaviour>().StartCoroutine(ResetColorAfterDelay(0.4f));
            }
        }

        private System.Collections.IEnumerator ResetColorAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            SetColor(_isDragging ? _draggingColor : _normalColor);
        }
    }
}
