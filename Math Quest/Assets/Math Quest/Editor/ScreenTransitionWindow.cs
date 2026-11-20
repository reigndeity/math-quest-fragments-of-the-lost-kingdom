using System.Collections.Generic;
using MathQuest.Bootstrap;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.Editor
{
    /// <summary>
    /// Math Quest > Screen Transition. One place to change how the loading transition looks and how long it takes,
    /// to preview it, and to switch it on or off for every place that can use it in the open scenes.
    /// </summary>
    public sealed class ScreenTransitionWindow : EditorWindow
    {
        private const string PrefabPath = "Assets/Math Quest/Resources/ScreenTransition.prefab";

        private GameObject prefab;
        private ScreenTransition transition;
        private Vector2 scroll;
        private bool showPlaces = true;
        private string filter = string.Empty;
        private readonly Dictionary<string, bool> foldouts = new();

        // Edit-mode preview.
        private GameObject previewObject;
        private ScreenTransition previewTransition;
        private double previewStart;
        private bool previewHoldCovered;

        [MenuItem("Math Quest/Screen Transition")]
        public static void Open()
        {
            ScreenTransitionWindow window = GetWindow<ScreenTransitionWindow>("Screen Transition");
            window.minSize = new Vector2(380f, 420f);
        }

        private void OnEnable() => Load();

        private void OnDisable() => StopPreview();

        private void Load()
        {
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            transition = prefab != null ? prefab.GetComponent<ScreenTransition>() : null;
        }

        private void OnGUI()
        {
            if (transition == null)
            {
                Load();
                EditorGUILayout.HelpBox("The transition prefab was not found at " + PrefabPath + ".", MessageType.Warning);
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawLook();
            EditorGUILayout.Space(8);
            DrawTiming();
            EditorGUILayout.Space(8);
            DrawPreview();
            EditorGUILayout.Space(8);
            DrawPlaces();

            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------ look
        private void DrawLook()
        {
            EditorGUILayout.LabelField("Look", EditorStyles.boldLabel);

            var serialized = new SerializedObject(transition);
            var logo = serialized.FindProperty("logo").objectReferenceValue as Image;
            var circle = serialized.FindProperty("circle").objectReferenceValue as RectTransform;
            Image circleImage = circle != null ? circle.GetComponent<Image>() : null;

            if (logo != null)
            {
                EditorGUI.BeginChangeCheck();
                Sprite sprite = (Sprite)EditorGUILayout.ObjectField(
                    new GUIContent("Logo", "Shown in the middle of the circle. Leave empty for no logo."), logo.sprite, typeof(Sprite), false);
                float size = EditorGUILayout.Slider(new GUIContent("Logo size"), logo.rectTransform.sizeDelta.x, 100f, 900f);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(logo, "Change transition logo");
                    Undo.RecordObject(logo.rectTransform, "Change transition logo");
                    logo.sprite = sprite;
                    logo.rectTransform.sizeDelta = new Vector2(size, size);
                    Save(logo, logo.rectTransform);
                }
            }

            if (circleImage != null)
            {
                EditorGUI.BeginChangeCheck();
                Color color = EditorGUILayout.ColorField(new GUIContent("Circle colour"), circleImage.color, false, false, false);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(circleImage, "Change transition colour");
                    circleImage.color = new Color(color.r, color.g, color.b, 1f);
                    Save(circleImage);
                }
            }
        }

        // ---------------------------------------------------------------- timing
        private void DrawTiming()
        {
            EditorGUILayout.LabelField("Timing (seconds)", EditorStyles.boldLabel);

            var serialized = new SerializedObject(transition);
            serialized.Update();
            EditorGUI.BeginChangeCheck();
            Slider(serialized, "coverDuration", "Circle grows", 0.1f, 2f, "How long the circle takes to cover the screen.");
            Slider(serialized, "holdDuration", "Stays covered", 0f, 2f, "How long the screen stays covered after the change, so the new screen is ready.");
            Slider(serialized, "revealDuration", "Circle shrinks", 0.1f, 2f, "How long the circle takes to uncover the screen.");
            if (EditorGUI.EndChangeCheck())
            {
                serialized.ApplyModifiedProperties();
                Save(transition);
            }
        }

        private static void Slider(SerializedObject serialized, string property, string label, float min, float max, string tooltip)
        {
            SerializedProperty p = serialized.FindProperty(property);
            p.floatValue = EditorGUILayout.Slider(new GUIContent(label, tooltip), p.floatValue, min, max);
        }

        // --------------------------------------------------------------- preview
        private void DrawPreview()
        {
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Plays in the Game view. Keep the Game view visible.", MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Play transition", GUILayout.Height(26f)))
                    StartPreview(false);

                if (GUILayout.Button(previewObject != null && previewHoldCovered ? "Release" : "Show covered", GUILayout.Height(26f)))
                {
                    if (previewObject != null && previewHoldCovered)
                        StopPreview();
                    else
                        StartPreview(true);
                }
            }
        }

        private void StartPreview(bool holdCovered)
        {
            StopPreview();

            if (Application.isPlaying)
            {
                // In Play mode the real thing runs.
                if (!holdCovered)
                    ScreenTransition.Run(() => { });
                return;
            }

            previewObject = Instantiate(prefab);
            previewObject.hideFlags = HideFlags.HideAndDontSave;
            previewTransition = previewObject.GetComponent<ScreenTransition>();
            previewHoldCovered = holdCovered;
            previewStart = EditorApplication.timeSinceStartup;
            EditorApplication.update += TickPreview;
            TickPreview();
        }

        private void TickPreview()
        {
            if (previewObject == null || previewTransition == null)
            {
                StopPreview();
                return;
            }

            float t = (float)(EditorApplication.timeSinceStartup - previewStart);
            float cover = previewTransition.CoverDuration;
            float hold = previewTransition.HoldDuration;
            float reveal = previewTransition.RevealDuration;

            float progress;
            if (t < cover)
                progress = Mathf.SmoothStep(0f, 1f, t / cover);
            else if (previewHoldCovered || t < cover + hold)
                progress = 1f;
            else if (t < cover + hold + reveal)
                progress = Mathf.SmoothStep(1f, 0f, (t - cover - hold) / reveal);
            else
            {
                StopPreview();
                return;
            }

            previewTransition.ShowProgress(progress);
            InternalEditorRepaint();
        }

        private void StopPreview()
        {
            EditorApplication.update -= TickPreview;
            if (previewObject != null)
                DestroyImmediate(previewObject);

            previewObject = null;
            previewTransition = null;
            previewHoldCovered = false;
            InternalEditorRepaint();
            Repaint();
        }

        private static void InternalEditorRepaint() => UnityEditorInternal.InternalEditorUtility.RepaintAllViews();

        // ----------------------------------------------------------------- places
        private void DrawPlaces()
        {
            showPlaces = EditorGUILayout.Foldout(showPlaces, "Where it plays (open scenes)", true, EditorStyles.foldoutHeader);
            if (!showPlaces)
                return;

            EditorGUILayout.HelpBox(
                "Every button, panel or script in the open scenes that has a Use Transition tickbox. Tick the ones that should play the circle.",
                MessageType.None);
            filter = EditorGUILayout.TextField("Search", filter);

            // Group the places by the kind of component they are.
            var groups = new SortedDictionary<string, List<SerializedObject>>();
            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour == null || behaviour.gameObject.scene.rootCount == 0 || EditorUtility.IsPersistent(behaviour))
                    continue;

                var so = new SerializedObject(behaviour);
                if (so.FindProperty("useTransition") == null)
                    continue;

                string path = PathOf(behaviour.transform);
                if (!string.IsNullOrEmpty(filter) && path.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0
                    && behaviour.GetType().Name.IndexOf(filter, System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                string key = behaviour.GetType().Name;
                if (!groups.TryGetValue(key, out List<SerializedObject> list))
                    groups[key] = list = new List<SerializedObject>();
                list.Add(so);
            }

            if (groups.Count == 0)
            {
                EditorGUILayout.LabelField("Nothing found. Open the Menu or Battle scene.");
                return;
            }

            foreach (KeyValuePair<string, List<SerializedObject>> group in groups)
            {
                int on = 0;
                foreach (SerializedObject so in group.Value)
                    if (so.FindProperty("useTransition").boolValue) on++;

                foldouts.TryGetValue(group.Key, out bool open);
                using (new EditorGUILayout.HorizontalScope())
                {
                    open = EditorGUILayout.Foldout(open, group.Key + "  (" + on + " of " + group.Value.Count + " on)", true);
                    if (GUILayout.Button("All on", GUILayout.Width(60f))) SetAll(group.Value, true);
                    if (GUILayout.Button("All off", GUILayout.Width(60f))) SetAll(group.Value, false);
                }
                foldouts[group.Key] = open;

                if (!open)
                    continue;

                EditorGUI.indentLevel++;
                foreach (SerializedObject so in group.Value)
                {
                    so.Update();
                    SerializedProperty flag = so.FindProperty("useTransition");
                    var component = (MonoBehaviour)so.targetObject;

                    EditorGUI.BeginChangeCheck();
                    bool value = EditorGUILayout.ToggleLeft(PathOf(component.transform) + DescribeAction(so), flag.boolValue);
                    if (EditorGUI.EndChangeCheck())
                    {
                        flag.boolValue = value;
                        so.ApplyModifiedProperties();
                        EditorSceneManager.MarkSceneDirty(component.gameObject.scene);
                    }
                }
                EditorGUI.indentLevel--;
            }
        }

        // Panel Toggle Buttons: say whether the button shows or hides which panel.
        private static string DescribeAction(SerializedObject so)
        {
            SerializedProperty target = so.FindProperty("target");
            SerializedProperty action = so.FindProperty("action");
            if (target == null || action == null || target.objectReferenceValue == null)
                return string.Empty;

            return "   [" + action.enumDisplayNames[action.enumValueIndex] + " " + target.objectReferenceValue.name + "]";
        }

        private static void SetAll(List<SerializedObject> list, bool value)
        {
            foreach (SerializedObject so in list)
            {
                so.Update();
                so.FindProperty("useTransition").boolValue = value;
                so.ApplyModifiedProperties();
                EditorSceneManager.MarkSceneDirty(((MonoBehaviour)so.targetObject).gameObject.scene);
            }
        }

        private static string PathOf(Transform t)
        {
            string path = t.name;
            int depth = 0;
            while (t.parent != null && depth++ < 3)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }

            return path;
        }

        private static void Save(params Object[] changed)
        {
            foreach (Object obj in changed)
                EditorUtility.SetDirty(obj);

            AssetDatabase.SaveAssets();
        }
    }

    /// <summary>Adds a button to the transition prefab's Inspector that opens the window.</summary>
    [CustomEditor(typeof(ScreenTransition))]
    public sealed class ScreenTransitionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open the Screen Transition window", GUILayout.Height(28f)))
                ScreenTransitionWindow.Open();

            EditorGUILayout.Space(4);
            DrawDefaultInspector();
        }
    }
}
