using UnityEngine;
using UISystemModule.Core.Interfaces;

namespace UISystemModule.UIElements
{
    public abstract class BaseUIElement : MonoBehaviour, IUIElement
    {        [SerializeField] protected string _elementId;
        [SerializeField] protected bool _startActive = true;
        
        protected bool _isInitialized = false;
        protected bool _isActive = false;
        
        public string ElementId => _elementId;
        public bool IsActive => _isActive;
        
        protected virtual void Awake()
        {
            if (string.IsNullOrEmpty(_elementId)) _elementId = gameObject.name;
        }
        
        protected virtual void Start()
        {
            if (_startActive)
            {
                Initialize();
                Show();
            }
        }
        
        public virtual void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
            OnInitialize();
        }
        
        public virtual void Show()
        {
            if (!_isInitialized) Initialize();
            gameObject.SetActive(true);
            _isActive = true;
            OnShow();
        }
        
        public virtual void Hide()
        {
            gameObject.SetActive(false);
            _isActive = false;
            OnHide();
        }
        
        public virtual void Destroy()
        {
            OnDestroy();
            if (gameObject != null) DestroyImmediate(gameObject);
        }
        
        protected virtual void OnInitialize() { }
        protected virtual void OnShow() { }
        protected virtual void OnHide() { }
        protected virtual void OnDestroy() { }
    }
}
