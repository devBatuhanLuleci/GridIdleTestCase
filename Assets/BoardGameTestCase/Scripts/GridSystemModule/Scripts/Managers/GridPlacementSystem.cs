using System.Collections.Generic;
using UnityEngine;
using GridSystemModule.Core.Interfaces;
using GridSystemModule.Core.Models;
using GridSystemModule.Managers;
using PlacementModule.Settings;
using BoardGameTestCase.Core.Common;
using DG.Tweening;
using GameModule.Core.Interfaces;
using GameState = GameModule.Core.Interfaces.GameState;

namespace GridSystemModule.Services
{
    public class GridPlacementSystem : MonoBehaviour, IGridPlacementSystem
    {
        [SerializeField] private Vector2Int _gridDimensions = new Vector2Int(4, 8);
        [SerializeField] private Material _validPlacementMaterial;
        [SerializeField] private Material _invalidPlacementMaterial;
        [SerializeField] private PlacementAnimationSettings _placementAnimationSettings;
        [SerializeField] private List<Vector2Int> _debugOccupiedTiles = new List<Vector2Int>();
        [SerializeField] private List<Vector2Int> _debugAvailableTiles = new List<Vector2Int>();
        [SerializeField] private int _debugPlacedObjectsCount = 0;
        
        private Dictionary<Vector2Int, IPlaceable> _occupiedTilesWithObjects = new Dictionary<Vector2Int, IPlaceable>();
        
        private Dictionary<Vector2Int, IPlaceable> _placedObjects = new Dictionary<Vector2Int, IPlaceable>();
        private HashSet<Vector2Int> _occupiedTiles = new HashSet<Vector2Int>();
        private HashSet<Vector2Int> _availableTiles = new HashSet<Vector2Int>();
        private IPlaceable _currentDraggedObject;
        private Camera _camera;
        private Vector2Int _lastEvaluatedGridPosition = new Vector2Int(int.MinValue, int.MinValue);
        private bool _gridReady = false;
        private IPlaceable _pendingPlaceable;
        private bool _dragStartWasPlaced;
        private Vector2Int _dragStartGridPos;
        private Vector3 _dragStartWorldPos;
        private List<BaseTile> _highlightedTiles = new List<BaseTile>();
        private IGameFlowController _gameFlowController;
        
        private bool IsPlacingState()
        {
            if (_gameFlowController == null)
            {
                _gameFlowController = ServiceLocator.Instance?.Get<IGameFlowController>();
            }
            return _gameFlowController != null && _gameFlowController.CurrentGameState == GameState.Placing;
        }
        
        public Vector2Int GridDimensions => _gridDimensions;
        public IPlaceable CurrentDraggedObject => _currentDraggedObject;
        
        [ContextMenu("Test Coordinate Conversion")]
        public void TestCoordinateConversion()
        {
            for (int x = 0; x < _gridDimensions.x; x++)
            {
                for (int y = 0; y < _gridDimensions.y; y++)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    Vector3 worldPos = GridToWorld(gridPos);
                    Vector2Int backToGrid = WorldToGrid(worldPos);
                }
            }
        }
        
        public void SyncGridDimensions(int width, int height)
        {
            _gridDimensions = new Vector2Int(width, height);
            RebuildAvailabilityCache();
        }
        
        public void SyncAllSettingsFromGridSettings()
        {
            var gridManager = ServiceLocator.Instance.Get<GridManager>();
            if (gridManager != null && gridManager.GridSettings != null)
            {
                _gridDimensions = new Vector2Int(gridManager.GridSettings.Width, gridManager.GridSettings.Height);
            }
        }
        
        [ContextMenu("Force Sync from GridManager")]
        public void ForceSyncFromGridManager()
        {
            SyncAllSettingsFromGridSettings();
            RebuildAvailabilityCache();
        }
        
        private void Awake()
        {
            if (ServiceLocator.Instance != null)
            {
                ServiceLocator.Instance.Register<GridPlacementSystem>(this);
                ServiceLocator.Instance.Register<IGridPlacementSystem>(this);
            }

            _camera = Camera.main;

            if (_placementAnimationSettings == null)
            {
                _placementAnimationSettings = PlacementAnimationSettings.LoadOrDefaults();
            }
        }
        
        private void OnEnable()
        {

            GridSystemModule.Managers.GridManager.OnGridReady += HandleGridReady;

            _gridReady = CheckGridAlreadyReady();
        }
        
        private void OnDisable()
        {
            GridSystemModule.Managers.GridManager.OnGridReady -= HandleGridReady;
            
            ClearTileHighlight();
            
            if (_currentDraggedObject != null)
            {
                var mb = _currentDraggedObject as MonoBehaviour;
                if (mb != null && mb.transform != null)
                {
                    mb.transform.DOKill(true);
                }
                _currentDraggedObject = null;
            }
        }
        
        private void OnDestroy()
        {
            if (ServiceLocator.Instance != null)
            {
                ServiceLocator.Instance.Unregister<GridPlacementSystem>();
                ServiceLocator.Instance.Unregister<IGridPlacementSystem>();
            }
            
            ClearTileHighlight();
            
            foreach (var obj in _placedObjects.Values)
            {
                var mb = obj as MonoBehaviour;
                if (mb != null && mb.transform != null)
                {
                    mb.transform.DOKill(true);
                }
            }
            
            _placedObjects.Clear();
            _occupiedTiles.Clear();
            _availableTiles.Clear();
            _currentDraggedObject = null;
        }
        
        private void Start()
        {

            SyncAllSettingsFromGridSettings();
            RebuildAvailabilityCache();
        }

        private void HandleGridReady()
        {
            _gridReady = true;
            if (_pendingPlaceable != null)
            {
                var toStart = _pendingPlaceable;
                _pendingPlaceable = null;
                StartDragging(toStart);
            }
        }
        
        private bool CheckGridAlreadyReady()
        {
            var gridManager = ServiceLocator.Instance.Get<GridSystemModule.Managers.GridManager>();
            if (gridManager == null) return false;
            var tiles = gridManager.GetAllTiles();
            return tiles != null && tiles.Count > 0;
        }
        
