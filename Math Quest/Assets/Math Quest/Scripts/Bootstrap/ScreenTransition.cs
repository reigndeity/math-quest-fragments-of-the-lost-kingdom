using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.Bootstrap
{
    /// <summary>
    /// The loading transition: a black circle grows from the middle of the screen until it covers everything (with
    /// a logo in the middle), whatever has to change is changed while the screen is covered, then the circle shrinks
    /// away again.
    ///
    /// Use it from anywhere:
    ///   ScreenTransition.Run(() => panel.SetActive(true));                  // change something while covered
    ///   ScreenTransition.Run(() => LoadSomething());                        // or a coroutine to wait for
    ///   SceneLoader.Replace("Battle", scene, true);                         // swap scenes behind it
    ///
    /// The transition lives in a prefab at Resources/ScreenTransition. It is created the first time it is needed and
    /// stays for the whole game, so it works from every scene, including when a scene is played on its own.
    /// </summary>
    public sealed class ScreenTransition : MonoBehaviour
    {
        private const string ResourceName = "ScreenTransition";

        private static ScreenTransition instance;

        [Header("Pieces")]
        [Tooltip("The black circle. It is scaled up until it covers the whole screen.")]
        [SerializeField] private RectTransform circle;
        [Tooltip("The logo in the middle of the circle. Leave its sprite empty to show no logo.")]
        [SerializeField] private Image logo;
        [Tooltip("The CanvasGroup on the root. It blocks taps while the transition runs.")]
        [SerializeField] private CanvasGroup blocker;

        [Header("Timing (seconds)")]
        [SerializeField, Min(0.05f)] private float coverDuration = 0.5f;
        [Tooltip("How long the screen stays covered after the change, so the new screen is ready before it is shown.")]
        [SerializeField, Min(0f)] private float holdDuration = 0.25f;
        [SerializeField, Min(0.05f)] private float revealDuration = 0.5f;

        private readonly List<Func<IEnumerator>> pending = new();
        private RectTransform canvasRect;
        private bool running;
        private bool covered;

        /// <summary>True from the moment the circle starts growing until it has fully shrunk away.</summary>
        public static bool IsRunning => instance != null && instance.running;

        public float CoverDuration => coverDuration;
        public float HoldDuration => holdDuration;
        public float RevealDuration => revealDuration;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        /// <summary>Shows the transition at one point, 0 (nothing) to 1 (covered). The editor window uses this for its preview.</summary>
        public void ShowProgress(float progress)
        {
            if (canvasRect == null)
                canvasRect = GetComponent<RectTransform>();

            Apply(progress);
        }

        /// <summary>Covers the screen, runs the action while it is covered, then uncovers.</summary>
        public static void Run(Action whileCovered)
        {
            Run(() => Wrap(whileCovered));
        }

        /// <summary>Covers the screen, runs the coroutine (and waits for it) while covered, then uncovers.</summary>
        public static void Run(Func<IEnumerator> whileCovered)
        {
            ScreenTransition transition = Get();
            if (transition == null)
            {
                // No transition prefab: do the change straight away rather than not at all.
                Execute(whileCovered);
                return;
            }

            transition.Begin(whileCovered);
        }

        private static IEnumerator Wrap(Action action)
        {
            action?.Invoke();
            yield break;
        }

        private static void Execute(Func<IEnumerator> work)
        {
            IEnumerator routine = work?.Invoke();
            if (routine == null)
                return;

            // Without a transition object there is nothing to run a coroutine on, so finish it in one go.
            while (routine.MoveNext()) { }
        }

        private static ScreenTransition Get()
        {
            if (instance == null)
            {
                ScreenTransition prefab = Resources.Load<ScreenTransition>(ResourceName);
                if (prefab == null)
                {
                    Debug.LogWarning("There is no ScreenTransition prefab in a Resources folder, so there is no transition.");
                    return null;
                }

                instance = Instantiate(prefab);
                instance.name = ResourceName;
                DontDestroyOnLoad(instance.gameObject);
            }

            return instance;
        }

        private void Awake()
        {
            canvasRect = GetComponent<RectTransform>();
            SetIdle();
        }

        private void Begin(Func<IEnumerator> work)
        {
            if (running)
            {
                // Two things asked for at once (for example a Hide and a Show on one button): they share one
                // transition. Before the screen is covered they just wait their turn; after, they run at once.
                if (!covered)
                    pending.Add(work);
                else
                    StartCoroutine(work());
                return;
            }

            pending.Add(work);
            StartCoroutine(Sequence());
        }

        private IEnumerator Sequence()
        {
            running = true;
            covered = false;
            blocker.blocksRaycasts = true;
            blocker.interactable = true;

            yield return Animate(0f, 1f, coverDuration);
            covered = true;

            // Everything that was asked for runs while nothing can be seen.
            for (int i = 0; i < pending.Count; i++)
            {
                IEnumerator routine = pending[i]?.Invoke();
                if (routine != null)
                    yield return StartCoroutine(routine);
            }
            pending.Clear();

            // Give the new screen a couple of frames to start, and a moment to be seen as ready.
            yield return null;
            yield return null;
            if (holdDuration > 0f)
                yield return new WaitForSecondsRealtime(holdDuration);

            yield return Animate(1f, 0f, revealDuration);

            covered = false;
            running = false;
            SetIdle();
        }

        private IEnumerator Animate(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                Apply(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration))));
                yield return null;
            }

            Apply(to);
        }

        // progress 0: nothing on screen. 1: the screen is covered.
        private void Apply(float progress)
        {
            bool visible = progress > 0.001f;
            circle.gameObject.SetActive(visible);
            if (!visible)
            {
                logo.gameObject.SetActive(false);
                return;
            }

            // The circle must reach the corners: its diameter has to be as long as the screen's diagonal.
            Vector2 screen = canvasRect.rect.size;
            if (screen.sqrMagnitude < 1f)
                screen = new Vector2(Screen.width, Screen.height); // the canvas has not been laid out yet
            float full = screen.magnitude / Mathf.Max(1f, circle.rect.width);
            circle.localScale = Vector3.one * (full * 1.02f * progress);

            if (logo != null)
            {
                bool hasLogo = logo.sprite != null;
                logo.gameObject.SetActive(hasLogo);
                if (hasLogo)
                {
                    // The logo appears once the circle is big enough to hold it, and goes first when it shrinks.
                    float show = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.7f, progress));
                    logo.color = new Color(1f, 1f, 1f, show);
                    logo.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.6f, 1f, show);
                }
            }
        }

        private void SetIdle()
        {
            if (circle != null) circle.gameObject.SetActive(false);
            if (logo != null) logo.gameObject.SetActive(false);
            if (blocker != null)
            {
                blocker.blocksRaycasts = false;
                blocker.interactable = false;
            }
        }
    }
}
