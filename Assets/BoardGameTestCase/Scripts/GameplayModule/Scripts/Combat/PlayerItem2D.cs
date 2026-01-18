using System.Collections.Generic;
using UnityEngine;
using BoardGameTestCase.Core.Common;
using GridSystemModule.Core.Interfaces;
using UISystemModule.UIElements;
using GameModule.Core.Interfaces;
using GameplayModule;

namespace GameplayModule
{
    /// <summary>
    /// Player controller that manages the throwing of grid item copies at targets.
    /// It listens to reload completion events from placed grid items.
    /// </summary>
    public class PlayerItem2D : MonoBehaviour
    {
        [Header("Projectile Settings")]
        [SerializeField] private BezierProjectile _projectilePrefab;
        [SerializeField] private Transform _throwPoint;
        [SerializeField] private float vfxScale = 1.0f;
        [SerializeField] private float _projectileScaleMultiplier = 0.5f;
        [SerializeField] private float _throwDuration = 0.6f;
        [SerializeField] private float _throwHeight = 3f;

        private IGridPlacementSystem _placementSystem;
        private IEnemySpawner _enemySpawner;
        
        // Use a dictionary to store actions so we can properly unsubscribe
        private Dictionary<GridItem2D, System.Action> _reloadCallbacks = new Dictionary<GridItem2D, System.Action>();
        private HashSet<GridItem2D> _trackedItems = new HashSet<GridItem2D>();

        private void Awake()
        {
            ServiceLocator.Instance.Register<PlayerItem2D>(this);
        }

        private void Start()
        {
            if (_throwPoint == null)
            {
                _throwPoint = transform.Find("ThrowPoint");
            }

            _placementSystem = ServiceLocator.Instance.Get<IGridPlacementSystem>();
            _enemySpawner = ServiceLocator.Instance.Get<IEnemySpawner>();

            if (_placementSystem != null)
            {
                _placementSystem.OnItemPlaced += HandleItemPlaced;
            }
            
            // Track globally placed items that might have been loaded/placed before this Start()
            RefreshTrackedItems();
        }

        private void OnDestroy()
        {
            if (_placementSystem != null)
            {
                _placementSystem.OnItemPlaced -= HandleItemPlaced;
            }

            foreach (var kvp in _reloadCallbacks)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.OnReloadComplete -= kvp.Value;
                }
            }
            
            ServiceLocator.Instance?.Unregister<PlayerItem2D>();
        }

        private void RefreshTrackedItems()
        {
            GridItem2D[] allItems = Object.FindObjectsByType<GridItem2D>(FindObjectsSortMode.None);
            foreach (var item in allItems)
            {
                if (item.IsPlaced)
                {
                    TrackItem(item);
                }
            }
        }

        private void HandleItemPlaced(IPlaceable placeable)
    {
        var item = placeable as GridItem2D;
        if (item != null)
        {
            Debug.Log($"[PlayerItem2D] HandleItemPlaced: {item.name} at {item.GridPosition}");
            TrackItem(item);
        }
    }

        private void TrackItem(GridItem2D item)
        {
            if (_trackedItems.Contains(item)) return;

            _trackedItems.Add(item);
            
            // Create a specific callback for this item
            System.Action callback = () => HandleReloadComplete(item);
            _reloadCallbacks[item] = callback;
            
            item.OnReloadComplete += callback;
        }

        private void HandleReloadComplete(GridItem2D item)
        {
            if (item == null) return;
            // Allow launch if placed OR if being dragged (for already placed items)
            if (!item.IsPlaced && !item.IsDragging) return;
            
            LaunchProjectile(item);
        }

        private void LaunchProjectile(GridItem2D item)
        {
            EnemyItem2D nearestEnemy = FindNearestEnemy();
            
            if (nearestEnemy == null)
            {
                // If no enemy found, we delay the reload restart slightly to avoid infinite tight loop
                // or just wait. For now, let's just restart the reload so it's ready again.
                // An optimal way would be to wait until an enemy appears.
                item.StartReloadAnimation();
                return;
            }

            if (_projectilePrefab == null)
            {
                Debug.LogWarning("PlayerItem2D: Projectile Prefab is not assigned!");
                item.StartReloadAnimation();
                return;
            }

            // Create projectile at throw point
            BezierProjectile projectile = Instantiate(_projectilePrefab, _throwPoint != null ? _throwPoint.position : transform.position, Quaternion.identity);
            
            // Match the scale of the original item instance and apply multiplier (default 0.5)
            projectile.transform.localScale = item.transform.localScale * vfxScale * _projectileScaleMultiplier;
            
            // Requirement: "kopyasini olusturup" - we use the same sprite
            Sprite itemSprite = item.GetDefenceItemData()?.Sprite;
            
            projectile.Initialize(
                nearestEnemy,
                item.Damage,
                itemSprite,
                _throwDuration,
                _throwHeight,
                (p) => 
                {
                    // Action finished: projective reaches target
                    // Requirement: "atmad bittikten sonra hedefe ulasinca tekrar reload'a girmesi lazim"
                    if (item != null)
                    {
                        item.StartReloadAnimation();
                    }
                }
            );
        }

        private EnemyItem2D FindNearestEnemy()
        {
            if (_enemySpawner == null) return null;
            
            var enemies = _enemySpawner.SpawnedEnemies;
            EnemyItem2D nearest = null;
            float minDistance = float.MaxValue;
            Vector3 myPos = transform.position;

            foreach (var enemy in enemies)
            {
                if (enemy is EnemyItem2D enemy2D && enemy2D.IsAlive)
                {
                    float dist = Vector3.Distance(myPos, enemy2D.transform.position);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        nearest = enemy2D;
                    }
                }
            }

            return nearest;
        }
    }
}
