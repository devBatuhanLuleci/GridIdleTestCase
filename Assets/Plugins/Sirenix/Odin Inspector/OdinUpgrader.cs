using UnityEngine;
using UnityEditor;
using System.IO;

namespace Sirenix.OdinInspector
{
    /// <summary>
    /// Odin Inspector upgrader utility
    /// </summary>
    public static class OdinUpgrader
    {
        [MenuItem("Tools/Odin Inspector/Upgrade Odin Inspector")]
        public static void UpgradeOdinInspector()
        {
            Debug.Log("Odin Inspector upgrade process started...");
            
            // Check if Odin Inspector is properly installed
            if (IsOdinInspectorInstalled())
            {
                Debug.Log("Odin Inspector is already properly installed.");
                return;
            }
            
            Debug.LogWarning("Odin Inspector installation may be incomplete. Please reimport the Odin Inspector package from the Asset Store.");
        }
        
        private static bool IsOdinInspectorInstalled()
        {
            // Check for key Odin Inspector files
            string[] requiredFiles = {
                "Assets/Plugins/Sirenix/Assemblies/Sirenix.OdinInspector.Attributes.dll",
                "Assets/Plugins/Sirenix/Assemblies/Sirenix.OdinInspector.Editor.dll"
            };
            
            foreach (string file in requiredFiles)
            {
                if (!File.Exists(file))
                {
                    return false;
                }
            }
            
            return true;
        }
    }
}
