using UnityEngine;

namespace BoardGameTestCase.Core.ScriptableObjects
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Board Game/Enemy Data", order = 2)]
    public class EnemyData : ScriptableObject
    {        [SerializeField] private Sprite _sprite;
        [SerializeField] private string _displayName = "Enemy";        [SerializeField] private int _health = 10;
        [SerializeField] private float _speed = 1.0f;        [SerializeField] private string _enemyId;
        [SerializeField] private string _type = "Basic";        [SerializeField] private string _description = "";
        
        public Sprite Sprite => _sprite;
        public string DisplayName => _displayName;
        public int Health => _health;
        public float Speed => _speed;
        public string EnemyId => string.IsNullOrEmpty(_enemyId) ? name : _enemyId;
        public string Type => _type;
        public string Description => _description;
    }
}
