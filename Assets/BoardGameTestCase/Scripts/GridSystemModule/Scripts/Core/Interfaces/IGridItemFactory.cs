using UnityEngine;
using GridSystemModule.Core.Interfaces;
using BoardGameTestCase.Core.ScriptableObjects;

namespace GridSystemModule.Core.Interfaces
{
    public interface IGridItemFactory
    {
        GameObject CreateGridItem(ScriptableObject itemData, Vector3 position, bool isGhost = false, float ghostAlpha = 1.0f);
        IPlaceable CreateGridItem2D(DefenceItemData itemData, Vector3 position, bool isGhost = false);
    }
}

