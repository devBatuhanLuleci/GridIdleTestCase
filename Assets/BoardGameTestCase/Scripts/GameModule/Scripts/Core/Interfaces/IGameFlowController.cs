namespace GameModule.Core.Interfaces
{
    public interface IGameFlowController
    {
        void StartGame();
        void StartFight();
        void EndFight();
        void SetWin();
        void SetLose();
        void RestartGame();
        GameState CurrentGameState { get; }
    }
}

