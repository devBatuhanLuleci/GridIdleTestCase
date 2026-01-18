using UnityEditor;
using UnityEngine;
using GameplayModule;

namespace GameplayModule.Editor
{
    [CustomEditor(typeof(EnemyItem2D))]
    public class EnemyItem2DEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EnemyItem2D enemy = (EnemyItem2D)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Development Tools", EditorStyles.boldLabel);

            if (GUILayout.Button("Take 1 Damage"))
            {
                enemy.TakeDamage(1);
            }

            if (GUILayout.Button("Kill Enemy"))
            {
                enemy.TakeDamage(enemy.CurrentHealth);
            }
        }
    }
}
