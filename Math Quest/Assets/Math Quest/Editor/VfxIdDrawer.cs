using System.Collections.Generic;
using MathQuest.Data;
using UnityEditor;
using UnityEngine;

namespace MathQuest.Editor
{
    /// <summary>
    /// Shows a string field marked [VfxId] as a dropdown of every VfxResource in the project,
    /// so ids never have to be typed (or mistyped).
    /// </summary>
    [CustomPropertyDrawer(typeof(VfxIdAttribute))]
    public sealed class VfxIdDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.String)
            {
                EditorGUI.PropertyField(position, property, label);
                return;
            }

            var ids = new List<string> { "(none)" };
            foreach (string guid in AssetDatabase.FindAssets("t:VfxResource"))
            {
                var vfx = AssetDatabase.LoadAssetAtPath<VfxResource>(AssetDatabase.GUIDToAssetPath(guid));
                if (vfx != null)
                    ids.Add(vfx.Id);
            }

            ids.Sort(1, ids.Count - 1, System.StringComparer.OrdinalIgnoreCase);

            string current = property.stringValue;
            int index = string.IsNullOrEmpty(current) ? 0 : ids.IndexOf(current);
            if (index < 0)
            {
                // The id points at nothing: keep it visible so it can be fixed.
                ids.Add(current + "  (missing)");
                index = ids.Count - 1;
            }

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();
            int picked = EditorGUI.Popup(position, label.text, index, ids.ToArray());
            if (EditorGUI.EndChangeCheck() && picked != index)
                property.stringValue = picked == 0 ? string.Empty : ids[picked];
            EditorGUI.EndProperty();
        }
    }
}
