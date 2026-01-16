using GridSystemModule.Core.Interfaces;
using BoardGameTestCase.Core.ScriptableObjects;

namespace GameModule.Core.Interfaces
{
    public interface ICombatComponentAttacher
    {
        void AttachCombatComponent(IPlaceable placeable, DefenceItemData itemData);
    }
}

