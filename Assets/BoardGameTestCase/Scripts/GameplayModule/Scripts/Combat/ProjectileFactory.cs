using UnityEngine;
using BoardGameTestCase.Core.Common;
using GameModule.Core.Interfaces;

namespace GameplayModule
{
    public class ProjectileFactory : MonoBehaviour, IProjectileFactory
    {
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private Sprite _defaultProjectileSprite;
        
        private void Awake()
        {
            ServiceLocator.Instance.Register<ProjectileFactory>(this);
            ServiceLocator.Instance.Register<IProjectileFactory>(this);
        }
        
        private void OnDestroy()
        {
            ServiceLocator.Instance?.Unregister<ProjectileFactory>();
            ServiceLocator.Instance?.Unregister<IProjectileFactory>();
        }
        
        MonoBehaviour IProjectileFactory.CreateProjectile(Vector3 position, int damage, float speed, IEnemy target, Sprite sprite)
        {
            if (target is EnemyItem2D enemyItem2D)
            {
                return CreateProjectile(position, damage, speed, enemyItem2D, sprite);
            }
            return null;
        }
        
        public Projectile CreateProjectile(Vector3 position, int damage, float speed, EnemyItem2D target, Sprite sprite = null)
        {
            if (target == null || !target.IsAlive) return null;
            
            Projectile projectileInstance = null;
            SpriteRenderer spriteRenderer = null;
            
            if (_projectilePrefab != null)
            {
                projectileInstance = Instantiate(_projectilePrefab, position, Quaternion.identity);
            }
            
            if (projectileInstance == null)
            {
                GameObject projectileObj = new GameObject("Projectile");
                projectileInstance = projectileObj.AddComponent<Projectile>();
                spriteRenderer = projectileObj.AddComponent<SpriteRenderer>();
                projectileObj.transform.position = position;
            }
            
            if (projectileInstance == null) return null;
            
            if (spriteRenderer != null)
            {
                projectileInstance.SetSpriteRenderer(spriteRenderer);
            }
            
            Sprite projectileSprite = sprite != null ? sprite : _defaultProjectileSprite;
            projectileInstance.Initialize(damage, speed, target, projectileSprite);
            
            return projectileInstance;
        }
        
        public void SetProjectilePrefab(Projectile prefab)
        {
            _projectilePrefab = prefab;
        }
        
        public void SetDefaultSprite(Sprite sprite)
        {
            _defaultProjectileSprite = sprite;
        }
    }
}

