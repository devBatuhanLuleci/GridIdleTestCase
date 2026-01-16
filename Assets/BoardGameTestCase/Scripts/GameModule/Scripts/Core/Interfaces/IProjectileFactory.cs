using UnityEngine;
using GameModule.Core.Interfaces;

namespace GameModule.Core.Interfaces
{
    public interface IProjectileFactory
    {
        MonoBehaviour CreateProjectile(Vector3 position, int damage, float speed, IEnemy target, Sprite sprite = null);
    }
}

