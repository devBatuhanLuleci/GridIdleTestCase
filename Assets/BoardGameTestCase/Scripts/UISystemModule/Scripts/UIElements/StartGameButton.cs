using UnityEngine;
using UnityEngine.UI;
using BoardGameTestCase.Core.Common;
using UISystemModule.Core.Interfaces;
using UISystemModule.Core;
using GameModule.Core.Interfaces;
using GameModule.Core;
using GameState = GameModule.Core.Interfaces.GameState;

namespace UISystemModule.UIElements
{
    public class StartGameButton : BaseUIButton
    {        [SerializeField] private string _startFightText = "Start Fight";
        [SerializeField] private string _restartText = "Restart Game";
        [SerializeField] private string _fightInProgressText = "Fight In Progress";
        
        private IGameFlowController _gameFlowController;
        private IStateController _stateController;
        private CompositeDisposable _disposables = new CompositeDisposable();
        
        protected override void Awake()
        {
            base.Awake();
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            _gameFlowController = ServiceLocator.Instance.Get<IGameFlowController>();
            _stateController = ServiceLocator.Instance.Get<IStateController>();
            
            if (_stateController != null)
            {
                _stateController.OnStateChanged += OnStateChangedOld;
                UpdateButtonText();
            }
            
            var eventBusSubscription = EventBus.Instance.Subscribe<GameModule.Core.GameStateChangedEvent>(OnStateChangedEventBus);
            _disposables.Add(eventBusSubscription);
        }
        
        protected override void OnDestroy()
        {
            if (_stateController != null) _stateController.OnStateChanged -= OnStateChangedOld;
            _disposables?.Dispose();
            base.OnDestroy();
        }
        
        protected override void OnButtonClickedInternal()
        {
            if (_gameFlowController == null || _stateController == null) return;
            
            switch (_stateController.CurrentState)
            {
                case GameState.Placing:
                    _gameFlowController.StartFight();
                    break;
                case GameState.Fight:
                    break;
                case GameState.Win:
                case GameState.Lose:
                    _gameFlowController.StartGame();
                    break;
            }
        }
        
        private void OnStateChangedOld(GameState newState)
        {
            UpdateButtonText();
            UpdateButtonInteractable();
        }
        
        private void OnStateChangedEventBus(GameModule.Core.GameStateChangedEvent evt)
        {
            UpdateButtonText();
            UpdateButtonInteractable();
        }
        
        private void UpdateButtonText()
        {
            if (_stateController == null) return;
            
            switch (_stateController.CurrentState)
            {
                case GameState.Placing:
                    SetText(_startFightText);
                    break;
                case GameState.Fight:
                    SetText(_fightInProgressText);
                    break;
                case GameState.Win:
                case GameState.Lose:
                    SetText(_restartText);
                    break;
            }
        }
        
        private void UpdateButtonInteractable()
        {
            if (_stateController == null) return;
            SetInteractable(_stateController.CurrentState != GameState.Fight);
        }
    }
}
