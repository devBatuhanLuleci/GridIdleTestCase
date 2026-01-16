using UnityEngine;
using UnityEngine.UI;
using UISystemModule.UIElements;
using BoardGameTestCase.Core.Common;
using UISystemModule.Core.Interfaces;
using GameModule.Core.Interfaces;
using GameState = GameModule.Core.Interfaces.GameState;

namespace UISystemModule.UIElements
{
    public class GameUIPanel : BaseUIPanel
    {        [SerializeField] private StartGameButton _startGameButton;
        
        private IStateController _stateController;
        
        protected override void Awake()
        {
            base.Awake();
            _stateController = ServiceLocator.Instance.Get<IStateController>();
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (_stateController != null) _stateController.OnStateChanged += OnStateChanged;
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_stateController != null) _stateController.OnStateChanged -= OnStateChanged;
        }
        
        private void OnStateChanged(GameState newState)
        {
            Show();
        }
    }
}
