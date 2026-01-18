using UnityEngine;
using GridSystemModule.Core.Interfaces;

namespace GridSystemModule.Core.Interfaces
{
    public interface IGridTrashBin
    {
        bool IsPointOver(Vector3 worldPoint);
        void SetHighlight(bool active);
        void Show(bool active);
        Transform Transform { get; }
    }
}
