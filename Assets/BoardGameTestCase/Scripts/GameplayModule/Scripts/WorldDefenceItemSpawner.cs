using UnityEngine;
using BoardGameTestCase.Core.ScriptableObjects;
using BoardGameTestCase.Core.Common;
using GameModule.Core.Interfaces;
using UISystemModule.UIElements;

namespace GameplayModule
{
    /// <summary>
    /// Spawns a limited number of world-space defence items (GridItem2D) at startup
    /// using the current level's defence item list.
    /// </summary>
    public class WorldDefenceItemSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject _placeablePrefab;
        [SerializeField] private Vector3 _startPosition = new Vector3(-2f, 0f, 0f);
        [SerializeField] private Vector3 _step = new Vector3(2f, 0f, 0f);
        [SerializeField] private int _maxSpawn = 3;

        private ILevelDataProvider _levelDataProvider;

        private void Awake()
        {
            _levelDataProvider = ServiceLocator.Instance?.TryGet<ILevelDataProvider>();
        }

        private void Start()
        {
            if (_placeablePrefab == null) return;
            if (_levelDataProvider == null || _levelDataProvider.CurrentLevel == null) return;

            int spawned = 0;
            var defenceItems = _levelDataProvider.CurrentLevel.DefenceItems;
            if (defenceItems == null) return;

            foreach (var entry in defenceItems)
            {
                if (entry?.DefenceItemData == null) continue;

                int quantity = Mathf.Max(1, entry.Quantity);
                for (int i = 0; i < quantity && spawned < _maxSpawn; i++)
                {
                    Spawn(entry.DefenceItemData, spawned);
                    spawned++;
                }

                if (spawned >= _maxSpawn) break;
            }
        }

        private void Spawn(DefenceItemData data, int index)
        {
            var pos = _startPosition + _step * index;
            var go = Instantiate(_placeablePrefab, pos, Quaternion.identity);

            var gridItem = go.GetComponent<GridItem2D>();
            if (gridItem != null)
            {
                gridItem.SetDefenceItemData(data);
            }

            var spriteHandler = go.GetComponent<SpriteGridItemDragHandler>() ?? go.AddComponent<SpriteGridItemDragHandler>();
            spriteHandler.SetDefenceItemData(data);
        }
    }
}
