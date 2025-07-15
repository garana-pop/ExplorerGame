using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuitConfirmationController : MonoBehaviour
{
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI confirmationText;

    [Header("オーディオ")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;

    private MainMenuController mainMenuController;

    private void Start()
    {
        mainMenuController = GetComponentInParent<MainMenuController>();

        // 確認テキストの設定
        if (confirmationText != null)
        {
            confirmationText.text = "ゲームを終了しますか？";
        }

        // ボタンイベントの登録
        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        cancelButton.onClick.AddListener(OnCancelButtonClicked);
    }

    private void OnConfirmButtonClicked()
    {
        PlayClickSound();

        // アプリケーションの終了
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