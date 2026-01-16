using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using BoardGameTestCase.Core.Common;
using UISystemModule.Core.Interfaces;
using GameModule.Core.Interfaces;

namespace UISystemModule.UIElements
{
    public class LosePanel : BaseUIPanel
    {
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panelRect;
        [SerializeField] private float _displayDuration = 3f;
        [SerializeField] private float _fadeInDuration = 0.5f;
        [SerializeField] private float _fadeOutDuration = 0.5f;
        [SerializeField] private float _scaleAnimationDuration = 0.3f;
        [SerializeField] private Ease _fadeEase = Ease.OutQuad;
        [SerializeField] private Ease _scaleEase = Ease.OutBack;
        
        private IGameFlowController _gameFlowController;
        private Sequence _animationSequence;
        private bool _isShowing = false;
        
        protected override void Awake()
        {
            base.Awake();
            
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
            
            if (_panelRect == null)
            {
                _panelRect = GetComponent<RectTransform>();
            }
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            _gameFlowController = ServiceLocator.Instance?.Get<IGameFlowController>();
            
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
            
            if (_panelRect != null)
            {
                _panelRect.localScale = Vector3.zero;
            }
            
            Hide();
        }
        
        protected override void OnDestroy()
        {
            if (_animationSequence != null && _animationSequence.IsActive())
            {
                _animationSequence.Kill();
            }
            base.OnDestroy();
        }
        
        public void ShowLoseAnimation()
        {
            if (_isShowing) return;
            _isShowing = true;
            
            Show();
            
            if (_canvasGroup == null || _panelRect == null) return;
            
            if (_animationSequence != null && _animationSequence.IsActive())
            {
                _animationSequence.Kill();
            }
            
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _panelRect.localScale = Vector3.zero;
            
            _animationSequence = DOTween.Sequence();
            
            _animationSequence.Append(DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 1f, _fadeInDuration).SetEase(_fadeEase));
            _animationSequence.Join(_panelRect.DOScale(Vector3.one, _scaleAnimationDuration).SetEase(_scaleEase));
            
            _animationSequence.AppendInterval(_displayDuration);
            
            _animationSequence.Append(DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 0f, _fadeOutDuration).SetEase(_fadeEase));
            _animationSequence.Join(_panelRect.DOScale(Vector3.zero, _fadeOutDuration).SetEase(Ease.InBack));
            
            _animationSequence.OnComplete(() =>
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
                _isShowing = false;
                Hide();
                OnLoseAnimationComplete();
            });
        }
        
        private void OnLoseAnimationComplete()
        {
            if (_gameFlowController != null)
            {
                _gameFlowController.RestartGame();
            }
        }
    }
}
