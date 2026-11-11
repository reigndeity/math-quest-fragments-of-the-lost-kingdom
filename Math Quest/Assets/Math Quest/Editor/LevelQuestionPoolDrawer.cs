using MathQuest.Data;
using UnityEditor;
using UnityEngine;

namespace MathQuest.Editor
{
    /// <summary>
    /// A level's pool entry: the pool, a toggle, and the question count only when the toggle is on.
    /// The row is titled with the pool's name instead of "Element 0".
    /// </summary>
    [CustomPropertyDrawer(typeof(LevelQuestionPool))]
    public sealed class LevelQuestionPoolDrawer : PropertyDrawer
    {
        private static readonly float Line = EditorGUIUtility.singleLineHeight;
        private const float Gap = 2f;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            int lines = property.FindPropertyRelative("overrideCount").boolValue ? 3 : 2;
            return lines * Line + (lines - 1) * Gap;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            SerializedProperty pool = property.FindPropertyRelative("pool");
            SerializedProperty overrideCount = property.FindPropertyRelative("overrideCount");
            SerializedProperty count = property.FindPropertyRelative("questionCount");

            string title = pool.objectReferenceValue != null ? pool.objectReferenceValue.name : "Pool";
            float y = position.y;

            EditorGUI.PropertyField(new Rect(position.x, y, position.width, Line), pool, new GUIContent(title));
            y += Line + Gap;

            EditorGUI.PropertyField(new Rect(position.x, y, position.width, Line), overrideCount,
                new GUIContent("Override Count", overrideCount.tooltip));
            y += Line + Gap;

            if (overrideCount.boolValue)
            {
                EditorGUI.PropertyField(new Rect(position.x, y, position.width, Line), count,
                    new GUIContent("Question Count", count.tooltip));
            }

            EditorGUI.EndProperty();
        }
    }
}
