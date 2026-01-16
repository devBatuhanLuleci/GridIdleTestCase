using System.Collections.Generic;
using UnityEngine;

namespace GridSystemModule.Core.Models
{    public class GridData
    {
        public Dictionary<Vector2, BaseTile> Tiles { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public GridData(int width, int height)
        {
            Width = width;
            Height = height;
            Tiles = new Dictionary<Vector2, BaseTile>();
        }

        public void AddTile(Vector2 position, BaseTile tile)
        {
            Tiles[position] = tile;
        }

        public void RemoveTile(Vector2 position)
        {
            Tiles.Remove(position);
        }

        public void ClearAllTiles()
        {
            Tiles.Clear();
        }

        public bool HasTileAt(Vector2 position)
        {
            return Tiles.ContainsKey(position);
        }

        public BaseTile GetTileAt(Vector2 position)
        {
            return Tiles.TryGetValue(position, out var tile) ? tile : null;
        }
    }
}
