using System;
using System.Collections.Generic;
using UnityEngine;

namespace BoardGameTestCase.Core.Common
{
    public class ServiceLocator : MonoBehaviour
    {
        private static ServiceLocator _instance;
        private Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private Dictionary<Type, List<object>> _multiServices = new Dictionary<Type, List<object>>();
        
        public static ServiceLocator Instance
        {
            get
            {
                if (_instance == null || _instance.gameObject == null)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        return null;
                    }
#endif
                    var go = new GameObject("ServiceLocator");
                    _instance = go.AddComponent<ServiceLocator>();
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null || _instance.gameObject == null)
            {
                _instance = this;
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
#if UNITY_EDITOR
        private void OnDisable()
        {
            if (!Application.isPlaying && _instance == this)
            {
                try
                {
                    Clear();
                }
                catch
                {
                }
                finally
                {
                    _instance = null;
                }
            }
        }
#endif
        
        public void Register<T>(T service) where T : class
        {
            Type serviceType = typeof(T);
            
            if (!_multiServices.ContainsKey(serviceType))
            {
                _multiServices[serviceType] = new List<object>();
            }
            
            if (!_multiServices[serviceType].Contains(service))
            {
                _multiServices[serviceType].Add(service);
            }
            
            _services[serviceType] = service;
        }
        
        public void Unregister<T>() where T : class
        {
            Type serviceType = typeof(T);
            _services.Remove(serviceType);
            
            if (_multiServices.ContainsKey(serviceType))
            {
                _multiServices[serviceType].Clear();
            }
        }
        
        public void Unregister<T>(T service) where T : class
        {
            Type serviceType = typeof(T);
            
            if (_multiServices.ContainsKey(serviceType))
            {
                _multiServices[serviceType].Remove(service);
            }
            
            if (_services.ContainsKey(serviceType) && _services[serviceType] == service)
            {
                _services.Remove(serviceType);
            }
        }
        
        public T Get<T>() where T : class
        {
            Type serviceType = typeof(T);
            if (_services.TryGetValue(serviceType, out var service)) return service as T;
            return null;
        }
        
        public T TryGet<T>() where T : class
        {
            Type serviceType = typeof(T);
            if (_services.TryGetValue(serviceType, out var service))
            {
                return service as T;
            }
            return default(T);
        }
        
        public bool IsRegistered<T>() where T : class => _services.ContainsKey(typeof(T));
        
        public IEnumerable<T> GetAll<T>() where T : class
        {
            List<T> results = new List<T>();
            Type serviceType = typeof(T);
            
            if (_multiServices.ContainsKey(serviceType))
            {
                foreach (var service in _multiServices[serviceType])
                {
                    if (service is T t)
                    {
                        results.Add(t);
                    }
                }
            }
            else
            {
                foreach (var service in _services.Values)
                {
                    if (service is T t)
                    {
                        results.Add(t);
                    }
                }
            }
            
            return results;
        }
        
        public void Clear()
        {
            _services.Clear();
            _multiServices.Clear();
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
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosing += OnSceneClosing;
            UnityEditor.SceneManagement.EditorSceneManager.sceneClosed += OnSceneClosed;
            UnityEditor.SceneManagement.EditorSceneManager.sceneOpened += OnSceneOpened;
        }
        
        private static void OnSceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
        {
            if (!UnityEditor.EditorApplication.isPlaying && _instance != null)
            {
                try
                {
                    if (_instance.gameObject == null)
                    {
                        CleanupInstance();
                        return;
                    }
                    
                    var instanceScene = _instance.gameObject.scene;
                    if (instanceScene.IsValid() && (instanceScene == scene || instanceScene.name == scene.name))
                    {
                        CleanupInstance();
                    }
                }
                catch
                {
                    CleanupInstance();
                }
            }
        }
        
        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                CleanupInstance();
            }
        }
        
        private static void OnSceneClosed(UnityEngine.SceneManagement.Scene scene)
        {
            if (!UnityEditor.EditorApplication.isPlaying && _instance != null)
            {
                try
                {
                    if (_instance.gameObject == null)
                    {
                        CleanupInstance();
                        return;
                    }
                    
                    var instanceScene = _instance.gameObject.scene;
                    if (!instanceScene.IsValid() || instanceScene == scene)
                    {
                        CleanupInstance();
                    }
                }
                catch
                {
                    CleanupInstance();
                }
            }
        }
        
        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode)
        {
            if (!UnityEditor.EditorApplication.isPlaying && _instance != null)
            {
                if (_instance.gameObject == null || !_instance.gameObject.scene.IsValid())
                {
                    CleanupInstance();
                }
            }
        }
        
        private static void CleanupInstance()
        {
            if (_instance != null)
            {
                try
                {
                    _instance.Clear();
                    if (_instance.gameObject != null)
                    {
                        UnityEngine.Object.DestroyImmediate(_instance.gameObject);
                    }
                }
                catch
                {
                }
                finally
                {
                    _instance = null;
                }
            }
        }
#endif
    }
}
