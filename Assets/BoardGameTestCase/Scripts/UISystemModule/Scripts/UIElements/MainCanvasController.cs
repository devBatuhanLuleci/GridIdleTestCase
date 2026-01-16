using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using BoardGameTestCase.Core.Common;
using UISystemModule.Core.Interfaces;
using UISystemModule.Core;
using GameModule.Core.Interfaces;
using GameModule.Core;

namespace UISystemModule.UIElements
{
    public class MainCanvasController : BaseUIPanel
    {
        [SerializeField] private WinPanel _winPanel;
        [SerializeField] private LosePanel _losePanel;
        [SerializeField] private TextMeshProUGUI _currentLevelText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _fadeInDuration = 1f;
        [SerializeField] private float _fadeOutDuration = 0.5f;
        [SerializeField] private Ease _fadeEase = Ease.OutQuad;
        
        private ILevelDataProvider _levelDataProvider;
        private IStateController _stateController;
        private Sequence _fadeSequence;
        private CompositeDisposable _disposables = new CompositeDisposable();
        
        private int GetLevelNumberFromManager()
        {
            if (_levelDataProvider == null)
            {
                _levelDataProvider = ServiceLocator.Instance?.Get<ILevelDataProvider>();
            }
            
            if (_levelDataProvider != null && _levelDataProvider.CurrentLevelNumber > 0)
            {
                return _levelDataProvider.CurrentLevelNumber;
            }
            
            int savedLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
            return savedLevel;
        }
        
        protected override void Awake()
        {
            base.Awake();
            ServiceLocator.Instance?.Register<MainCanvasController>(this);
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            _levelDataProvider = ServiceLocator.Instance?.Get<ILevelDataProvider>();
            _stateController = ServiceLocator.Instance?.Get<IStateController>();
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
            
            SubscribeToEvents();
            UpdateLevelText();
        }
        
        private void SubscribeToEvents()
        {
            var stateChangedSubscription = EventBus.Instance.Subscribe<GameModule.Core.GameStateChangedEvent>(OnGameStateChanged);
            _disposables.Add(stateChangedSubscription);
            
            var levelNumberChangedSubscription = EventBus.Instance.Subscribe<GameModule.Core.CurrentLevelNumberChangedEvent>(OnCurrentLevelNumberChanged);
            _disposables.Add(levelNumberChangedSubscription);
        }
        
        private void OnCurrentLevelNumberChanged(GameModule.Core.CurrentLevelNumberChangedEvent evt)
        {
            UpdateLevelTextWithNumber(evt.CurrentLevelNumber);
        }
        
        private void OnGameStateChanged(GameModule.Core.GameStateChangedEvent evt)
        {
            if (evt.NewState == GameState.Fight)
            {
                FadeOutCanvas();
            }
            else if (evt.NewState == GameState.Placing)
            {
                UpdateLevelText();
                FadeInCanvas();
            }
        }
        
        private void UpdateLevelText()
        {
            if (_currentLevelText == null) return;
            
            int levelNumber = 1;
            if (_levelDataProvider != null)
            {
                levelNumber = _levelDataProvider.CurrentLevelNumber;
                if (levelNumber <= 0)
                {
                    levelNumber = PlayerPrefs.GetInt("CurrentLevel", 1);
                }
            }
            else
            {
                levelNumber = PlayerPrefs.GetInt("CurrentLevel", 1);
            }
            
            UpdateLevelTextWithNumber(levelNumber);
        }
        
        private void UpdateLevelTextWithNumber(int levelNumber)
        {
            if (_currentLevelText != null)
            {
                _currentLevelText.text = $"Level {levelNumber}";
            }
        }
        
        private void FadeInCanvas()
        {
            if (_canvasGroup == null) return;
            
            if (_fadeSequence != null && _fadeSequence.IsActive())
            {
                _fadeSequence.Kill();
            }
            
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            
            _fadeSequence = DOTween.Sequence();
            _fadeSequence.Append(DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 1f, _fadeInDuration).SetEase(_fadeEase));
        }
        
        private void FadeOutCanvas()
        {
            if (_canvasGroup == null) return;
            
            if (_fadeSequence != null && _fadeSequence.IsActive())
            {
                _fadeSequence.Kill();
            }
            
            _fadeSequence = DOTween.Sequence();
            _fadeSequence.Append(DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 0f, _fadeOutDuration).SetEase(_fadeEase));
            _fadeSequence.OnComplete(() =>
            {
                if (_canvasGroup != null)
                {
                    _canvasGroup.interactable = false;
                    _canvasGroup.blocksRaycasts = false;
                }
            });
        }
        
        protected override void OnDestroy()
        {
            if (_fadeSequence != null && _fadeSequence.IsActive())
            {
                _fadeSequence.Kill();
            }
            
            _disposables?.Dispose();
            ServiceLocator.Instance?.Unregister<MainCanvasController>();
            base.OnDestroy();
        }
        
        public WinPanel GetWinPanel() => _winPanel;
        public LosePanel GetLosePanel() => _losePanel;
    }
}

