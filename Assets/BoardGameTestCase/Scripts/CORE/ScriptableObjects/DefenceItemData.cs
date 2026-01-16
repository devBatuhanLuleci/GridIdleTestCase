using UnityEngine;

namespace BoardGameTestCase.Core.ScriptableObjects
{
    public enum AttackDirection
    {
        Forward,
        All
    }
    
    [CreateAssetMenu(fileName = "DefenceItemData", menuName = "Board Game/Defence Item Data", order = 1)]
    public class DefenceItemData : ScriptableObject
    {
        [Header("Basic Info")]
        [SerializeField] private Sprite _sprite;
        [SerializeField] private string _displayName = "Defence Item";
        
        [Header("Combat")]
        [SerializeField] private int _damage = 10;
        [SerializeField] private float _range = 2.0f;
        [SerializeField] private float _attackInterval = 1.0f;
        [SerializeField] private AttackDirection _attackDirection = AttackDirection.Forward;
        [SerializeField] private int _health = 100;
        
        [Header("Economy")]
        [SerializeField] private int _cost = 50;
        
        [Header("Grid Settings")]
        [SerializeField] private Vector2Int _gridSize = new Vector2Int(1, 1);
        
        [Header("ID & Metadata")]
        [SerializeField] private string _itemId;
        [SerializeField] private string _type = "Basic";
        [SerializeField] private string _description = "";

        [Header("Preview (Optional)")]
        [Tooltip("If assigned, the editor preview uses this grid settings (cell size, spacing, colors) to render the footprint.")]
        [SerializeField] private Object _gridPreviewSettings;

        [Header("Rendering (Ghost Item)")]
        [SortingLayer]
        [SerializeField] private string _ghostSortingLayerName = "Default";
        [SerializeField] private int _ghostSortingOrder = 10;
        
        public Sprite Sprite => _sprite;
        public string DisplayName => _displayName;
        public int Damage => _damage;
        public float Range => _range;
        public float AttackInterval => _attackInterval;
        public AttackDirection AttackDirection => _attackDirection;
        public int Health => _health;
        public int Cost => _cost;
        public Vector2Int GridSize => _gridSize;
        public string ItemId => string.IsNullOrEmpty(_itemId) ? name : _itemId;
        public string Type => _type;
        public string Description => _description;
        public string GhostSortingLayerName => _ghostSortingLayerName;
        public int GhostSortingOrder => _ghostSortingOrder;
        // Editor-only access; runtime returns null
        public Object GridPreviewSettingsObject => _gridPreviewSettings;
    }
}
