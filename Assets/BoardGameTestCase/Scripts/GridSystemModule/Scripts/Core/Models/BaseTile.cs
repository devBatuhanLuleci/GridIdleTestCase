using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using GridSystemModule.Core.Interfaces;
using DG.Tweening;

namespace GridSystemModule.Core.Models
{
    public abstract class BaseTile : MonoBehaviour, ITile
    {
        [SerializeField] protected string tileName = "Base Tile";
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected SpriteRenderer highlightRenderer;
        [SerializeField] protected new Collider2D collider2D;
        
        private static Sprite _runtimeHighlightSprite;
        private Tween _highlightAnimationTween;

        public string TileName => tileName;
        public Vector2 Position => transform.position;

        private List<Transform> SubscribedGridItems => GetSubscribedGridItems();

        private List<Transform> GetSubscribedGridItems()
        {
            var items = new List<Transform>();
            if (transform == null) return items;
            
            var placementSystem = BoardGameTestCase.Core.Common.ServiceLocator.Instance?.Get<IGridPlacementSystem>();
            if (placementSystem == null) return items;
            
            var gridPos = placementSystem.WorldToGrid(transform.position);
            var uniqueTransforms = new HashSet<Transform>();
            
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    Vector2Int checkPos = new Vector2Int(gridPos.x + x, gridPos.y + y);
                    var placeable = placementSystem.GetObjectAt(checkPos);
                    if (placeable != null && placeable.IsPlaced)
                    {
                        var mb = placeable as MonoBehaviour;
                        if (mb != null && mb.transform.parent == transform)
                        {
                            if (uniqueTransforms.Add(mb.transform))
                            {
                                items.Add(mb.transform);
                            }
                        }
                    }
                }
            }
            
            return items;
        }

        protected virtual void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                spriteRenderer.sortingOrder = 0;
            }
            SetupHighlightRenderer();
            if (collider2D == null)
            {
                var boxCollider = gameObject.AddComponent<BoxCollider2D>();
                boxCollider.isTrigger = false;
                boxCollider.enabled = true;
                collider2D = boxCollider;
            }
            else collider2D.enabled = true;

            UpdateColliderBounds();
            gameObject.SetActive(true);
        }

        private void UpdateColliderBounds()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null) return;
            if (collider2D is BoxCollider2D box)
            {
                var bounds = spriteRenderer.sprite.bounds;
                box.size = bounds.size;
                box.offset = bounds.center;
            }
        }

        protected virtual void Start() { }
        
        protected virtual void Update()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                CheckForMouseClick();
            }
            
            if (Touchscreen.current != null)
            {
                CheckForNewInputSystemTouch();
            }
        }
        
        private void CheckForNewInputSystemTouch()
        {
            var touches = Touchscreen.current.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var touch = touches[i];
                if (touch.press.wasPressedThisFrame)
                {
                    CheckForTouchClick(touch.position.ReadValue());
                }
            }
        }
        
        private void CheckForTouchClick(Vector2 touchPosition)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 worldPosition = cam.ScreenToWorldPoint(new Vector3(touchPosition.x, touchPosition.y, cam.nearClipPlane));
                if (collider2D != null && collider2D.OverlapPoint(worldPosition)) OnTileClicked();
            }
        }
        
        private void CheckForMouseClick()
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 worldPosition = cam.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cam.nearClipPlane));
                if (collider2D != null && collider2D.OverlapPoint(worldPosition)) OnTileClicked();
            }
        }

        public virtual void Initialize(int x, int y) { }

        public virtual void OnTileClicked() { }

        public virtual void OnTileEnter() { }

        public virtual void OnTileExit() { }

        protected virtual void OnMouseEnter() => OnTileEnter();

        protected virtual void OnMouseExit() => OnTileExit();
        
        protected virtual void OnMouseOver() { }

        protected virtual void OnMouseDown()
        {
            if (Mouse.current == null) OnTileClicked();
        }
        
        private void SetupHighlightRenderer()
        {
            Transform highlightChild = transform.Find("Highlight");
            if (highlightChild != null)
            {
                if (highlightRenderer == null)
                {
                    highlightRenderer = highlightChild.gameObject.GetComponent<SpriteRenderer>();
                    if (highlightRenderer == null)
                    {
                        highlightRenderer = highlightChild.gameObject.AddComponent<SpriteRenderer>();
                    }
                }
            }
            else if (Application.isPlaying)
            {
                GameObject highlightObj = new GameObject("Highlight");
                highlightObj.transform.SetParent(transform);
                highlightObj.transform.localPosition = Vector3.zero;
                highlightObj.transform.localScale = Vector3.one;
                highlightRenderer = highlightObj.AddComponent<SpriteRenderer>();
                
                // Copy sprite from the main tile renderer
                var tileRenderer = GetComponent<SpriteRenderer>();
                if (tileRenderer != null && tileRenderer.sprite != null)
                {
                    highlightRenderer.sprite = tileRenderer.sprite;
                }
            }
            
            if (highlightRenderer != null)
            {
                highlightRenderer.enabled = true;
                highlightRenderer.gameObject.SetActive(false);
                
                // Use sortingOrder higher than main tile to show on top
                highlightRenderer.sortingOrder = 10;
            }
        }
        
        public void ShowHighlight(Color color)
        {
            ShowHighlight(color, minAlpha: 0.6f, animationDuration: 0.5f);
        }
        
        public void ShowHighlight(Color color, float minAlpha, float animationDuration)
        {
            if (highlightRenderer == null) SetupHighlightRenderer();
            if (highlightRenderer != null)
            {
                // Kill existing tween if any
                if (_highlightAnimationTween != null && _highlightAnimationTween.IsActive())
                {
                    _highlightAnimationTween.Kill();
                }
                
                highlightRenderer.gameObject.SetActive(true);
                highlightRenderer.color = color;
                
                // Create looping alpha animation
                Color startColor = color;
                Color endColor = color;
                endColor.a = minAlpha;
                
                _highlightAnimationTween = DOTween.Sequence()
                    .Append(DOTween.To(
                        () => highlightRenderer.color,
                        x => highlightRenderer.color = x,
                        endColor,
                        animationDuration
                    ))
                    .Append(DOTween.To(
                        () => highlightRenderer.color,
                        x => highlightRenderer.color = x,
                        startColor,
                        animationDuration
                    ))
                    .SetLoops(-1, LoopType.Restart);
            }
        }
        
        public void HideHighlight()
        {
            // Kill animation
            if (_highlightAnimationTween != null && _highlightAnimationTween.IsActive())
            {
                _highlightAnimationTween.Kill();
            }
            
            if (highlightRenderer != null)
            {
                highlightRenderer.gameObject.SetActive(false);
            }
        }
      
    }
}
