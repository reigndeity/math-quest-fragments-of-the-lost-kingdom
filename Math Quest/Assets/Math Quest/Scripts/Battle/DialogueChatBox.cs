using Febucci.UI.Core;
using TMPro;
using UnityEngine;

namespace MathQuest.Battle
{
    /// <summary>
    /// The speech bubble prefab. Types its text with the Text Animator typewriter and can
    /// mirror itself for the player, whose bubble points the other way.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DialogueChatBox : MonoBehaviour
    {
        [SerializeField] private TypewriterCore typewriter;
        [Tooltip("The text label. It is mirrored back when the bubble is mirrored, so it never reads backwards.")]
        [SerializeField] private TMP_Text label;

        public bool IsTyping => typewriter != null && typewriter.isShowingText;

        /// <summary>Mirrors the bubble and its text sideways (scale x = -1), or restores them.</summary>
        public void SetMirrored(bool mirrored)
        {
            float x = mirrored ? -1f : 1f;
            SetScaleX((RectTransform)transform, x);
            if (label != null)
                SetScaleX(label.rectTransform, x);
        }

        public void Show(string text)
        {
            typewriter.ShowText(text);
        }

        /// <summary>Finishes the current line immediately.</summary>
        public void Skip()
        {
            typewriter.SkipTypewriter();
        }

        private static void SetScaleX(RectTransform target, float x)
        {
            Vector3 scale = target.localScale;
            scale.x = x;
            target.localScale = scale;
        }
    }
}
