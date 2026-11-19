using MathQuest.Data;
using UnityEngine;
using UnityEngine.UI;

namespace MathQuest.UI
{
    /// <summary>
    /// On the CharacterSelectionPanel. The player picks the boy or the girl; the picked one shows its
    /// Selected marker, and Proceed only works once one is picked. Nothing is picked at first.
    /// Which panel opens next is set up with Panel Toggle Buttons on the Proceed button.
    /// </summary>
    public sealed class CharacterSelectionView : MonoBehaviour
    {
        [Header("Boy")]
        [SerializeField] private Button boyButton;
        [SerializeField] private GameObject boySelected;

        [Header("Girl")]
        [SerializeField] private Button girlButton;
        [SerializeField] private GameObject girlSelected;

        [Header("Proceed")]
        [Tooltip("Can only be pressed once a character is picked.")]
        [SerializeField] private Button proceedButton;

        private bool hasPicked;
        private PlayerCharacter picked = PlayerCharacter.Boy;

        public PlayerCharacter Picked => picked;

        private void Awake()
        {
            if (boyButton != null) boyButton.onClick.AddListener(PickBoy);
            if (girlButton != null) girlButton.onClick.AddListener(PickGirl);
        }

        private void OnDestroy()
        {
            if (boyButton != null) boyButton.onClick.RemoveListener(PickBoy);
            if (girlButton != null) girlButton.onClick.RemoveListener(PickGirl);
        }

        private void OnEnable() => Refresh();

        public void PickBoy() => Pick(PlayerCharacter.Boy);

        public void PickGirl() => Pick(PlayerCharacter.Girl);

        private void Pick(PlayerCharacter character)
        {
            picked = character;
            hasPicked = true;
            PlayerProfile.Character = character;
            Refresh();
        }

        private void Refresh()
        {
            if (boySelected != null) boySelected.SetActive(hasPicked && picked == PlayerCharacter.Boy);
            if (girlSelected != null) girlSelected.SetActive(hasPicked && picked == PlayerCharacter.Girl);
            if (proceedButton != null) proceedButton.interactable = hasPicked;
        }
    }
}
