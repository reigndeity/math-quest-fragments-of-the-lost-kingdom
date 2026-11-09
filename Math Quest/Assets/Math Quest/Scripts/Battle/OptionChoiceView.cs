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
        [Tooltip("Shown while this option is the selected one. If left empty, the child called Selected is used.")]
        [SerializeField] private GameObject selectedMarker;

        private CanvasGroup group;
        private Animator animator;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            animator = GetComponent<Animator>();

            if (selectedMarker == null)
            {
                Transform found = transform.Find("Selected");
                if (found != null)
                    selectedMarker = found.gameObject;
            }

            if (selectedMarker != null)
            {
                // The marker is only a picture. If it caught clicks, it would sit on top of this
                // option and keep it from being tapped.
                foreach (Graphic graphic in selectedMarker.GetComponentsInChildren<Graphic>(true))
                    graphic.raycastTarget = false;

                selectedMarker.SetActive(false);
            }
        }

        public void Bind(string text, Action clicked)
        {
            label.text = text;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => clicked?.Invoke());
        }

        /// <summary>
        /// Hides the option without taking it out of the layout: scale 0 (what its pop animation
        /// starts from), animator off, not clickable. Reveal() plays the pop.
        /// </summary>
        public void Conceal()
        {
            if (animator != null)
                animator.enabled = false;

            transform.localScale = Vector3.zero;

            if (group != null)
            {
                group.interactable = false;
                group.blocksRaycasts = false;
            }
        }

        /// <summary>Makes the option clickable and plays its animation (or just shows it if it has none).</summary>
        public void Reveal()
        {
            if (group != null)
            {
                group.interactable = true;
                group.blocksRaycasts = true;
            }

            if (animator != null)
                animator.enabled = true; // starts its state from the beginning, scale 0 up to 1
            else
                transform.localScale = Vector3.one;
        }

        public void SetSelected(bool selected)
        {
            if (selectedMarker != null)
                selectedMarker.SetActive(selected);
        }

        public void SetInteractable(bool interactable)
        {
            button.interactable = interactable;
        }
    }
}
