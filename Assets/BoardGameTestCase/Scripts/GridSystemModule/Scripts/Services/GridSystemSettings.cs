using UnityEngine;
using GridSystemModule.Core.Models;

namespace GridSystemModule.Services
{
    [CreateAssetMenu(fileName = "GridSystemSettings", menuName = "Grid System/Settings")]
    public class GridSystemSettings : ScriptableObject
    {
        [SerializeField] private int _width = 4;
        [SerializeField] private int _height = 8;
        [SerializeField] private Vector2 _cellSize = new Vector2(1f, 1f);
        [SerializeField] private Vector2 _cellSpacing = new Vector2(0.1f, 0.1f);
        [SerializeField] private bool _centerGrid = true;
        [SerializeField] private BaseTile _normalGrassTilePrefab;
        [SerializeField] private BaseTile _darkGrassTilePrefab;
        [SerializeField] private Vector3 _cameraOffset = new Vector3(0, 0, -10);
        [SerializeField] private float _cameraSize = 5f;
        [SerializeField] private Color _validHighlightColor = Color.white;
        [SerializeField] private Color _invalidHighlightColor = Color.red;
        [SerializeField] private float _highlightMinAlpha = 0.6f;
        [SerializeField] private float _highlightAnimationDuration = 0.5f;
        
        // Optional: assign an existing prefab to overwrite on regeneration
        // If left null, a new prefab will be created at the default path.
        [SerializeField] private GameObject _tilesPrefab;
        [SerializeField] private string _tilesPrefabPath = "Assets/BoardGameTestCase/Prefabs/GAMEPLAY/Tiles/Tiles.prefab";
        
        public int Width => _width;
        public int Height => _height;
        public Vector2 CellSize => _cellSize;
        public Vector2 CellSpacing => _cellSpacing;
        public bool CenterGrid => _centerGrid;
        public BaseTile NormalGrassTilePrefab => _normalGrassTilePrefab;
        public BaseTile DarkGrassTilePrefab => _darkGrassTilePrefab;
        public Vector3 CameraOffset => _cameraOffset;
        public float CameraSize => _cameraSize;
        public Color ValidHighlightColor => _validHighlightColor;
        public Color InvalidHighlightColor => _invalidHighlightColor;
        public float HighlightMinAlpha => _highlightMinAlpha;
        public float HighlightAnimationDuration => _highlightAnimationDuration;
        public GameObject TilesPrefab => _tilesPrefab;
        public string TilesPrefabPath => _tilesPrefabPath;

        public BaseTile GetGrassTileForPosition(int x, int y)
        {
            if ((x + y) % 2 == 0)
                return _normalGrassTilePrefab;
            else
                return _darkGrassTilePrefab;
        }
        
        public GridConfiguration CreateGridConfiguration(Transform camera, Transform tilesParent)
        {
            return new GridConfiguration(_width, _height, _normalGrassTilePrefab, camera, tilesParent, this);
        }

        public Vector3 GetCameraPosition()
        {
            float centerX = (float)_width / 2 - 0.5f;
            float centerY = (float)_height / 2 - 0.5f;
            return new Vector3(centerX, centerY, _cameraOffset.z);
        }

        private void OnValidate()
        {
            if (_width < 1) _width = 1;
            if (_height < 1) _height = 1;
            if (_cameraSize < 1) _cameraSize = 1;
        }
    }
}
