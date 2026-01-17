using UnityEngine;
using UnityEngine.InputSystem;
using BoardGameTestCase.Core.Common;
using GameModule.Core.Interfaces;
using GridSystemModule.Core.Interfaces;
using GameState = GameModule.Core.Interfaces.GameState;

namespace GridSystemModule.Managers
{
    /// <summary>
    /// Listens for the new Input System action that signals a grid reset request
    /// and forwards it to the placement system.
    /// </summary>
    public class GridPlacementResetInput : MonoBehaviour
    {
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private string _actionMapName = "Player";
        [SerializeField] private string _resetActionName = "ResetPlacement";
        [SerializeField] private bool _onlyAllowDuringPlacingState = true;

        private InputAction _resetAction;
        private IGridPlacementSystem _placementSystem;
        private IGameFlowController _gameFlowController;

        private void Awake()
        {
            _gameFlowController = ServiceLocator.Instance?.TryGet<IGameFlowController>();
        }

        private void OnEnable()
        {
            Debug.Log("GridPlacementResetInput: OnEnable called; preparing to bind reset action.");
            EnsureActionBinding();
            if (_resetAction != null)
            {
                _resetAction.performed += OnResetPerformed;
                _resetAction.Enable();
                Debug.Log($"GridPlacementResetInput: Listening for '{_resetActionName}' on map '{_actionMapName}' (bindings: {_resetAction.bindings.Count}).");
            }
            else
            {
                Debug.LogWarning("GridPlacementResetInput: Reset action is not bound; press R will be ignored.");
            }
        }

        private void OnDisable()
        {
            if (_resetAction != null)
            {
                _resetAction.performed -= OnResetPerformed;
                _resetAction.Disable();
            }
        }

        private void EnsureActionBinding()
        {
            if (_resetAction != null)
            {
                return;
            }

            if (_inputActions == null)
            {
                var playerInput = GetComponent<PlayerInput>();
                if (playerInput != null)
                {
                    _inputActions = playerInput.actions;
                    Debug.Log("GridPlacementResetInput: Auto-assigned InputActionAsset from PlayerInput component.");
                }
                else
                {
                    Debug.LogWarning("GridPlacementResetInput: No PlayerInput component found to auto-assign actions.");
                }
            }

            if (_inputActions == null)
            {
                Debug.LogWarning("GridPlacementResetInput: InputActionAsset reference is missing.");
                return;
            }

            var actionMap = _inputActions.FindActionMap(_actionMapName, false);
            if (actionMap == null)
            {
                Debug.LogWarning($"GridPlacementResetInput: Action map '{_actionMapName}' not found in asset '{_inputActions.name}'.");
                return;
            }
            else
            {
                Debug.Log($"GridPlacementResetInput: Found action map '{_actionMapName}' in asset '{_inputActions.name}'.");
            }

            _resetAction = actionMap.FindAction(_resetActionName, false);
            if (_resetAction == null)
            {
                Debug.LogWarning($"GridPlacementResetInput: Action '{_resetActionName}' not found in map '{_actionMapName}'.");
            }
            else
            {
                Debug.Log($"GridPlacementResetInput: Bound action '{_resetActionName}' with {_resetAction.bindings.Count} bindings.");
            }
        }

        private bool CanProcessReset()
        {
            if (!_onlyAllowDuringPlacingState)
            {
                return true;
            }

            if (_gameFlowController == null)
            {
                _gameFlowController = ServiceLocator.Instance?.TryGet<IGameFlowController>();
            }

            return _gameFlowController != null && _gameFlowController.CurrentGameState == GameState.Placing;
        }

        private void OnResetPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed)
            {
                return;
            }

            if (!CanProcessReset())
            {
                Debug.Log("GridPlacementResetInput: Reset ignored because game state is not Placing.");
                return;
            }

            if (_placementSystem == null)
            {
                _placementSystem = ServiceLocator.Instance?.TryGet<IGridPlacementSystem>();
            }

            if (_placementSystem == null)
            {
                Debug.LogWarning("GridPlacementResetInput: Placement system not available; cannot reset.");
                return;
            }

            Debug.Log("GridPlacementResetInput: Reset action performed; clearing placements.");
            _placementSystem.ResetPlacementsToInventory();
        }
    }
}
