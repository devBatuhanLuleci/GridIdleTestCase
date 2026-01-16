using UnityEngine;
using System.Collections.Generic;
using GridSystemModule.Core.Models;

namespace GridSystemModule.Core.Interfaces
{
    public interface IGridService
    {
        void GenerateGrid();
        void ClearGrid();
        BaseTile GetTileAtPosition(Vector2 position);
        bool IsPositionValid(Vector2 position);
        Dictionary<Vector2, BaseTile> GetAllTiles();
    }
}
