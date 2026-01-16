using System.Collections.Generic;
using UISystemModule.Core.Interfaces;

namespace UISystemModule.Core.Interfaces
{
    public interface IUIPanel : IUIElement
    {
        IReadOnlyList<IUIElement> ChildElements { get; }
        void AddChild(IUIElement element);
        void RemoveChild(IUIElement element);
        IUIElement GetChild(string elementId);
        void ClearChildren();
    }
}
