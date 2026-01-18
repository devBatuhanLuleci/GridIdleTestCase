using System;
using System.Collections.Generic;
using UnityEngine;

namespace BoardGameTestCase.Core.Common
{
    public class EventBus : MonoBehaviour
    {
        private static EventBus _instance;
        private Dictionary<Type, List<IEventHandler>> _handlers = new Dictionary<Type, List<IEventHandler>>();
        private Dictionary<Type, List<object>> _handlerObjects = new Dictionary<Type, List<object>>();
        
        public static EventBus Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("EventBus");
                    _instance = go.AddComponent<EventBus>();
                    go.transform.SetParent(null);
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        public IDisposable Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null) return null;
            
            Type eventType = typeof(T);
            if (!_handlers.ContainsKey(eventType))
            {
                _handlers[eventType] = new List<IEventHandler>();
                _handlerObjects[eventType] = new List<object>();
            }
            
            var wrapper = new EventHandler<T>(handler);
            _handlers[eventType].Add(wrapper);
            _handlerObjects[eventType].Add(handler);
            
            return new EventSubscription<T>(this, handler);
        }
        
        public void Publish<T>(T eventData) where T : IGameEvent
        {
            Type eventType = typeof(T);
            if (!_handlers.ContainsKey(eventType)) return;
            
            var handlersCopy = new List<IEventHandler>(_handlers[eventType]);
            foreach (var handler in handlersCopy)
            {
                try
                {
                    handler.Handle(eventData);
                }
                catch (Exception)
                {
                }
            }
        }
        
        internal void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null) return;
            
            Type eventType = typeof(T);
            if (!_handlers.ContainsKey(eventType)) return;
            
            int index = _handlerObjects[eventType].IndexOf(handler);
            if (index >= 0)
            {
                _handlers[eventType].RemoveAt(index);
                _handlerObjects[eventType].RemoveAt(index);
                
                if (_handlers[eventType].Count == 0)
                {
                    _handlers.Remove(eventType);
                    _handlerObjects.Remove(eventType);
                }
            }
        }
        
        public void Clear()
        {
            _handlers.Clear();
            _handlerObjects.Clear();
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                Clear();
                _instance = null;
            }
        }
        
        private void OnApplicationQuit()
        {
            if (_instance == this)
            {
                Clear();
                _instance = null;
            }
        }
        
#if UNITY_EDITOR
        private static bool _editorCleanupRegistered = false;
        
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterEditorCleanup()
        {
            if (_editorCleanupRegistered) return;
            _editorCleanupRegistered = true;
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                if (_instance != null)
                {
                    _instance.Clear();
                    if (_instance.gameObject != null) UnityEngine.Object.DestroyImmediate(_instance.gameObject);
                    _instance = null;
                }
            }
        }
#endif
    }
    
    internal interface IEventHandler
    {
        void Handle(object eventData);
    }
    
    internal class EventHandler<T> : IEventHandler where T : IGameEvent
    {
        private readonly Action<T> _handler;
        
        public EventHandler(Action<T> handler)
        {
            _handler = handler;
        }
        
        public void Handle(object eventData)
        {
            if (eventData is T typedEvent)
            {
                _handler(typedEvent);
            }
        }
    }
    
    public class EventSubscription<T> : IDisposable where T : IGameEvent
    {
        private readonly EventBus _eventBus;
        private readonly Action<T> _handler;
        private bool _isDisposed = false;
        
        public EventSubscription(EventBus eventBus, Action<T> handler)
        {
            _eventBus = eventBus;
            _handler = handler;
        }
        
        public void Dispose()
        {
            if (_isDisposed) return;
            _isDisposed = true;
            _eventBus?.Unsubscribe(_handler);
        }
    }
    
    public interface IGameEvent
    {
    }
}


