using UnityEngine;

namespace UISystemModule.Core.Interfaces
{
    public interface IUIElement
    {
        string ElementId { get; }
        bool IsActive { get; }
        void Initialize();
        void Show();
        void Hide();
        void Destroy();
    }
}
