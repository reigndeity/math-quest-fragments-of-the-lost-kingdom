using MathQuest.Data;
using UnityEditor;
using UnityEngine;

namespace MathQuest.Editor
{
    /// <summary>
    /// The inspector of a VFX asset. Steps are grouped under Start, Impact and End, in the order
    /// the skill plays them. Each step is a small card with what you change most (prefab, where,
    /// delay, travel); the rest is behind a "More" foldout.
    /// </summary>
    [CustomEditor(typeof(VfxResource))]
    public sealed class VfxResourceEditor : UnityEditor.Editor
    {
        private static readonly Color StartColor = new(0.45f, 0.75f, 1f);
        private static readonly Color ImpactColor = new(1f, 0.55f, 0.45f);
        private static readonly Color EndColor = new(0.7f, 0.85f, 0.5f);

        // A button press is remembered here and carried out after the list has been drawn and saved.
        private int pendingAdd = -1;
        private int pendingRemove = -1;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("id"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("impactDelay"),
                new GUIContent("Impact Delay (s)", "Seconds from the skill starting until the hit lands: a heart is lost and the Impact steps play. For a charge-up, make it as long as the charge."));

            EditorGUILayout.PropertyField(serializedObject.FindProperty("replyDelay"),
                new GUIContent("Reply Delay (s)", "Seconds after the hit before the End steps play and the reply starts."));

            SerializedProperty steps = serializedObject.FindProperty("steps");

            DrawSection(steps, VfxTrigger.Start, "START  when the skill begins", StartColor);
            DrawSection(steps, VfxTrigger.Impact, "IMPACT  when the hit lands (heart lost)", ImpactColor);
            DrawSection(steps, VfxTrigger.End, "END  after the hit, before the reply", EndColor);

            bool changed = false;
            if (pendingAdd >= 0)
            {
                AddStep(steps, (VfxTrigger)pendingAdd);
                pendingAdd = -1;
                changed = true;
            }
            else if (pendingRemove >= 0)
            {
                steps.DeleteArrayElementAtIndex(pendingRemove);
                pendingRemove = -1;
                changed = true;
            }

            serializedObject.ApplyModifiedProperties();

            // The list changed size, so end this layout pass; the next one draws the new list.
            if (changed)
                GUIUtility.ExitGUI();
        }

        private void DrawSection(SerializedProperty steps, VfxTrigger trigger, string title, Color color)
        {
            EditorGUILayout.Space(8f);

            Rect bar = EditorGUILayout.GetControlRect(false, 22f);
            EditorGUI.DrawRect(new Rect(bar.x, bar.y, 4f, bar.height), color); // just the coloured edge, no filled bar
            GUI.Label(new Rect(bar.x + 10f, bar.y + 2f, bar.width - 80f, 18f), title, EditorStyles.boldLabel);

            if (GUI.Button(new Rect(bar.xMax - 64f, bar.y + 1f, 62f, 20f), "+ Step"))
                pendingAdd = (int)trigger;

            int shown = 0;
            for (int i = 0; i < steps.arraySize; i++)
            {
                SerializedProperty step = steps.GetArrayElementAtIndex(i);
                if (step.FindPropertyRelative("trigger").enumValueIndex != (int)trigger)
                    continue;

                shown++;
                if (DrawStep(step))
                    pendingRemove = i;
            }

            if (shown == 0)
                EditorGUILayout.LabelField("Nothing here.", EditorStyles.centeredGreyMiniLabel);

        }

        private static void AddStep(SerializedProperty steps, VfxTrigger trigger)
        {
            int index = steps.arraySize;
            steps.InsertArrayElementAtIndex(index);
            SerializedProperty step = steps.GetArrayElementAtIndex(index);

            step.FindPropertyRelative("trigger").enumValueIndex = (int)trigger;
            step.FindPropertyRelative("delay").floatValue = 0f;
            step.FindPropertyRelative("prefab").objectReferenceValue = null;
            // Start effects usually begin on the caster, impact effects land on the target.
            step.FindPropertyRelative("position").enumValueIndex =
                (int)(trigger == VfxTrigger.Start ? VfxPosition.Caster : VfxPosition.Target);
            step.FindPropertyRelative("anchor").enumValueIndex = (int)VfxAnchorPoint.Center;
            step.FindPropertyRelative("travel").boolValue = false;
            step.isExpanded = true;
        }

