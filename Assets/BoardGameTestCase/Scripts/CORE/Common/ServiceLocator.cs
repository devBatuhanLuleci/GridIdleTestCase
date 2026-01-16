using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoardGameTestCase.Core.Common
{
    public class ServiceLocator : MonoBehaviour
    {
        private static ServiceLocator _instance;
        private static bool _isQuitting = false;
        private Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private Dictionary<Type, List<object>> _multiServices = new Dictionary<Type, List<object>>();
        
        public static ServiceLocator Instance
        {
            get
            {
                if (_isQuitting) return null;
                
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
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null || _instance.gameObject == null)
            {
                _instance = this;
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
                _isQuitting = true;
                Clear();
                _instance = null;
            }
        }
        
        private void OnApplicationQuit()
        {
            _isQuitting = true;
            if (_instance == this)
            {
                Clear();
                _instance = null;
                
                // Destroy the GameObject when application quits
                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
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
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                // Aggressively destroy ALL ServiceLocator instances when scene is closing
                try
                {
                    var allLocators = FindObjectsOfType<ServiceLocator>(true);
                    foreach (var locator in allLocators)
                    {
                        if (locator != null && locator.gameObject != null)
                        {
                            try
                            {
                                UnityEngine.Object.DestroyImmediate(locator.gameObject);
                            }
                            catch { }
                        }
                    }
                }
                catch { }
                finally
                {
                    _instance = null;
                }
            }
        }
        
        private static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange state)
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
            {
                // Set quitting flag FIRST to prevent new instances
                _isQuitting = true;
                
                // Force cleanup when exiting play mode - destroy ALL ServiceLocator instances
                CleanupInstance();
                
                // Double-check: find and destroy any remaining ServiceLocator instances
                try
                {
                    var remaining = FindObjectsOfType<ServiceLocator>(true);
                    foreach (var locator in remaining)
                    {
                        if (locator != null && locator.gameObject != null)
                        {
                            UnityEngine.Object.DestroyImmediate(locator.gameObject);
                        }
                    }
                }
                catch { }
            }
            else if (state == UnityEditor.PlayModeStateChange.EnteredEditMode)
            {
                // Reset quitting flag when entering edit mode
                _isQuitting = false;
            }
        }
        
        private static void OnSceneClosed(UnityEngine.SceneManagement.Scene scene)
        {
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                // Extra safety: destroy any remaining ServiceLocator instances
                try
                {
                    var allLocators = FindObjectsOfType<ServiceLocator>(true);
                    foreach (var locator in allLocators)
                    {
                        if (locator != null && locator.gameObject != null)
                        {
                            try
                            {
                                UnityEngine.Object.DestroyImmediate(locator.gameObject);
                            }
                            catch { }
                        }
                    }
                }
                catch { }
                finally
                {
                    _instance = null;
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
            
            // Also find and destroy any other ServiceLocator instances that might exist
            // Include inactive GameObjects with true parameter
            #if UNITY_EDITOR
            try
            {
                var allLocators = FindObjectsOfType<ServiceLocator>(true);
                foreach (var locator in allLocators)
                {
                    if (locator != null && locator.gameObject != null && locator.gameObject != _instance?.gameObject)
                    {
                        UnityEngine.Object.DestroyImmediate(locator.gameObject);
                    }
                }
            }
            catch { }
            #endif
        }
#endif
    }
}
