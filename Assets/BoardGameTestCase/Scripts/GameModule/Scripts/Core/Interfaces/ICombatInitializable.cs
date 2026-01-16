using BoardGameTestCase.Core.ScriptableObjects;
using GridSystemModule.Core.Interfaces;

namespace GameModule.Core.Interfaces
{
    public interface ICombatInitializable
    {
        void StartCombat();
        void StopCombat();
        void SetDefenceItemData(DefenceItemData data);
        void SetPlaceable(IPlaceable placeable);
    }
}

