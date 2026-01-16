using System;
using UnityEngine;

namespace BoardGameTestCase.Core.Common
{
    public abstract class DisposableMonoBehaviour : MonoBehaviour, IDisposable
    {
        private CompositeDisposable _disposables = new CompositeDisposable();
        private bool _isDisposed = false;
        
        protected CompositeDisposable Disposables => _disposables;
        
        protected virtual void Awake()
        {
        }
        
        protected virtual void Start()
        {
        }
        
        protected virtual void OnDestroy()
        {
            Dispose();
        }
        
        protected virtual void OnDisable()
        {
        }
        
        public virtual void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _disposables?.Dispose();
            OnDisposed();
        }
        
        protected virtual void OnDisposed()
        {
        }
        
        protected void Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            var subscription = EventBus.Instance.Subscribe(handler);
            _disposables.Add(subscription);
        }
    }
}


