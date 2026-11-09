using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Febucci.UI;
using Febucci.UI.Core;
using TexDrawLib;
using TexDrawLib.Core;
using TMPro;
using UnityEngine;

namespace MathQuest.Battle
{
    public enum ChatBoxGrow { Bottom, Middle, Top }
    public enum ChatBoxSide { Left, Middle, Right }

    /// <summary>
    /// How a speech bubble types and sizes itself to its line. Lives on the
    /// BattleDialogueController so it can be tuned live while a bubble is on screen.
    /// Sizes are in canvas units.
    /// </summary>
    [Serializable]
    public sealed class ChatBoxSettings
    {
        [Header("Typing")]
        [Tooltip("Seconds per letter. Higher is slower.")]
        [Min(0.001f)] public float secondsPerLetter = 0.035f;
        [Tooltip("Pause after . ! ? (half of this after , ; :). Plain lines only.")]
        [Min(0f)] public float pauseAfterPunctuation = 0.3f;

        [Tooltip("On: equations type glyph by glyph like the rest of the line. Off: each equation appears whole as a block.")]
        public bool typeEquations = true;

        [Header("Font size")]
        [Min(1f)] public float maxFontSize = 52f;
        [Min(1f)] public float minFontSize = 28f;
        [Tooltip("How much smaller each attempt is.")]
        [Min(0.5f)] public float fontStep = 1f;

        [Header("Bubble height")]
        [Tooltip("The bubble never gets shorter than this, however short the line is.")]
        [Min(0f)] public float minBubbleHeight = 140f;
        [Tooltip("The tallest the bubble may get. Long lines shrink the font before they grow past this.")]
        [Min(0f)] public float maxBubbleHeight = 300f;
        [Tooltip("Which edge stays put while the height changes. Bottom keeps the bubble's tail where it is.")]
        public ChatBoxGrow growFrom = ChatBoxGrow.Bottom;

        [Header("Bubble width")]
        [Tooltip("Off: the bubble is always Max Bubble Width wide and only its height adapts. On: a short line also narrows the bubble to fit the text, down to Min Bubble Width.")]
        public bool fitWidth = false;
        [Min(0f)] public float minBubbleWidth = 160f;
        [Min(0f)] public float maxBubbleWidth = 600f;
        [Tooltip("Which side stays put while the width changes. Right keeps the tail in place on the enemy bubble (the player bubble is mirrored automatically).")]
        public ChatBoxSide growWidthFrom = ChatBoxSide.Right;

        public (float, float, float, float, float, float, float, ChatBoxGrow, bool, float, float, ChatBoxSide) Key =>
            (secondsPerLetter, pauseAfterPunctuation, maxFontSize, minFontSize, fontStep,
             minBubbleHeight, maxBubbleHeight, growFrom, fitWidth, minBubbleWidth, maxBubbleWidth, growWidthFrom);
    }

