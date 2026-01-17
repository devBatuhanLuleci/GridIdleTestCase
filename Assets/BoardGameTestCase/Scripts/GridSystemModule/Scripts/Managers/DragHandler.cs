using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using GridSystemModule.Services;

namespace GridSystemModule
{
    /// <summary>
    /// Responsible for managing drag operations and state during object dragging.
    /// Extracted from GridPlacementSystem to focus on drag handling logic only.
    /// </summary>
    public class DragHandler
    {
        private IPlaceable _currentDraggedObject;
        private bool _dragStartWasPlaced;
        private Vector2Int _dragStartGridPos;
        private Vector3 _dragStartWorldPos;
        private Vector2Int _lastEvaluatedGridPosition;

        private readonly GridPlacementSystem _gridPlacementSystem;
        private readonly PlacementValidator _placementValidator;
        private readonly GridCoordinateConverter _coordinateConverter;
        private readonly HighlightManager _highlightManager;
        private readonly GridManager _gridManager;

        public IPlaceable CurrentDraggedObject => _currentDraggedObject;
        public bool IsDragging => _currentDraggedObject != null;

        public DragHandler(
            GridPlacementSystem gridPlacementSystem,
            PlacementValidator placementValidator,
            GridCoordinateConverter coordinateConverter,
            HighlightManager highlightManager,
            GridManager gridManager = null)
        {
            _gridPlacementSystem = gridPlacementSystem;
            _placementValidator = placementValidator;
            _coordinateConverter = coordinateConverter;
            _highlightManager = highlightManager;
            _gridManager = gridManager ?? ServiceLocator.Instance.GetService<GridManager>();
        }

        /// <summary>
        /// Starts a drag operation for the specified placeable object.
        /// Stores the initial state and removes the object from the grid if it was placed.
        /// </summary>
        public void StartDragging(IPlaceable placeable)
        {
            if (placeable == null) return;

            _dragStartWasPlaced = placeable.IsPlaced;
            _dragStartGridPos = placeable.GridPosition;

            // Calculate center position for multi-tile objects
            var mbForKill = placeable as MonoBehaviour;
            if (mbForKill != null)
            {
                _dragStartWorldPos = mbForKill.transform.position;
            }
            else
            {
                var occupiedPositions = new List<Vector2Int>();
                for (int x = 0; x < placeable.GridSize.x; x++)
                {
                    for (int y = 0; y < placeable.GridSize.y; y++)
                    {
                        occupiedPositions.Add(new Vector2Int(placeable.GridPosition.x + x, placeable.GridPosition.y + y));
                    }
                }
                _dragStartWorldPos = _coordinateConverter.MultiTileGridToWorld(occupiedPositions);
            }

            if (_dragStartWasPlaced)
            {
                _gridPlacementSystem.RemoveObject(placeable);
            }

            _currentDraggedObject = placeable;
            placeable.IsDragging = true;
            placeable.OnDragStart();

            if (mbForKill != null && mbForKill.transform != null)
            {
                mbForKill.transform.DOKill(true);
            }

            _lastEvaluatedGridPosition = new Vector2Int(int.MinValue, int.MinValue);
        }

        /// <summary>
        /// Updates the drag state and highlights valid placement positions.
        /// Only re-evaluates when the object moves by its full size in grid cells.
        /// </summary>
        public void UpdateDrag(Vector3 worldPosition)
        {
            if (_currentDraggedObject == null) return;

            // Convert world position from object center to grid anchor
            Vector3 adjustedWorldPos = worldPosition;
            Vector2Int objectSize = _currentDraggedObject.GridSize;

            if (_gridManager != null && _gridManager.GridSettings != null)
            {
                Vector2 cellSize = _gridManager.GridSettings.CellSize;
                Vector2 cellSpacing = _gridManager.GridSettings.CellSpacing;
                float cellSizeX = cellSize.x + cellSpacing.x;
                float cellSizeY = cellSize.y + cellSpacing.y;

                // Offset: (size - 1) / 2 cells from center to bottom-left
                float offsetX = (objectSize.x - 1) * 0.5f * cellSizeX;
                float offsetY = (objectSize.y - 1) * 0.5f * cellSizeY;

                adjustedWorldPos = worldPosition - new Vector3(offsetX, offsetY, 0f);
            }

            Vector2Int gridPos = _coordinateConverter.WorldToGrid(adjustedWorldPos);
            
            // Only re-evaluate when moving by full object size
            long dx = System.Math.Abs((long)gridPos.x - _lastEvaluatedGridPosition.x);
            long dy = System.Math.Abs((long)gridPos.y - _lastEvaluatedGridPosition.y);
            if (dx < objectSize.x && dy < objectSize.y)
            {
                return;
            }
            
            _lastEvaluatedGridPosition = gridPos;

            _currentDraggedObject.OnDrag(worldPosition);

            bool isValid = _placementValidator.IsValidPlacement(gridPos, _currentDraggedObject.GridSize, _currentDraggedObject);

            IPlaceable occupant = null;
            for (int x = 0; x < _currentDraggedObject.GridSize.x; x++)
            {
                for (int y = 0; y < _currentDraggedObject.GridSize.y; y++)
                {
                    Vector2Int checkPos = new Vector2Int(gridPos.x + x, gridPos.y + y);
                    var objAtPos = _gridPlacementSystem.GetObjectAt(checkPos);
                    if (objAtPos != null && objAtPos != _currentDraggedObject)
                    {
                        occupant = objAtPos;
                        break;
                    }
                }
                if (occupant != null) break;
            }

            if (occupant != null)
            {
                if (!_dragStartWasPlaced)
                {
                    isValid = false;
                }
                else
                {
                    Vector2Int occupantPos = occupant.GridPosition;
                    Vector2Int draggedStartPos = _dragStartWasPlaced ? _dragStartGridPos : occupantPos;

                    bool canDraggedToOccupant = _placementValidator.IsValidPlacement(occupantPos, _currentDraggedObject.GridSize, occupant);
                    bool canOccupantToDragged = _dragStartWasPlaced ? _placementValidator.IsValidPlacement(draggedStartPos, occupant.GridSize, _currentDraggedObject) : true;

                    isValid = canDraggedToOccupant && canOccupantToDragged;
                }
            }

            _highlightManager.HighlightTileAt(gridPos, _currentDraggedObject.GridSize, _gridPlacementSystem, isValid);
        }

