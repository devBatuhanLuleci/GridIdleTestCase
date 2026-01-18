using UnityEngine;

namespace BoardGameTestCase.Core
{
    /// <summary>
    /// Comprehensive static helper for Bezier curve calculations and utilities.
    /// Supports Linear, Quadratic, and Cubic Bezier curves.
    /// </summary>
    public static class BezierUtils
    {
        #region Point Calculations

        /// <summary>
        /// Calculates a point on a Linear Bezier curve (Lerp).
        /// </summary>
        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, float t)
        {
            return Vector3.Lerp(p0, p1, t);
        }

        /// <summary>
        /// Calculates a point on a Quadratic Bezier curve.
        /// </summary>
        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return oneMinusT * oneMinusT * p0 +
                   2f * oneMinusT * t * p1 +
                   t * t * p2;
        }

        /// <summary>
        /// Calculates a point on a Cubic Bezier curve.
        /// </summary>
        public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return oneMinusT * oneMinusT * oneMinusT * p0 +
                   3f * oneMinusT * oneMinusT * t * p1 +
                   3f * oneMinusT * t * t * p2 +
                   t * t * t * p3;
        }

        #endregion

        #region First Derivative (Velocity)

        /// <summary>
        /// Calculates the velocity (first derivative) at t on a Quadratic Bezier curve.
        /// </summary>
        public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            return 2f * (1f - t) * (p1 - p0) +
                   2f * t * (p2 - p1);
        }

        /// <summary>
        /// Calculates the velocity (first derivative) at t on a Cubic Bezier curve.
        /// </summary>
        public static Vector3 GetFirstDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            t = Mathf.Clamp01(t);
            float oneMinusT = 1f - t;
            return 3f * oneMinusT * oneMinusT * (p1 - p0) +
                   6f * oneMinusT * t * (p2 - p1) +
                   3f * t * t * (p3 - p2);
        }

        #endregion

        #region Helper Utilities

        /// <summary>
        /// Gets the tangent (normalized direction) at t on a Quadratic Bezier curve.
        /// </summary>
        public static Vector3 GetTangent(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            return GetFirstDerivative(p0, p1, p2, t).normalized;
        }

        /// <summary>
        /// Gets the tangent (normalized direction) at t on a Cubic Bezier curve.
        /// </summary>
        public static Vector3 GetTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            return GetFirstDerivative(p0, p1, p2, p3, t).normalized;
        }

        /// <summary>
        /// Approximates the length of a Quadratic Bezier curve using a set number of samples.
        /// </summary>
        public static float ApproximateLength(Vector3 p0, Vector3 p1, Vector3 p2, int samples = 10)
        {
            float length = 0;
            Vector3 lastPoint = p0;
            for (int i = 1; i <= samples; i++)
            {
                Vector3 currentPoint = GetPoint(p0, p1, p2, i / (float)samples);
                length += Vector3.Distance(lastPoint, currentPoint);
                lastPoint = currentPoint;
            }
            return length;
        }

        /// <summary>
        /// Calculates an automatic control point for a quadratic curve between start and end.
        /// Useful for "arcing" movements.
        /// </summary>
        public static Vector3 GetAutomaticControlPoint(Vector3 start, Vector3 end, float heightOffset, Vector3 upAxis)
        {
            Vector3 midPoint = (start + end) * 0.5f;
            return midPoint + upAxis * heightOffset;
        }

        #endregion
    }
}
