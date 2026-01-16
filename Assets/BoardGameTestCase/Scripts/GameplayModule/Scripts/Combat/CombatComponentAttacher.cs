using UnityEngine;
using BoardGameTestCase.Core.Common;
using GameModule.Core.Interfaces;
using GridSystemModule.Core.Interfaces;
using BoardGameTestCase.Core.ScriptableObjects;

namespace GameplayModule
{
    public class CombatComponentAttacher : MonoBehaviour, ICombatComponentAttacher
    {
        private void Awake()
        {
            ServiceLocator.Instance.Register<ICombatComponentAttacher>(this);
        }
        
        private void OnDestroy()
        {
            ServiceLocator.Instance?.Unregister<ICombatComponentAttacher>();
        }
        
        public void AttachCombatComponent(IPlaceable placeable, DefenceItemData itemData)
        {
        }
    }
}

