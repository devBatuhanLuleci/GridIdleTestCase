using System;

namespace GameModule.Core.Interfaces
{
    public interface IStateController
    {
        GameState CurrentState { get; }
        event Action<GameState> OnStateChanged;
        void SetPlacing();
        void SetFight();
        void SetWin();
        void SetLose();
    }
}

