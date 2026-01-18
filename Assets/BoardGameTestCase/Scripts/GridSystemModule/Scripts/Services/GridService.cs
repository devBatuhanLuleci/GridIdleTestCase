using UnityEngine;
using System.Collections.Generic;
using GridSystemModule.Core.Interfaces;
using GridSystemModule.Core.Models;
using GridSystemModule.Tiles;

namespace GridSystemModule.Services
{    public class GridService : IGridService
    {
        private readonly IGridConfiguration _configuration;
        private readonly GridData _gridData;

        public GridService(IGridConfiguration configuration)
        {
            _configuration = configuration;
            _gridData = new GridData(_configuration.Width, _configuration.Height);
        }

        public void GenerateGrid()
        {
            ClearGrid();
            
            for (int x = 0; x < _configuration.Width; x++)
            {
                for (int y = 0; y < _configuration.Height; y++)
                {
                    CreateTileAt(x, y);
                }
            }
        }

        public void ClearGrid()
        {
            var tilesToDestroy = new List<BaseTile>(_gridData.Tiles.Values);
            
            foreach (var tile in tilesToDestroy)
            {
                if (tile != null)
                {
                    if (Application.isPlaying)
                        Object.Destroy(tile.gameObject);
                    else
                        Object.DestroyImmediate(tile.gameObject);
                }
            }

            _gridData.ClearAllTiles();
            ClearTilesParent();
        }

        public BaseTile GetTileAtPosition(Vector2 position)
        {
            return _gridData.GetTileAt(position);
        }

        public bool IsPositionValid(Vector2 position)
        {
            return position.x >= 0 && position.x < _configuration.Width &&
                   position.y >= 0 && position.y < _configuration.Height;
        }

        public Dictionary<Vector2, BaseTile> GetAllTiles()
        {
            return new Dictionary<Vector2, BaseTile>(_gridData.Tiles);
        }

        private void CreateTileAt(int x, int y)
        {
            var grassTilePrefab = GetGrassTileForPosition(x, y);
            if (grassTilePrefab == null) return;

            var position = new Vector2(x, y);
            
            var settings = GetGridSystemSettings();
            Vector2 cellSize = settings != null ? settings.CellSize : Vector2.one;
            Vector2 cellSpacing = settings != null ? settings.CellSpacing : Vector2.zero;

            var spawnedTile = Object.Instantiate(
                grassTilePrefab, 
                Vector3.zero, // Position will be set via localPosition
                Quaternion.identity
            );
            
            if (_configuration.TilesParent != null)
            {
                spawnedTile.transform.SetParent(_configuration.TilesParent, false);
                
                // Calculate and apply local position including spacing
                float px = x * (cellSize.x + cellSpacing.x);
                float py = y * (cellSize.y + cellSpacing.y);
                
                spawnedTile.transform.localPosition = new Vector3(px, py, 0);
                
                // Calculate proper scale based on sprite size
                // Sprite's natural world size = sprite.rect.size / ppu
                var spriteRenderer = spawnedTile.GetComponent<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    float spriteWidth = spriteRenderer.sprite.rect.width / spriteRenderer.sprite.pixelsPerUnit;
                    float spriteHeight = spriteRenderer.sprite.rect.height / spriteRenderer.sprite.pixelsPerUnit;
                    
                    // Calculate scale to fit tile into CellSize
                    float scaleX = spriteWidth > 0 ? cellSize.x / spriteWidth : 1f;
                    float scaleY = spriteHeight > 0 ? cellSize.y / spriteHeight : 1f;
                    
                    spawnedTile.transform.localScale = new Vector3(scaleX, scaleY, 1f);
                }
                else
                {
                    // Fallback: scale to CellSize if no sprite renderer
                    spawnedTile.transform.localScale = new Vector3(cellSize.x, cellSize.y, 1f);
                }
            }

            spawnedTile.name = $"Tile_{x}_{y}";
            spawnedTile.Initialize(x, y);
            
            _gridData.AddTile(position, spawnedTile);
        }
        
        private BaseTile GetGrassTileForPosition(int x, int y)
        {

            var config = _configuration as GridConfiguration;
            if (config?.Settings != null)
            {
                return config.Settings.GetGrassTileForPosition(x, y);
            }
            
            return _configuration.GrassTile;
        }
        
        private GridSystemModule.Services.GridSystemSettings GetGridSystemSettings()
        {

            var config = _configuration as GridConfiguration;
            return config?.Settings;
        }

        private void ClearTilesParent()
        {
            if (_configuration.TilesParent != null)
            {
                for (int i = _configuration.TilesParent.childCount - 1; i >= 0; i--)
                {
                    var child = _configuration.TilesParent.GetChild(i);
                    if (Application.isPlaying)
                        Object.Destroy(child.gameObject);
                    else
                        Object.DestroyImmediate(child.gameObject);
                }
            }
        }
    }
}
