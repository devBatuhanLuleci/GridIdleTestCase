using System.Collections.Generic;
using UnityEngine;
using BoardGameTestCase.Core.Common;
using GameModule.Core.Interfaces;
using GameModule.Core;
using BoardGameTestCase.Core.ScriptableObjects;

namespace GameModule.Managers
{
    public class LevelManager : MonoBehaviour, IInitializable, ILevelDataProvider
    {        [SerializeField] private List<LevelData> _levels = new List<LevelData>();
        [SerializeField] private int _currentLevelIndex = 0;
        
        private bool _isInitialized = false;
        
        public bool IsInitialized => _isInitialized;
        
        public LevelData CurrentLevel 
        { 
            get 
            { 
                if (_levels == null || _levels.Count == 0) return null;
                int dataIndex = _currentLevelIndex >= _levels.Count ? _levels.Count - 1 : _currentLevelIndex;
                return dataIndex >= 0 && dataIndex < _levels.Count ? _levels[dataIndex] : null;
            }
        }
        public int CurrentLevelNumber => _currentLevelIndex + 1;
        public int TotalLevels => _levels.Count;
        public bool HasNextLevel => true;
        public IReadOnlyList<LevelData> Levels => _levels;
        
        private void Awake()
        {
            ServiceLocator.Instance.Register<LevelManager>(this);
            ServiceLocator.Instance.Register<ILevelDataProvider>(this);
        }
        
        private void OnDestroy()
        {
            ServiceLocator.Instance?.Unregister<LevelManager>();
            ServiceLocator.Instance?.Unregister<ILevelDataProvider>();
        }
        
        public void Initialize()
        {
            if (_isInitialized) return;
            
            int savedLevel = PlayerPrefs.GetInt("CurrentLevel", 1);
            _currentLevelIndex = savedLevel - 1;
            
            if (_currentLevelIndex < 0)
            {
                _currentLevelIndex = 0;
            }
            
            _isInitialized = true;
            PublishCurrentLevelNumberChanged();
        }
        
        private void PublishCurrentLevelNumberChanged()
        {
            EventBus.Instance?.Publish(new CurrentLevelNumberChangedEvent(CurrentLevelNumber));
        }
        
        public bool SetLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= _levels.Count) return false;
            if (_levels[levelIndex] == null) return false;
            _currentLevelIndex = levelIndex;
            PublishCurrentLevelNumberChanged();
            return true;
        }
        
        public bool SetLevel(LevelData levelData)
        {
            if (levelData == null) return false;
            int index = _levels.IndexOf(levelData);
            if (index < 0) return false;
            return SetLevel(index);
        }
        
        public bool NextLevel()
        {
            _currentLevelIndex++;
            
            int nextLevelNumber = _currentLevelIndex + 1;
            PlayerPrefs.SetInt("CurrentLevel", nextLevelNumber);
            PlayerPrefs.Save();
            
            PublishCurrentLevelNumberChanged();
            return true;
        }
        
        public bool PreviousLevel()
        {
            if (_currentLevelIndex <= 0) return false;
            _currentLevelIndex--;
            PublishCurrentLevelNumberChanged();
            return true;
        }
        
        public void ResetToFirstLevel()
        {
            _currentLevelIndex = 0;
            PublishCurrentLevelNumberChanged();
        }
        
        public LevelData GetLevelByNumber(int levelNumber)
        {
            if (_levels == null || _levels.Count == 0) return null;
            int requestedIndex = levelNumber - 1;
            int dataIndex = requestedIndex >= _levels.Count ? _levels.Count - 1 : requestedIndex;
            return dataIndex >= 0 && dataIndex < _levels.Count ? _levels[dataIndex] : null;
        }
        
        public LevelData GetLevelByIndex(int index)
        {
            if (_levels == null || _levels.Count == 0) return null;
            int dataIndex = index >= _levels.Count ? _levels.Count - 1 : index;
            return dataIndex >= 0 && dataIndex < _levels.Count ? _levels[dataIndex] : null;
        }
        
        public void AddLevel(LevelData levelData)
        {
            if (levelData == null) return;
            if (_levels.Contains(levelData)) return;
            _levels.Add(levelData);
        }
        
        public bool RemoveLevel(LevelData levelData)
        {
            if (levelData == null) return false;
            bool removed = _levels.Remove(levelData);
            if (removed && _currentLevelIndex >= _levels.Count) _currentLevelIndex = Mathf.Max(0, _levels.Count - 1);
            return removed;
        }
        
        public void ClearLevels()
        {
            _levels.Clear();
            _currentLevelIndex = 0;
        }
    }
}

