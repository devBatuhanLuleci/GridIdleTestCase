using UnityEngine;
using GridSystemModule.Core.Interfaces;
using GridSystemModule.Core;
using GridSystemModule.Services;
using BoardGameTestCase.Core.Common;

namespace GridSystemModule.Managers
{
    public class PlacementManager : MonoBehaviour, IInitializable
    {
        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;
        [SerializeField] private GridPlacementSystem _gridPlacementSystem;
        [SerializeField] private bool _dontDestroyOnLoad = true;
        
        public IGridPlacementSystem GridPlacementSystem => _gridPlacementSystem;
        
        private void Awake()
        {
            ServiceLocator.Instance.Register<PlacementManager>(this);
            
            if (_dontDestroyOnLoad)
            {
                if (transform.parent == null)
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
        }
        
        private void OnDestroy()
        {
            if (ServiceLocator.Instance != null)
            {
                ServiceLocator.Instance.Unregister<PlacementManager>();
            }
        }
        
        public void Initialize()
        {
            if (_isInitialized) return;
            InitializePlacementSystem();
            _isInitialized = true;
        }
        
        private void InitializePlacementSystem()
        {
            if (_gridPlacementSystem == null)
            {
                _gridPlacementSystem = ServiceLocator.Instance?.Get<GridPlacementSystem>();
            }
            
            if (_gridPlacementSystem != null)
            {
                ServiceLocator.Instance?.Register<GridPlacementSystem>(_gridPlacementSystem);
                ServiceLocator.Instance?.Register<IGridPlacementSystem>(_gridPlacementSystem);
            }
        }
        
        public bool PlaceObject(IPlaceable placeable, Vector2Int gridPosition)
        {
            if (_gridPlacementSystem == null) return false;
            return _gridPlacementSystem.PlaceObject(placeable, gridPosition);
        }
        
        public bool RemoveObject(IPlaceable placeable)
        {
            if (_gridPlacementSystem == null) return false;
            return _gridPlacementSystem.RemoveObject(placeable);
        }
        
        public bool IsValidPlacement(Vector2Int gridPosition, Vector2Int objectSize, IPlaceable excludeObject = null)
        {
            if (_gridPlacementSystem == null) return false;
            return _gridPlacementSystem.IsValidPlacement(gridPosition, objectSize, excludeObject);
        }
        
        public IPlaceable GetObjectAt(Vector2Int gridPosition)
        {
            if (_gridPlacementSystem == null) return null;
            return _gridPlacementSystem.GetObjectAt(gridPosition);
        }
        
        public Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            if (_gridPlacementSystem == null) return Vector2Int.zero;
            return _gridPlacementSystem.WorldToGrid(worldPosition);
        }
        
        public Vector3 GridToWorld(Vector2Int gridPosition)
        {
            if (_gridPlacementSystem == null) return Vector3.zero;
            return _gridPlacementSystem.GridToWorld(gridPosition);
        }
        
        public void ClearAll()
        {
            if (_gridPlacementSystem == null) return;
            _gridPlacementSystem.ClearAll();
        }
        
        public Vector2Int[] GetOccupiedPositions()
        {
            if (_gridPlacementSystem == null) return new Vector2Int[0];
            return _gridPlacementSystem.GetOccupiedPositions();
        }
    }
}
