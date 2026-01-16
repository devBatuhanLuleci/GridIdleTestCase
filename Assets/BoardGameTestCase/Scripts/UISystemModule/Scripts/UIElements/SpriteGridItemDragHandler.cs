using UnityEngine;
using BoardGameTestCase.Core.ScriptableObjects;

namespace UISystemModule.UIElements
{
    [RequireComponent(typeof(GridItem2D))]
    public class SpriteGridItemDragHandler : MonoBehaviour
    {
        [SerializeField] private DefenceItemData _defenceItemData;
        private GridItem2D _gridItem;

        private void Awake()
        {
            _gridItem = GetComponent<GridItem2D>();
            ApplyData();
        }

        public void SetDefenceItemData(DefenceItemData data)
        {
            _defenceItemData = data;
            ApplyData();
        }

        private void ApplyData()
        {
            if (_gridItem != null && _defenceItemData != null)
            {
                _gridItem.SetDefenceItemData(_defenceItemData);
            }
        }
    }
}
