using UnityEngine;
using GridSystemModule.Core.Models;
using BoardGameTestCase.Core.Common;

namespace GridSystemModule.Managers
{
    /// <summary>
    /// Handles coordinate conversion between world space and grid space.
    /// </summary>
    public class GridCoordinateConverter
    {
        private GridManager _gridManager;
        private Transform _tilesParent;
        
        public GridCoordinateConverter(GridManager gridManager = null)
        {
            _gridManager = gridManager;
            FindTilesParent();
        }
        
        public void SetGridManager(GridManager gridManager)
        {
            _gridManager = gridManager;
            FindTilesParent();
        }
        
        private void FindTilesParent()
        {
            if (_gridManager != null)
            {
                _tilesParent = _gridManager.TilesParent;
            }
        }
        
        /// <summary>
        /// Converts world position to grid coordinates.
        /// </summary>
        public Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            if (_tilesParent != null)
            {
                Vector3 parentPos = _tilesParent.position;
                Quaternion parentRot = _tilesParent.rotation;
                Vector3 parentScale = _tilesParent.lossyScale;
                
                Vector3 relativePos = Quaternion.Inverse(parentRot) * (worldPosition - parentPos);
                relativePos = new Vector3(
                    relativePos.x / parentScale.x,
                    relativePos.y / parentScale.y,
                    relativePos.z / parentScale.z
                );
                
                var gridManager = _gridManager ?? ServiceLocator.Instance?.Get<GridManager>();
                Vector2 cellSize = Vector2.one;
                Vector2 cellSpacing = Vector2.zero;
                
                if (gridManager != null && gridManager.GridSettings != null)
                {
                    cellSize = gridManager.GridSettings.CellSize;
                    cellSpacing = gridManager.GridSettings.CellSpacing;
                }
                
                float cellSizeX = cellSize.x + cellSpacing.x;
                float cellSizeY = cellSize.y + cellSpacing.y;
                
                int x = Mathf.RoundToInt(relativePos.x / cellSizeX);
                int y = Mathf.RoundToInt(relativePos.y / cellSizeY);
                return new Vector2Int(x, y);
            }
            
            // Fallback: use GridManager
            var gridManager2 = ServiceLocator.Instance?.Get<GridManager>();
            if (gridManager2 != null)
            {
                var allTiles = gridManager2.GetAllTiles();
                
                if (allTiles != null && allTiles.Count > 0)
                {
                    BaseTile nearestTile = null;
                    float nearestDistance = float.MaxValue;
                    
                    foreach (var tile in allTiles)
                    {
                        float distance = Vector3.Distance(worldPosition, tile.transform.position);
                        if (distance < nearestDistance)
                        {
                            nearestDistance = distance;
                            nearestTile = tile;
                        }
                    }
                    
                    if (nearestTile != null)
                    {
                        return nearestTile.GridPosition;
                    }
                }
            }
            
            return Vector2Int.zero;
        }
        
        /// <summary>
        /// Converts grid coordinates to world position.
        /// </summary>
        public Vector3 GridToWorld(Vector2Int gridPosition)
        {
            var gridManager = _gridManager ?? ServiceLocator.Instance?.Get<GridManager>();
            var tile = gridManager?.GetTileAt(gridPosition);
            
            if (tile != null)
            {
                return tile.transform.position;
            }
            
            if (gridManager != null && gridManager.GridSettings != null)
            {
                float cellWidth = gridManager.GridSettings.CellSize.x + gridManager.GridSettings.CellSpacing.x;
                float cellHeight = gridManager.GridSettings.CellSize.y + gridManager.GridSettings.CellSpacing.y;
                
                Vector3 localPos = new Vector3(gridPosition.x * cellWidth, gridPosition.y * cellHeight, 0f);
                
                if (_tilesParent != null)
                {
                    return _tilesParent.TransformPoint(localPos);
                }
                
                return localPos;
            }
            
            return Vector3.zero;
        }
        
        /// <summary>
        /// Converts multiple grid positions to a single world position (average center).
        /// Used for calculating the center of multi-tile objects.
        /// </summary>
        public Vector3 MultiTileGridToWorld(System.Collections.Generic.List<Vector2Int> gridPositions)
        {
            if (gridPositions == null || gridPositions.Count == 0)
            {
                return Vector3.zero;
            }

            Vector3 totalPos = Vector3.zero;
            foreach (var pos in gridPositions)
            {
                totalPos += GridToWorld(pos);
            }

            return totalPos / gridPositions.Count;
        }
        
        /// <summary>
        /// Get tiles parent transform.
        /// </summary>
        public Transform GetTilesParent()
        {
            if (_tilesParent != null)
            {
                return _tilesParent;
            }
            
            if (_gridManager != null)
            {
                return _gridManager.TilesParent;
            }
            
            return null;
        }
    }
}
