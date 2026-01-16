using UnityEngine;
using BoardGameTestCase.Core.ScriptableObjects;
using BoardGameTestCase.Core.Common;
using PlacementModule.Interfaces;
using GridSystemModule.Core.Interfaces;

namespace PlacementModule
{
    public class GhostObjectCreator : MonoBehaviour, IGhostObjectCreator, IInitializable
    {
        private bool _isInitialized = false;
        public bool IsInitialized => _isInitialized;
        private IGridItemFactory _gridItemFactory;

        private void Awake()
        {
            ServiceLocator.Instance.Register<IGhostObjectCreator>(this);
            ServiceLocator.Instance.Register<GhostObjectCreator>(this);
        }

        private void OnDestroy()
        {
            ServiceLocator.Instance?.Unregister<IGhostObjectCreator>();
            ServiceLocator.Instance?.Unregister<GhostObjectCreator>();
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            EnsureFactory();
            _isInitialized = true;
        }

        public GameObject CreateGhostObject(DefenceItemData itemData, Vector3 position, float ghostAlpha, float ghostScale)
        {
            if (itemData == null) return null;

            EnsureFactory();
            if (_gridItemFactory == null) return null;

            var ghostGameObject = _gridItemFactory.CreateGridItem(itemData, position, isGhost: true, ghostAlpha: ghostAlpha);
            if (ghostGameObject == null) return null;

            ghostGameObject.transform.localScale = Vector3.one * ghostScale;
            return ghostGameObject;
        }

        private void EnsureFactory()
        {
            if (_gridItemFactory != null) return;
            _gridItemFactory = ServiceLocator.Instance.TryGet<IGridItemFactory>();
        }
    }
}

