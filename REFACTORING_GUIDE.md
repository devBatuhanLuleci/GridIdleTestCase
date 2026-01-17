# Grid System Refactoring Guide

## Completed Extractions ✓

### 1. **GridCoordinateConverter** (Services/GridCoordinateConverter.cs)
- ✓ `WorldToGrid(Vector3 worldPosition)` - Converts world coords to grid coords
- ✓ `GridToWorld(Vector2Int gridPosition)` - Converts grid coords to world coords
- ✓ `MultiTileGridToWorld(List<Vector2Int> gridPositions)` - Averages multi-tile positions
- ✓ Used by: DragHandler, AnimationPlayer, GridPlacementSystem

### 2. **PlacementValidator** (Managers/PlacementValidator.cs)
- ✓ `IsValidPlacement(Vector2Int gridPosition, Vector2Int objectSize, IPlaceable excludeObject)` - Core placement validation
- ✓ `IsTileOccupied(Vector2Int pos)` - Checks single tile occupancy
- ✓ `GetOccupant(Vector2Int pos)` - Gets object at position
- ✓ `GetOccupiedPositions()` - Lists all occupied positions
- ✓ `TryFindAlternatePosition()` - Spiral search for placement
- ✓ Used by: DragHandler, GridPlacementSystem

### 3. **DragHandler** (Managers/DragHandler.cs)
- ✓ `StartDragging(IPlaceable placeable)` - Initializes drag state
- ✓ `UpdateDrag(Vector3 worldPosition)` - Continuous drag updates with grid evaluation
- ✓ `EndDragging(Vector3 worldPosition)` - Completes drag, places object
- ✓ `CancelDrag()` - Reverts drag operation
- ✓ `Reset()` - Clears drag state
- ✓ Uses: PlacementValidator, GridCoordinateConverter, HighlightManager
- ✓ Dependencies: Requires GridPlacementSystem public methods

### 4. **HighlightManager** (Managers/HighlightManager.cs)
- ✓ `HighlightTileAt(Vector2Int gridPos, Vector2Int objectSize, GridPlacementSystem gridSystem, bool isValid)`
- ✓ `ClearTileHighlight()` - Removes all highlights
- ✓ Uses grid settings for colors and animation duration
- ✓ Uses: GridPlacementSystem.FindTileAtPosition()

### 5. **AnimationPlayer** (Managers/AnimationPlayer.cs)
- ✓ `PlayPlacementAnimation(IPlaceable placeable, Vector2Int gridPos, bool wasAutoSnappedFromInvalid)`
- ✓ `PlaySwapAnimation(IPlaceable placeable, Vector3 targetWorldPos)`
- ✓ `CalculatePivotOffset(MonoBehaviour mb)` - Sprite pivot calculation
- ✓ Uses: GridCoordinateConverter, PlacementAnimationSettings

---

## Remaining Refactoring Tasks

### Phase 2: GridPlacementSystem Simplification

**Goal**: Replace extracted methods with delegated calls, keeping core orchestration logic

#### Methods to Replace in GridPlacementSystem:

```csharp
// Create private instances (late-init in Awake/OnGridReady)
private PlacementValidator _placementValidator;
private DragHandler _dragHandler;
private HighlightManager _highlightManager;
private AnimationPlayer _animationPlayer;

// In OnGridReady():
_placementValidator = new PlacementValidator(_placedObjects, _occupiedTiles, _gridDimensions, _gridManager);
_dragHandler = new DragHandler(this, _placementValidator, _coordinateConverter, _highlightManager, _gridManager);
_highlightManager = new HighlightManager(_gridManager);
_animationPlayer = new AnimationPlayer(_placementAnimationSettings, _coordinateConverter);
```

**Methods to Remove from GridPlacementSystem:**
- `IsValidPlacement()` → delegate to `_placementValidator.IsValidPlacement()`
- `IsTileOccupied()` → delegate to `_placementValidator.IsTileOccupied()`
- `GetOccupant()` → delegate to `_placementValidator.GetOccupant()`
- `StartDragging()` → delegate to `_dragHandler.StartDragging()`
- `UpdateDrag()` → delegate to `_dragHandler.UpdateDrag()`
- `EndDragging()` → delegate to `_dragHandler.EndDragging()`
- `HighlightTileAt()` → delegate to `_highlightManager.HighlightTileAt()`
- `ClearTileHighlight()` → delegate to `_highlightManager.ClearTileHighlight()`
- `TryPlayPlacementAnimation()` → delegate to `_animationPlayer.PlayPlacementAnimation()`
- `TryPlaySwapAnimation()` → delegate to `_animationPlayer.PlaySwapAnimation()`

