using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuitConfirmationController : MonoBehaviour
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI confirmationText;

    [Header("�I�[�f�B�I")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;

    private MainMenuController mainMenuController;

    private void Start()
    {
        mainMenuController = GetComponentInParent<MainMenuController>();

        // �m�F�e�L�X�g�̐ݒ�
        if (confirmationText != null)
        {
            confirmationText.text = "�Q�[�����I�����܂����H";
        }

        // �{�^���C�x���g�̓o�^
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
    }

    private void OnConfirmButtonClicked()
    {
        PlayClickSound();

        // �A�v���P�[�V�����̏I��
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnCancelButtonClicked()
    {
        PlayClickSound();

        if (mainMenuController != null)
        {
            mainMenuController.ReturnToMainMenu();
        }
    }

    private void PlayClickSound()
    {
        if (audioSource != null && clickSound != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
}