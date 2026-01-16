using UnityEngine;
using GridSystemModule.Core.Interfaces;

namespace GridSystemModule.Core.Interfaces
{
    public interface IGridPlacementSystem
    {
        Vector2Int GridDimensions { get; }
        IPlaceable CurrentDraggedObject { get; }
        bool IsValidPlacement(Vector2Int gridPosition, Vector2Int objectSize, IPlaceable excludeObject = null);
        Vector2Int[] GetOccupiedPositions();
        bool PlaceObject(IPlaceable placeable, Vector2Int gridPosition, bool skipOnPlacedCallback = false);
        bool RemoveObject(IPlaceable placeable);
        IPlaceable GetObjectAt(Vector2Int gridPosition);
        Vector2Int WorldToGrid(Vector3 worldPosition);
        Vector3 GridToWorld(Vector2Int gridPosition);
        Vector3 MultiTileGridToWorld(IEnumerable<Vector2Int> gridPositions);
        void StartDragging(IPlaceable placeable);
        void UpdateDrag(Vector3 worldPosition);
        void EndDragging(Vector3 worldPosition);
        void ClearAll();
        void AutoPlaceAllItemsFromInventory();
    }
}