**Keep in GridPlacementSystem (Core Orchestration):**
- `PlaceObject()` - Places object and updates occupancy
- `RemoveObject()` - Removes object from grid
- `TrySwapWithOccupant()` - Swap logic
- `RevertDraggedToStartPosition()` - Revert logic
- `FinishDragWithoutPlacement()` - Failed placement handling
- `FindNearestValidPosition()` - Already using PlacementValidator
- `FindTileAtPosition()` - Public helper
- `GetObjectAt()` - Public lookup
- `RebuildAvailabilityCache()` - Grid state management

---

### Phase 3: GridItem2D Refactoring (724 lines)

**Goal**: Extract input handling, drag control, and visual updates

#### Extract: DragInputHandler
**Purpose**: Detect and process input (mouse/touch)

```csharp
// Methods to extract:
- OnBeginDrag(PointerEventData)
- OnDrag(PointerEventData)
- OnEndDrag(PointerEventData)
- UpdateDragPosition() [input detection only]
- GetMouseWorldPosition()

// Dependencies:
- EventSystem
- InputSystem
- New InputSystem (Mouse/Touch)
```

#### Extract: DragController
**Purpose**: Orchestrate drag flow with animations

```csharp
// Methods to extract:
- StartManualDrag(Vector3) [with DoTween smooth initial movement]
- ManualDragToNewPosition(Vector3, float duration, Ease ease)
- ApplyDrag(Vector3 worldPosition)

// Dependencies:
- DoTween
- IGridPlacementSystem
- GridItem2D (back-reference for updates)
```

#### Extract: VisualUpdater
**Purpose**: Handle sprite and visual state changes

```csharp
// Methods to extract:
- SetColor(Color)
- ApplySprite(Sprite)
- EnsureCollider2D()
- UpdateVisualState(State)

// Keep in GridItem2D:
- State machine logic
- Drag detection
- Integration with IGridPlacementSystem
```

**Expected Size After Refactoring:**
- GridItem2D: ~300-350 lines (main orchestrator)
- DragInputHandler: ~100 lines
- DragController: ~150 lines
- VisualUpdater: ~100 lines

---

### Phase 4: DefenceItemPanel Refactoring (500+ lines)

**Goal**: Separate Canvas and Sprite UI rendering modes

#### Extract: CanvasItemsManager
**Purpose**: Handle Canvas mode item display

```csharp
// Methods to extract:
- RefreshCanvasItems()
- CreateItemUI(DefenceItemData)
- DestroyCanvasItemUI(DefenceItemData)
- ClearCanvasItems()

// Keep: _itemHandlers (Dictionary<DefenceItemData, UIGridItemDragHandler>)
// Keep: _displayCanvas, _itemUIContent references
```

#### Extract: SpriteItemsManager
**Purpose**: Handle Sprite mode item display

```csharp
// Methods to extract:
- RefreshSpriteItems()
- SyncSpriteItems(DefenceItemData, int, int)
- CreateSpriteItem(DefenceItemData, int index)
- DestroySpriteItems(DefenceItemData)
- ClearSpriteItems()

// Keep: _spriteItemHandlers (Dictionary<DefenceItemData, List<SpriteGridItemDragHandler>>)
// Keep: _spawnParent, _spriteItemPoolPosition references
```

#### Extract: ItemUIFactory
**Purpose**: Factory pattern for item UI creation

```csharp
// Methods to extract (static or singleton):
- CreateCanvasItemUI(DefenceItemData)
- CreateSpriteItem(DefenceItemData)
- ConfigureUIElement(Image/TextMeshProUGUI/GridItem2D)

// Dependencies:
- Prefab references
- Theme/Style settings
```

**Expected Size After Refactoring:**
- DefenceItemPanel: ~250-300 lines (dispatcher/orchestrator)
- CanvasItemsManager: ~200 lines
- SpriteItemsManager: ~200 lines
- ItemUIFactory: ~150 lines

---

## Refactoring Steps (In Order)

### Step 1: Simplify GridPlacementSystem
- [ ] Create ComponentUpdater instance variables
- [ ] Wrap public method calls with delegation
- [ ] Test all grid operations still work
- [ ] Commit: "refactor: GridPlacementSystem now uses extracted components"

### Step 2: Extract GridItem2D Components
- [ ] Create DragInputHandler class
- [ ] Create DragController class  
- [ ] Create VisualUpdater class
- [ ] Update GridItem2D to use components
- [ ] Test dragging, visual updates, placement
- [ ] Commit: "refactor: decompose GridItem2D into InputHandler, DragController, VisualUpdater"

