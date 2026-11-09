using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.Battle
{
    /// <summary>One answer button on the OptionChoice prefab.</summary>
    [DisallowMultipleComponent]
    public sealed class OptionChoiceView : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text label;

        public void Bind(string text, Action clicked)
        {
            label.text = text;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => clicked?.Invoke());
        }

        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
        }
    }
}
