using System.Collections.Generic;
using UnityEngine;

namespace UISystemModule.Core.Interfaces
{
    public interface IUIManager
    {
        IReadOnlyDictionary<string, IUIElement> Elements { get; }
        Canvas MainCanvas { get; }
        Camera MainCamera { get; }
        void RegisterElement(IUIElement element);
        void UnregisterElement(string elementId);
        IUIElement GetElement(string elementId);
        void ShowElement(string elementId);
        void HideElement(string elementId);
        void ClearAll();
    }
}