### Step 3: Extract DefenceItemPanel Components
- [ ] Create CanvasItemsManager class
- [ ] Create SpriteItemsManager class
- [ ] Create ItemUIFactory class
- [ ] Update DefenceItemPanel to use managers
- [ ] Test Canvas mode, Sprite mode, mode switching
- [ ] Commit: "refactor: decompose DefenceItemPanel into CanvasItemsManager, SpriteItemsManager, ItemUIFactory"

### Step 4: Final Testing & Documentation
- [ ] Run all test scenes
- [ ] Test multi-tile placement, dragging, swapping
- [ ] Test UI rendering in both modes
- [ ] Verify all animations work
- [ ] Commit: "refactor: complete modular grid system architecture"

---

## Key Integration Points

### DragHandler ↔ GridPlacementSystem
- DragHandler calls `gridPlacementSystem.RemoveObject()`, `PlaceObject()`, `GetObjectAt()`
- DragHandler calls `gridPlacementSystem.TrySwapWithOccupant()`
- DragHandler calls `gridPlacementSystem.FindNearestValidPosition()`
- DragHandler calls `gridPlacementSystem.RevertDraggedToStartPosition()`
- DragHandler calls `gridPlacementSystem.FinishDragWithoutPlacement()`

### HighlightManager ↔ GridPlacementSystem
- HighlightManager calls `gridPlacementSystem.FindTileAtPosition()`
- GridPlacementSystem calls `_highlightManager.HighlightTileAt()` from DragHandler

### AnimationPlayer ↔ GridCoordinateConverter
- AnimationPlayer calls `_coordinateConverter.MultiTileGridToWorld()`

### DragController ↔ GridItem2D
- Requires back-reference to parent GridItem2D for color/sprite updates

---

## Testing Checklist

After Each Phase:
- [ ] No compilation errors
- [ ] Grid item placement works (single tile)
- [ ] Multi-tile (2x2, 3x2, etc.) placement works
- [ ] Dragging works smoothly with DoTween
- [ ] Grid highlighting shows correct positions
- [ ] Highlight changes at correct threshold
- [ ] Swapping works
- [ ] Auto-snap works
- [ ] Animations play correctly
- [ ] Placement animations respect settings
- [ ] UI refreshes correctly
- [ ] Canvas and Sprite modes both work
- [ ] Mode switching works

---

## Architecture Diagram (Post-Refactoring)

```
GridPlacementSystem (Main Orchestrator)
├── PlacementValidator (validates placements)
├── DragHandler (manages drag flow)
│   ├── PlacementValidator
│   ├── GridCoordinateConverter
│   └── HighlightManager
├── HighlightManager (renders highlights)
├── AnimationPlayer (plays animations)
│   └── GridCoordinateConverter
└── [Placement logic, occupancy tracking, grid state]

GridItem2D (Item UI)
├── DragInputHandler (input detection)
├── DragController (drag orchestration)
│   └── IGridPlacementSystem
├── VisualUpdater (sprite/color updates)
└── [State machine, event handlers]

DefenceItemPanel (Inventory UI)
├── CanvasItemsManager (Canvas UI)
├── SpriteItemsManager (Sprite UI)
├── ItemUIFactory (UI creation)
└── [Mode dispatcher, refresh logic]
```

---

## Expected Benefits

1. **Single Responsibility**: Each class has one clear purpose
2. **Testability**: Components can be tested in isolation
3. **Maintainability**: Easy to find and modify specific logic
4. **Reusability**: Components can be used in other systems
5. **Readability**: Code is more self-documenting
6. **Flexibility**: Easy to swap implementations (e.g., different highlight systems)

---

## Commit History (Expected)

1. ✓ "refactor: extract GridCoordinateConverter, PlacementValidator, DragHandler, HighlightManager"
2. ⏳ "refactor: simplify GridPlacementSystem with delegated components"
3. ⏳ "refactor: decompose GridItem2D into focused components"
4. ⏳ "refactor: decompose DefenceItemPanel into manager components"
5. ⏳ "test: verify all refactored systems work correctly"

---

## Notes

- All existing public interfaces remain unchanged (IGridPlacementSystem, IPlaceable)
- ServiceLocator is used for component discovery where needed
- GridCoordinateConverter is the hub for all position conversions
- DragHandler is the central drag orchestrator
- PlacementValidator encapsulates all placement rules
- HighlightManager handles all visual feedback during dragging
