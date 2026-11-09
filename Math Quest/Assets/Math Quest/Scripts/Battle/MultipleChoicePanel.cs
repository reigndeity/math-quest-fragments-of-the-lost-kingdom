using System;
using System.Collections.Generic;
using MathQuest.Data;
using UnityEngine;

namespace MathQuest.Battle
{
    /// <summary>
    /// The MultipleChoiceHolder prefab. Spawns one OptionChoice per option of the answer
    /// (the grid layout on this object arranges them). Tapping an option selects it and
    /// shows its Selected marker; the answer is only handed in when the dedicated submit
    /// button is pressed. It does not decide whether the answer is correct.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MultipleChoicePanel : MonoBehaviour
    {
        [SerializeField] private OptionChoiceView optionPrefab;

        private readonly List<OptionChoiceView> options = new();
        private Action selectionChanged;
        private bool locked;

        /// <summary>Index of the selected option in the answer's Options, or -1 if none.</summary>
        public int SelectedIndex { get; private set; } = -1;

        public bool HasSelection => SelectedIndex >= 0;

        public int OptionCount => options.Count;

        /// <summary>
        /// Spawns the options. <paramref name="changed"/> fires whenever the selection changes.
        /// With <paramref name="concealed"/> they all exist (so the layout is final) but stay
        /// hidden until <see cref="RevealOption"/> is called for each.
        /// </summary>
        public void Show(MultipleChoiceAnswerResource answer, Action changed, bool concealed = false)
        {
            selectionChanged = changed;
            SelectedIndex = -1;
            locked = false;

            // Destroy only takes effect at the end of the frame, so also hide old children now:
            // layout groups ignore inactive children, which keeps the measured height correct.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            options.Clear();

            for (int i = 0; i < answer.Options.Count; i++)
            {
                int index = i;
                OptionChoiceView view = Instantiate(optionPrefab, transform, false);
                view.Bind(answer.Options[i], () => Select(index));
                view.SetSelected(false);
                if (concealed)
                    view.Conceal();
                options.Add(view);
            }
        }

        /// <summary>Pops one concealed option in.</summary>
        public void RevealOption(int index)
        {
            if (index >= 0 && index < options.Count)
                options[index].Reveal();
        }

        /// <summary>Stops the options reacting to taps, for example once the answer is submitted.</summary>
        public void Lock()
        {
            locked = true;
            foreach (OptionChoiceView view in options)
                view.SetInteractable(false);
        }

        private void Select(int index)
        {
            if (locked || index == SelectedIndex)
                return;

            SelectedIndex = index;
            for (int i = 0; i < options.Count; i++)
                options[i].SetSelected(i == index);

            selectionChanged?.Invoke();
        }
    }
}
