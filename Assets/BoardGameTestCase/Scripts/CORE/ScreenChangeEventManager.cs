using UnityEngine;
using System;

namespace BoardGameTestCase.Core
{
    /// <summary>
    /// Central manager that dispatches events when the screen size or orientation changes.
    /// Eliminates the need for multiple scripts to poll Screen.width/height in Update.
    /// </summary>
    public class ScreenChangeEventManager : MonoBehaviour
    {
        public static event Action OnScreenSizeChanged;
        
        private Vector2 _lastScreenSize;
        private static ScreenChangeEventManager _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            _lastScreenSize = new Vector2(Screen.width, Screen.height);
        }

        private void Update()
        {
            // Only poll once in a single central manager
            if (Screen.width != _lastScreenSize.x || Screen.height != _lastScreenSize.y)
            {
                _lastScreenSize = new Vector2(Screen.width, Screen.height);
                OnScreenSizeChanged?.Invoke();
                Debug.Log($"[ScreenChangeEventManager] Screen size changed to: {_lastScreenSize.x}x{_lastScreenSize.y}");
            }
        }
    }
}
