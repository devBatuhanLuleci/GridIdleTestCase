using System;

namespace GameModule.Core.Interfaces
{
    public interface IEnemy
    {
        bool IsAlive { get; }
        event Action<IEnemy> OnDeath;
        void TakeDamage(int damage);
    }
}

