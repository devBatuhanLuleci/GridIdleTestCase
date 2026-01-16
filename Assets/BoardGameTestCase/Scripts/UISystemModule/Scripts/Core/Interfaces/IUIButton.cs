using System;
using UISystemModule.Core.Interfaces;

namespace UISystemModule.Core.Interfaces
{
    public interface IUIButton : IUIElement
    {
        string ButtonText { get; set; }
        bool IsInteractable { get; set; }
        event Action<IUIButton> OnButtonClicked;
        void SetText(string text);
        void SetInteractable(bool interactable);
    }
}
