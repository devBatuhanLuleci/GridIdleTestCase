using UnityEngine;
using System.Collections.Generic;
using GridSystemModule.Core.Interfaces;
using GridSystemModule.Core.Models;
using GridSystemModule.Services;
using BoardGameTestCase.Core.Common;
 

namespace GridSystemModule.Managers
{
    public class GridManager : MonoBehaviour, IInitializable
    {
        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;
        public static System.Action OnGridReady;        [SerializeField] private GridSystemSettings _gridSettings;
        [SerializeField] private Transform _camera;
        [SerializeField] private Transform _tilesParent;
        [SerializeField] private bool _autoGenerateOnStart = true;        [SerializeField] private bool _showGizmos = false;

        private Dictionary<Vector2, BaseTile> _generatedTiles = new Dictionary<Vector2, BaseTile>();

        public int TileCount => _generatedTiles?.Count ?? 0;

        public Transform TilesParent => _tilesParent;
        
        public GridSystemSettings GridSettings => _gridSettings;

        private IGridService _gridService;
        private IGridConfiguration _gridConfiguration;
        
        private Vector3 _lastTilesParentScale;
        private Vector3 _lastTilesParentPosition;
        private Quaternion _lastTilesParentRotation;

        private void Awake()
        {
            ServiceLocator.Instance.Register<GridManager>(this);
        }
        
        private void OnDestroy()
        {
            if (ServiceLocator.Instance != null)
            {
                ServiceLocator.Instance.Unregister<GridManager>();
            }
        }
        
        public void Initialize()
        {
            if (_isInitialized) return;
            InitializeGridSystem();
            _isInitialized = true;
        }
    
        public void InitializeGridSystem()
        {
            if (_gridSettings == null) return;
            
            EnsureTilesParentExists();
            
            _gridConfiguration = _gridSettings.CreateGridConfiguration(_camera, _tilesParent);
            _gridService = new GridService(_gridConfiguration);
            
            if (_camera != null)
            {
                SetupCamera();
            }
        }

        private void Start()
        {
            if (_autoGenerateOnStart)
            {
                Invoke(nameof(CheckAndGenerateGrid), 0.1f);
            }
        }
        
        private void CheckAndGenerateGrid()
        {
            GenerateGrid();
        }
        
        private void SetupCamera()
        {
            if (_gridSettings == null) return;
            
            var camera = _camera.GetComponent<Camera>();
            if (camera != null)
            {
                camera.orthographicSize = _gridSettings.CameraSize;
            }
        }

        public void GenerateGrid()
        {
            EnsureTilesParentExists();
            
            if (_gridService == null)
            {
                InitializeGridSystem();
            }

            if (_gridService == null) return;
            _gridService.GenerateGrid();
            
            UpdateGeneratedTilesList();
            
            TrackTilesParentTransform();
            
            SyncGridPlacementSystem();

            OnGridReady?.Invoke();
        }
        
        private void UpdateGeneratedTilesList()
        {
            _generatedTiles.Clear();
            var allTiles = GetAllTiles();
            if (allTiles != null)
            {
                foreach (var kvp in allTiles)
                {
                    _generatedTiles[kvp.Key] = kvp.Value;
                }
            }
        }
        
        private void EnsureTilesParentExists()
        {
            if (_tilesParent == null)
            {
                _tilesParent = new GameObject("TilesParent").transform;
                _tilesParent.SetParent(transform);
                _tilesParent.localPosition = Vector3.zero;
                _tilesParent.localRotation = Quaternion.identity;
                _tilesParent.localScale = Vector3.one;
            }
        }
        
        private void TrackTilesParentTransform()
        {
            if (_tilesParent != null)
            {
                _lastTilesParentScale = _tilesParent.localScale;
                _lastTilesParentPosition = _tilesParent.localPosition;
                _lastTilesParentRotation = _tilesParent.localRotation;
            }
        }
        
        private void LateUpdate()
        {
            if (_tilesParent == null || _gridService == null) return;
            
            bool scaleChanged = _tilesParent.localScale != _lastTilesParentScale;
            bool positionChanged = _tilesParent.localPosition != _lastTilesParentPosition;
            bool rotationChanged = _tilesParent.localRotation != _lastTilesParentRotation;
            
            if (scaleChanged || positionChanged || rotationChanged)
            {
                MaintainTileLocalTransforms();
                
                TrackTilesParentTransform();
            }
        }
        
        private void MaintainTileLocalTransforms()
        {
            var allTiles = _gridService.GetAllTiles();
            if (allTiles == null) return;
            
            foreach (var kvp in allTiles)
            {
                Vector2 gridPos = kvp.Key;
                BaseTile tile = kvp.Value;
                
                if (tile != null && tile.transform != null && tile.transform.parent == _tilesParent)
                {
                    Vector3 localPosition = new Vector3(gridPos.x, gridPos.y, 0);
                    tile.transform.localPosition = localPosition;
                    tile.transform.localScale = Vector3.one;
                }
            }
        }

        public void ClearGrid()
        {
            if (_gridService == null)
            {
                InitializeGridSystem();
            }

            if (_gridService == null) return;
            _gridService.ClearGrid();
            _generatedTiles.Clear();
        }

        public void ClearAndRegenerateGrid()
        {
            ClearGrid();
            GenerateGrid();
        }

        public BaseTile GetTileAtPosition(Vector2 position)
        {
            return _gridService?.GetTileAtPosition(position) as BaseTile;
        }

        public bool IsPositionValid(Vector2 position)
        {
            return _gridService?.IsPositionValid(position) ?? false;
        }

        public System.Collections.Generic.Dictionary<Vector2, BaseTile> GetAllTiles()
        {
            var tiles = _gridService?.GetAllTiles();
            var baseTiles = new System.Collections.Generic.Dictionary<Vector2, BaseTile>();
            
            if (tiles != null)
            {
                foreach (var kvp in tiles)
                {
                    if (kvp.Value is BaseTile baseTile)
                    {
                        baseTiles[kvp.Key] = baseTile;
                    }
                }
            }
            
            return baseTiles;
        }

        private void SyncGridPlacementSystem()
        {
            if (_gridConfiguration == null) return;
            
        }

        private void OnValidate()
        {
            if (_gridSettings != null)
            {
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!_showGizmos || _gridSettings == null) return;
            
            Gizmos.color = Color.white;
            
            float edgeOffset = 0.5f;
            
            for (int x = 0; x <= _gridSettings.Width; x++)
            {
                float xPos = x - edgeOffset;
                Vector3 start = new Vector3(xPos, -edgeOffset, 0);
                Vector3 end = new Vector3(xPos, _gridSettings.Height - edgeOffset, 0);
                
                Gizmos.DrawLine(start, end);
            }
            
            for (int y = 0; y <= _gridSettings.Height; y++)
            {
                float yPos = y - edgeOffset;
                Vector3 start = new Vector3(-edgeOffset, yPos, 0);
                Vector3 end = new Vector3(_gridSettings.Width - edgeOffset, yPos, 0);
                
                Gizmos.DrawLine(start, end);
            }
        }
    }
}