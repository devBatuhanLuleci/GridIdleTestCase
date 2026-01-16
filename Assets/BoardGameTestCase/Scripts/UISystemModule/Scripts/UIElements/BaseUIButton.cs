using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;
using UISystemModule.Core.Interfaces;

namespace UISystemModule.UIElements
{
    public abstract class BaseUIButton : BaseUIElement, IUIButton
    {        [SerializeField] protected Button _button;
        [SerializeField] protected TMPro.TextMeshProUGUI _buttonText;
        [SerializeField] protected string _defaultText = "Button";
        
        protected bool _isInteractable = true;
        
        public string ButtonText { get; set; }
        public bool IsInteractable { get => _isInteractable; set => SetInteractable(value); }
        public event Action<IUIButton> OnButtonClicked;
        
        protected override void Awake()
        {
            base.Awake();
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (_button != null) _button.onClick.AddListener(OnButtonClick);
            SetText(_defaultText);
            SetInteractable(_isInteractable);
        }
        
        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_button != null) _button.onClick.RemoveListener(OnButtonClick);
        }
        
        public virtual void SetText(string text)
        {
            ButtonText = text;
            if (_buttonText != null) _buttonText.text = text;
        }
        
        public virtual void SetInteractable(bool interactable)
        {
            _isInteractable = interactable;
            if (_button != null) _button.interactable = interactable;
        }
        
        protected virtual void OnButtonClick()
        {
            if (!_isInteractable) return;
            OnButtonClicked?.Invoke(this);
            OnButtonClickedInternal();
        }
        
        protected virtual void OnButtonClickedInternal() { }
    }
}
