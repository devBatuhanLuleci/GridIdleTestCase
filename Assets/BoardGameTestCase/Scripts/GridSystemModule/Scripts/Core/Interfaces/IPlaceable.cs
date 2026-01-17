using UnityEngine;

namespace GridSystemModule.Core.Interfaces
{
    public interface IPlaceable
    {
        string PlaceableId { get; }
        Vector2Int GridSize { get; }
        bool IsDragging { get; set; }
        bool IsPlaced { get; set; }
        Vector2Int GridPosition { get; set; }
        // Provide direct access to the underlying Transform to avoid repeated casts
        Transform Transform { get; }
        void OnDragStart();
        void OnDrag(Vector3 worldPosition);
        void OnDrop(Vector2Int gridPosition, bool isValid);
        void OnPlaced(Vector2Int gridPosition);
        void OnRemoved();
        Vector3? GetOriginalScale();
        void PlayFailAnimation();
    }
}
