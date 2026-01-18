using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAtlasUVSync : MonoBehaviour
{
    private SpriteRenderer _spriteRenderer;
    private MaterialPropertyBlock _propBlock;
    private static readonly int SpriteUVsProp = Shader.PropertyToID("_SpriteUVs");
    private Sprite _lastSprite;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        Init();
        if (_spriteRenderer == null) return;

        Sprite currentSprite = _spriteRenderer.sprite;
        if (currentSprite == null) return;

        // Update if sprite changes (works with Animator/SpriteSheets)
        if (currentSprite != _lastSprite)
        {
            _lastSprite = currentSprite;
            SyncUVs();
        }
    }

    private void SyncUVs()
    {
        if (_spriteRenderer == null || _spriteRenderer.sprite == null) return;
        Init();

        Vector4 uvRect = UnityEngine.Sprites.DataUtility.GetOuterUV(_spriteRenderer.sprite);
        
        float scaleX = uvRect.z - uvRect.x;
        float scaleY = uvRect.w - uvRect.y;
        float offsetX = uvRect.x;
        float offsetY = uvRect.y;

        _spriteRenderer.GetPropertyBlock(_propBlock);
        _propBlock.SetVector(SpriteUVsProp, new Vector4(scaleX, scaleY, offsetX, offsetY));
        _spriteRenderer.SetPropertyBlock(_propBlock);
    }

    private void OnValidate()
    {
        Init();
        if (_spriteRenderer != null && _spriteRenderer.sprite != null)
        {
            SyncUVs();
        }
    }
}
