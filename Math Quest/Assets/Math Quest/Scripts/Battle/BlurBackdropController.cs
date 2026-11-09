using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>
    /// Blurred backdrop behind the answer panel. The whole battle is UI on an overlay
    /// canvas, so a shader cannot sample what is behind it. Instead this takes a
    /// snapshot of the screen just before the panel appears, shrinks it, shows it on a
    /// full-screen RawImage with the Blur shader, and fades the effect between 0 and 1.
    ///
    /// The overlay colour (for example black at 50% alpha) is set here, not on the RawImage.
    ///
    /// Use: yield return Capture(); then show the panel, then FadeIn().
    /// Put this on an object that is always active (not on the panel itself).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BlurBackdropController : MonoBehaviour
    {
        [Tooltip("Full-screen RawImage, the first child of AnswerPanel so it sits behind everything else. Its Color is an overlay on the blur, for example black at 50% alpha to darken it.")]
        [SerializeField] private RawImage image;
        [Tooltip("The MathQuest/UI/Blur shader (the Blur file in Shaders).")]
        [SerializeField] private Shader shader;
        [Tooltip("How much the snapshot is shrunk before blurring. Higher is smoother and cheaper.")]
        [SerializeField, Range(1, 8)] private int downsample = 4;
        [Tooltip("Blur size at full strength, in pixels of the shrunken snapshot.")]
        [SerializeField, Min(0f)] private float blurRadius = 10f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.4f;
        [Tooltip("Colour laid over the blur once it has fully faded in. The alpha is how strongly it covers: black at 50% darkens the blur by half. Overrides the RawImage's own Color.")]
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.5f);

        /// <summary>How long a fade takes, so callers can wait for FadeOut to finish.</summary>
        public float FadeDuration => fadeDuration;

        private static readonly int AmountId = Shader.PropertyToID("_Amount");
        private static readonly int RadiusId = Shader.PropertyToID("_Radius");

        private Material material;
        private RenderTexture blurred;
        private Texture2D snapshot;
        private int builtDownsample;
        private Coroutine fade;
        private float amount;

        private void Awake()
        {
            EnsureInitialized();
        }

        // Safe to call more than once. Also covers the case where a method runs before
        // Awake did, for example from the component's context menu.
        private void EnsureInitialized()
        {
            if (material != null)
                return;

            if (image == null || shader == null)
            {
                Debug.LogError("BlurBackdropController needs both the Image and the Shader assigned.", this);
                return;
            }

            material = new Material(shader);
            image.material = material;
            material.SetFloat(RadiusId, blurRadius);
            image.color = overlayColor;
            SetAmount(0f);
            image.enabled = false;
        }

        // Lets Blur Radius and Downsample be tweaked live in the Inspector while the blur shows.
        private void Update()
        {
            if (material == null)
                return;

            material.SetFloat(RadiusId, blurRadius);
            image.color = overlayColor;

            if (snapshot != null && downsample != builtDownsample)
                RebuildBlurred();
        }

        private void OnDestroy()
        {
            if (material != null) Destroy(material);
            if (snapshot != null) Destroy(snapshot);
            ReleaseBlurred();
        }

        /// <summary>Takes the snapshot. Call this while the answer panel is still hidden.</summary>
        public IEnumerator Capture()
        {
            EnsureInitialized();
            if (material == null) yield break;

            StopFade();
            image.enabled = false;
            SetAmount(0f);

            // Let a frame render without the backdrop, so a retake never captures the old blur,
            // then wait until that frame is complete.
            yield return null;
            yield return new WaitForEndOfFrame();

            if (snapshot != null) Destroy(snapshot);
            snapshot = EnsureSrgb(ScreenCapture.CaptureScreenshotAsTexture());
            snapshot.filterMode = FilterMode.Bilinear;

            RebuildBlurred();
        }

        // Shrinking first makes the blur smooth with few samples.
        private void RebuildBlurred()
        {
            ReleaseBlurred();
            blurred = new RenderTexture(
                Mathf.Max(1, snapshot.width / downsample),
                Mathf.Max(1, snapshot.height / downsample),
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Graphics.Blit(snapshot, blurred);
            builtDownsample = downsample;

            image.texture = blurred;
        }

        // A screenshot already holds display-ready colours. In a Linear colour space Unity can
        // treat the captured texture as raw data, which brightens everything a second time
        // (a washed-out, whitish blur). Re-uploading it as an sRGB texture avoids that.
        private static Texture2D EnsureSrgb(Texture2D source)
        {
            if (QualitySettings.activeColorSpace != ColorSpace.Linear || source.isDataSRGB)
                return source;

            var copy = new Texture2D(source.width, source.height, source.format, false, false);
            copy.LoadRawTextureData(source.GetRawTextureData<byte>());
            copy.Apply(false);
            Destroy(source);
            return copy;
        }

        private void ReleaseBlurred()
        {
            if (blurred == null)
                return;

            blurred.Release();
            Destroy(blurred);
            blurred = null;
        }

        public void FadeIn()
        {
            EnsureInitialized();
            if (material == null) return;

            image.enabled = true;
            StartFade(1f, disableWhenDone: false);
        }

        public void FadeOut()
        {
            EnsureInitialized();
            if (material == null) return;

            StartFade(0f, disableWhenDone: true);
        }

        private void StartFade(float target, bool disableWhenDone)
        {
            StopFade();
            fade = StartCoroutine(FadeRoutine(target, disableWhenDone));
        }

        private void StopFade()
        {
            if (fade != null) StopCoroutine(fade);
            fade = null;
        }

        private IEnumerator FadeRoutine(float target, bool disableWhenDone)
        {
            float from = amount;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetAmount(Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, elapsed / fadeDuration)));
                yield return null;
            }

            SetAmount(target);
            if (disableWhenDone) image.enabled = false;
            fade = null;
        }

        private void SetAmount(float value)
        {
            amount = value;
            material.SetFloat(AmountId, value);
        }

        [ContextMenu("Test: capture and fade in")]
        private void TestFadeIn()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Press Play first. The snapshot can only be taken while the game runs.", this);
                return;
            }

            StartCoroutine(TestRoutine());
        }

        [ContextMenu("Test: fade out")]
        private void TestFadeOut()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Press Play first.", this);
                return;
            }

            FadeOut();
        }

        private IEnumerator TestRoutine()
        {
            yield return Capture();
            FadeIn();
        }
    }
}
