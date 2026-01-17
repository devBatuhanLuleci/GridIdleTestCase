using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using GridSystemModule.Services;

namespace GridSystemModule
{
    /// <summary>
    /// Responsible for playing placement and swap animations on placed objects.
    /// Extracted from GridPlacementSystem to focus on animation logic only.
    /// </summary>
    public class AnimationPlayer
    {
        private readonly PlacementAnimationSettings _placementAnimationSettings;
        private readonly GridCoordinateConverter _coordinateConverter;

        public AnimationPlayer(PlacementAnimationSettings placementAnimationSettings, GridCoordinateConverter coordinateConverter = null)
        {
            _placementAnimationSettings = placementAnimationSettings;
            _coordinateConverter = coordinateConverter ?? ServiceLocator.Instance.GetService<GridCoordinateConverter>();
        }

        /// <summary>
        /// Plays placement animation for a newly placed object.
        /// Handles position movement, scale punch, and auto-snapped position transitions.
        /// </summary>
        public void PlayPlacementAnimation(IPlaceable placeable, Vector2Int gridPos, bool wasAutoSnappedFromInvalid)
        {
            if (placeable == null || _placementAnimationSettings == null) return;
            var mb = placeable as MonoBehaviour;
            if (mb == null || mb.transform == null) return;

            var t = mb.transform;

            // Calculate center position for multi-tile objects
            var occupiedPositions = new List<Vector2Int>();
            for (int x = 0; x < placeable.GridSize.x; x++)
            {
                for (int y = 0; y < placeable.GridSize.y; y++)
                {
                    occupiedPositions.Add(new Vector2Int(gridPos.x + x, gridPos.y + y));
                }
            }
            var target = _coordinateConverter.MultiTileGridToWorld(occupiedPositions);

            // Apply pivot offset
            Vector3 pivotOffset = CalculatePivotOffset(mb);
            target += pivotOffset;

            if (t != null)
            {
                t.DOKill(true);
            }
            else
            {
                return;
            }

            // Position animation
            if (_placementAnimationSettings.enablePositionTween)
            {
                float duration = wasAutoSnappedFromInvalid && _placementAnimationSettings.useSeparateInvalidSnap
                    ? _placementAnimationSettings.invalidSnapDuration
                    : _placementAnimationSettings.positionDuration;
                Ease ease = wasAutoSnappedFromInvalid && _placementAnimationSettings.useSeparateInvalidSnap
                    ? _placementAnimationSettings.invalidSnapEase
                    : _placementAnimationSettings.positionEase;
                float overshoot = wasAutoSnappedFromInvalid && _placementAnimationSettings.useSeparateInvalidSnap
                    ? _placementAnimationSettings.invalidSnapOvershoot
                    : _placementAnimationSettings.positionOvershoot;

                if (t != null && t.gameObject != null)
                {
                    t.DOMove(target, duration)
                        .SetEase(ease, overshoot)
                        .SetTarget(t)
                        .OnKill(() => { });
                }
            }

            // Scale punch animation
            if (_placementAnimationSettings.enableScalePunch && t != null && t.gameObject != null)
            {
                Vector3 original = t.localScale;

                t.DOPunchScale(_placementAnimationSettings.punchScale, _placementAnimationSettings.punchDuration,
                    _placementAnimationSettings.punchVibrato, _placementAnimationSettings.punchElasticity)
                    .SetTarget(t)
                    .OnComplete(() => {
                        if (t != null && t.gameObject != null)
                        {
                            t.localScale = original;
                        }
                    })
                    .OnKill(() => {
                        if (t != null && t.gameObject != null)
                        {
                            t.localScale = original;
                        }
                    });
            }
        }

        /// <summary>
        /// Plays swap animation when two objects exchange positions.
        /// </summary>
        public void PlaySwapAnimation(IPlaceable placeable, Vector3 targetWorldPos)
        {
            if (placeable == null || _placementAnimationSettings == null) return;
            if (!_placementAnimationSettings.enableSwapAnimation) return;

            var mb = placeable as MonoBehaviour;
            if (mb == null || mb.transform == null) return;

            var t = mb.transform;

            if (t != null)
            {
                t.DOKill(true);
            }
            else
            {
                return;
            }

            // Position animation
            if (t != null && t.gameObject != null)
            {
                t.DOMove(targetWorldPos, _placementAnimationSettings.swapDuration)
                    .SetEase(_placementAnimationSettings.swapEase, _placementAnimationSettings.swapOvershoot)
                    .SetTarget(t)
                    .OnKill(() => { });
            }

            // Scale punch animation
            if (_placementAnimationSettings.enableSwapScalePunch && t != null && t.gameObject != null)
            {
                Vector3 original = t.localScale;

                var originalScale = placeable.GetOriginalScale();
                if (originalScale.HasValue)
                {
                    original = originalScale.Value;
                    t.localScale = original;
                }

                t.DOPunchScale(_placementAnimationSettings.swapPunchScale, _placementAnimationSettings.swapPunchDuration,
                    _placementAnimationSettings.swapPunchVibrato, _placementAnimationSettings.swapPunchElasticity)
                    .SetTarget(t)
                    .OnComplete(() => {
                        if (t != null && t.gameObject != null)
                        {
                            t.localScale = original;
                        }
                    })
                    .OnKill(() => {
                        if (t != null && t.gameObject != null)
                        {
                            t.localScale = original;
                        }
                    });
            }
        }

        /// <summary>
        /// Calculates the pivot offset for a MonoBehaviour's sprite.
        /// Converts sprite pivot from normalized (0-1) to world space offset.
        /// </summary>
        private Vector3 CalculatePivotOffset(MonoBehaviour mb)
        {
            if (mb == null) return Vector3.zero;

            var spriteRenderer = mb.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null || spriteRenderer.sprite == null)
            {
                return Vector3.zero;
            }

            Sprite sprite = spriteRenderer.sprite;
            Vector2 pivot = sprite.pivot;
            Vector2 spriteSize = sprite.bounds.size;

            // Pivot is in pixels, convert to world space
            float pixelsPerUnit = sprite.pixelsPerUnit;
            Vector3 pivotWorldOffset = new Vector3(
                (pivot.x - spriteSize.x * pixelsPerUnit * 0.5f) / pixelsPerUnit,
                (pivot.y - spriteSize.y * pixelsPerUnit * 0.5f) / pixelsPerUnit,
                0f
            );

            return pivotWorldOffset;
        }
    }
}
