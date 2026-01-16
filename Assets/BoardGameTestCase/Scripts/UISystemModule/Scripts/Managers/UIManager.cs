using System.Collections.Generic;
using UnityEngine;
using UISystemModule.Core.Interfaces;
using UISystemModule.UIElements;
using UISystemModule.Core;
using BoardGameTestCase.Core.Common;
using GameModule.Core;

namespace UISystemModule.Managers
{
    public class UIManager : MonoBehaviour, IUIManager, IInitializable
    {
        [SerializeField] private Transform _uiRoot;
        [SerializeField] private Canvas _mainCanvas;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private bool _dontDestroyOnLoad = true;
        
        private Dictionary<string, IUIElement> _elements = new Dictionary<string, IUIElement>();
        private bool _isInitialized = false;
        private CompositeDisposable _disposables = new CompositeDisposable();
        
        public IReadOnlyDictionary<string, IUIElement> Elements => _elements;
        public Canvas MainCanvas => _mainCanvas;
        public Camera MainCamera => _mainCamera;
        public bool IsInitialized => _isInitialized;
        
        private void Awake()
        {
            ServiceLocator.Instance.Register<UIManager>(this);
            ServiceLocator.Instance.Register<IUIManager>(this);
            if (_dontDestroyOnLoad && transform.parent == null) DontDestroyOnLoad(gameObject);
            if (_uiRoot == null) _uiRoot = transform;
        }
        
        private void OnDestroy()
        {
            _disposables?.Dispose();
            ServiceLocator.Instance?.Unregister<UIManager>();
            ServiceLocator.Instance?.Unregister<IUIManager>();
            ClearAll();
        }
        
        public void Initialize()
        {
            if (_isInitialized) return;
            
            if (_mainCanvas != null && _mainCamera == null)
            {
                _mainCamera = _mainCanvas.worldCamera;
            }
            
            InitializeUIElements();
            InitializeInitializables();
            SubscribeToEvents();
            _isInitialized = true;
        }
        
        private void SubscribeToEvents()
        {
            var winSubscription = EventBus.Instance.Subscribe<GameModule.Core.GameEndedEvent>(OnGameEnded);
            _disposables.Add(winSubscription);
        }
        
        private void OnGameEnded(GameModule.Core.GameEndedEvent evt)
        {
            var mainCanvasController = ServiceLocator.Instance?.Get<MainCanvasController>();
            if (mainCanvasController == null) return;
            
            if (evt.IsWin)
            {
                var winPanel = mainCanvasController.GetWinPanel();
                if (winPanel != null)
                {
                    winPanel.ShowWinAnimation();
                }
            }
            else
            {
                var losePanel = mainCanvasController.GetLosePanel();
                if (losePanel != null)
                {
                    losePanel.ShowLoseAnimation();
                }
            }
        }
        
        private void InitializeUIElements()
        {
            CollectUIElementsFromTransform(transform);
        }
        
        private void InitializeInitializables()
        {
            CollectInitializablesFromTransform(transform);
        }
        
        private void CollectUIElementsFromTransform(Transform parent)
        {
            if (parent == null) return;
            
            var components = parent.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component is IUIElement element)
                {
                    RegisterElement(element);
                }
            }
            
            for (int i = 0; i < parent.childCount; i++)
            {
                CollectUIElementsFromTransform(parent.GetChild(i));
            }
        }
        
        private void CollectInitializablesFromTransform(Transform parent)
        {
            if (parent == null) return;
            
            var components = parent.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component == this) continue;
                
                if (component is IInitializable initializable && !initializable.IsInitialized)
                {
                    initializable.Initialize();
                }
            }
            
            for (int i = 0; i < parent.childCount; i++)
            {
                CollectInitializablesFromTransform(parent.GetChild(i));
            }
        }
        
        public void RegisterElement(IUIElement element)
        {
            if (element == null) return;
            
            string elementId = element.ElementId;
            if (string.IsNullOrEmpty(elementId)) elementId = element.GetType().Name;
            
            _elements[elementId] = element;
            if (element is MonoBehaviour monoElement && _uiRoot != null) monoElement.transform.SetParent(_uiRoot, false);
        }
        
        public void UnregisterElement(string elementId)
        {
            if (string.IsNullOrEmpty(elementId)) return;
            if (_elements.TryGetValue(elementId, out var element))
            {
                _elements.Remove(elementId);
                element.Destroy();
            }
        }
        
        public IUIElement GetElement(string elementId)
        {
            if (string.IsNullOrEmpty(elementId)) return null;
            _elements.TryGetValue(elementId, out var element);
            return element;
        }
        
        public void ShowElement(string elementId)
        {
            var element = GetElement(elementId);
            element?.Show();
        }
        
        public void HideElement(string elementId)
        {
            var element = GetElement(elementId);
            element?.Hide();
        }
        
        public void ClearAll()
        {
            var elementsToDestroy = new List<IUIElement>(_elements.Values);
            foreach (var element in elementsToDestroy) element.Destroy();
            _elements.Clear();
        }
    }
}
