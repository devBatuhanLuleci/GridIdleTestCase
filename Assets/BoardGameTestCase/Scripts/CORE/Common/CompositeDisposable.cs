using System;
using System.Collections.Generic;

namespace BoardGameTestCase.Core.Common
{
    public class CompositeDisposable : IDisposable
    {
        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private bool _isDisposed = false;
        
        public void Add(IDisposable disposable)
        {
            if (_isDisposed)
            {
                disposable?.Dispose();
                return;
            }
            
            if (disposable != null)
            {
                _disposables.Add(disposable);
            }
        }
        
        public void AddRange(IEnumerable<IDisposable> disposables)
        {
            if (_isDisposed)
            {
                foreach (var disposable in disposables)
                {
                    disposable?.Dispose();
                }
                return;
            }
            
            foreach (var disposable in disposables)
            {
                if (disposable != null)
                {
                    _disposables.Add(disposable);
                }
            }
        }
        
        public void Remove(IDisposable disposable)
        {
            if (_isDisposed) return;
            _disposables.Remove(disposable);
        }
        
        public void Clear()
        {
            if (_isDisposed) return;
            
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            
            foreach (var disposable in _disposables)
            {
                disposable?.Dispose();
            }
            _disposables.Clear();
        }
        
        public int Count => _disposables.Count;
        public bool IsDisposed => _isDisposed;
    }
}


