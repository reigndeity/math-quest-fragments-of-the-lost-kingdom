using System;
using System.Collections.Generic;
using MathQuest.Data;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace MathQuest.Editor
{
    /// <summary>
    /// Window for authoring VFX: every VFX in the project on the left, the selected one on the
    /// right with a timeline of what plays when, and the skills that use it. Open it from
    /// Math Quest > VFX Editor, or by double-clicking a VFX asset.
    /// </summary>
    public sealed class VfxEditorWindow : EditorWindow
    {
        private const string NewVfxFolder = "Assets/Math Quest/Resources/VFX";
        private const float ListWidth = 230f;
        private const float DetailMaxWidth = 620f; // with Narrow on, the editor stops growing here
        private const string NarrowKey = "MathQuest.VfxEditor.Narrow2";

        private readonly List<VfxResource> all = new();
        private readonly List<SkillResource> skills = new();
        private VfxResource selected;
        private UnityEditor.Editor selectedEditor;
        private string search = string.Empty;
        private Vector2 listScroll;
        private Vector2 detailScroll;

        // On: the editor stays 620 wide, however wide the window is. Off (default): it fills the window.
        private bool narrow;

        private float MaxWidth => narrow ? DetailMaxWidth : 100000f;

        [MenuItem("Math Quest/VFX Editor")]
        public static void Open()
        {
            var window = GetWindow<VfxEditorWindow>("VFX Editor");
            window.minSize = new Vector2(640f, 380f);
            window.Show();
        }

        [OnOpenAsset]
        private static bool OpenFromProject(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not VfxResource vfx)
                return false;

            Open();
            GetWindow<VfxEditorWindow>().Select(vfx);
            return true;
        }

        private void OnEnable()
        {
            narrow = EditorPrefs.GetBool(NarrowKey, false);
            Refresh();
            FollowSelection();
        }

        private void OnDisable()
        {
            DestroyImmediate(selectedEditor);
        }

        private void OnFocus() => Refresh();
        private void OnProjectChange() => Refresh();
        private void OnSelectionChange() => FollowSelection();

        private void FollowSelection()
        {
            if (Selection.activeObject is VfxResource vfx)
            {
                Select(vfx);
                Repaint();
            }
        }

        private void Select(VfxResource vfx)
        {
            if (selected == vfx)
                return;

            selected = vfx;
            DestroyImmediate(selectedEditor);
            selectedEditor = vfx != null ? UnityEditor.Editor.CreateEditor(vfx) : null;
            detailScroll = Vector2.zero;
        }

        private void Refresh()
        {
            all.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:VfxResource"))
            {
                var vfx = AssetDatabase.LoadAssetAtPath<VfxResource>(AssetDatabase.GUIDToAssetPath(guid));
                if (vfx != null)
                    all.Add(vfx);
            }

            all.Sort((a, b) => string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase));

            skills.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:SkillResource"))
            {
                var skill = AssetDatabase.LoadAssetAtPath<SkillResource>(AssetDatabase.GUIDToAssetPath(guid));
                if (skill != null)
                    skills.Add(skill);
            }

            if (selected == null)
                Select(null);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawList();
            DrawDetail();
            EditorGUILayout.EndHorizontal();
        }

        // ---- left: the list -----------------------------------------------------------------

        private void DrawList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(ListWidth));

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            search = GUILayout.TextField(search, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("+ New", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                CreateNew();
            EditorGUILayout.EndHorizontal();

            listScroll = EditorGUILayout.BeginScrollView(listScroll);
            foreach (VfxResource vfx in all)
            {
                if (vfx == null)
                    continue;

                if (!string.IsNullOrEmpty(search) && vfx.Id.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                bool isSelected = vfx == selected;
                var style = new GUIStyle(EditorStyles.label) { padding = new RectOffset(8, 4, 3, 3) };
                Rect row = EditorGUILayout.GetControlRect(false, 22f);

                if (isSelected)
                    EditorGUI.DrawRect(row, new Color(0.24f, 0.45f, 0.75f, 0.5f));
                else if (row.Contains(Event.current.mousePosition))
                    EditorGUI.DrawRect(row, new Color(1f, 1f, 1f, 0.06f));

                GUI.Label(row, vfx.Id, style);

                if (Event.current.type == EventType.MouseDown && row.Contains(Event.current.mousePosition))
                {
                    Select(vfx);
                    Selection.activeObject = vfx;
                    Event.current.Use();
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.LabelField($"{all.Count} VFX", EditorStyles.centeredGreyMiniLabel);
            EditorGUILayout.EndVertical();
        }

        private void CreateNew()
        {
            if (!AssetDatabase.IsValidFolder(NewVfxFolder))
            {
                Debug.LogWarning($"{NewVfxFolder} does not exist. Create it first: VFX must be in a Resources/VFX folder.");
                return;
            }

            var vfx = CreateInstance<VfxResource>();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{NewVfxFolder}/VFX_New.asset");
            AssetDatabase.CreateAsset(vfx, path);
            vfx.id = vfx.name;
            EditorUtility.SetDirty(vfx);
            AssetDatabase.SaveAssets();

            Refresh();
            Select(vfx);
            Selection.activeObject = vfx;
        }

        // ---- right: the selected VFX --------------------------------------------------------

        private void DrawDetail()
        {
            EditorGUILayout.BeginVertical();

            if (selected == null)
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField("Pick a VFX on the left, or make a new one.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();
                return;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.MaxWidth(MaxWidth));
            GUILayout.Label(AssetDatabase.GetAssetPath(selected), EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            bool newNarrow = GUILayout.Toggle(narrow, "Narrow", EditorStyles.toolbarButton, GUILayout.Width(56f));
            if (newNarrow != narrow)
            {
                narrow = newNarrow;
                EditorPrefs.SetBool(NarrowKey, narrow);
            }

            if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(40f)))
                EditorGUIUtility.PingObject(selected);
            if (GUILayout.Button("Delete", EditorStyles.toolbarButton, GUILayout.Width(50f)))
                DeleteSelected();
            EditorGUILayout.EndHorizontal();

            if (selected == null)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            detailScroll = EditorGUILayout.BeginScrollView(detailScroll);
            GUILayout.Space(4f);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(MaxWidth));

            if (!AssetDatabase.GetAssetPath(selected).Contains("/Resources/VFX/"))
            {
                EditorGUILayout.HelpBox(
                    "This VFX is not in a Resources/VFX folder, so the game will not find it. Move it there.", MessageType.Warning);
            }

            DrawTimeline(selected);
            GUILayout.Space(6f);

            if (selectedEditor != null)
                selectedEditor.OnInspectorGUI();

            DrawUsedBy(selected);

            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DeleteSelected()
        {
            if (!EditorUtility.DisplayDialog("Delete VFX", $"Move '{selected.Id}' to the trash?", "Delete", "Cancel"))
                return;

            string path = AssetDatabase.GetAssetPath(selected);
            Select(null);
            AssetDatabase.MoveAssetToTrash(path);
            Refresh();
            GUIUtility.ExitGUI(); // a dialog was shown mid-layout
        }

        // ---- timeline -----------------------------------------------------------------------

        private static void DrawTimeline(VfxResource vfx)
        {
            if (vfx.steps == null || vfx.steps.Count == 0)
                return;

            float impact = vfx.impactDelay;
            float endAt = impact + vfx.replyDelay;

            // Where each step begins and ends, in seconds from the skill starting.
            var begins = new List<float>();
            var ends = new List<float>();
            float total = endAt;
            foreach (VfxStep step in vfx.steps)
            {
                float baseTime = step.trigger switch
                {
                    VfxTrigger.Impact => impact,
                    VfxTrigger.End => endAt,
                    _ => 0f
                };

                float begin = baseTime + step.delay;
                float end = begin + (step.travel ? step.travelDuration : step.lifetime);
                begins.Add(begin);
                ends.Add(end);
                total = Mathf.Max(total, end);
            }

            total = Mathf.Max(total, 0.5f) * 1.03f;

            const float rowHeight = 16f;
            const float labelWidth = 130f;
            float height = 22f + vfx.steps.Count * (rowHeight + 2f) + 4f;
            Rect area = EditorGUILayout.GetControlRect(false, height);
            EditorGUI.DrawRect(area, new Color(0f, 0f, 0f, 0.18f));

            var track = new Rect(area.x + labelWidth, area.y + 20f, area.width - labelWidth - 8f, area.height - 24f);
            float X(float time) => track.x + track.width * (time / total);

            // Ruler.
            float tick = total > 4f ? 1f : 0.5f;
            for (float t = 0f; t <= total; t += tick)
            {
                EditorGUI.DrawRect(new Rect(X(t), track.y - 4f, 1f, track.height + 4f), new Color(1f, 1f, 1f, 0.07f));
                GUI.Label(new Rect(X(t) + 2f, area.y + 2f, 40f, 14f), t.ToString("0.#") + "s", EditorStyles.miniLabel);
            }

            // Start, impact and end markers.
            DrawMarker(track, X(0f), "Start", new Color(0.45f, 0.75f, 1f));
            DrawMarker(track, X(impact), "Impact: heart lost", new Color(1f, 0.55f, 0.45f));
            DrawMarker(track, X(endAt), "End: reply", new Color(0.7f, 0.85f, 0.5f));

            // One bar per step. Travelling steps are drawn brighter for the flight.
            for (int i = 0; i < vfx.steps.Count; i++)
            {
                VfxStep step = vfx.steps[i];
                float y = track.y + i * (rowHeight + 2f);
                Color color = step.trigger switch
                {
                    VfxTrigger.Impact => new Color(1f, 0.55f, 0.45f),
                    VfxTrigger.End => new Color(0.7f, 0.85f, 0.5f),
                    _ => new Color(0.45f, 0.75f, 1f)
                };

                string name = step.prefab != null ? step.prefab.name : "(no prefab)";
                GUI.Label(new Rect(area.x + 6f, y, labelWidth - 8f, rowHeight), name, EditorStyles.miniLabel);

                var bar = new Rect(X(begins[i]), y + 2f, Mathf.Max(3f, X(ends[i]) - X(begins[i])), rowHeight - 4f);
                EditorGUI.DrawRect(bar, new Color(color.r, color.g, color.b, step.travel ? 0.9f : 0.45f));
            }

            GUI.Label(new Rect(area.x + 6f, area.y + 2f, labelWidth, 14f), "Timeline", EditorStyles.miniBoldLabel);
        }

        private static void DrawMarker(Rect track, float x, string label, Color color)
        {
            EditorGUI.DrawRect(new Rect(x, track.y - 4f, 2f, track.height + 4f), color);
            var style = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = color } };
            GUI.Label(new Rect(x + 3f, track.yMax - 12f, 110f, 12f), label, style);
        }

        // ---- used by ------------------------------------------------------------------------

        private void DrawUsedBy(VfxResource vfx)
        {
            GUILayout.Space(10f);
            EditorGUILayout.LabelField("Used by", EditorStyles.boldLabel);

            bool any = false;
            foreach (SkillResource skill in skills)
            {
                if (skill == null || skill.vfxId != vfx.Id)
                    continue;

                any = true;
                if (GUILayout.Button(skill.name, EditorStyles.linkLabel))
                {
                    Selection.activeObject = skill;
                    EditorGUIUtility.PingObject(skill);
                }
            }

            if (!any)
                EditorGUILayout.LabelField("No skill uses this VFX yet.", EditorStyles.centeredGreyMiniLabel);
        }
    }
}
