using System.Collections.Generic;
using MathQuest.Data;
using MathQuest.UI;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MathQuest.Editor
{
    /// <summary>
    /// The Act Level Designer: every act on the left, its level map in the middle, the selected level
    /// on the right. The layout is saved on the ActResource, so the scene only needs a LevelMapView
    /// that is told which act to show. Open it from Math Quest > Act Level Designer, or by
    /// double-clicking an Act asset.
    ///
    /// On the map: drag a level to move it, double-click empty space to add one, Delete removes the
    /// selected one. The red line is where the scroll stops.
    /// </summary>
    public sealed class ActLevelDesignerWindow : EditorWindow
    {
        private const float ListWidth = 180f;
        private const float PanelWidth = 270f;
        private const float Pad = 40f;            // space around the map inside the canvas
        private const float NodeRadius = 80f;     // the Level prefab is 160 wide
        private static readonly Vector2 FallbackArea = new(1080f, 1220f); // used when no Level Map View is in the open scene
        private const float Headroom = 1000f;     // empty canvas above the scroll limit, room to add levels
        private const string ZoomKey = "MathQuest.ActLevelDesigner.Zoom";
        private const string SnapKey = "MathQuest.ActLevelDesigner.Snap";
        private const string LiveKey = "MathQuest.ActLevelDesigner.Live";

        private readonly List<ActResource> acts = new();
        private ActResource act;
        private SerializedObject serializedAct;
        private int selectedSlot = -1;

        private Vector2 listScroll, mapScroll, panelScroll;
        private float zoom = 0.3f;
        private bool snap = true;
        private float snapSize = 20f;
        private bool livePreview = true;
        private bool scrollToBottomNextPaint = true;

        private bool dragging;
        private Vector2 dragOffset;    // map units between the mouse and the node's centre
        private float frozenHeight;    // canvas height held still during a drag, so the map does not jump

        private static float HalfWidth => ActResource.MapReferenceWidth / 2f;

        // The scroll area the map is shown in, at its size in the open scene (the Game view's size),
        // so the canvas here is exactly what the game shows. x positions are stored for a 1080-wide
        // map and stretched to the area's width, as LevelMapView does.
        private Vector2 area = FallbackArea;
        private bool areaFromScene;
        private float AreaScale => area.x / ActResource.MapReferenceWidth;

        [MenuItem("Math Quest/Act Level Designer")]
        public static void Open()
        {
            var window = GetWindow<ActLevelDesignerWindow>("Act Level Designer");
            window.minSize = new Vector2(760f, 420f);
            window.Show();
        }

        [OnOpenAsset]
        private static bool OpenFromProject(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not ActResource actResource)
                return false;

            Open();
            GetWindow<ActLevelDesignerWindow>().Select(actResource);
            return true;
        }

        private void OnEnable()
        {
            zoom = EditorPrefs.GetFloat(ZoomKey, 0.3f);
            snap = EditorPrefs.GetBool(SnapKey, true);
            livePreview = EditorPrefs.GetBool(LiveKey, true);
            Undo.undoRedoPerformed += OnUndoRedo;
            Refresh();
            if (act == null && acts.Count > 0)
                Select(acts[0]);
        }

        private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

        private void OnFocus() => Refresh();
        private void OnProjectChange() => Refresh();

        private void OnUndoRedo()
        {
            serializedAct?.Update();
            if (act != null && selectedSlot >= act.levels.Count)
                selectedSlot = act.levels.Count - 1;
            UpdateLivePreview();
            Repaint();
        }

        private void Refresh()
        {
            acts.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:ActResource"))
            {
                var found = AssetDatabase.LoadAssetAtPath<ActResource>(AssetDatabase.GUIDToAssetPath(guid));
                if (found != null)
                    acts.Add(found);
            }

            acts.Sort((a, b) => a.act.CompareTo(b.act));
            Repaint();
        }

        private void Select(ActResource actResource)
        {
            act = actResource;
            serializedAct = act != null ? new SerializedObject(act) : null;
            selectedSlot = -1;
            dragging = false;
            scrollToBottomNextPaint = true;
            Repaint();
        }

        // ---------------------------------------------------------------- layout

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawActList();

            if (act == null)
            {
                EditorGUILayout.HelpBox("Pick an act on the left, or create one with Create > Math Quest > Resources > Act Resource.", MessageType.Info);
                EditorGUILayout.EndHorizontal();
                return;
            }

            serializedAct.Update();

            EditorGUILayout.BeginVertical();
            DrawToolbar();
            EditorGUILayout.BeginHorizontal();
            DrawMap();
            DrawPanel();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawActList()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(ListWidth), GUILayout.ExpandHeight(true));
            EditorGUILayout.LabelField("Acts", EditorStyles.boldLabel);
            listScroll = EditorGUILayout.BeginScrollView(listScroll);

            foreach (ActResource item in acts)
            {
                string label = string.IsNullOrEmpty(item.displayName) ? item.name : item.displayName;
                var style = new GUIStyle(EditorStyles.label) { fontStyle = item == act ? FontStyle.Bold : FontStyle.Normal };
                Rect row = EditorGUILayout.GetControlRect(false, 22f);
                if (item == act)
                    EditorGUI.DrawRect(row, new Color(0.24f, 0.48f, 0.9f, 0.35f));
                if (GUI.Button(row, $"{label}   ({item.levels.Count})", style))
                    Select(item);
            }

            EditorGUILayout.EndScrollView();

            if (act != null && GUILayout.Button("Ping Asset"))
                EditorGUIUtility.PingObject(act);
            EditorGUILayout.EndVertical();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("Zoom", GUILayout.Width(38f));
            EditorGUI.BeginChangeCheck();
            zoom = GUILayout.HorizontalSlider(zoom, 0.12f, 0.7f, GUILayout.Width(110f));
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetFloat(ZoomKey, zoom);

            GUILayout.Space(10f);
            EditorGUI.BeginChangeCheck();
            snap = GUILayout.Toggle(snap, new GUIContent("Snap", "Dragged levels land on a grid of this many UI units."), EditorStyles.toolbarButton, GUILayout.Width(44f));
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(SnapKey, snap);
            using (new EditorGUI.DisabledScope(!snap))
                snapSize = Mathf.Max(1f, EditorGUILayout.FloatField(snapSize, EditorStyles.toolbarTextField, GUILayout.Width(36f)));

            GUILayout.Space(10f);
            if (GUILayout.Button(new GUIContent("Add Level", "Adds the next level above the last one."), EditorStyles.toolbarButton, GUILayout.Width(70f)))
                AddSlot(NextPosition());
            if (GUILayout.Button(new GUIContent("Bottom", "Scrolls the map to level 1."), EditorStyles.toolbarButton, GUILayout.Width(52f)))
                scrollToBottomNextPaint = true;

            GUILayout.FlexibleSpace();
            GUILayout.Label($"{act.levels.Count} levels   map height {act.MapHeight:0}   area {area.x:0} x {area.y:0}{(areaFromScene ? "" : " (default: no Level Map View in the scene)")}", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();
        }

        // ---------------------------------------------------------------- the map

        private float CanvasHeight => dragging ? frozenHeight : Mathf.Max(act.MapHeight, area.y) + Headroom;

        private Vector2 MapToGui(Vector2 map, float height) =>
            new(Pad + (map.x * AreaScale + area.x / 2f) * zoom, Pad + (height - map.y) * zoom);

        private Vector2 GuiToMap(Vector2 gui, float height) =>
            new(((gui.x - Pad) / zoom - area.x / 2f) / AreaScale, height - (gui.y - Pad) / zoom);

        private void DrawMap()
        {
            if (Event.current.type == EventType.Layout && !dragging)
                ReadArea();

            float height = CanvasHeight;
            var content = new Rect(0f, 0f, area.x * zoom + Pad * 2f, height * zoom + Pad * 2f);

            Rect view = GUILayoutUtility.GetRect(10f, 100000f, 10f, 100000f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            EditorGUI.DrawRect(view, new Color(0.13f, 0.13f, 0.13f));

            if (scrollToBottomNextPaint && Event.current.type == EventType.Repaint)
            {
                mapScroll.y = Mathf.Max(0f, content.height - view.height);
                scrollToBottomNextPaint = false;
            }

            // The act background stays still behind the scrolling map, like CurrentActLevelBackground in the game.
            var window = new Rect(view.x + Pad - mapScroll.x, view.y + Pad, area.x * zoom, Mathf.Min(area.y * zoom, view.height - Pad * 2f));
            if (Event.current.type == EventType.Repaint)
                PaintFixedBackground(window);

            mapScroll = GUI.BeginScrollView(view, mapScroll, content);
            var mapRect = new Rect(Pad, Pad, area.x * zoom, height * zoom);

            if (Event.current.type == EventType.Repaint)
                PaintMap(mapRect, height);

            HandleMapInput(mapRect, height);
            GUI.EndScrollView();
        }

        private void PaintMap(Rect mapRect, float height)
        {
            // The scroll area: see-through, the fixed background shows behind it.
            EditorGUI.DrawRect(mapRect, new Color(1f, 1f, 1f, act.background != null ? 0.03f : 0.06f));

            // Grid every 100 units, and a labelled line every scroll area height (one screenful of map).
            for (float y = 0f; y <= height; y += 100f)
            {
                float gy = MapToGui(new Vector2(0f, y), height).y;
                EditorGUI.DrawRect(new Rect(mapRect.x, gy, mapRect.width, 1f), new Color(1f, 1f, 1f, 0.05f));
            }

            for (int screen = 1; screen * area.y <= height; screen++)
            {
                float gy = MapToGui(new Vector2(0f, screen * area.y), height).y;
                EditorGUI.DrawRect(new Rect(mapRect.x, gy, mapRect.width, 2f), new Color(0.5f, 0.8f, 1f, 0.6f));
                GUI.Label(new Rect(mapRect.x + 4f, gy, 160f, 16f), $"screen {screen}", MiniLabel(new Color(0.5f, 0.8f, 1f)));
            }

            // The area's sides.
            EditorGUI.DrawRect(new Rect(mapRect.x - 1f, mapRect.y, 1f, mapRect.height), new Color(1f, 1f, 1f, 0.3f));
            EditorGUI.DrawRect(new Rect(mapRect.xMax, mapRect.y, 1f, mapRect.height), new Color(1f, 1f, 1f, 0.3f));

            EditorGUI.DrawRect(new Rect(mapRect.center.x, mapRect.y, 1f, mapRect.height), new Color(1f, 1f, 1f, 0.08f));

            // Scroll limit.
            if (act.levels.Count > 0)
            {
                float limit = MapToGui(new Vector2(0f, act.MapHeight), height).y;
                EditorGUI.DrawRect(new Rect(mapRect.x, limit - 1f, mapRect.width, 3f), new Color(1f, 0.3f, 0.3f, 0.9f));
                GUI.Label(new Rect(mapRect.x + 4f, limit - 17f, 220f, 16f), "scroll limit (top of the map)", MiniLabel(new Color(1f, 0.45f, 0.45f)));
            }

            // Path between levels.
            var points = new List<Vector3>();
            foreach (ActLevelSlot slot in act.levels)
            {
                if (slot != null)
                    points.Add(MapToGui(slot.mapPosition, height));
            }

            if (points.Count > 1)
            {
                Handles.color = new Color(1f, 0.85f, 0.3f, 0.8f);
                Handles.DrawAAPolyLine(Mathf.Max(2f, 14f * zoom), points.ToArray());
            }

            // Levels.
            float radius = NodeRadius * zoom;
            var numberStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(56f * zoom), 9, 40),
                normal = { textColor = Color.white }
            };

            for (int i = 0; i < act.levels.Count; i++)
            {
                ActLevelSlot slot = act.levels[i];
                if (slot == null)
                    continue;

                Vector2 c = MapToGui(slot.mapPosition, height);
                bool selected = i == selectedSlot;
                Color fill = slot.level == null ? new Color(0.45f, 0.45f, 0.45f) : new Color(0.2f, 0.6f, 0.75f);

                Handles.color = selected ? new Color(1f, 0.85f, 0.2f) : new Color(0f, 0f, 0f, 0.8f);
                Handles.DrawSolidDisc(c, Vector3.forward, radius + (selected ? 4f : 2f));
                Handles.color = fill;
                Handles.DrawSolidDisc(c, Vector3.forward, radius);

                GUI.Label(new Rect(c.x - radius, c.y - radius, radius * 2f, radius * 2f), (i + 1).ToString(), numberStyle);

                string caption = slot.level != null ? slot.level.name : "empty";
                GUI.Label(new Rect(c.x - 90f, c.y + radius + 1f, 180f, 16f), caption,
                    new GUIStyle(MiniLabel(slot.level != null ? Color.white : new Color(0.75f, 0.75f, 0.75f))) { alignment = TextAnchor.UpperCenter });
            }
        }

        private void HandleMapInput(Rect mapRect, float height)
        {
            Event e = Event.current;
            int control = GUIUtility.GetControlID(FocusType.Keyboard);

            switch (e.GetTypeForControl(control))
            {
                case EventType.MouseDown when e.button == 0:
                {
                    int hit = HitTest(e.mousePosition, height);
                    if (hit >= 0)
                    {
                        selectedSlot = hit;
                        dragging = true;
                        frozenHeight = height;
                        dragOffset = act.levels[hit].mapPosition - GuiToMap(e.mousePosition, height);
                        Undo.RecordObject(act, "Move Level");
                        GUIUtility.hotControl = control;
                    }
                    else if (e.clickCount == 2 && mapRect.Contains(e.mousePosition))
                    {
                        AddSlot(Snap(GuiToMap(e.mousePosition, height)));
                    }
                    else if (mapRect.Contains(e.mousePosition))
                    {
                        selectedSlot = -1;
                    }

                    GUI.FocusControl(null); // leave any text field in the side panel
                    GUIUtility.keyboardControl = control; // so Delete reaches the map
                    e.Use();
                    break;
                }

                case EventType.MouseDrag when GUIUtility.hotControl == control && dragging && selectedSlot >= 0:
                {
                    Vector2 position = Snap(GuiToMap(e.mousePosition, height) + dragOffset);
                    position.x = Mathf.Clamp(position.x, -HalfWidth, HalfWidth);
                    position.y = Mathf.Max(0f, position.y);
                    act.levels[selectedSlot].mapPosition = position;
                    EditorUtility.SetDirty(act);
                    UpdateLivePreview();
                    e.Use();
                    break;
                }

                case EventType.MouseUp when GUIUtility.hotControl == control:
                    GUIUtility.hotControl = 0;
                    dragging = false;
                    serializedAct.Update();
                    e.Use();
                    break;

                case EventType.KeyDown when GUIUtility.keyboardControl == control && selectedSlot >= 0
                                            && (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace):
                    RemoveSlot(selectedSlot);
                    e.Use();
                    break;
            }
        }

        private int HitTest(Vector2 mouse, float height)
        {
            float radius = NodeRadius * zoom + 3f;
            for (int i = act.levels.Count - 1; i >= 0; i--) // topmost drawn first
            {
                if (act.levels[i] != null && Vector2.Distance(mouse, MapToGui(act.levels[i].mapPosition, height)) <= radius)
                    return i;
            }

            return -1;
        }

        private Vector2 Snap(Vector2 position) =>
            snap ? new Vector2(Mathf.Round(position.x / snapSize) * snapSize, Mathf.Round(position.y / snapSize) * snapSize) : position;

        // One step up from the last level, on the other side of the middle.
        private Vector2 NextPosition()
        {
            if (act.levels.Count == 0)
                return new Vector2(0f, 300f);

            Vector2 last = act.levels[^1].mapPosition;
            float x = Mathf.Abs(last.x) < 1f ? 220f : -last.x;
            return new Vector2(x, last.y + 300f);
        }

        // ---------------------------------------------------------------- the side panel

        private void DrawPanel()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(PanelWidth), GUILayout.ExpandHeight(true));
            panelScroll = EditorGUILayout.BeginScrollView(panelScroll);

            if (selectedSlot >= 0 && selectedSlot < act.levels.Count)
                DrawSelectedSlot();
            else
                EditorGUILayout.HelpBox("Drag a level to move it. Double-click empty space to add one. Delete removes the selected level.", MessageType.None);

            EditorGUILayout.Space(8f);
            DrawActSettings();
            EditorGUILayout.Space(8f);
            DrawSceneTools();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private void DrawSelectedSlot()
        {
            EditorGUILayout.LabelField($"Level {selectedSlot + 1}", EditorStyles.boldLabel);
            SerializedProperty slot = serializedAct.FindProperty("levels").GetArrayElementAtIndex(selectedSlot);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(slot.FindPropertyRelative("level"), new GUIContent("Level"));
            EditorGUILayout.PropertyField(slot.FindPropertyRelative("mapPosition"), new GUIContent("Position"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedAct.ApplyModifiedProperties();
                UpdateLivePreview();
            }

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(selectedSlot == 0))
            {
                if (GUILayout.Button(new GUIContent("◀ Earlier", "Swaps this level's resource with the one before it. The spots on the map stay.")))
                    SwapLevels(selectedSlot, selectedSlot - 1);
            }

            using (new EditorGUI.DisabledScope(selectedSlot == act.levels.Count - 1))
            {
                if (GUILayout.Button(new GUIContent("Later ▶", "Swaps this level's resource with the one after it. The spots on the map stay.")))
                    SwapLevels(selectedSlot, selectedSlot + 1);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Delete Level"))
            {
                RemoveSlot(selectedSlot);
                GUIUtility.ExitGUI(); // the list changed: start a fresh layout pass
            }
        }

        private void DrawActSettings()
        {
            EditorGUILayout.LabelField("Act", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedAct.FindProperty("displayName"));
            EditorGUILayout.PropertyField(serializedAct.FindProperty("background"));
            EditorGUILayout.PropertyField(serializedAct.FindProperty("mapTopPadding"), new GUIContent("Top Padding"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedAct.ApplyModifiedProperties();
                UpdateLivePreview();
            }
        }

        private void DrawSceneTools()
        {
            EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
            LevelMapView view = FindView();

            if (view == null)
            {
                EditorGUILayout.HelpBox("No Level Map View in the open scene. Add one to the scrolling content (CurrentActLevelBackground) to preview there.", MessageType.Info);
                return;
            }

            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.ObjectField("Level Map View", view, typeof(LevelMapView), true);

            EditorGUI.BeginChangeCheck();
            livePreview = EditorGUILayout.ToggleLeft(new GUIContent("Live preview", "While previewing, the scene follows your edits here."), livePreview);
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(LiveKey, livePreview);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Preview In Scene", "Spawns this act's levels into the scene. They are never saved with it.")))
                Preview(view);
            if (GUILayout.Button("Clear Preview"))
                view.Clear();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox("The scroll area keeps its size. The levels go in a LevelMapContent child it makes, which grows with the act. A Scroll Rect and a Rect Mask 2D are added to the area if missing.", MessageType.None);
        }

        // ---------------------------------------------------------------- edits

        private void AddSlot(Vector2 position)
        {
            Undo.RecordObject(act, "Add Level");
            act.levels.Add(new ActLevelSlot { mapPosition = position });
            selectedSlot = act.levels.Count - 1;
            Changed();
        }

        private void RemoveSlot(int index)
        {
            Undo.RecordObject(act, "Delete Level");
            act.levels.RemoveAt(index);
            selectedSlot = Mathf.Min(selectedSlot, act.levels.Count - 1);
            Changed();
        }

        private void SwapLevels(int a, int b)
        {
            Undo.RecordObject(act, "Reorder Levels");
            (act.levels[a].level, act.levels[b].level) = (act.levels[b].level, act.levels[a].level);
            selectedSlot = b;
            Changed();
        }

        private void Changed()
        {
            EditorUtility.SetDirty(act);
            serializedAct.Update();
            UpdateLivePreview();
            Repaint();
        }

        // ---------------------------------------------------------------- scene

        private static LevelMapView FindView() =>
            FindFirstObjectByType<LevelMapView>(FindObjectsInactive.Include);

        private void Preview(LevelMapView view)
        {
            Undo.RecordObject(view, "Preview Act");
            view.Show(act, true);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
        }

        private void UpdateLivePreview()
        {
            if (!livePreview || Application.isPlaying)
                return;

            LevelMapView view = FindView();
            if (view != null && view.Act == act && view.SpawnedLevels.Count > 0)
                view.Show(act, true);
        }

        private void ReadArea()
        {
            LevelMapView view = FindView();
            Vector2 size = view != null ? view.Area.rect.size : Vector2.zero;
            areaFromScene = size.x > 1f && size.y > 1f;
            area = areaFromScene ? size : FallbackArea;
        }

        // Fills the window like a stretched Image would (CurrentActLevelBackground).
        private void PaintFixedBackground(Rect window)
        {
            if (act.background == null)
                return;

            Sprite sprite = act.background;
            Texture2D texture = sprite.texture;
            Rect r = sprite.textureRect;
            var uv = new Rect(r.x / texture.width, r.y / texture.height, r.width / texture.width, r.height / texture.height);

            GUI.DrawTextureWithTexCoords(window, texture, uv);
            EditorGUI.DrawRect(window, new Color(0f, 0f, 0f, 0.3f)); // dim so the levels read
        }

        private static GUIStyle MiniLabel(Color color) =>
            new(EditorStyles.miniBoldLabel) { normal = { textColor = color } };
    }
}