        /// <summary>
        /// Ends the drag operation and attempts to place the object.
        /// Handles occupant swaps, auto-snapping to nearest valid position, and revert on failure.
        /// </summary>
        public void EndDragging(Vector3 worldPosition)
        {
            if (_currentDraggedObject == null) return;

            // Same adjustment as UpdateDrag - both X and Y offsets
            Vector3 adjustedWorldPos = worldPosition;
            Vector2Int objectSize = _currentDraggedObject.GridSize;

            if (_gridManager != null && _gridManager.GridSettings != null)
            {
                Vector2 cellSize = _gridManager.GridSettings.CellSize;
                Vector2 cellSpacing = _gridManager.GridSettings.CellSpacing;
                float cellSizeX = cellSize.x + cellSpacing.x;
                float cellSizeY = cellSize.y + cellSpacing.y;

                float offsetX = (objectSize.x - 1) * 0.5f * cellSizeX;
                float offsetY = (objectSize.y - 1) * 0.5f * cellSizeY;

                adjustedWorldPos = worldPosition - new Vector3(offsetX, offsetY, 0f);
            }

            Vector2Int gridPos = _coordinateConverter.WorldToGrid(adjustedWorldPos);

            bool wasAutoSnappedFromInvalid = false;

            IPlaceable occupant = null;
            Vector2Int? occupantGridPos = null;
            for (int x = 0; x < _currentDraggedObject.GridSize.x; x++)
            {
                for (int y = 0; y < _currentDraggedObject.GridSize.y; y++)
                {
                    Vector2Int checkPos = new Vector2Int(gridPos.x + x, gridPos.y + y);
                    var objAtPos = _gridPlacementSystem.GetObjectAt(checkPos);
                    if (objAtPos != null && objAtPos != _currentDraggedObject)
                    {
                        occupant = objAtPos;
                        occupantGridPos = checkPos;
                        break;
                    }
                }
                if (occupant != null) break;
            }

            if (occupant != null && occupantGridPos.HasValue)
            {
                occupantGridPos = occupant.GridPosition;

                if (!_dragStartWasPlaced)
                {
                    RevertAndFinish(gridPos);
                    return;
                }

                bool swapped = _gridPlacementSystem.TrySwapWithOccupant(_currentDraggedObject, occupant, occupantGridPos.Value, _dragStartGridPos);
                if (swapped)
                {
                    FinishDrag(true);
                    return;
                }

                RevertAndFinish(gridPos);
                return;
            }

            bool isValid = _placementValidator.IsValidPlacement(gridPos, _currentDraggedObject.GridSize, _currentDraggedObject);

            if (!isValid)
            {
                if (_dragStartWasPlaced)
                {
                    Vector2Int nearestValidPos = _gridPlacementSystem.FindNearestValidPosition(gridPos, _currentDraggedObject.GridSize, _currentDraggedObject);
                    if (nearestValidPos != gridPos)
                    {
                        gridPos = nearestValidPos;
                        isValid = true;
                        wasAutoSnappedFromInvalid = true;
                    }
                    else
                    {
                        RevertAndFinish(gridPos);
                        return;
                    }
                }
                else
                {
                    RevertAndFinish(gridPos);
                    return;
                }
            }

            bool placed = _gridPlacementSystem.PlaceObject(_currentDraggedObject, gridPos);

            if (placed)
            {
                _gridPlacementSystem.TryPlayPlacementAnimation(_currentDraggedObject, gridPos, wasAutoSnappedFromInvalid);
                _currentDraggedObject.OnDrop(gridPos, true);

                var draggedMb = _currentDraggedObject as MonoBehaviour;
                if (draggedMb != null && draggedMb.transform != null)
                {
                    draggedMb.transform.SetParent(null);
                }
            }
            else
            {
                _gridPlacementSystem.RevertDraggedToStartPosition();
                _currentDraggedObject.OnDrop(gridPos, false);
            }

            _highlightManager.ClearTileHighlight();
            FinishDrag(true);
        }

        /// <summary>
        /// Cancels the current drag operation and reverts the object to its start position.
        /// </summary>
        public void CancelDrag()
        {
            if (_currentDraggedObject == null) return;

            _gridPlacementSystem.RevertDraggedToStartPosition();
            _highlightManager.ClearTileHighlight();
            FinishDrag(false);
        }

        /// <summary>
        /// Resets the drag handler state.
        /// </summary>
        public void Reset()
        {
            _currentDraggedObject = null;
            _highlightManager.ClearTileHighlight();
            _lastEvaluatedGridPosition = new Vector2Int(int.MinValue, int.MinValue);
        }

        private void RevertAndFinish(Vector2Int gridPos)
        {
            _gridPlacementSystem.RevertDraggedToStartPosition();
            _gridPlacementSystem.FinishDragWithoutPlacement(gridPos);
            FinishDrag(false);
        }

        private void FinishDrag(bool placed)
        {
            _currentDraggedObject.IsDragging = false;
            _currentDraggedObject = null;
            _lastEvaluatedGridPosition = new Vector2Int(int.MinValue, int.MinValue);
        }
    }
}