    /// <summary>
    /// The speech bubble prefab. Plain lines type with the Text Animator typewriter. Lines
    /// containing math ($...$) show in the TEXDraw label instead: words come out letter by
    /// letter and each equation appears at once as a block. The whole line is laid out once,
    /// up front, so every letter appears exactly where it will stay. The bubble sizes itself
    /// to the whole line before typing starts, and can mirror itself for the player, whose
    /// bubble points the other way.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DialogueChatBox : MonoBehaviour
    {
        [Header("Plain text")]
        [SerializeField] private TypewriterCore typewriter;
        [Tooltip("The text label. It is mirrored back when the bubble is mirrored, so it never reads backwards.")]
        [SerializeField] private TMP_Text label;

        [Header("Math text")]
        [Tooltip("TEXDraw label, laid out like the text label. Used for any line containing $...$. Leave empty to type everything with the plain label.")]
        [SerializeField] private TEXDraw mathLabel;
        [Tooltip("Seconds per letter while a math line types. Only used when no chat box settings are given; normally the BattleDialogueController sets the speed.")]
        [SerializeField, Min(0.001f)] private float mathCharDelay = 0.02f;
        [Tooltip("Pause after each equation block appears.")]
        [SerializeField, Min(0f)] private float mathBlockPause = 0.15f;

        private const string HiddenColor = "\\color{#00000000}";

        private Coroutine mathRoutine;

        // Math-line reveal. The line is parsed by TEXDraw once; letters are then made visible by
        // changing their alpha, so the layout never changes while typing.
        private int mathCut;                  // characters of the line revealed so far
        private int[] plainBefore;            // plainBefore[i] = pixel-text letters before index i
        private int plainTotal;               // pixel-text letters in the whole line
        private readonly List<CharAtom> plainAtoms = new List<CharAtom>();
        private bool warnedNoLetters;
        private bool warnedNoGlyphs;

        // Glyph-by-glyph equations. The line is split into plain and equation segments in the
        // source text, and the same segments are found in TEXDraw's layout; glyphs of a segment
        // are then faded in as that segment's characters are typed.
        private struct SourceSegment { public int start, end, units; public bool isEquation; }
        private struct Fade { public CharAtom letter; public FractionAtom bar; }

        private static readonly FieldInfo FractionColor =
            typeof(FractionAtom).GetField("color", BindingFlags.Instance | BindingFlags.NonPublic);

        private bool? glyphMode;              // null until decided for the current line
        private bool[] typeable;              // characters that cost typing time
        private int[] unitBefore;             // typeable characters before index i, within its segment
        private readonly List<SourceSegment> sourceSegments = new List<SourceSegment>();
        private readonly List<List<Fade>> treeSegments = new List<List<Fade>>();
        private CanvasGroup mathGroup;        // hides the whole math label while TEXDraw settles

        private string currentText;
        private bool usingMath;
        private ChatBoxSettings fit;

        // The bubble's edges as laid out in the prefab, so resizing can keep one edge fixed.
        private bool designRead;
        private float designBottom, designMiddle, designTop;
        private float designLeft, designCenter, designRight;

        /// <summary>True from Show until the line has finished typing or was skipped.</summary>
        public bool IsTyping { get; private set; }

        /// <summary>Fires once when the current line finishes typing, or is skipped.</summary>
        public event Action Typed;

        private void Awake()
        {
            typewriter.onTextShowed.AddListener(OnPlainTyped);
            if (mathLabel != null)
            {
                mathGroup = mathLabel.GetComponent<CanvasGroup>();
                if (mathGroup == null)
                    mathGroup = mathLabel.gameObject.AddComponent<CanvasGroup>();
                mathGroup.interactable = false;
                mathGroup.blocksRaycasts = false;
                mathLabel.gameObject.SetActive(false);
            }
        }

        // TEXDraw sometimes parses the line again by itself (for example when the font texture is
        // rebuilt), which replaces its letter objects. Look them up again every frame while typing.
        private void LateUpdate()
        {
            if (usingMath && mathRoutine != null)
                SetMathReveal(mathCut);
        }

        /// <summary>Mirrors the bubble and its text sideways (scale x = -1), or restores them.</summary>
        public void SetMirrored(bool mirrored)
        {
            float x = mirrored ? -1f : 1f;
            SetScaleX((RectTransform)transform, x);
            if (label != null)
                SetScaleX(label.rectTransform, x);
            if (mathLabel != null)
                SetScaleX(mathLabel.rectTransform, x);
        }

        /// <summary>Shows a line, sized to fit with the given settings (may be null for no fitting).</summary>
        public void Show(string text, ChatBoxSettings fitSettings = null)
        {
            StopMath();
            fit = fitSettings;

            bool hasMath = mathLabel != null && !string.IsNullOrEmpty(text) && text.Contains('$');
            label.gameObject.SetActive(!hasMath);
            if (mathLabel != null)
                mathLabel.gameObject.SetActive(hasMath);

            currentText = text;
            usingMath = hasMath;
            mathCut = 0;
            if (mathGroup != null)
                mathGroup.alpha = 1f;
            glyphMode = null;
            if (hasMath)
            {
                BuildPlainCounts(text);
                BuildSegments(text);
            }

            if (string.IsNullOrEmpty(text))
            {
                IsTyping = false;
                return;
            }

            ApplySpeed();
            Refit(); // size the bubble to the whole line before typing starts

            IsTyping = true;
            if (hasMath)
                mathRoutine = StartCoroutine(TypeMath(text));
            else
                typewriter.ShowText(text);
        }

        /// <summary>Resizes the bubble and font again, for example after the settings changed.</summary>
        public void Refit()
        {
            if (fit == null || string.IsNullOrEmpty(currentText))
                return;

            ApplySpeed();

            RectTransform root = (RectTransform)transform;
            ReadDesign(root);

            RectTransform textRect = usingMath ? mathLabel.rectTransform : label.rectTransform;
            Vector2 inset = -textRect.sizeDelta; // the text is stretched inside the bubble

            // Always find the font size and height at the widest the bubble may be.
            float maxWidth = Mathf.Max(fit.minBubbleWidth, fit.maxBubbleWidth);
            ApplyWidth(root, maxWidth);

            float textWidth = maxWidth - inset.x;
            float maxBubble = Mathf.Max(fit.minBubbleHeight, fit.maxBubbleHeight);
            float maxText = maxBubble - inset.y;
            float minFont = Mathf.Min(fit.minFontSize, fit.maxFontSize);

            float chosen = minFont;
            float bubbleHeight = maxBubble;

            if (usingMath)
                mathLabel.overflow = Overflow.Visible; // measure the real size, do not shrink

            for (float size = fit.maxFontSize; size >= minFont; size -= fit.fontStep)
            {
                float needed = Measure(size, textWidth);
                if (needed <= maxText)
                {
                    chosen = size;
                    bubbleHeight = needed + inset.y;
                    break;
                }
            }

            // A line that fits on one row can also narrow the bubble to the text.
            if (fit.fitWidth)
            {
                float natural = NaturalWidth(chosen) + inset.x + 2f; // +2 so rounding never wraps the line
                if (natural < maxWidth)
                    ApplyWidth(root, Mathf.Clamp(natural, fit.minBubbleWidth, maxWidth));
            }

            ApplyFont(chosen);
            ApplyHeight(root, Mathf.Clamp(bubbleHeight, fit.minBubbleHeight, maxBubble));

            // Measuring re-parsed the line, so put the reveal back where it was.
            if (usingMath)
                SetMathReveal(mathCut);
        }

        /// <summary>Finishes the current line immediately.</summary>
        public void Skip()
        {
            if (!IsTyping)
                return;

            if (mathRoutine != null)
            {
                StopMath();
                if (mathGroup != null)
                    mathGroup.alpha = 1f;
                SetMathReveal(currentText.Length);
                Finish();
                return;
            }

            typewriter.SkipTypewriter(); // fires onTextShowed, which finishes the line
        }

        // Height the whole line needs at this font size, at the given text width.
        private float Measure(float size, float width)
        {
            if (usingMath)
            {
                string keep = mathLabel.text;
                mathLabel.text = currentText;
                mathLabel.size = size;
                mathLabel.CalculateLayoutInputHorizontal();
                mathLabel.CalculateLayoutInputVertical();
                float height = mathLabel.preferredHeight;
                mathLabel.text = keep;
                return height;
            }

            label.enableAutoSizing = false;
            label.fontSize = size;
            return label.GetPreferredValues(currentText, width, float.PositiveInfinity).y;
        }

        // Width of the whole line on a single row at this font size.
        private float NaturalWidth(float size)
        {
            if (usingMath)
            {
                string keep = mathLabel.text;
                mathLabel.text = currentText;
                mathLabel.size = size;
                mathLabel.CalculateLayoutInputHorizontal();
                float width = mathLabel.preferredWidth;
                mathLabel.text = keep;
                return width;
            }

            label.enableAutoSizing = false;
            label.fontSize = size;
            return label.GetPreferredValues(currentText, float.PositiveInfinity, float.PositiveInfinity).x;
        }

        private void ApplyWidth(RectTransform root, float width)
        {
            float pivotX = fit.growWidthFrom == ChatBoxSide.Left ? 0f : fit.growWidthFrom == ChatBoxSide.Middle ? 0.5f : 1f;
            float edge = fit.growWidthFrom == ChatBoxSide.Left ? designLeft
                       : fit.growWidthFrom == ChatBoxSide.Middle ? designCenter
                       : designRight;

            // The edge is measured in the bubble's own space. A mirrored bubble (scale x = -1)
            // has that space flipped relative to its parent, so the position flips with it.
            float mirror = root.localScale.x < 0f ? -1f : 1f;

            root.pivot = new Vector2(pivotX, root.pivot.y);
            root.anchoredPosition = new Vector2(edge * mirror, root.anchoredPosition.y);
            root.sizeDelta = new Vector2(width, root.sizeDelta.y);
        }

        private void ApplySpeed()
        {
            if (fit != null && typewriter is TypewriterByCharacter byCharacter)
            {
                byCharacter.waitForNormalChars = fit.secondsPerLetter;
                byCharacter.waitLong = fit.pauseAfterPunctuation;
                byCharacter.waitMiddle = fit.pauseAfterPunctuation * 0.5f;
            }
        }

        private void ApplyFont(float size)
        {
            if (usingMath)
            {
                mathLabel.size = size;
                mathLabel.overflow = Overflow.BestFit; // safety net if the minimum size still does not fit
            }
            else
            {
                label.enableAutoSizing = false;
                label.fontSize = size;
            }
        }

        private void ApplyHeight(RectTransform root, float height)
        {
            float pivotY = fit.growFrom == ChatBoxGrow.Bottom ? 0f : fit.growFrom == ChatBoxGrow.Middle ? 0.5f : 1f;
            float edge = fit.growFrom == ChatBoxGrow.Bottom ? designBottom
                       : fit.growFrom == ChatBoxGrow.Middle ? designMiddle
                       : designTop;

            root.pivot = new Vector2(root.pivot.x, pivotY);
            root.anchoredPosition = new Vector2(root.anchoredPosition.x, edge);
            root.sizeDelta = new Vector2(root.sizeDelta.x, height);
        }

        private void ReadDesign(RectTransform root)
        {
            if (designRead)
                return;

            designRead = true;
            float height = root.rect.height;
            designBottom = root.anchoredPosition.y - root.pivot.y * height;
            designMiddle = designBottom + height * 0.5f;
            designTop = designBottom + height;

            float width = root.rect.width;
            designLeft = root.anchoredPosition.x - root.pivot.x * width;
            designCenter = designLeft + width * 0.5f;
            designRight = designLeft + width;
        }

        private void OnPlainTyped()
        {
            // Ignore the signal unless the typewriter has really stopped.
            if (typewriter.isShowingText)
                return;

            Finish();
        }

        private void Finish()
        {
            if (!IsTyping)
                return;

            IsTyping = false;
            Typed?.Invoke();
        }

        private void BuildPlainCounts(string text)
        {
            plainBefore = new int[text.Length + 1];
            bool inMath = false;
            int count = 0;
            for (int i = 0; i < text.Length; i++)
            {
                plainBefore[i] = count;
                char c = text[i];
                if (c == '$')
                {
                    inMath = !inMath;
                    continue;
                }

                if (!inMath && !char.IsWhiteSpace(c))
                    count++;
            }

            plainBefore[text.Length] = count;
            plainTotal = count;
        }

        // Shows the first `cut` characters of the line. Equations that are not revealed yet are
        // hidden by one transparent-colour command placed right before the first of them (always
        // between a space and a '$', so it never splits a word). Pixel-text letters are hidden
        // one by one by their alpha, which does not touch the layout.
        private void SetMathReveal(int cut)
        {
            if (mathLabel == null || currentText == null)
                return;

            mathCut = Mathf.Clamp(cut, 0, currentText.Length);

            if (glyphMode == null)
                DecideGlyphMode();

            if (glyphMode == true)
            {
                if (mathLabel.text != currentText)
                    mathLabel.text = currentText; // never changes while typing, so nothing re-parses

                mathLabel.CalculateLayoutInputHorizontal();
                CollectSegments();
                ApplyGlyphs();
                return;
            }

            int q = mathCut >= currentText.Length ? -1 : currentText.IndexOf('$', mathCut);
            string composed = q < 0
                ? currentText
                : currentText.Substring(0, q) + HiddenColor + currentText.Substring(q);

            if (mathLabel.text != composed)
                mathLabel.text = composed;

            mathLabel.CalculateLayoutInputHorizontal(); // parses and boxes now if the text changed

            CollectPlainAtoms();
            ApplyLetters();
        }

        private void CollectPlainAtoms()
        {
            plainAtoms.Clear();
            VisitForLetters(mathLabel.orchestrator != null ? mathLabel.orchestrator.latestAtomCache : null);

            if (plainAtoms.Count == 0 && plainTotal > 0 && !warnedNoLetters)
            {
                warnedNoLetters = true;
                Debug.LogWarning("DialogueChatBox could not find the letters of a math line, so its " +
                                 "text will show at once. Equations still appear as blocks.", this);
            }
        }

        // The pixel-text letters are the characters of the words that sit directly in the paragraph.
        // Anything inside an equation is deeper and is left alone.
        private void VisitForLetters(Atom atom)
        {
            if (atom == null)
                return;

            if (atom is ParagraphAtom paragraph)
            {
                foreach (Atom child in paragraph.children)
                {
                    if (!(child is WordAtom word))
                        continue;

                    foreach (Atom letter in word.children)
                    {
                        if (letter is CharAtom ch)
                            plainAtoms.Add(ch);
                    }
                }

                return;
            }

            if (atom is LayoutAtom layout)
            {
                foreach (Atom child in layout.children)
                    VisitForLetters(child);
            }
        }

        private void ApplyLetters()
        {
            int n = plainAtoms.Count;
            if (n == 0)
                return;

            // Ligatures can make the atom count differ from the letter count, so scale.
            int shown = plainBefore[Mathf.Clamp(mathCut, 0, plainBefore.Length - 1)];
            int visible = plainTotal <= 0 ? n : Mathf.RoundToInt(shown * (float)n / plainTotal);

            // A letter is fully visible at the text's own alpha, or fully transparent.
            byte fullAlpha = (byte)Mathf.RoundToInt(mathLabel.color.a * 255f);

            bool changed = false;
            for (int k = 0; k < n; k++)
            {
                byte wanted = k < visible ? fullAlpha : (byte)0;
                Color32 color = plainAtoms[k].color;
                if (color.a == wanted)
                    continue;

                color.a = wanted;
                plainAtoms[k].color = color;
                changed = true;
            }

            if (changed)
            {
                // Re-box with the new colours; the text is not parsed again.
                mathLabel.SetLayoutDirty();
                mathLabel.SetVerticesDirty();
            }
        }

        // ---- glyph-by-glyph equations -------------------------------------------------------

        private void BuildSegments(string text)
        {
            // Which characters cost typing time. Inside an equation, commands (\frac), braces and
            // the ^ and _ markers produce no glyph of their own.
            typeable = new bool[text.Length];
            bool inMath = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '$')
                {
                    inMath = !inMath;
                    continue;
                }

                if (!inMath)
                {
                    typeable[i] = !char.IsWhiteSpace(c);
                    continue;
                }

                if (c == '\\')
                {
                    while (i + 1 < text.Length && char.IsLetter(text[i + 1]))
                        i++;
                    continue;
                }

                typeable[i] = !(char.IsWhiteSpace(c) || c == '{' || c == '}' || c == '^' || c == '_');
            }

