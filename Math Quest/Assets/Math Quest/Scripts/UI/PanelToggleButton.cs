using MathQuest.Bootstrap;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.UI
{
    /// <summary>
    /// Put on a Button: clicking it shows, hides or toggles the target object.
    /// Used to open and close panels without any menu logic.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class PanelToggleButton : MonoBehaviour
    {
        public enum PanelAction { Show, Hide, Toggle }

        [SerializeField] private GameObject target;
        [SerializeField] private PanelAction action = PanelAction.Show;

        [Tooltip("On: the loading transition (the circle) plays while the panel changes. If a button has several of these, tick it on all of them so they share one transition.")]
        [SerializeField] private bool useTransition;

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnClick);
        }

        private void OnDestroy()
        {
            if (button != null) button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            if (target == null) return;

            if (useTransition)
                ScreenTransition.Run(Apply);
            else
                Apply();
        }

        private void Apply()
        {
            if (target == null) return;

            switch (action)
            {
                case PanelAction.Show: target.SetActive(true); break;
                case PanelAction.Hide: target.SetActive(false); break;
                default: target.SetActive(!target.activeSelf); break;
            }
        }
    }
}
