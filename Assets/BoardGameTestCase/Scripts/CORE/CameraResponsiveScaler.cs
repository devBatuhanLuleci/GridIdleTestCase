using UnityEngine;

namespace BoardGameTestCase.Core
{
    /// <summary>
    /// Makes the camera responsive to different mobile screen sizes and aspect ratios.
    /// Attach this to your main Camera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraResponsiveScaler : MonoBehaviour
    {
        [Header("Reference Settings")]
        [Tooltip("Target aspect ratio (width/height). Default is 16:9 = 1.777")]
        [SerializeField] private float _targetAspectRatio = 16f / 9f;
        
        [Tooltip("Base orthographic size for the target aspect ratio")]
        [SerializeField] private float _baseOrthographicSize = 5f;
        
        [Header("Adjustment Settings")]
        [Tooltip("Scale mode for different aspect ratios")]
        [SerializeField] private ScaleMode _scaleMode = ScaleMode.FitHeight;
        
        [Tooltip("Apply safe area padding (for notches/cutouts)")]
        [SerializeField] private bool _useSafeArea = true;
        
        [Tooltip("Minimum orthographic size (prevents too much zoom)")]
        [SerializeField] private float _minOrthographicSize = 3f;
        
        [Tooltip("Maximum orthographic size (prevents too much zoom out)")]
        [SerializeField] private float _maxOrthographicSize = 10f;
        
        private Camera _camera;
        private float _initialOrthographicSize;
        
        public enum ScaleMode
        {
            FitWidth,      // Always show full width, height may be cropped
            FitHeight,     // Always show full height, width may be cropped
            Expand,        // Show more content on wider screens
            LetterBox      // Add black bars to maintain exact aspect ratio
        }
        
        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _initialOrthographicSize = _camera.orthographicSize;
        }
        
        private void Start()
        {
            ApplyResponsiveScale();
        }
        
        private void OnRectTransformDimensionsChange()
        {
            // Reapply when screen size changes (e.g., orientation change)
            if (_camera != null)
            {
                ApplyResponsiveScale();
            }
        }
        
        [ContextMenu("Apply Responsive Scale")]
        public void ApplyResponsiveScale()
        {
            if (_camera == null) return;
            
            float screenAspect = GetScreenAspect();
            float newOrthographicSize = CalculateOrthographicSize(screenAspect);
            
            // Clamp to min/max
            newOrthographicSize = Mathf.Clamp(newOrthographicSize, _minOrthographicSize, _maxOrthographicSize);
            
            _camera.orthographicSize = newOrthographicSize;
            
            Debug.Log($"[CameraResponsiveScaler] Screen: {Screen.width}x{Screen.height}, Aspect: {screenAspect:F2}, OrthographicSize: {newOrthographicSize:F2}");
        }
        
        private float GetScreenAspect()
        {
            if (_useSafeArea)
            {
                Rect safeArea = Screen.safeArea;
                return safeArea.width / safeArea.height;
            }
            
            return (float)Screen.width / Screen.height;
        }
        
        private float CalculateOrthographicSize(float screenAspect)
        {
            switch (_scaleMode)
            {
                case ScaleMode.FitWidth:
                    // Keep width constant, adjust height
                    return _baseOrthographicSize * (_targetAspectRatio / screenAspect);
                
                case ScaleMode.FitHeight:
                    // Keep height constant, adjust width (most common for mobile)
                    if (screenAspect < _targetAspectRatio)
                    {
                        // Narrower screen (e.g., 9:16 portrait)
                        return _baseOrthographicSize * (_targetAspectRatio / screenAspect);
                    }
                    return _baseOrthographicSize;
                
                case ScaleMode.Expand:
                    // Show more content on wider screens
                    if (screenAspect > _targetAspectRatio)
                    {
                        return _baseOrthographicSize;
                    }
                    return _baseOrthographicSize * (_targetAspectRatio / screenAspect);
                
                case ScaleMode.LetterBox:
                    // Maintain exact aspect ratio (you'll need to add black bars separately)
                    return _baseOrthographicSize;
                
                default:
                    return _baseOrthographicSize;
            }
        }
        
        private void OnValidate()
        {
            // Auto-update in editor when values change
            if (Application.isPlaying && _camera != null)
            {
                ApplyResponsiveScale();
            }
        }
    }
}