            sourceSegments.Clear();
            unitBefore = new int[text.Length + 1];

            int start = 0;
            bool equation = false;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] != '$')
                    continue;

                if (!equation)
                {
                    AddSegment(start, i, false);
                    start = i;
                    equation = true;
                }
                else
                {
                    AddSegment(start, i + 1, true);
                    start = i + 1;
                    equation = false;
                }
            }

            AddSegment(start, text.Length, equation);
        }

        private void AddSegment(int start, int end, bool isEquation)
        {
            int units = 0;
            for (int i = start; i < end; i++)
            {
                unitBefore[i] = units;
                if (typeable[i])
                    units++;
            }

            sourceSegments.Add(new SourceSegment { start = start, end = end, units = units, isEquation = isEquation });
        }

        private void DecideGlyphMode()
        {
            glyphMode = false;
            if (fit != null && !fit.typeEquations)
                return;

            // Look at TEXDraw's layout of the whole line and check it matches the source: plain,
            // equation, plain, equation, ... If it does not, equations simply appear as blocks.
            mathLabel.text = currentText;
            mathLabel.CalculateLayoutInputHorizontal();
            CollectSegments();

            glyphMode = treeSegments.Count == sourceSegments.Count;
            if (glyphMode == false && !warnedNoGlyphs)
            {
                warnedNoGlyphs = true;
                Debug.LogWarning("DialogueChatBox could not match a math line to its equations " +
                                 $"({treeSegments.Count} found, {sourceSegments.Count} expected), so equations " +
                                 "will appear as whole blocks.", this);
            }
        }

        private void CollectSegments()
        {
            treeSegments.Clear();
            Atom root = mathLabel.orchestrator != null ? mathLabel.orchestrator.latestAtomCache : null;
            ParagraphAtom paragraph = FindParagraph(root);
            if (paragraph == null)
                return;

            var plain = new List<Fade>();
            foreach (Atom child in paragraph.children)
            {
                if (child is WordAtom word)
                {
                    foreach (Atom letter in word.children)
                    {
                        if (letter is CharAtom ch)
                            plain.Add(new Fade { letter = ch });
                    }

                    continue;
                }

                var equation = new List<Fade>();
                CollectFades(child, equation);
                if (equation.Count == 0)
                    continue; // a space or other atom that draws nothing

                treeSegments.Add(plain);
                treeSegments.Add(equation);
                plain = new List<Fade>();
            }

            treeSegments.Add(plain);
        }

        private static ParagraphAtom FindParagraph(Atom atom)
        {
            if (atom is ParagraphAtom paragraph)
                return paragraph;

            if (atom is LayoutAtom layout)
            {
                foreach (Atom child in layout.children)
                {
                    ParagraphAtom found = FindParagraph(child);
                    if (found != null)
                        return found;
                }
            }

            return null;
        }

        // Everything an equation draws, in reading order: numerator, then the bar, then the
        // denominator; a base before its scripts.
        private static void CollectFades(Atom atom, List<Fade> into)
        {
            if (atom == null)
                return;

            if (atom is CharAtom ch)
            {
                into.Add(new Fade { letter = ch });
            }
            else if (atom is FractionAtom fraction)
            {
                CollectFades(fraction.numerator, into);
                if (fraction.line)
                    into.Add(new Fade { bar = fraction });
                CollectFades(fraction.denominator, into);
            }
            else if (atom is ScriptsAtom scripts)
            {
                CollectFades(scripts.atom, into);
                CollectFades(scripts.superscript, into);
                CollectFades(scripts.subscript, into);
            }
            else if (atom is AutoDelimitedGroupAtom delimited)
            {
                CollectFades(delimited.left, into);
                CollectFades(delimited.atom, into);
                CollectFades(delimited.right, into);
            }
            else if (atom is AccentedAtom accented)
            {
                CollectFades(accented.accent, into);
                CollectFades(accented.atom, into);
            }
            else if (atom is BoxedAtom boxed)
            {
                CollectFades(boxed.atom, into);
            }
            else if (atom is BoxedLinkAtom link)
            {
                CollectFades(link.atom, into);
            }
            else if (atom is BlockAtom block)
            {
                CollectFades(block.atom, into);
            }
            else if (atom is InlineAtom inline)
            {
                CollectFades(inline.atom, into);
            }
            else if (atom is LayoutAtom layout)
            {
                foreach (Atom child in layout.children)
                    CollectFades(child, into);
            }
        }

        private void ApplyGlyphs()
        {
            if (treeSegments.Count != sourceSegments.Count)
                return; // the layout is mid-change; the next frame will match again

            byte fullAlpha = (byte)Mathf.RoundToInt(mathLabel.color.a * 255f);
            bool changed = false;

            for (int s = 0; s < treeSegments.Count; s++)
            {
                SourceSegment segment = sourceSegments[s];
                List<Fade> fades = treeSegments[s];

                int typed = mathCut >= segment.end ? segment.units
                          : mathCut <= segment.start ? 0
                          : unitBefore[mathCut];

                int visible;
                if (mathCut >= segment.end)
                    visible = fades.Count;
                else if (segment.units <= 0)
                    visible = 0;
                else
                    visible = Mathf.RoundToInt(typed * (float)fades.Count / segment.units);

                for (int k = 0; k < fades.Count; k++)
                    changed |= SetAlpha(fades[k], k < visible ? fullAlpha : (byte)0);
            }

            if (changed)
            {
                mathLabel.SetLayoutDirty();
                mathLabel.SetVerticesDirty();
            }
        }

        private static bool SetAlpha(Fade fade, byte alpha)
        {
            if (fade.letter != null)
            {
                Color32 color = fade.letter.color;
                if (color.a == alpha)
                    return false;

                color.a = alpha;
                fade.letter.color = color;
                return true;
            }

            if (fade.bar != null && FractionColor != null)
            {
                var color = (Color32)FractionColor.GetValue(fade.bar);
                if (color.a == alpha)
                    return false;

                color.a = alpha;
                FractionColor.SetValue(fade.bar, color);
                return true;
            }

            return false;
        }

        private IEnumerator TypeMath(string text)
        {
            var blockWait = new WaitForSeconds(mathBlockPause);
            int i = 0;

            // TEXDraw re-parses a new line once or twice on its own while it settles. Keep the
            // label invisible for a few frames so none of that shows, then start typing.
            mathGroup.alpha = 0f;
            SetMathReveal(0);
            for (int frame = 0; frame < 3; frame++)
            {
                yield return null;
                SetMathReveal(0);
            }

            mathGroup.alpha = 1f;

            if (glyphMode == true)
            {
                while (i < text.Length)
                {
                    while (i < text.Length && !typeable[i])
                        i++;
                    if (i >= text.Length)
                        break;

                    i++; // one glyph
                    while (i < text.Length && !typeable[i])
                        i++; // spaces, commands and the $ signs ride along with the glyph before them

                    SetMathReveal(i);
                    yield return new WaitForSeconds(fit != null ? fit.secondsPerLetter : mathCharDelay);
                }

                mathRoutine = null;
                SetMathReveal(text.Length);
                Finish();
                yield break;
            }

            while (i < text.Length)
            {
                if (text[i] == '$')
                {
                    // An equation never shows half-built: jump to its closing $.
                    int close = text.IndexOf('$', i + 1);
                    i = close < 0 ? text.Length : close + 1;
                    SetMathReveal(i);
                    yield return blockWait;
                    continue;
                }

                i++;

                // Spaces appear together with the letter before them.
                while (i < text.Length && char.IsWhiteSpace(text[i]))
                    i++;

                SetMathReveal(i);
                yield return new WaitForSeconds(fit != null ? fit.secondsPerLetter : mathCharDelay);
            }

            mathRoutine = null;
            SetMathReveal(text.Length);
            Finish();
        }

        private void StopMath()
        {
            if (mathRoutine == null)
                return;

            StopCoroutine(mathRoutine);
            mathRoutine = null;
        }

        private static void SetScaleX(RectTransform target, float x)
        {
            Vector3 scale = target.localScale;
            scale.x = x;
            target.localScale = scale;
        }
    }
}
