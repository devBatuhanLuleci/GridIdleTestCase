using System.Collections.Generic;
using UnityEngine;
using UISystemModule.Core.Interfaces;

namespace UISystemModule.UIElements
{
    public abstract class BaseUIPanel : BaseUIElement, IUIPanel
    {        [SerializeField] protected Transform _contentParent;
        
        protected List<IUIElement> _childElements = new List<IUIElement>();
        
        public IReadOnlyList<IUIElement> ChildElements => _childElements;
        
        protected override void Awake()
        {
            base.Awake();
            if (_contentParent == null) _contentParent = transform;
        }
        
        public virtual void AddChild(IUIElement element)
        {
            if (element == null) return;
            if (!_childElements.Contains(element))
            {
                _childElements.Add(element);
                if (element is MonoBehaviour monoElement) monoElement.transform.SetParent(_contentParent, false);
                OnChildAdded(element);
            }
        }
        
        public virtual void RemoveChild(IUIElement element)
        {
            if (element == null) return;
            if (_childElements.Contains(element))
            {
                _childElements.Remove(element);
                OnChildRemoved(element);
            }
        }
        
        public virtual IUIElement GetChild(string elementId) => _childElements.Find(child => child.ElementId == elementId);
        
        public virtual void ClearChildren()
        {
            for (int i = _childElements.Count - 1; i >= 0; i--)
            {
                var child = _childElements[i];
                RemoveChild(child);
                child.Destroy();
            }
            _childElements.Clear();
        }
        
        protected virtual void OnChildAdded(IUIElement child) { }
        protected virtual void OnChildRemoved(IUIElement child) { }
        
        protected override void OnDestroy()
        {
            ClearChildren();
            base.OnDestroy();
        }
    }
}
