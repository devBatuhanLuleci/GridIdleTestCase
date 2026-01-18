using System;
using UnityEngine;
using DG.Tweening;
using BoardGameTestCase.Core;

namespace GameplayModule
{
    public class BezierProjectile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        
        private Material _materialInstance;
        private int _damage;
        private float _duration = 0.5f;
        private float _height = 2f;
        private EnemyItem2D _target;
        private Action<BezierProjectile> _onHit;
        
        private void Awake()
        {
            if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
            SetupMaterialInstance();
        }

        private void SetupMaterialInstance()
        {
            if (_spriteRenderer != null && _spriteRenderer.sharedMaterial != null && _materialInstance == null)
            {
                _materialInstance = new Material(_spriteRenderer.sharedMaterial);
                _spriteRenderer.material = _materialInstance;
            }
        }

        private void OnDestroy()
        {
            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
            }
        }

        public void Initialize(EnemyItem2D target, int damage, Sprite sprite, float duration, float height, Action<BezierProjectile> onHit)
        {
            _target = target;
            _damage = damage;
            _duration = duration;
            _height = height;
            _onHit = onHit;
            
            if (_spriteRenderer != null && sprite != null)
            {
                _spriteRenderer.sprite = sprite;
            }
            
            StartMovement();
        }
        
        private void StartMovement()
        {
            if (_target == null || !_target.IsAlive)
            {
                Destroy(gameObject);
                return;
            }
            
            Vector3 startPos = transform.position;
            // The target might move, but usually enemies in this genre are static or move slowly.
            // Requirement says "atmasi lazim", "hedefe ulasinca".
            
            DOVirtual.Float(0f, 1f, _duration, (t) =>
            {
                if (this == null) return;
                
                // If target dies during flight, we might just continue to last known position or destroy
                Vector3 endPos = _target != null ? _target.transform.position : startPos;
                Vector3 controlPoint = BezierUtils.GetAutomaticControlPoint(startPos, endPos, _height, Vector3.up);
                
                transform.position = BezierUtils.GetPoint(startPos, controlPoint, endPos, t);
                
                // Rotate to face direction
                if (t < 1f)
                {
                    Vector3 nextPoint = BezierUtils.GetPoint(startPos, controlPoint, endPos, Mathf.Min(1f, t + 0.05f));
                    Vector3 dir = nextPoint - transform.position;
                    if (dir != Vector3.zero)
                    {
                        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
                    }
                }
            })
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                HitTarget();
            });
        }
        
        private void HitTarget()
        {
            if (_target != null && _target.IsAlive)
            {
                _target.TakeDamage(_damage);
            }
            
            _onHit?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
