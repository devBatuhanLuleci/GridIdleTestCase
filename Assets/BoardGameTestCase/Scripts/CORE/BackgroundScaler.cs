using UnityEngine;

namespace BoardGameTestCase.Core
{
    /// <summary>
    /// Automatically scales a background sprite to fit the camera view on all screen sizes.
    /// Attach this to your background sprite GameObject.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class BackgroundScaler : MonoBehaviour
    {
        [Header("Scale Settings")]
        [Tooltip("How the background should fit the screen")]
        [SerializeField] private FitMode _fitMode = FitMode.Cover;
        
        [Tooltip("Vertical offset (useful for parallax)")]
        [SerializeField] private float _verticalOffset = 0f;
        
        [Tooltip("Reference camera (null = main camera)")]
        [SerializeField] private Camera _targetCamera;
        
        [Tooltip("Additional scale multiplier")]
        [SerializeField] private float _scaleMultiplier = 1f;

        private SpriteRenderer _spriteRenderer;
        
        public enum FitMode
        {
            Cover,      // Fill entire screen (may crop sprite)
            Contain,    // Show entire sprite (may have gaps)
            Stretch     // Stretch to exact screen size (may distort)
        }
        
        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }
        }

        private void OnEnable()
        {
            ScreenChangeEventManager.OnScreenSizeChanged += ScaleToFitCamera;
        }

        private void OnDisable()
        {
            ScreenChangeEventManager.OnScreenSizeChanged -= ScaleToFitCamera;
        }
        
        private void Start()
        {
            ScaleToFitCamera();
        }
        
        [ContextMenu("Scale To Fit Camera")]
        public void ScaleToFitCamera()
        {
            if (_spriteRenderer == null || _targetCamera == null)
            {
                Debug.LogWarning("[BackgroundScaler] Missing SpriteRenderer or Camera!");
                return;
            }
            
            if (_spriteRenderer.sprite == null)
            {
                Debug.LogWarning("[BackgroundScaler] No sprite assigned to SpriteRenderer!");
                return;
            }
            
            // Calculate world dimensions visible by camera
            float worldHeight = _targetCamera.orthographicSize * 2f;
            float worldWidth = worldHeight * _targetCamera.aspect;
            
            // Get sprite dimensions
            Vector2 spriteSize = _spriteRenderer.sprite.bounds.size;
            
            // Calculate required scale
            float scaleX = worldWidth / spriteSize.x;
            float scaleY = worldHeight / spriteSize.y;
            
            float finalScale = 1f;
            
            switch (_fitMode)
            {
                case FitMode.Cover:
                    // Use the larger scale to ensure no gaps
                    finalScale = Mathf.Max(scaleX, scaleY);
                    break;
                
                case FitMode.Contain:
                    // Use the smaller scale to show entire sprite
                    finalScale = Mathf.Min(scaleX, scaleY);
                    break;
                
                case FitMode.Stretch:
                    // Different scale for X and Y (may distort)
                    transform.localScale = new Vector3(scaleX * _scaleMultiplier, scaleY * _scaleMultiplier, 1);
                    ApplyVerticalOffset();
                    return;
            }
            
            // Apply uniform scale
            finalScale *= _scaleMultiplier;
            transform.localScale = new Vector3(finalScale, finalScale, 1);
            
            ApplyVerticalOffset();
            
            Debug.Log($"[BackgroundScaler] Scaled to {finalScale:F2}x for screen {Screen.width}x{Screen.height}");
        }
        
        private void ApplyVerticalOffset()
        {
            if (_verticalOffset != 0f)
            {
                Vector3 pos = transform.position;
                pos.y = _verticalOffset;
                transform.position = pos;
            }
        }
        
        private void OnValidate()
        {
            // Auto-update in editor when values change
            if (Application.isPlaying && _spriteRenderer != null && _targetCamera != null)
            {
                ScaleToFitCamera();
            }
        }
    }
}
