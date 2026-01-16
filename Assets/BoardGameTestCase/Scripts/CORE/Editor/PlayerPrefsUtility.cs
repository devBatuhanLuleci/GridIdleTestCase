#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace BoardGameTestCase.Core.Editor
{
    public static class PlayerPrefsUtility
    {
        [MenuItem("Tools/Delete All PlayerPrefs", false, 100)]
        public static void DeleteAllPlayerPrefs()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete All PlayerPrefs",
                "Are you sure you want to delete all PlayerPrefs? This action cannot be undone.",
                "Delete",
                "Cancel"
            );

            if (confirmed)
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                Debug.Log("All PlayerPrefs have been deleted.");
                EditorUtility.DisplayDialog(
                    "PlayerPrefs Deleted",
                    "All PlayerPrefs have been successfully deleted.",
                    "OK"
                );
            }
        }
    }
}
#endif

