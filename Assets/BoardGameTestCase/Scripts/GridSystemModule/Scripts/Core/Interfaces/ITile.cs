using UnityEngine;

namespace GridSystemModule.Core.Interfaces
{
    public interface ITile
    {
        string TileName { get; }
        Vector2 Position { get; }
        void Initialize(int x, int y);
        void OnTileClicked();
        void OnTileEnter();
        void OnTileExit();
    }
}
