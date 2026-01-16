using UnityEngine;
using BoardGameTestCase.Core.ScriptableObjects;

namespace PlacementModule.Interfaces
{
    public interface IGhostObjectCreator
    {
        GameObject CreateGhostObject(DefenceItemData itemData, Vector3 position, float ghostAlpha, float ghostScale);
    }
}