        private void CreatePlacementPreview()
        {

        }
        
        public bool IsValidPlacement(Vector2Int gridPosition, Vector2Int objectSize, IPlaceable excludeObject = null)
        {

            if (gridPosition.x < 0 || gridPosition.y < 0 ||
                gridPosition.x + objectSize.x > _gridDimensions.x ||
                gridPosition.y + objectSize.y > _gridDimensions.y)
            {
                return false;
            }
            
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
        
        public Vector2Int[] GetOccupiedPositions()
        {
            return new List<Vector2Int>(_placedObjects.Keys).ToArray();
        }
        
        private void RebuildAvailabilityCache()
        {
            _availableTiles.Clear();
            _occupiedTiles.Clear();
            
            int bottomHalfThreshold = _gridDimensions.y / 2;
            
            for (int x = 0; x < _gridDimensions.x; x++)
            {
                for (int y = 0; y < bottomHalfThreshold; y++)
                {
                    _availableTiles.Add(new Vector2Int(x, y));
                }
            }
            foreach (var pos in _placedObjects.Keys)
            {
                _occupiedTiles.Add(pos);
                _availableTiles.Remove(pos);
            }
            UpdateDebugInspectorLists();
        }
        
        private void UpdateDebugInspectorLists()
        {
            _debugOccupiedTiles.Clear();
            _debugOccupiedTiles.AddRange(_occupiedTiles);
            _debugAvailableTiles.Clear();
            _debugAvailableTiles.AddRange(_availableTiles);
            _debugPlacedObjectsCount = _placedObjects.Count;
            
            _occupiedTilesWithObjects.Clear();
            foreach (var pos in _occupiedTiles)
            {
                if (_placedObjects.TryGetValue(pos, out var obj))
                {
                    _occupiedTilesWithObjects[pos] = obj;
                }
            }
        }

        public bool IsTileOccupied(Vector2Int pos)
        {
            return _occupiedTiles.Contains(pos);
        }

        public IPlaceable GetOccupant(Vector2Int pos)
        {
            _placedObjects.TryGetValue(pos, out var obj);
            return obj;
        }

        public IReadOnlyCollection<Vector2Int> GetAvailableTiles()
        {
            return _availableTiles;
        }
        
        public bool PlaceObject(IPlaceable placeable, Vector2Int gridPosition, bool skipOnPlacedCallback = false)
        {
            if (!IsPlacingState()) return false;
            return PlaceObjectInternal(placeable, gridPosition, skipOnPlacedCallback);
        }
        
        private bool PlaceObjectInternal(IPlaceable placeable, Vector2Int gridPosition, bool skipOnPlacedCallback = false)
        {
            if (placeable == null) 
            {
                return false;
            }
            
            if (!IsValidPlacement(gridPosition, placeable.GridSize, placeable)) return false;
            
            if (placeable.IsPlaced)
            {
                RemoveObject(placeable);
            }
            
            var occupiedPositions = new List<Vector2Int>();
            for (int x = 0; x < placeable.GridSize.x; x++)
            {
                for (int y = 0; y < placeable.GridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(gridPosition.x + x, gridPosition.y + y);
                    occupiedPositions.Add(pos);
                    _placedObjects[pos] = placeable;
                    _occupiedTiles.Add(pos);
                    _availableTiles.Remove(pos);
                }
            }
            
            placeable.GridPosition = gridPosition;
            placeable.IsPlaced = true;
            
            var placeableMb = placeable as MonoBehaviour;
            if (placeableMb != null)
            {
                Vector3 centerPosition = MultiTileGridToWorld(occupiedPositions);
                Vector3 pivotOffset = CalculatePivotOffset(placeableMb);
                placeableMb.transform.position = centerPosition + pivotOffset;
            }
            
            if (!skipOnPlacedCallback)
            {
                placeable.OnPlaced(gridPosition);
            }
            
            RebuildAvailabilityCache();
            UpdateDebugInspectorLists();
            return true;
        }
        
        public bool RemoveObject(IPlaceable placeable)
        {
            if (placeable == null || !placeable.IsPlaced) return false;
            
            var positionsToRemove = new List<Vector2Int>();
            foreach (var kvp in _placedObjects)
            {
                if (kvp.Value == placeable)
                {
                    positionsToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var pos in positionsToRemove)
            {
                _placedObjects.Remove(pos);
                _occupiedTiles.Remove(pos);
                if (pos.x >= 0 && pos.y >= 0 && pos.x < _gridDimensions.x && pos.y < _gridDimensions.y)
                {
                    _availableTiles.Add(pos);
                }
            }
            
            placeable.IsPlaced = false;
            placeable.OnRemoved();
            
            var placeableMb = placeable as MonoBehaviour;
            if (placeableMb != null)
            {
                placeableMb.transform.SetParent(null);
            }
            
            RebuildAvailabilityCache();
            UpdateDebugInspectorLists();
            
            return true;
        }
        
        public IPlaceable GetObjectAt(Vector2Int gridPosition)
        {
            _placedObjects.TryGetValue(gridPosition, out var obj);
            return obj;
        }
        
        public Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            Transform tilesParent = GetTilesParent();
            if (tilesParent != null)
            {
                Vector3 parentPos = tilesParent.position;
                Quaternion parentRot = tilesParent.rotation;
                Vector3 parentScale = tilesParent.lossyScale;
                
                Vector3 relativePos = Quaternion.Inverse(parentRot) * (worldPosition - parentPos);
                relativePos = new Vector3(
                    relativePos.x / parentScale.x,
                    relativePos.y / parentScale.y,
                    relativePos.z / parentScale.z
                );
                
                // Get CellSize and CellSpacing from GridManager settings
                var gridManager = ServiceLocator.Instance?.Get<GridManager>();
                Vector2 cellSize = Vector2.one;
                Vector2 cellSpacing = Vector2.zero;
                
                if (gridManager != null && gridManager.GridSettings != null)
                {
                    cellSize = gridManager.GridSettings.CellSize;
                    cellSpacing = gridManager.GridSettings.CellSpacing;
                }
                
                // Calculate grid position accounting for cell size and spacing
                float cellSizeX = cellSize.x + cellSpacing.x;
                float cellSizeY = cellSize.y + cellSpacing.y;
                
                int x = Mathf.RoundToInt(relativePos.x / cellSizeX);
                int y = Mathf.RoundToInt(relativePos.y / cellSizeY);
                return new Vector2Int(x, y);
            }
            
            var gridManager2 = ServiceLocator.Instance?.Get<GridManager>();
            if (gridManager2 != null)
            {
                var allTiles = gridManager2.GetAllTiles();
                if (allTiles != null && allTiles.Count > 0)
                {
                    BaseTile nearestTile = null;
                    Vector2? nearestGridPos = null;
                    float nearestDistance = float.MaxValue;
                    
                    foreach (var kvp in allTiles)
                    {
                        if (kvp.Value != null)
                        {
                            float distance = Vector3.Distance(worldPosition, kvp.Value.transform.position);
                            if (distance < nearestDistance)
                            {
                                nearestDistance = distance;
                                nearestTile = kvp.Value;
                                nearestGridPos = kvp.Key;
                            }
                        }
                    }
                    
                    if (nearestGridPos.HasValue)
                    {
                        return new Vector2Int(Mathf.RoundToInt(nearestGridPos.Value.x), Mathf.RoundToInt(nearestGridPos.Value.y));
                    }
                }
            }
            
            // Fallback: no spacing assumption
            int wx = Mathf.RoundToInt(worldPosition.x);
            int wy = Mathf.RoundToInt(worldPosition.y);
            return new Vector2Int(wx, wy);
        }
        
        public Vector3 GridToWorld(Vector2Int gridPosition)
        {
            var tile = FindTileAtPosition(gridPosition);
            if (tile != null)
            {
                return tile.transform.position;
            }
            
            // Get CellSize and CellSpacing from GridManager settings
            var gridManager = ServiceLocator.Instance?.Get<GridManager>();
            Vector2 cellSize = Vector2.one;
            Vector2 cellSpacing = Vector2.zero;
            
            if (gridManager != null && gridManager.GridSettings != null)
            {
                cellSize = gridManager.GridSettings.CellSize;
                cellSpacing = gridManager.GridSettings.CellSpacing;
            }
            
            // Calculate local position accounting for cell size and spacing
            float cellSizeX = cellSize.x + cellSpacing.x;
            float cellSizeY = cellSize.y + cellSpacing.y;
            
            Vector3 localPosition = new Vector3(
                gridPosition.x * cellSizeX,
                gridPosition.y * cellSizeY,
                0
            );
            
            Transform tilesParent = GetTilesParent();
            if (tilesParent != null)
            {
                Vector3 parentPos = tilesParent.position;
                Quaternion parentRot = tilesParent.rotation;
                Vector3 parentScale = tilesParent.lossyScale;
                
                Vector3 scaledLocalPos = new Vector3(
                    localPosition.x * parentScale.x,
                    localPosition.y * parentScale.y,
                    localPosition.z * parentScale.z
                );
                
                return parentPos + parentRot * scaledLocalPos;
            }
            
            return localPosition;
        }
        
        private Transform GetTilesParent()
        {
            var gridManager = ServiceLocator.Instance.Get<GridManager>();
            if (gridManager != null)
            {
                return gridManager.TilesParent;
            }
            
            var gm = ServiceLocator.Instance.Get<GridManager>();
            return gm != null ? gm.TilesParent : null;
        }
        
        public void StartDragging(IPlaceable placeable)
        {
            if (!IsPlacingState()) return;
            if (!_gridReady)
            {
                _pendingPlaceable = placeable;
                return;
            }
            if (placeable == null) return;
            
            _dragStartWasPlaced = placeable.IsPlaced;
            _dragStartGridPos = placeable.GridPosition;
            var mbForKill = placeable as MonoBehaviour;
            
            // Calculate center position for multi-tile objects
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
                _dragStartWorldPos = MultiTileGridToWorld(occupiedPositions);
            }
            
            if (_dragStartWasPlaced)
            {
                RemoveObject(placeable);
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
        
        public void UpdateDrag(Vector3 worldPosition)
        {
            if (!IsPlacingState())
            {
                if (_currentDraggedObject != null)
                {
                    EndDragging(worldPosition);
                }
                return;
            }
            if (!_gridReady || _currentDraggedObject == null) return;
            
            // Convert world position from object center to grid anchor
            Vector3 adjustedWorldPos = worldPosition;
            Vector2Int objectSize = _currentDraggedObject.GridSize;
            
            var gridManager = ServiceLocator.Instance?.Get<GridManager>();
            if (gridManager != null && gridManager.GridSettings != null)
            {
                Vector2 cellSize = gridManager.GridSettings.CellSize;
                Vector2 cellSpacing = gridManager.GridSettings.CellSpacing;
                float cellSizeX = cellSize.x + cellSpacing.x;
                float cellSizeY = cellSize.y + cellSpacing.y;
                
                // Offset: (size - 1) / 2 cells from center to bottom-left
                float offsetX = (objectSize.x - 1) * 0.5f * cellSizeX;
                float offsetY = (objectSize.y - 1) * 0.5f * cellSizeY;
                
                adjustedWorldPos = worldPosition - new Vector3(offsetX, offsetY, 0f);
            }
            
            Vector2Int gridPos = WorldToGrid(adjustedWorldPos);
            if (gridPos == _lastEvaluatedGridPosition)
            {
                return;
            }
            _lastEvaluatedGridPosition = gridPos;
            
            _currentDraggedObject.OnDrag(worldPosition);
            
            bool isValid = IsValidPlacement(gridPos, _currentDraggedObject.GridSize, _currentDraggedObject);
            
            IPlaceable occupant = null;
            for (int x = 0; x < _currentDraggedObject.GridSize.x; x++)
            {
                for (int y = 0; y < _currentDraggedObject.GridSize.y; y++)
                {
                    Vector2Int checkPos = new Vector2Int(gridPos.x + x, gridPos.y + y);
                    var objAtPos = GetObjectAt(checkPos);
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
                    
                    bool canDraggedToOccupant = IsValidPlacement(occupantPos, _currentDraggedObject.GridSize, occupant);
                    bool canOccupantToDragged = _dragStartWasPlaced ? IsValidPlacement(draggedStartPos, occupant.GridSize, _currentDraggedObject) : true;
                    
                    isValid = canDraggedToOccupant && canOccupantToDragged;
                }
            }
            
            HighlightTileAt(gridPos, isValid);
        }
        
        public void EndDragging(Vector3 worldPosition)
        {
            if (!_gridReady) return;
            if (_currentDraggedObject == null) return;
            
            // Same adjustment as UpdateDrag - both X and Y offsets
            Vector3 adjustedWorldPos = worldPosition;
            Vector2Int objectSize = _currentDraggedObject.GridSize;
            
            var gridManager = ServiceLocator.Instance?.Get<GridManager>();
            if (gridManager != null && gridManager.GridSettings != null)
            {
                Vector2 cellSize = gridManager.GridSettings.CellSize;
                Vector2 cellSpacing = gridManager.GridSettings.CellSpacing;
                float cellSizeX = cellSize.x + cellSpacing.x;
                float cellSizeY = cellSize.y + cellSpacing.y;
                
                float offsetX = (objectSize.x - 1) * 0.5f * cellSizeX;
                float offsetY = (objectSize.y - 1) * 0.5f * cellSizeY;
                
                adjustedWorldPos = worldPosition - new Vector3(offsetX, offsetY, 0f);
            }
            
            Vector2Int gridPos = WorldToGrid(adjustedWorldPos);
            bool wasAutoSnappedFromInvalid = false;
            
            
            IPlaceable occupant = null;
            Vector2Int? occupantGridPos = null;
            for (int x = 0; x < _currentDraggedObject.GridSize.x; x++)
            {
                for (int y = 0; y < _currentDraggedObject.GridSize.y; y++)
                {
                    Vector2Int checkPos = new Vector2Int(gridPos.x + x, gridPos.y + y);
                    var objAtPos = GetObjectAt(checkPos);
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

                    RevertDraggedToStartPosition();
                    FinishDragWithoutPlacement(gridPos);
                    return;
                }
                
                bool swapped = TrySwapWithOccupant(_currentDraggedObject, occupant, occupantGridPos.Value, _dragStartGridPos);
                if (swapped)
                {
                    return;
                }

                RevertDraggedToStartPosition();
                FinishDragWithoutPlacement(gridPos);
                return;
            }
            
            bool isValid = IsValidPlacement(gridPos, _currentDraggedObject.GridSize, _currentDraggedObject);
            
            if (!isValid)
            {

                if (_dragStartWasPlaced)
                {
                    Vector2Int nearestValidPos = FindNearestValidPosition(gridPos, _currentDraggedObject.GridSize, _currentDraggedObject);
                    if (nearestValidPos != gridPos)
                    {
                        gridPos = nearestValidPos;
                        isValid = true;
                        wasAutoSnappedFromInvalid = true;
                    }
                    else
                    {

                        RevertDraggedToStartPosition();
                        FinishDragWithoutPlacement(gridPos);
                        return;
                    }
                }
                else
                {

                    RevertDraggedToStartPosition();
                    FinishDragWithoutPlacement(gridPos);
                    return;
                }
            }
            
            bool placed = PlaceObject(_currentDraggedObject, gridPos);
            
            if (placed)
            {
                TryPlayPlacementAnimation(_currentDraggedObject, gridPos, wasAutoSnappedFromInvalid);
                _currentDraggedObject.OnDrop(gridPos, true);
                
                var draggedMb = _currentDraggedObject as MonoBehaviour;
                if (draggedMb != null && draggedMb.transform != null)
                {
                    draggedMb.transform.SetParent(null);
                }
            }
            else
            {
                RevertDraggedToStartPosition();
                _currentDraggedObject.OnDrop(gridPos, false);
            }
            
            _currentDraggedObject.IsDragging = false;
            _currentDraggedObject = null;
            _lastEvaluatedGridPosition = new Vector2Int(int.MinValue, int.MinValue);
            
            ClearTileHighlight();
        }

        public void RevertDraggedToStartPosition()
        {
            var mb = _currentDraggedObject as MonoBehaviour;
            if (mb == null || mb.transform == null) return;
            
            mb.transform.DOKill(true);

            float duration = _placementAnimationSettings != null && _placementAnimationSettings.useSeparateInvalidSnap
                ? _placementAnimationSettings.invalidSnapDuration
                : (_placementAnimationSettings != null ? _placementAnimationSettings.positionDuration : 0.1f);
            Ease ease = _placementAnimationSettings != null && _placementAnimationSettings.useSeparateInvalidSnap
                ? _placementAnimationSettings.invalidSnapEase
                : (_placementAnimationSettings != null ? _placementAnimationSettings.positionEase : Ease.OutCubic);
            float overshoot = _placementAnimationSettings != null && _placementAnimationSettings.useSeparateInvalidSnap
                ? _placementAnimationSettings.invalidSnapOvershoot
                : (_placementAnimationSettings != null ? _placementAnimationSettings.positionOvershoot : 1f);
            
            if (mb != null && mb.transform != null && mb.gameObject != null)
            {
                var startPos = mb.transform.position;
                Debug.Log($"[GridPlacementSystem] DOMove Revert: {mb.name} from {startPos} to {_dragStartWorldPos} duration {duration} ease {ease} overshoot {overshoot}");
                mb.transform.DOMove(_dragStartWorldPos, duration)
                    .SetEase(ease, overshoot)
                    .SetTarget(mb.transform)
                    .OnKill(() => { });
            }
        }

        public bool TrySwapWithOccupant(IPlaceable dragged, IPlaceable occupant, Vector2Int occupantPos, Vector2Int draggedStartPos)
        {
            if (dragged == null || occupant == null) return false;
            
            var draggedMb = dragged as MonoBehaviour;
            var occupantMb = occupant as MonoBehaviour;
            
            if (draggedMb == null || occupantMb == null) return false;
            if (draggedMb.gameObject == null || occupantMb.gameObject == null) return false;
            
            Vector2Int actualOccupantPos = occupant.GridPosition;
            Vector2Int actualDraggedStartPos = _dragStartWasPlaced ? _dragStartGridPos : actualOccupantPos;
            
            // Calculate center positions for multi-tile objects
            Vector3 draggedWorldStart;
            if (draggedMb.transform != null)
            {
                draggedWorldStart = draggedMb.transform.position;
            }
            else
            {
                var draggedOccupiedPositions = new List<Vector2Int>();
                for (int x = 0; x < dragged.GridSize.x; x++)
                {
                    for (int y = 0; y < dragged.GridSize.y; y++)
                    {
                        draggedOccupiedPositions.Add(new Vector2Int(actualDraggedStartPos.x + x, actualDraggedStartPos.y + y));
                    }
                }
                draggedWorldStart = MultiTileGridToWorld(draggedOccupiedPositions);
            }
            
            Vector3 occupantWorldStart;
            if (occupantMb.transform != null)
            {
                occupantWorldStart = occupantMb.transform.position;
            }
            else
            {
                var occupantOccupiedPositions = new List<Vector2Int>();
                for (int x = 0; x < occupant.GridSize.x; x++)
                {
                    for (int y = 0; y < occupant.GridSize.y; y++)
                    {
                        occupantOccupiedPositions.Add(new Vector2Int(actualOccupantPos.x + x, actualOccupantPos.y + y));
                    }
                }
                occupantWorldStart = MultiTileGridToWorld(occupantOccupiedPositions);
            }

            if (draggedMb.transform != null)
            {
                draggedMb.transform.DOKill(true);
            }
            if (occupantMb.transform != null)
            {
                occupantMb.transform.DOKill(true);
            }

            var draggedPositionsToRemove = new List<Vector2Int>();
            var occupantPositionsToRemove = new List<Vector2Int>();
            
            foreach (var kvp in _placedObjects)
            {
                if (kvp.Value == dragged)
                {
                    draggedPositionsToRemove.Add(kvp.Key);
                }
                if (kvp.Value == occupant)
                {
                    occupantPositionsToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var pos in draggedPositionsToRemove)
            {
                _placedObjects.Remove(pos);
                _occupiedTiles.Remove(pos);
                if (pos.x >= 0 && pos.y >= 0 && pos.x < _gridDimensions.x && pos.y < _gridDimensions.y)
                {
                    _availableTiles.Add(pos);
                }
            }
            
            foreach (var pos in occupantPositionsToRemove)
            {
                _placedObjects.Remove(pos);
                _occupiedTiles.Remove(pos);
                if (pos.x >= 0 && pos.y >= 0 && pos.x < _gridDimensions.x && pos.y < _gridDimensions.y)
                {
                    _availableTiles.Add(pos);
                }
            }
            
            if (draggedMb.gameObject == null || occupantMb.gameObject == null) return false;

            bool canDraggedToOccupant = IsValidPlacement(actualOccupantPos, dragged.GridSize, dragged);

            bool canOccupantToDragged = _dragStartWasPlaced ? IsValidPlacement(actualDraggedStartPos, occupant.GridSize, occupant) : true;

            if (!canDraggedToOccupant || !canOccupantToDragged)
            {
                if (draggedMb != null && draggedMb.gameObject != null && _dragStartWasPlaced)
                {
                    PlaceObject(dragged, actualDraggedStartPos);
                }
                if (occupantMb != null && occupantMb.gameObject != null)
                {
                    PlaceObject(occupant, actualOccupantPos);
                }
                return false;
            }

            var draggedTargetPositions = new List<Vector2Int>();
            var occupantTargetPositions = _dragStartWasPlaced ? new List<Vector2Int>() : null;
            
            for (int x = 0; x < dragged.GridSize.x; x++)
            {
                for (int y = 0; y < dragged.GridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(actualOccupantPos.x + x, actualOccupantPos.y + y);
                    draggedTargetPositions.Add(pos);
                    _placedObjects[pos] = dragged;
                    _occupiedTiles.Add(pos);
                    _availableTiles.Remove(pos);
                }
            }
            dragged.GridPosition = actualOccupantPos;
            dragged.IsPlaced = true;
            
            if (_dragStartWasPlaced)
            {
                for (int x = 0; x < occupant.GridSize.x; x++)
                {
                    for (int y = 0; y < occupant.GridSize.y; y++)
                    {
                        Vector2Int pos = new Vector2Int(actualDraggedStartPos.x + x, actualDraggedStartPos.y + y);
                        occupantTargetPositions?.Add(pos);
                        _placedObjects[pos] = occupant;
                        _occupiedTiles.Add(pos);
                        _availableTiles.Remove(pos);
                    }
                }
                occupant.GridPosition = actualDraggedStartPos;
                occupant.IsPlaced = true;
            }
            else
            {

                occupant.IsPlaced = false;
            }
            
            Vector3 draggedTargetWorld = MultiTileGridToWorld(draggedTargetPositions);
            Vector3 occupantTargetWorld = _dragStartWasPlaced && occupantTargetPositions != null
                ? MultiTileGridToWorld(occupantTargetPositions)
                : occupantWorldStart;
            
            // Apply pivot offsets
            Vector3 draggedPivotOffset = CalculatePivotOffset(draggedMb);
            Vector3 occupantPivotOffset = CalculatePivotOffset(occupantMb);
            
            draggedTargetWorld += draggedPivotOffset;
            occupantTargetWorld += occupantPivotOffset;

            if (draggedMb != null)
            {
                draggedMb.transform.position = draggedTargetWorld;
            }
            
            if (_dragStartWasPlaced && occupantMb != null)
            {
                occupantMb.transform.position = occupantTargetWorld;
            }

            RebuildAvailabilityCache();

            if (draggedMb != null && draggedMb.transform != null)
            {
                draggedMb.transform.position = draggedWorldStart;
            }
            if (occupantMb != null && occupantMb.transform != null && _dragStartWasPlaced)
            {

                occupantMb.transform.position = occupantWorldStart;
            }

            if (draggedMb != null && draggedMb.gameObject != null && draggedMb.transform != null)
            {
                TryPlaySwapAnimation(dragged, draggedTargetWorld);
            }
            if (occupantMb != null && occupantMb.gameObject != null && occupantMb.transform != null && _dragStartWasPlaced)
            {

                TryPlaySwapAnimation(occupant, occupantTargetWorld);
            }

            if (draggedMb != null && draggedMb.transform != null)
            {
                draggedMb.transform.SetParent(null);
            }
            if (occupantMb != null && occupantMb.transform != null)
            {
                occupantMb.transform.SetParent(null);
            }

            if (_currentDraggedObject != null)
            {
                _currentDraggedObject.OnDrop(actualOccupantPos, true);
                _currentDraggedObject.IsDragging = false;
            }
            _currentDraggedObject = null;
            _lastEvaluatedGridPosition = new Vector2Int(int.MinValue, int.MinValue);
            ClearTileHighlight();
            return true;
        }

        public void FinishDragWithoutPlacement(Vector2Int finalGridPos)
        {
            _currentDraggedObject.OnDrop(finalGridPos, false);
            _currentDraggedObject.IsDragging = false;
            _currentDraggedObject = null;
            _lastEvaluatedGridPosition = new Vector2Int(int.MinValue, int.MinValue);
            ClearTileHighlight();
        }

        public void TryPlayPlacementAnimation(IPlaceable placeable, Vector2Int gridPos, bool wasAutoSnappedFromInvalid)
        {
            if (placeable == null || _placementAnimationSettings == null) return;
            var mb = placeable as MonoBehaviour;
            if (mb == null || mb.transform == null) return; 

            var t = mb.transform;
            
            // Calculate center position for multi-tile objects
            var occupiedPositions = new List<Vector2Int>();
            for (int x = 0; x < placeable.GridSize.x; x++)
            {
                for (int y = 0; y < placeable.GridSize.y; y++)
                {
                    occupiedPositions.Add(new Vector2Int(gridPos.x + x, gridPos.y + y));
                }
            }
            var target = MultiTileGridToWorld(occupiedPositions);
            
            // Apply pivot offset
            Vector3 pivotOffset = CalculatePivotOffset(mb);
            target += pivotOffset;
            
            if (t != null)
            {
                t.DOKill(true);
            }
            else
            {
                return;
            }

            if (_placementAnimationSettings.enablePositionTween)
            {
                float duration = wasAutoSnappedFromInvalid && _placementAnimationSettings.useSeparateInvalidSnap
                    ? _placementAnimationSettings.invalidSnapDuration
                    : _placementAnimationSettings.positionDuration;
                Ease ease = wasAutoSnappedFromInvalid && _placementAnimationSettings.useSeparateInvalidSnap
                    ? _placementAnimationSettings.invalidSnapEase
                    : _placementAnimationSettings.positionEase;
                float overshoot = wasAutoSnappedFromInvalid && _placementAnimationSettings.useSeparateInvalidSnap
                    ? _placementAnimationSettings.invalidSnapOvershoot
                    : _placementAnimationSettings.positionOvershoot;

                if (t != null && t.gameObject != null)
                {
                    var startPos = t.position;
                    Debug.Log($"[GridPlacementSystem] DOMove Placement: {t.name} from {startPos} to {target} duration {duration} ease {ease} overshoot {overshoot} autoSnap={wasAutoSnappedFromInvalid}");
                    // Start from object's last position (current transform)
                    t.DOMove(target, duration)
                        .SetEase(ease, overshoot)
                        .SetTarget(t)
                        .OnKill(() => { });
                }
            }

            if (_placementAnimationSettings.enableScalePunch && t != null && t.gameObject != null)
            {
                Vector3 original = t.localScale; // use current scale to avoid unintended changes

                t.DOPunchScale(_placementAnimationSettings.punchScale, _placementAnimationSettings.punchDuration, _placementAnimationSettings.punchVibrato, _placementAnimationSettings.punchElasticity)
                    .SetTarget(t)
                    .OnComplete(() => {
                        if (t != null && t.gameObject != null)
                        {
                            t.localScale = original;
                        }
                    })
                    .OnKill(() => {
                        if (t != null && t.gameObject != null)
                        {
                            t.localScale = original;
                        }
                    });
            }
        }

        private void TryPlaySwapAnimation(IPlaceable placeable, Vector3 targetWorldPos)
        {
            if (placeable == null || _placementAnimationSettings == null) return;
            if (!_placementAnimationSettings.enableSwapAnimation) return;
            
            var mb = placeable as MonoBehaviour;
            if (mb == null || mb.transform == null) return;

            var t = mb.transform;

            if (t != null)
            {
                t.DOKill(true);
            }
            else
            {
                return;
            }

            if (t != null && t.gameObject != null)
            {
                var startPos = t.position;
                Debug.Log($"[GridPlacementSystem] DOMove Swap: {t.name} from {startPos} to {targetWorldPos} duration {_placementAnimationSettings.swapDuration} ease {_placementAnimationSettings.swapEase} overshoot {_placementAnimationSettings.swapOvershoot}");
                t.DOMove(targetWorldPos, _placementAnimationSettings.swapDuration)
                    .SetEase(_placementAnimationSettings.swapEase, _placementAnimationSettings.swapOvershoot)
                    .SetTarget(t)
                    .OnKill(() => { });
            }

            if (_placementAnimationSettings.enableSwapScalePunch && t != null && t.gameObject != null)
            {
                Vector3 original = t.localScale;
                
                var originalScale = placeable.GetOriginalScale();
                if (originalScale.HasValue)
                {
                    original = originalScale.Value;
                    t.localScale = original;
                }
                
                t.DOPunchScale(_placementAnimationSettings.swapPunchScale, _placementAnimationSettings.swapPunchDuration, 
                    _placementAnimationSettings.swapPunchVibrato, _placementAnimationSettings.swapPunchElasticity)
                    .SetTarget(t)
                    .OnComplete(() => {
                        if (t != null && t.gameObject != null)
                        {
                            t.localScale = original;
                        }
                    })
                    .OnKill(() => {
                        if (t != null && t.gameObject != null)
                        {
                            t.localScale = original;
                        }
                    });
            }
        }
        
        public void ClearAll()
        {
            var uniqueObjectsToDestroy = new HashSet<MonoBehaviour>();
            
            foreach (var obj in _occupiedTilesWithObjects.Values)
            {
                var mb = obj as MonoBehaviour;
                if (mb != null)
                {
                    uniqueObjectsToDestroy.Add(mb);
                }
            }
            
            var positionsToRemove = new List<Vector2Int>(_occupiedTilesWithObjects.Keys);
            foreach (var pos in positionsToRemove)
            {
                var obj = _occupiedTilesWithObjects[pos];
                if (obj != null)
                {
                    obj.IsPlaced = false;
                    obj.OnRemoved();
                }
                _occupiedTilesWithObjects.Remove(pos);
                
                if (_placedObjects.ContainsKey(pos))
                {
                    _placedObjects.Remove(pos);
                }
            }
            
            _occupiedTiles.Clear();
            _availableTiles.Clear();
            RebuildAvailabilityCache();
            
            foreach (var mb in uniqueObjectsToDestroy)
            {
                if (mb != null && mb.transform != null)
                {
                    mb.transform.DOKill(true);
                    mb.transform.SetParent(null);
                    if (mb.gameObject != null)
                    {
                        Object.DestroyImmediate(mb.gameObject);
                    }
                }
            }
            
            _currentDraggedObject = null;
            UpdateDebugInspectorLists();
        }
        
        
        private void UpdatePreviewVisuals(bool isValid)
        {

        }
        
        public Vector2Int FindNearestValidPosition(Vector2Int targetPosition, Vector2Int objectSize, IPlaceable excludeObject)
        {

            int bottomHalfThreshold = _gridDimensions.y / 2;
            
            int maxX = Mathf.Max(0, _gridDimensions.x - objectSize.x);
            int maxY = Mathf.Max(0, bottomHalfThreshold - objectSize.y);
            if (maxX < 0 || maxY < 0)
            {
                return targetPosition;
            }

            Vector2Int start = new Vector2Int(
                Mathf.Clamp(targetPosition.x, 0, maxX),
                Mathf.Clamp(targetPosition.y, 0, maxY)
            );

            if (IsValidPlacement(start, objectSize, excludeObject))
            {
                return start;
            }

            int maxRadius = Mathf.Max(_gridDimensions.x, _gridDimensions.y);
            for (int r = 1; r <= maxRadius; r++)
            {

                for (int dx = -r; dx <= r; dx++)
                {
                    int xTop = start.x + dx;
                    int yTop = start.y + r;
                    int xBot = start.x + dx;
                    int yBot = start.y - r;

                    if (xTop >= 0 && xTop <= maxX && yTop >= 0 && yTop <= maxY)
                    {
                        var pos = new Vector2Int(xTop, yTop);
                        if (IsValidPlacement(pos, objectSize, excludeObject)) return pos;
                    }
                    if (xBot >= 0 && xBot <= maxX && yBot >= 0 && yBot <= maxY)
                    {
                        var pos = new Vector2Int(xBot, yBot);
                        if (IsValidPlacement(pos, objectSize, excludeObject)) return pos;
                    }
                }

                for (int dy = -r + 1; dy <= r - 1; dy++)
                {
                    int xRight = start.x + r;
                    int yRight = start.y + dy;
                    int xLeft = start.x - r;
                    int yLeft = start.y + dy;

                    if (xRight >= 0 && xRight <= maxX && yRight >= 0 && yRight <= maxY)
                    {
                        var pos = new Vector2Int(xRight, yRight);
                        if (IsValidPlacement(pos, objectSize, excludeObject)) return pos;
                    }
                    if (xLeft >= 0 && xLeft <= maxX && yLeft >= 0 && yLeft <= maxY)
                    {
                        var pos = new Vector2Int(xLeft, yLeft);
                        if (IsValidPlacement(pos, objectSize, excludeObject)) return pos;
                    }
                }
            }

            return start;
        }
        
        private void HighlightTileAt(Vector2Int gridPos, bool isValid)
        {

            ClearTileHighlight();
            
            if (_currentDraggedObject == null) return;
            
            Color highlightColor = GetHighlightColor(isValid);
            
            for (int x = 0; x < _currentDraggedObject.GridSize.x; x++)
            {
                for (int y = 0; y < _currentDraggedObject.GridSize.y; y++)
                {
                    Vector2Int checkPos = new Vector2Int(gridPos.x + x, gridPos.y + y);
                    var tile = FindTileAtPosition(checkPos);
                    if (tile != null)
                    {
                        var gridManager = ServiceLocator.Instance?.Get<GridManager>();
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
        
        private Color GetHighlightColor(bool isValid)
        {
            var gridManager = ServiceLocator.Instance.Get<GridManager>();
            if (gridManager != null && gridManager.GridSettings != null)
            {
                return isValid ? gridManager.GridSettings.ValidHighlightColor : gridManager.GridSettings.InvalidHighlightColor;
            }
            
            return isValid ? Color.white : Color.red;
        }
        
        private void ClearTileHighlight()
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
        
        public BaseTile FindTileAtPosition(Vector2Int gridPos)
        {
            var gridManager = ServiceLocator.Instance.Get<GridManager>();
            if (gridManager == null)
            {
                return null;
            }

            var tile = gridManager.GetTileAtPosition(new Vector2(gridPos.x, gridPos.y));
            if (tile != null)
            {
                return tile;
            }

            var allTiles = gridManager.GetAllTiles();
            if (allTiles == null || allTiles.Count == 0)
            {
                return null;
            }

            BaseTile nearestTile = null;
            float nearestDistance = float.MaxValue;
            
            foreach (var kvp in allTiles)
            {
                var key = kvp.Key;
                var tileObj = kvp.Value;
                
                if (tileObj == null) continue;
                
                int tileX = Mathf.RoundToInt(key.x);
                int tileY = Mathf.RoundToInt(key.y);
                
                if (tileX == gridPos.x && tileY == gridPos.y)
                {
                    return tileObj;
                }
                
                float distance = Vector2.Distance(new Vector2(tileX, tileY), new Vector2(gridPos.x, gridPos.y));
                if (distance < nearestDistance && distance < 1.5f)
                {
                    nearestDistance = distance;
                    nearestTile = tileObj;
                }
            }

            return nearestTile;
        }
        
        public void AutoPlaceAllItemsFromInventory()
        {
            if (!_gridReady)
            {
                Debug.LogWarning("GridPlacementSystem: Cannot auto-place items, grid is not ready yet.");
                return;
            }
            
            var levelDataProvider = ServiceLocator.Instance?.TryGet<ILevelDataProvider>();
            var inventoryManager = ServiceLocator.Instance?.TryGet<IInventoryManager>();
            var gridItemFactory = ServiceLocator.Instance?.TryGet<IGridItemFactory>();
            
            if (levelDataProvider == null || inventoryManager == null || gridItemFactory == null)
            {
                Debug.LogWarning("GridPlacementSystem: Auto-place failed - missing dependencies.");
                return;
            }
            
            if (levelDataProvider.CurrentLevel == null) return;
            
            var currentLevel = levelDataProvider.CurrentLevel;
            if (currentLevel.DefenceItems == null || currentLevel.DefenceItems.Count == 0) return;
            
            RebuildAvailabilityCache();
            
            List<Vector2Int> availablePositions = GetShuffledAvailablePositions();
            
            foreach (var entry in currentLevel.DefenceItems)
            {
                var itemData = entry.DefenceItemData;
                int quantity = inventoryManager.GetAvailableQuantity(itemData);
                
                if (itemData == null || quantity <= 0) continue;
                
                for (int i = 0; i < quantity; i++)
                {
                    Vector2Int? validPosition = FindRandomValidPosition(itemData.GridSize, availablePositions);
                    if (!validPosition.HasValue) break;
                    
                    // Calculate center position for multi-tile objects
                    var occupiedPositions = new List<Vector2Int>();
                    for (int x = 0; x < itemData.GridSize.x; x++)
                    {
                        for (int y = 0; y < itemData.GridSize.y; y++)
                        {
                            occupiedPositions.Add(new Vector2Int(validPosition.Value.x + x, validPosition.Value.y + y));
                        }
                    }
                    Vector3 worldPosition = MultiTileGridToWorld(occupiedPositions);
                    var placeable = gridItemFactory.CreateGridItem2D(itemData, worldPosition, false);
                    
                    if (placeable != null)
                    {
                        bool placed = PlaceObjectInternal(placeable, validPosition.Value);
                        if (placed)
                        {
                            inventoryManager.ConsumeItem(itemData);
                            MarkPositionAsUsed(validPosition.Value, itemData.GridSize, availablePositions);
                        }
                        else
                        {
                            var mb = placeable as MonoBehaviour;
                            if (mb != null) Destroy(mb.gameObject);
                        }
                    }
                }
            }
            
            RebuildAvailabilityCache();
            UpdateDebugInspectorLists();
        }
        
        private List<Vector2Int> GetShuffledAvailablePositions()
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            
            int bottomHalfThreshold = _gridDimensions.y / 2;
            
            for (int x = 0; x < _gridDimensions.x; x++)
            {
                for (int y = 0; y < bottomHalfThreshold; y++)
                {
                    positions.Add(new Vector2Int(x, y));
                }
            }
            
            for (int i = 0; i < positions.Count; i++)
            {
                Vector2Int temp = positions[i];
                int randomIndex = Random.Range(i, positions.Count);
                positions[i] = positions[randomIndex];
                positions[randomIndex] = temp;
            }
            
            return positions;
        }
        
        private Vector2Int? FindRandomValidPosition(Vector2Int itemSize, List<Vector2Int> availablePositions)
        {
            foreach (var position in availablePositions)
            {
                if (IsValidPlacement(position, itemSize))
                {
                    return position;
                }
            }
            
            return null;
        }
        
        private void MarkPositionAsUsed(Vector2Int position, Vector2Int itemSize, List<Vector2Int> availablePositions)
        {
            for (int x = 0; x < itemSize.x; x++)
            {
                for (int y = 0; y < itemSize.y; y++)
                {
                    Vector2Int usedPos = new Vector2Int(position.x + x, position.y + y);
                    availablePositions.Remove(usedPos);
                }
            }
        }

        private IEnumerable<Vector2Int> EnumeratePositions(Vector2Int origin, Vector2Int size)
        {
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    yield return new Vector2Int(origin.x + x, origin.y + y);
                }
            }
        }

        public Vector3 MultiTileGridToWorld(IEnumerable<Vector2Int> gridPositions)
        {
            if (gridPositions == null)
                return Vector3.zero;

            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (var pos in gridPositions)
            {
                sum += GridToWorld(pos);
                count++;
            }

            return count > 0 ? sum / count : Vector3.zero;
        }
        
        private Vector3 CalculatePivotOffset(MonoBehaviour mb)
        {
            if (mb == null) return Vector3.zero;
            
            var spriteRenderer = mb.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null || spriteRenderer.sprite == null) return Vector3.zero;
            
            var sprite = spriteRenderer.sprite;
            var bounds = sprite.bounds;
            
            // Calculate the offset from sprite center to pivot
            // Pivot is in normalized coordinates (0-1), convert to local space
            Vector2 pivotNormalized = sprite.pivot / sprite.pixelsPerUnit;
            Vector2 spriteSize = sprite.rect.size / sprite.pixelsPerUnit;
            
            // Calculate offset: center is at (0.5, 0.5) in normalized space
            Vector2 centerNormalized = spriteSize * 0.5f;
            Vector2 offset = pivotNormalized - centerNormalized;
            
            // Apply transform scale to offset
            Vector3 scaledOffset = new Vector3(
                offset.x * mb.transform.localScale.x,
                offset.y * mb.transform.localScale.y,
                0
            );
            
            return scaledOffset;
        }
    }
}
