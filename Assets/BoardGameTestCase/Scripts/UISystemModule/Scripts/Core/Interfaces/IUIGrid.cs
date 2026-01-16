using UnityEngine;
using UISystemModule.Core.Interfaces;

namespace UISystemModule.Core.Interfaces
{
    public interface IUIGrid : IUIPanel
    {
        Vector2Int GridSize { get; }
        Vector2 CellSize { get; }
        Vector2 CellSpacing { get; }
        IUIElement GetElementAt(int x, int y);
        void SetElementAt(int x, int y, IUIElement element);
        void ClearElementAt(int x, int y);
        Vector2Int WorldToGrid(Vector3 worldPosition);
        Vector3 GridToWorld(Vector2Int gridPosition);
    }
}
