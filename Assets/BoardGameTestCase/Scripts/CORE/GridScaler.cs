using UnityEngine;

namespace BoardGameTestCase.Core
{
    /// <summary>
    /// Automatically scales the grid tile parent to fit different screen sizes.
    /// Attach this to your TilesParent GameObject.
    /// Works in conjunction with CameraResponsiveScaler.
    /// Note: GridManager automatically maintains tile local scales, so tiles won't be affected by parent scale changes.
    /// </summary>
    public class GridScaler : MonoBehaviour
    {
        [Header("Reference Settings")]
        [Tooltip("Target screen width for reference scale")]
        [SerializeField] private float _referenceScreenWidth = 1920f;
        
        [Tooltip("Target screen height for reference scale")]
        [SerializeField] private float _referenceScreenHeight = 1080f;
        
        [Tooltip("Base scale at reference resolution")]
        [SerializeField] private Vector3 _baseScale = Vector3.one;
        
        [Header("Scale Settings")]
        [Tooltip("How to scale the grid")]
        [SerializeField] private ScaleMode _scaleMode = ScaleMode.ScaleWithScreenSize;
        
        [Tooltip("Match width or height (0 = width, 1 = height, 0.5 = both)")]
        [Range(0f, 1f)]
        [SerializeField] private float _matchWidthOrHeight = 0.5f;
        
        [Tooltip("Minimum scale limit")]
        [SerializeField] private float _minScale = 0.5f;
        
        [Tooltip("Maximum scale limit")]
        [SerializeField] private float _maxScale = 2f;
        
        [Header("Reference camera (null = main camera)")]
        [SerializeField] private Camera _targetCamera;
        
        [Header("Debug")]
        [Tooltip("Show debug logs")]
        [SerializeField] private bool _showDebugLogs = false;
        
        private Vector2 _lastScreenSize;
        private Vector3 _initialScale;
        
        public enum ScaleMode
        {
            ScaleWithScreenSize,    // Scale based on screen resolution
            ScaleWithCameraSize,    // Scale based on camera orthographic size
            FixedScale              // Keep base scale (no responsive scaling)
        }
        
        private void Awake()
        {
            _initialScale = transform.localScale;
            
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }
        }
        
        private void OnEnable()
        {
            ScreenChangeEventManager.OnScreenSizeChanged += ApplyResponsiveScale;
        }

        private void OnDisable()
        {
            ScreenChangeEventManager.OnScreenSizeChanged -= ApplyResponsiveScale;
        }

        private void Start()
        {
            ApplyResponsiveScale();
        }
        
        [ContextMenu("Apply Responsive Scale")]
        public void ApplyResponsiveScale()
        {
            float scaleFactor = CalculateScaleFactor();
            
            // Clamp scale
            scaleFactor = Mathf.Clamp(scaleFactor, _minScale, _maxScale);
            
            // Apply scale
            Vector3 newScale = _baseScale * scaleFactor;
            transform.localScale = newScale;
            
            if (_showDebugLogs)
            {
                Debug.Log($"[GridScaler] Applied scale: {scaleFactor:F2}x (Screen: {Screen.width}x{Screen.height})");
            }
        }
        
        private float CalculateScaleFactor()
        {
            switch (_scaleMode)
            {
                case ScaleMode.ScaleWithScreenSize:
                    return CalculateScreenBasedScale();
                
                case ScaleMode.ScaleWithCameraSize:
                    return CalculateCameraBasedScale();
                
                case ScaleMode.FixedScale:
                    return 1f;
                
                default:
                    return 1f;
            }
        }
        
        private float CalculateScreenBasedScale()
        {
            // Calculate scale based on screen resolution (similar to Canvas Scaler)
            float widthScale = Screen.width / _referenceScreenWidth;
            float heightScale = Screen.height / _referenceScreenHeight;
            
            // Lerp between width and height scale based on match value
            float scaleFactor = Mathf.Lerp(widthScale, heightScale, _matchWidthOrHeight);
            
            return scaleFactor;
        }
        
        private float CalculateCameraBasedScale()
        {
            if (_targetCamera == null) return 1f;
            
            // Calculate based on camera orthographic size
            // Assumes reference is orthographic size 5
            float referenceOrthoSize = 5f;
            float currentOrthoSize = _targetCamera.orthographicSize;
            
            // Inverse relationship: larger camera size = smaller grid scale
            float scaleFactor = referenceOrthoSize / currentOrthoSize;
            
            return scaleFactor;
        }
        
        /// <summary>
        /// Set base scale (useful for runtime adjustments)
        /// </summary>
        public void SetBaseScale(Vector3 newBaseScale)
        {
            _baseScale = newBaseScale;
            ApplyResponsiveScale();
        }
        
        /// <summary>
        /// Reset to initial scale
        /// </summary>
        [ContextMenu("Reset To Initial Scale")]
        public void ResetToInitialScale()
        {
            transform.localScale = _initialScale;
        }
        
        private void OnValidate()
        {
            // Auto-update in editor when values change
            if (Application.isPlaying && _targetCamera != null)
            {
                ApplyResponsiveScale();
            }
        }
    }
}
