using MathQuest.Data;
using UnityEditor;
using UnityEngine;

namespace MathQuest.Editor
{
    /// <summary>
    /// Shows only what matters for a skill. With a VFX chosen, everything else is hidden because
    /// the VFX decides it. Without one, the simple single-prefab fields appear, and the
    /// rotation/scale fields only show when they are switched on.
    /// </summary>
    [CustomEditor(typeof(SkillResource))]
    public sealed class SkillResourceEditor : UnityEditor.Editor
    {
        private bool showOverrides;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            SerializedProperty vfxId = serializedObject.FindProperty("vfxId");
            EditorGUILayout.PropertyField(vfxId);

            if (!string.IsNullOrEmpty(vfxId.stringValue))
            {
                EditorGUILayout.HelpBox(
                    "Timing, placement and effects come from this VFX. Open it to change them.", MessageType.None);

                if (GUILayout.Button("Select this VFX"))
                    PingVfx(vfxId.stringValue);
            }
            else
            {
                DrawSimpleEffect();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawSimpleEffect()
        {
            EditorGUILayout.Space(4f);
            SerializedProperty prefab = serializedObject.FindProperty("vfxPrefab");
            EditorGUILayout.PropertyField(prefab);

            if (prefab.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox(
                    "No effect: the hit still lands. Pick a VFX above for something more than a single prefab.", MessageType.None);
                return;
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("placement"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("anchor"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("impactDelay"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lifetime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sortingOrder"));

            showOverrides = EditorGUILayout.Foldout(showOverrides, "Overrides (rotation, size, offset)", true);
            if (!showOverrides)
                return;

            EditorGUI.indentLevel++;

            SerializedProperty overrideRotation = serializedObject.FindProperty("overrideRotation");
            EditorGUILayout.PropertyField(overrideRotation);
            if (overrideRotation.boolValue)
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rotation"));

            SerializedProperty overrideScale = serializedObject.FindProperty("overrideScale");
            EditorGUILayout.PropertyField(overrideScale);
            EditorGUILayout.PropertyField(serializedObject.FindProperty(overrideScale.boolValue ? "customScale" : "scale"));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("positionOffset"));

            EditorGUI.indentLevel--;
        }

        private static void PingVfx(string id)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:VfxResource"))
            {
                var vfx = AssetDatabase.LoadAssetAtPath<VfxResource>(AssetDatabase.GUIDToAssetPath(guid));
                if (vfx != null && vfx.Id == id)
                {
                    Selection.activeObject = vfx;
                    EditorGUIUtility.PingObject(vfx);
                    return;
                }
            }
        }
    }
}
