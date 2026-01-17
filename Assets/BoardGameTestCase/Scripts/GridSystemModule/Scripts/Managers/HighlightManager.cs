using UnityEngine;
using System.Collections.Generic;
using GridSystemModule.Core.Models;
using GridSystemModule.Services;
using BoardGameTestCase.Core.Common;

namespace GridSystemModule.Managers
{
    /// <summary>
    /// Responsible for managing tile highlight visualization during drag operations.
    /// Extracted from GridPlacementSystem to focus on highlight rendering only.
    /// </summary>
    public class HighlightManager
    {
        private List<BaseTile> _highlightedTiles = new List<BaseTile>();
        private readonly GridManager _gridManager;

        public HighlightManager(GridManager gridManager = null)
        {
            _gridManager = gridManager ?? ServiceLocator.Instance?.Get<GridManager>();
        }

        /// <summary>
        /// Highlights all tiles where an object would be placed, with color indicating validity.
        /// </summary>
        public void HighlightTileAt(Vector2Int gridPos, Vector2Int objectSize, GridPlacementSystem gridSystem, bool isValid)
        {
            ClearTileHighlight();

            Color highlightColor = GetHighlightColor(isValid);

            for (int x = 0; x < objectSize.x; x++)
            {
                for (int y = 0; y < objectSize.y; y++)
                {
                    Vector2Int checkPos = new Vector2Int(gridPos.x + x, gridPos.y + y);
                    var tile = gridSystem.FindTileAtPosition(checkPos);
                    if (tile != null)
                    {
                        var gridManager = _gridManager ?? ServiceLocator.Instance?.Get<GridManager>();
                        float minAlpha = 0.6f;
                        float duration = 0.5f;
                        if (gridManager?.GridSettings != null)
                        {
                            minAlpha = gridManager.GridSettings.HighlightMinAlpha;
                            duration = gridManager.GridSettings.HighlightAnimationDuration;
                        }
                        tile.ShowHighlight(highlightColor, minAlpha, duration);
                        _highlightedTiles.Add(tile);
                    }
                }
            }
        }

        /// <summary>
        /// Clears all currently highlighted tiles.
        /// </summary>
        public void ClearTileHighlight()
        {
            foreach (var tile in _highlightedTiles)
            {
                if (tile != null)
                {
                    tile.HideHighlight();
                }
            }
            _highlightedTiles.Clear();
        }

        /// <summary>
        /// Gets the appropriate highlight color based on placement validity.
        /// </summary>
        private Color GetHighlightColor(bool isValid)
        {
            var gridManager = _gridManager ?? ServiceLocator.Instance?.Get<GridManager>();
            if (gridManager != null && gridManager.GridSettings != null)
            {
                return isValid ? gridManager.GridSettings.ValidHighlightColor : gridManager.GridSettings.InvalidHighlightColor;
            }

            return isValid ? Color.white : Color.red;
        }
    }
}
