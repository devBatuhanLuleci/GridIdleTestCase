using UnityEngine;
using System.Collections.Generic;
using GridSystemModule.Core.Interfaces;
using BoardGameTestCase.Core.Common;

namespace GridSystemModule.Managers
{
    /// <summary>
    /// Responsible for validating grid placements and checking tile occupancy.
    /// Extracted from GridPlacementSystem to focus on validation logic only.
    /// </summary>
    public class PlacementValidator
    {
        private readonly Dictionary<Vector2Int, IPlaceable> _placedObjects;
        private readonly HashSet<Vector2Int> _occupiedTiles;
        private readonly Vector2Int _gridDimensions;
        private readonly GridManager _gridManager;

        public PlacementValidator(
            Dictionary<Vector2Int, IPlaceable> placedObjects,
            HashSet<Vector2Int> occupiedTiles,
            Vector2Int gridDimensions,
            GridManager gridManager = null)
        {
            _placedObjects = placedObjects;
            _occupiedTiles = occupiedTiles;
            _gridDimensions = gridDimensions;
            _gridManager = gridManager ?? ServiceLocator.Instance?.Get<GridManager>();
        }

        /// <summary>
        /// Checks if an object of the given size can be placed at the specified grid position.
        /// Validates:
        /// - Grid bounds
        /// - Grid placement restrictions (top half only)
        /// - Tile occupancy (unless excluded)
        /// </summary>
        public bool IsValidPlacement(Vector2Int gridPosition, Vector2Int objectSize, IPlaceable excludeObject = null)
        {
            // Check grid bounds
            if (gridPosition.x < 0 || gridPosition.y < 0 ||
                gridPosition.x + objectSize.x > _gridDimensions.x ||
                gridPosition.y + objectSize.y > _gridDimensions.y)
            {
                return false;
            }

            // Check grid placement restrictions (top half only)
            int bottomHalfThreshold = _gridDimensions.y / 2;
            for (int x = 0; x < objectSize.x; x++)
            {
                for (int y = 0; y < objectSize.y; y++)
                {
                    Vector2Int checkPos = new Vector2Int(gridPosition.x + x, gridPosition.y + y);

                    if (checkPos.y >= bottomHalfThreshold)
                    {
                        return false;
                    }
                }
            }

            // Check tile occupancy
            for (int x = 0; x < objectSize.x; x++)
            {
                for (int y = 0; y < objectSize.y; y++)
                {
                    Vector2Int checkPos = new Vector2Int(gridPosition.x + x, gridPosition.y + y);

                    if (_placedObjects.TryGetValue(checkPos, out var existingObject))
                    {
                        if (existingObject != excludeObject)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if a specific tile position is occupied.
        /// </summary>
        public bool IsTileOccupied(Vector2Int pos)
        {
            return _occupiedTiles.Contains(pos);
        }

        /// <summary>
        /// Gets the object occupying a specific tile position, if any.
        /// </summary>
        public IPlaceable GetOccupant(Vector2Int pos)
        {
            _placedObjects.TryGetValue(pos, out var obj);
            return obj;
        }

        /// <summary>
        /// Gets all positions occupied by objects on the grid.
        /// </summary>
        public Vector2Int[] GetOccupiedPositions()
        {
            return new List<Vector2Int>(_placedObjects.Keys).ToArray();
        }

        /// <summary>
        /// Attempts to find an alternate position for an object of the given size,
        /// starting from the preferred position and spiraling outward.
        /// </summary>
        public bool TryFindAlternatePosition(Vector2Int preferredPosition, Vector2Int objectSize, 
            IPlaceable excludeObject, out Vector2Int alternatePosition)
        {
            alternatePosition = Vector2Int.zero;

            if (IsValidPlacement(preferredPosition, objectSize, excludeObject))
            {
                alternatePosition = preferredPosition;
                return true;
            }

            int searchRadius = Mathf.Max(_gridDimensions.x, _gridDimensions.y);

            for (int radius = 1; radius < searchRadius; radius++)
            {
                // Right side
                for (int x = preferredPosition.x + radius; x < preferredPosition.x + radius + 1; x++)
                {
                    for (int y = preferredPosition.y - radius; y <= preferredPosition.y + radius; y++)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        if (IsValidPlacement(pos, objectSize, excludeObject))
                        {
                            alternatePosition = pos;
                            return true;
                        }
                    }
                }

                // Top side
                for (int y = preferredPosition.y + radius; y < preferredPosition.y + radius + 1; y++)
                {
                    for (int x = preferredPosition.x - radius; x <= preferredPosition.x + radius; x++)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        if (IsValidPlacement(pos, objectSize, excludeObject))
                        {
                            alternatePosition = pos;
                            return true;
                        }
                    }
                }

                // Left side
                for (int x = preferredPosition.x - radius; x > preferredPosition.x - radius - 1; x--)
                {
                    for (int y = preferredPosition.y - radius; y <= preferredPosition.y + radius; y++)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        if (IsValidPlacement(pos, objectSize, excludeObject))
                        {
                            alternatePosition = pos;
                            return true;
                        }
                    }
                }

                // Bottom side
                for (int y = preferredPosition.y - radius; y > preferredPosition.y - radius - 1; y--)
                {
                    for (int x = preferredPosition.x - radius; x <= preferredPosition.x + radius; x++)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        if (IsValidPlacement(pos, objectSize, excludeObject))
                        {
                            alternatePosition = pos;
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
