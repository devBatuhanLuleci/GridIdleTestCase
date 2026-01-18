using UnityEngine;

namespace BoardGameTestCase.Core
{
    /// <summary>
    /// Creates a parallax scrolling effect for background layers.
    /// Attach this to background elements like clouds, trees, mountains.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [Header("Parallax Settings")]
        [Tooltip("Parallax speed multiplier (0 = static, 1 = moves with camera)")]
        [Range(0f, 1f)]
        [SerializeField] private float _parallaxFactor = 0.5f;
        
        [Tooltip("Reference camera (null = main camera)")]
        [SerializeField] private Camera _targetCamera;
        
        [Header("Auto Scroll (Optional)")]
        [Tooltip("Enable automatic horizontal scrolling")]
        [SerializeField] private bool _autoScroll = false;
        
        [Tooltip("Auto scroll speed (units per second)")]
        [SerializeField] private float _autoScrollSpeed = 0.5f;
        
        [Header("Infinite Scroll (Optional)")]
        [Tooltip("Enable infinite horizontal scrolling")]
        [SerializeField] private bool _infiniteScroll = false;
        
        [Tooltip("Width of the sprite for infinite scroll")]
        [SerializeField] private float _spriteWidth = 10f;
        
        private Vector3 _previousCameraPosition;
        private Vector3 _startPosition;
        
        private void Start()
        {
            if (_targetCamera == null)
            {
                _targetCamera = Camera.main;
            }
            
            if (_targetCamera != null)
            {
                _previousCameraPosition = _targetCamera.transform.position;
            }
            
            _startPosition = transform.position;
            
            // Auto-detect sprite width for infinite scroll
            if (_infiniteScroll)
            {
                SpriteRenderer sr = GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    _spriteWidth = sr.sprite.bounds.size.x * transform.localScale.x;
                }
            }
        }
        
        private void LateUpdate()
        {
            if (_targetCamera == null) return;
            
            Vector3 currentCameraPosition = _targetCamera.transform.position;
            Vector3 deltaMovement = currentCameraPosition - _previousCameraPosition;
            
            // Apply parallax effect
            Vector3 parallaxOffset = new Vector3(
                deltaMovement.x * _parallaxFactor,
                deltaMovement.y * _parallaxFactor,
                0
            );
            
            transform.position += parallaxOffset;
            
            // Apply auto scroll
            if (_autoScroll)
            {
                transform.position += new Vector3(_autoScrollSpeed * Time.deltaTime, 0, 0);
            }
            
            // Apply infinite scroll
            if (_infiniteScroll)
            {
                float distanceFromStart = transform.position.x - _startPosition.x;
                
                if (Mathf.Abs(distanceFromStart) >= _spriteWidth)
                {
                    // Reset position
                    float offset = Mathf.Sign(distanceFromStart) * _spriteWidth;
                    transform.position = new Vector3(
                        transform.position.x - offset,
                        transform.position.y,
                        transform.position.z
                    );
                    _startPosition = transform.position;
                }
            }
            
            _previousCameraPosition = currentCameraPosition;
        }
        
        /// <summary>
        /// Reset parallax to initial position
        /// </summary>
        [ContextMenu("Reset Position")]
        public void ResetPosition()
        {
            transform.position = _startPosition;
            if (_targetCamera != null)
            {
                _previousCameraPosition = _targetCamera.transform.position;
            }
        }
    }
}
