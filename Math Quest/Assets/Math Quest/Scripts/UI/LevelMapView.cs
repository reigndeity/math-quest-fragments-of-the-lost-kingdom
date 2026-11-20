using System.Collections.Generic;
using MathQuest.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.UI
{
    /// <summary>
    /// Shows an act's level map inside a scroll area. Put it on the area the levels scroll in (for example
    /// LevelScrollArea): it adds a Scroll Rect and a Rect Mask 2D there if they are missing, and makes a
    /// "LevelMapContent" child that holds the levels. The area itself keeps its size; only the content
    /// inside it grows with the act.
    ///
    /// One Level prefab is spawned per level at the spot saved on the ActResource (laid out in
    /// Math Quest > Act Level Designer). The content is as tall as the highest level plus the act's
    /// Top Padding, so the scroll stops there. Optionally sets an Image (like CurrentActLevelBackground)
    /// to the act's background, so the same scene works for every act.
    ///
    /// Which levels are open, and their stars and best times, come from the save (SaveSystem).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class LevelMapView : MonoBehaviour
    {
        private const string ContentName = "LevelMapContent";

        [Tooltip("The act shown when the scene starts. Later the act selection screen will choose it with Show().")]
        [SerializeField] private ActResource act;
        [Tooltip("Spawned once per level.")]
        [SerializeField] private GameObject levelPrefab;
        [Tooltip("The TextMeshPro child of the Level prefab that shows the level number.")]
        [SerializeField] private string numberTextName = "CurrentLevelText";
        [Tooltip("Optional. Its sprite is set to the act's background when the act is shown (for example CurrentActLevelBackground).")]
        [SerializeField] private Image actBackground;
        [Tooltip("When the map is shown, scroll to the bottom (level 1).")]
        [SerializeField] private bool startAtBottom = true;
        [SerializeField] private float scrollSensitivity = 30f;
        [Tooltip("How far, in UI units, levels fade out at the top and bottom edges of the scroll area instead of being cut off. 0 is a hard edge.")]
        [SerializeField, Min(0)] private int edgeFade = 120;

        private readonly List<GameObject> spawned = new();
        private readonly List<LevelNode> nodes = new();
        private RectTransform rectTransform;
        private RectTransform content;

        public ActResource Act => act;

        /// <summary>The scroll area: the visible window the map scrolls in. Its size is never changed.</summary>
        public RectTransform Area => rectTransform != null ? rectTransform : rectTransform = (RectTransform)transform;

        public IReadOnlyList<GameObject> SpawnedLevels => spawned;
        public IReadOnlyList<LevelNode> Nodes => nodes;

        /// <summary>The selected level, or null. Always an unlocked or completed one.</summary>
        public LevelNode Selected { get; private set; }

        /// <summary>The selection changed. The Proceed button will listen to this.</summary>
        public event System.Action<LevelNode> SelectionChanged;

        /// <summary>An unlocked or finished level was tapped (even if it was already selected). Opens its info panel.</summary>
        public event System.Action<LevelNode> LevelClicked;

        /// <summary>A locked level was tapped. Nothing listens yet: a hook for a later shake or sound.</summary>
        public event System.Action<LevelNode> LockedLevelTapped;

        private void Start()
        {
            if (act != null)
                Show(act);
        }

        /// <summary>Clears the map and shows this act's levels.</summary>
        public void Show(ActResource actToShow, bool editorPreview = false)
        {
            act = actToShow;
            Clear();
            if (act == null)
                return;

            if (actBackground != null && act.background != null)
                actBackground.sprite = act.background;

            if (levelPrefab == null)
                return;

            RectTransform map = EnsureContent(editorPreview);
            EnsureScrolling(map);
            SizeContent(map);

            float widthScale = Area.rect.width > 0f ? Area.rect.width / ActResource.MapReferenceWidth : 1f;

            for (int i = 0; i < act.levels.Count; i++)
            {
                ActLevelSlot slot = act.levels[i];
                if (slot == null)
                    continue;

                GameObject level = Instantiate(levelPrefab, map, false);
                level.name = $"Level {i + 1}";
                if (editorPreview)
                    level.hideFlags = HideFlags.DontSave; // a preview is never saved into the scene

                var rect = (RectTransform)level.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f); // bottom middle of the map
                rect.anchoredPosition = new Vector2(slot.mapPosition.x * widthScale, slot.mapPosition.y);

                if (level.TryGetComponent(out LevelNode node))
                {
                    node.Setup(i, slot.level);
                    node.Clicked += tapped =>
                    {
                        Select(tapped);
                        LevelClicked?.Invoke(tapped);
                    };
                    node.LockedClicked += tapped => LockedLevelTapped?.Invoke(tapped);
                    nodes.Add(node);
                }
                else
                {
                    SetNumber(level.transform, i + 1);
                }

                spawned.Add(level);
            }

            ApplyProgress();
            Select(CurrentLevel());

            if (startAtBottom)
                ScrollToBottom();
        }

        /// <summary>Selects this level (and unselects the last one). Locked levels and null are ignored.</summary>
        public void Select(LevelNode node)
        {
            if (node == null || !node.CanSelect || node == Selected)
                return;

            if (Selected != null)
                Selected.SetSelected(false);

            Selected = node;
            Selected.SetSelected(true);
            SelectionChanged?.Invoke(Selected);
        }

        // Levels unlock strictly in order, from the save: a finished level shows its best stars and time, the first
        // level that is not finished (after a finished one) is unlocked, everything after is locked. A spot with no
        // level made yet is always locked.
        private void ApplyProgress()
        {
            bool previousFinished = true; // the first level is always open
            for (int i = 0; i < nodes.Count; i++)
            {
                LevelNode node = nodes[i];
                if (node.Level == null)
                {
                    node.SetProgress(LevelNodeState.Locked);
                    previousFinished = false;
                    continue;
                }

                LevelRecord record = SaveSystem.GetLevel(node.Level);
                if (record != null)
                {
                    node.SetProgress(LevelNodeState.Completed, record.stars, record.bestTime);
                    previousFinished = true;
                }
                else if (previousFinished)
                {
                    node.SetProgress(LevelNodeState.Unlocked);
                    previousFinished = false;
                }
                else
                {
                    node.SetProgress(LevelNodeState.Locked);
                }
            }
        }

        /// <summary>Reads the save again (after a win or a reset) and updates every level.</summary>
        public void RefreshProgress()
        {
            if (nodes.Count == 0)
                return;

            ApplyProgress();

            if (Selected != null && !Selected.CanSelect)
                Selected = null;

            Select(CurrentLevel());
        }

        private void OnEnable()
        {
            SaveSystem.Changed += RefreshProgress;
            RefreshProgress(); // coming back to the map after a battle
        }

        private void OnDisable()
        {
            SaveSystem.Changed -= RefreshProgress;
        }

        // The level to preselect: the newest unlocked one, or the last finished one if every level is done.
        private LevelNode CurrentLevel()
        {
            LevelNode lastCompleted = null;
            foreach (LevelNode node in nodes)
            {
                if (node.State == LevelNodeState.Unlocked)
                    return node;
                if (node.State == LevelNodeState.Completed)
                    lastCompleted = node;
            }

            return lastCompleted;
        }

        /// <summary>Removes the levels this view spawned.</summary>
        public void Clear()
        {
            foreach (GameObject level in spawned)
                DestroyObject(level);
            spawned.Clear();
            nodes.Clear();
            Selected = null;

            // A preview left over from before a script reload (no longer in the list above).
            if (!Application.isPlaying)
            {
                Transform map = transform.Find(ContentName);
                if (map != null && (map.gameObject.hideFlags & HideFlags.DontSave) != 0)
                {
                    DestroyImmediate(map.gameObject);
                    content = null;
                }
                else if (map != null)
                {
                    for (int i = map.childCount - 1; i >= 0; i--)
                    {
                        GameObject child = map.GetChild(i).gameObject;
                        if ((child.hideFlags & HideFlags.DontSave) != 0)
                            DestroyImmediate(child);
                    }
                }
            }
        }

        public void ScrollToBottom()
        {
            if (!TryGetComponent(out ScrollRect scroll))
                return;

            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = 0f;
        }

        // The child the levels live in. Made on demand; an editor preview's one is never saved.
        private RectTransform EnsureContent(bool editorPreview)
        {
            if (content == null)
            {
                Transform found = transform.Find(ContentName);
                content = found as RectTransform;
            }

            if (content == null)
            {
                var go = new GameObject(ContentName, typeof(RectTransform));
                go.layer = gameObject.layer;
                if (editorPreview)
                    go.hideFlags = HideFlags.DontSave;
                content = (RectTransform)go.transform;
                content.SetParent(transform, false);
            }

            return content;
        }

        private void EnsureScrolling(RectTransform map)
        {
            if (!TryGetComponent(out ScrollRect scroll))
            {
                scroll = gameObject.AddComponent<ScrollRect>();
                scroll.movementType = ScrollRect.MovementType.Elastic;
                scroll.inertia = true;
                scroll.scrollSensitivity = scrollSensitivity;
            }

            scroll.content = map;
            scroll.viewport = Area;
            scroll.horizontal = false;
            scroll.vertical = true;

            if (!TryGetComponent(out RectMask2D mask))
                mask = gameObject.AddComponent<RectMask2D>();
            mask.softness = new Vector2Int(0, edgeFade); // fade the top and bottom, keep the sides sharp
        }

#if UNITY_EDITOR
        // Change Edge Fade in the Inspector and see it at once.
        private void OnValidate()
        {
            if (TryGetComponent(out RectMask2D mask))
                mask.softness = new Vector2Int(0, edgeFade);
        }
#endif

        // Full width of the area, anchored to its bottom, as tall as the act's levels need (never shorter than the area).
        private void SizeContent(RectTransform map)
        {
            float height = Mathf.Max(act.MapHeight, Area.rect.height);
            map.anchorMin = new Vector2(0f, 0f);
            map.anchorMax = new Vector2(1f, 0f);
            map.pivot = new Vector2(0.5f, 0f);
            map.sizeDelta = new Vector2(0f, height);
            map.anchoredPosition = Vector2.zero;
        }

        private static void DestroyObject(GameObject target)
        {
            if (target == null)
                return;

            if (Application.isPlaying)
                Destroy(target);
            else
                DestroyImmediate(target);
        }

        private void SetNumber(Transform level, int number)
        {
            Transform label = FindDeep(level, numberTextName);
            if (label != null && label.TryGetComponent(out TMP_Text text))
                text.text = number.ToString();
        }

        private static Transform FindDeep(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;

                Transform found = FindDeep(child, childName);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