        // Draws one card. Returns true if its remove button was pressed.
        private static bool DrawStep(SerializedProperty step)
        {
            bool remove = false;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Title row: foldout with the prefab's name, and the remove button.
            Object prefab = step.FindPropertyRelative("prefab").objectReferenceValue;
            EditorGUILayout.BeginHorizontal();
            step.isExpanded = EditorGUILayout.Foldout(step.isExpanded, prefab != null ? prefab.name : "(no prefab)", true, EditorStyles.foldoutHeader);
            if (GUILayout.Button("X", GUILayout.Width(22f)))
                remove = true;
            EditorGUILayout.EndHorizontal();

            if (step.isExpanded)
            {
                // The things you change most.
                EditorGUILayout.PropertyField(step.FindPropertyRelative("prefab"), GUIContent.none);

                EditorGUILayout.BeginHorizontal();
                Labeled("At", step.FindPropertyRelative("position"), 44f, 90f);
                Labeled(string.Empty, step.FindPropertyRelative("anchor"), 0f, 80f);
                GUILayout.FlexibleSpace();
                Labeled("Delay", step.FindPropertyRelative("delay"), 40f, 70f);
                EditorGUILayout.EndHorizontal();

                SerializedProperty travel = step.FindPropertyRelative("travel");
                EditorGUILayout.BeginHorizontal();
                float oldWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 44f;
                EditorGUILayout.PropertyField(travel, new GUIContent("Travel"), GUILayout.Width(70f));
                EditorGUIUtility.labelWidth = oldWidth;

                if (travel.boolValue)
                {
                    Labeled("to", step.FindPropertyRelative("travelTo"), 16f, 80f);
                    Labeled(string.Empty, step.FindPropertyRelative("travelAnchor"), 0f, 70f);
                    GUILayout.FlexibleSpace();
                    Labeled("in", step.FindPropertyRelative("travelDuration"), 16f, 60f);
                    GUILayout.Label("s", GUILayout.Width(10f));
                }

                EditorGUILayout.EndHorizontal();

                DrawMore(step);
            }

            EditorGUILayout.EndVertical();
            return remove;
        }

        private static void DrawMore(SerializedProperty step)
        {
            SerializedProperty more = step.FindPropertyRelative("sortingOrder"); // any child: its isExpanded is the foldout state
            more.isExpanded = EditorGUILayout.Foldout(more.isExpanded, "More", true);
            if (!more.isExpanded)
                return;

            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(step.FindPropertyRelative("trigger"), new GUIContent("Happens at"));
            EditorGUILayout.PropertyField(step.FindPropertyRelative("offset"));

            SerializedProperty rotationMode = step.FindPropertyRelative("rotationMode");
            EditorGUILayout.PropertyField(rotationMode, new GUIContent("Rotation"));
            if (rotationMode.enumValueIndex == (int)VfxRotationMode.Custom)
                EditorGUILayout.PropertyField(step.FindPropertyRelative("rotation"), new GUIContent(" "));

            SerializedProperty scaleMode = step.FindPropertyRelative("scaleMode");
            EditorGUILayout.PropertyField(scaleMode, new GUIContent("Size"));
            if (scaleMode.enumValueIndex == (int)VfxScaleMode.Multiply)
                EditorGUILayout.PropertyField(step.FindPropertyRelative("scale"), new GUIContent(" "));
            else if (scaleMode.enumValueIndex == (int)VfxScaleMode.Custom)
                EditorGUILayout.PropertyField(step.FindPropertyRelative("customScale"), new GUIContent(" "));

            EditorGUILayout.PropertyField(step.FindPropertyRelative("parentToAnchor"), new GUIContent("Follow character"));
            EditorGUILayout.PropertyField(step.FindPropertyRelative("lifetime"), new GUIContent("Lifetime (s)"));
            EditorGUILayout.PropertyField(step.FindPropertyRelative("sortingOrder"));

            EditorGUI.indentLevel--;
        }

        // A property with a small label in a fixed-width box, for packing several into one row.
        private static void Labeled(string label, SerializedProperty property, float labelWidth, float fieldWidth)
        {
            float oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = labelWidth;
            EditorGUILayout.PropertyField(property, new GUIContent(label), GUILayout.Width(labelWidth + fieldWidth));
            EditorGUIUtility.labelWidth = oldWidth;
        }
    }
}
