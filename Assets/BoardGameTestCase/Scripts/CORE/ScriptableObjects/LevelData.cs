using System;
using System.Collections.Generic;
using UnityEngine;

namespace BoardGameTestCase.Core.ScriptableObjects
{
    [Serializable]
    public class DefenceItemEntry
    {
        [SerializeField] private DefenceItemData _defenceItemData;
        [SerializeField] private int _quantity = 1;
        
        public DefenceItemData DefenceItemData => _defenceItemData;
        public int Quantity => _quantity;
    }
    
    [Serializable]
    public class EnemyEntry
    {
        [SerializeField] private EnemyData _enemyData;
        [SerializeField] private int _quantity = 1;
        
        public EnemyData EnemyData => _enemyData;
        public int Quantity => _quantity;
    }
    
    [CreateAssetMenu(fileName = "LevelData", menuName = "Board Game/Level Data", order = 3)]
    public class LevelData : ScriptableObject
    {        [SerializeField] private int _levelNumber = 1;
        [SerializeField] private string _displayName = "Level 1";        [SerializeField] private List<DefenceItemEntry> _defenceItems = new List<DefenceItemEntry>();        [SerializeField] private List<EnemyEntry> _enemies = new List<EnemyEntry>();
        
        public int LevelNumber => _levelNumber;
        public string DisplayName => _displayName;
        public IReadOnlyList<DefenceItemEntry> DefenceItems => _defenceItems;
        public IReadOnlyList<EnemyEntry> Enemies => _enemies;
        
        public int GetTotalDefenceItemCount()
        {
            int total = 0;
            foreach (var entry in _defenceItems)
            {
                total += entry.Quantity;
            }
            return total;
        }
        
        public int GetTotalEnemyCount()
        {
            int total = 0;
            foreach (var entry in _enemies)
            {
                total += entry.Quantity;
            }
            return total;
        }
        
        public DefenceItemEntry GetDefenceItemEntry(DefenceItemData itemData)
        {
            return _defenceItems.Find(entry => entry.DefenceItemData == itemData);
        }
        
        public EnemyEntry GetEnemyEntry(EnemyData enemyData)
        {
            return _enemies.Find(entry => entry.EnemyData == enemyData);
        }
    }
}
