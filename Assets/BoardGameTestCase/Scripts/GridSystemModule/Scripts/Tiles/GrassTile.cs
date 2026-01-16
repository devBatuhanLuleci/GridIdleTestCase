using UnityEngine;
using GridSystemModule.Core.Interfaces;
using GridSystemModule.Core.Models;

namespace GridSystemModule.Tiles
{    public class GrassTile : BaseTile
    {

        protected override void Awake()
        {
            base.Awake();
            tileName = "Grass Tile";
        }

        public override void Initialize(int x, int y)
        {
            base.Initialize(x, y);
            if (spriteRenderer != null)
            {
                if (spriteRenderer.sprite == null) CreateColoredSprite();
                spriteRenderer.enabled = true;
            }
        }
        
        private void CreateColoredSprite()
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            spriteRenderer.sprite = sprite;
        }

        public override void OnTileClicked()
        {
            base.OnTileClicked();
        }
    }
}
