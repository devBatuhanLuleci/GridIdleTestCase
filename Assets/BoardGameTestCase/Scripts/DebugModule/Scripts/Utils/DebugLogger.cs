using UnityEngine;

namespace DebugModule.Utils
{
    /// <summary>
    /// Centralized debug logging utility
    /// </summary>
    public static class DebugLogger
    {
        [Header("Debug Settings")]
        private static bool _enableLogging = true;
        private static bool _enablePlacementLogs = true;
        private static bool _enableDragLogs = true;
        private static bool _enableTileLogs = true;
        
        /// <summary>
        /// Enable or disable all logging
        /// </summary>
        public static bool EnableLogging
        {
            get => _enableLogging;
            set => _enableLogging = value;
        }
        
        /// <summary>
        /// Enable or disable placement-related logs
        /// </summary>
        public static bool EnablePlacementLogs
        {
            get => _enablePlacementLogs;
            set => _enablePlacementLogs = value;
        }
        
        /// <summary>
        /// Enable or disable drag-related logs
        /// </summary>
        public static bool EnableDragLogs
        {
            get => _enableDragLogs;
            set => _enableDragLogs = value;
        }
        
        /// <summary>
        /// Enable or disable tile-related logs
        /// </summary>
        public static bool EnableTileLogs
        {
            get => _enableTileLogs;
            set => _enableTileLogs = value;
        }
        
        /// <summary>
        /// Log placement-related information
        /// </summary>
        public static void LogPlacement(string message)
        {
        }
        
        /// <summary>
        /// Log drag-related information
        /// </summary>
        public static void LogDrag(string message)
        {
        }
        
        /// <summary>
        /// Log tile-related information
        /// </summary>
        public static void LogTile(string message)
        {
        }
        
        /// <summary>
        /// Log tile-related information in green for highlight-related messages
        /// </summary>
        public static void LogTileGreen(string message)
        {
        }
        
        /// <summary>
        /// Log general information
        /// </summary>
        public static void Log(string message)
        {
        }
        
        /// <summary>
        /// Log warning messages
        /// </summary>
        public static void LogWarning(string message)
        {
        }
        
        /// <summary>
        /// Log error messages
        /// </summary>
        public static void LogError(string message)
        {
        }
        
        /// <summary>
        /// Log coordinate information with detailed breakdown
        /// </summary>
        public static void LogCoordinates(string context, Vector3 worldPos, Vector2Int gridPos, Vector2 screenPos)
        {
        }
        
        /// <summary>
        /// Log placement validation results
        /// </summary>
        public static void LogPlacementValidation(string context, bool isValid, string reason = "")
        {
        }
    }
}
