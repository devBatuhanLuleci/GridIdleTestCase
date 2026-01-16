using System;
using UnityEngine;
using BoardGameTestCase.Core.Common;
using GameModule.Core.Interfaces;
using GameModule.Core;

namespace GameModule.Managers
{
    public class StateManager : MonoBehaviour, IInitializable, IStateController
    {        [SerializeField] private GameState _currentStateDisplay = GameState.Placing;
        
        private GameState _currentState = GameState.Placing;
        private GameState _previousState = GameState.Placing;
        private bool _isInitialized = false;
        
        public GameState CurrentState => _currentState;
        public event Action<GameState> OnStateChanged;
        public bool IsInitialized => _isInitialized;
        
        private void Awake()
        {
            ServiceLocator.Instance.Register<StateManager>(this);
            ServiceLocator.Instance.Register<IStateController>(this);
            _currentStateDisplay = _currentState;
        }
        
        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
        }
        
        private void OnDestroy()
        {
            ServiceLocator.Instance?.Unregister<StateManager>();
            ServiceLocator.Instance?.Unregister<IStateController>();
        }
        
#if UNITY_EDITOR
        private void Update()
        {
            if (Application.isPlaying) _currentStateDisplay = _currentState;
        }
        
        private void OnValidate()
        {
            if (_currentStateDisplay != _currentState) _currentStateDisplay = _currentState;
        }
        
        private void Reset()
        {
            _currentStateDisplay = _currentState;
        }
#endif
        
        public void ChangeState(GameState newState)
        {
            if (_currentState == newState) return;
            _previousState = _currentState;
            _currentState = newState;
            _currentStateDisplay = _currentState;
            OnStateChanged?.Invoke(_currentState);
            EventBus.Instance.Publish(new GameStateChangedEvent(newState, _previousState));
        }
        
        public void SetPlacing() => ChangeState(GameState.Placing);
        public void SetFight() => ChangeState(GameState.Fight);
        public void SetWin() => ChangeState(GameState.Win);
        public void SetLose() => ChangeState(GameState.Lose);
    }
}

