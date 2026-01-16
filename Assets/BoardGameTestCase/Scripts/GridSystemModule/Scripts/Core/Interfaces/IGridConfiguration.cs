using UnityEngine;
using GridSystemModule.Core.Models;

namespace GridSystemModule.Core.Interfaces
{
    public interface IGridConfiguration
    {
        int Width { get; }
        int Height { get; }
        BaseTile GrassTile { get; }
        Transform Camera { get; }
        Transform TilesParent { get; }
    }
}
