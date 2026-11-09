using System;
using System.Collections.Generic;
using MathQuest.Data;
using UnityEngine;

namespace MathQuest.Battle
{
    /// <summary>
    /// The MultipleChoiceHolder prefab. Spawns one OptionChoice per option of the answer
    /// (the grid layout on this object arranges them) and reports the tapped index. It
    /// does not decide whether the answer is correct.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MultipleChoicePanel : MonoBehaviour
    {
        [SerializeField] private OptionChoiceView optionPrefab;

        private readonly List<OptionChoiceView> options = new();
        private Action<int> onSubmitted;
        private bool answered;

        public void Show(MultipleChoiceAnswerResource answer, Action<int> submitted)
        {
            onSubmitted = submitted;
            answered = false;

            // Clears any sample options left in the prefab, and any from a previous question.
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
            options.Clear();

            for (int i = 0; i < answer.Options.Count; i++)
            {
                int index = i;
                OptionChoiceView view = Instantiate(optionPrefab, transform, false);
                view.Bind(answer.Options[i], () => Submit(index));
                options.Add(view);
            }
        }

        private void Submit(int index)
        {
            if (answered)
                return;

            answered = true;
            foreach (OptionChoiceView view in options)
                view.SetInteractable(false);

            onSubmitted?.Invoke(index);
        }
    }
}
