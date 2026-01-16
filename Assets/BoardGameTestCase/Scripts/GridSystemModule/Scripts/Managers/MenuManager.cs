using UnityEngine;
using UnityEngine.UI;
using GridSystemModule.Core.Models;
using BoardGameTestCase.Core.Common;

namespace GridSystemModule.Managers
{
    public class MenuManager : MonoBehaviour, IInitializable
    {
        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;
        [SerializeField] private GameObject _tileInfoObject;
        [SerializeField] private UnityEngine.UI.Text _tileInfoText;

        private void Awake()
        {
            ServiceLocator.Instance.Register<MenuManager>(this);
        }
        
        private void OnDestroy()
        {
            if (ServiceLocator.Instance != null) ServiceLocator.Instance.Unregister<MenuManager>();
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            _isInitialized = true;
        }
        
        public void ShowTileInfo(BaseTile tile)
        {
            if (_tileInfoObject == null) return;

            if (tile == null)
            {
                _tileInfoObject.SetActive(false);
                return;
            }

            if (_tileInfoText != null)
            {
                _tileInfoText.text = $"Tile: {tile.TileName}\nPosition: {tile.Position}";
            }

            _tileInfoObject.SetActive(true);
        }
    }
}