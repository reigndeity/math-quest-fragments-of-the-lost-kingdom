using MathQuest.Data;
using UnityEditor;

namespace MathQuest.Editor
{
    /// <summary>
    /// The inspector of a character. Draws the usual fields, but hides the ones that only belong to the
    /// player (the girl prefab) while the role is Enemy.
    /// </summary>
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CharacterResource))]
    public sealed class CharacterResourceEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty role = serializedObject.FindProperty("role");
            bool isPlayer = role.hasMultipleDifferentValues || role.enumValueIndex == (int)CharacterRole.Player;

            SerializedProperty property = serializedObject.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;

                if (property.propertyPath == "m_Script")
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(property);
                    continue;
                }

                if (property.propertyPath == "girlPrefab" && !isPlayer)
                    continue;

                EditorGUILayout.PropertyField(property, true);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
