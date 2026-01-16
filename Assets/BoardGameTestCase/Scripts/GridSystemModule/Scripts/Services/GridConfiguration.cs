using UnityEngine;
using GridSystemModule.Core.Interfaces;
using GridSystemModule.Core.Models;

namespace GridSystemModule.Services
{    [System.Serializable]
    public class GridConfiguration : IGridConfiguration
    {
        [SerializeField] private int _width = 10;
        [SerializeField] private int _height = 10;
        [SerializeField] private BaseTile _grassTile;
        [SerializeField] private Transform _camera;
        [SerializeField] private Transform _tilesParent;
        [SerializeField] private GridSystemSettings _settings;

        public int Width => _width;
        public int Height => _height;
        public BaseTile GrassTile => _grassTile;
        public Transform Camera => _camera;
        public Transform TilesParent => _tilesParent;
        public GridSystemSettings Settings => _settings;

        public GridConfiguration(int width, int height, BaseTile grassTile, Transform camera, Transform tilesParent, GridSystemSettings settings = null)
        {
            _width = width;
            _height = height;
            _grassTile = grassTile;
            _camera = camera;
            _tilesParent = tilesParent;
            _settings = settings;
        }
    }
}
