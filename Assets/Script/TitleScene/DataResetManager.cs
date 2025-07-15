using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class DataResetManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dataResetConfirmationPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Settings")]
    [SerializeField] private float messageDisplayDuration = 2.0f;
    [SerializeField] private string deletionMessage = "保存されていたデータはすべて削除されました";

    [Header("Text Animation Settings")]
    [Tooltip("文字全体のフェードイン時間（秒）")]
    [SerializeField] private float textFadeInDuration = 1.0f;

    [Tooltip("文字の最初の透明度（0.0～1.0）")]
    [SerializeField] private float startAlpha = 0.0f;

    [Tooltip("文字の最終透明度（0.0～1.0）")]
    [SerializeField] private float endAlpha = 1.0f;

    private void Start()
    {
        // ボタンイベントの設定
        if (yesButton != null)
            yesButton.onClick.AddListener(OnYesButtonClicked);

        if (noButton != null)
            noButton.onClick.AddListener(OnNoButtonClicked);

        // 初期状態でメッセージパネルを非表示
        if (messagePanel != null)
            messagePanel.SetActive(false);
    }

    private void OnYesButtonClicked()
    {
        StartCoroutine(DeleteDataAndRestart());
    }

    private void OnNoButtonClicked()
    {
        // データ初期化確認パネルを非表示
        if (dataResetConfirmationPanel != null)
            dataResetConfirmationPanel.SetActive(false);
    }

    private IEnumerator DeleteDataAndRestart()
    {
        // 1. 全セーブデータを削除
        DeleteAllSaveData();

        // 2. 削除完了メッセージをふわっと表示
        yield return StartCoroutine(ShowDeletionMessageWithFadeEffect());

        // 3. メッセージ表示時間だけ待機
        yield return new WaitForSeconds(messageDisplayDuration);

        // 4. ゲームを再起動
        RestartGame();
    }

    private void DeleteAllSaveData()
    {
        // GameSaveManagerを使用してセーブデータを削除
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.DeleteAllSaveData();
        }

        // その他のセーブデータがある場合はここで削除
        // 例: カスタムセーブファイルの削除など
    }

    /// <summary>
    /// 削除完了メッセージをふわっと表示する
    /// </summary>
    private IEnumerator ShowDeletionMessageWithFadeEffect()
    {
        if (messagePanel == null || messageText == null)
        {
            Debug.LogWarning("DataResetManager: メッセージパネルまたはテキストが設定されていません");
            yield break;
        }

        // メッセージパネルを表示
        messagePanel.SetActive(true);

        // メッセージテキストを設定
        messageText.text = deletionMessage;

        // 文字全体をフェードインで表示
        yield return StartCoroutine(FadeInText());
    }

    /// <summary>
    /// テキスト全体をふわっと表示する
    /// </summary>
    private IEnumerator FadeInText()
    {
        if (messageText == null) yield break;

        Color originalColor = messageText.color;
        float timer = 0f;

        // 初期状態：透明に設定
        messageText.color = new Color(originalColor.r, originalColor.g, originalColor.b, startAlpha);

        // フェードインアニメーション
        while (timer < textFadeInDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / textFadeInDuration;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, progress);

            messageText.color = new Color(originalColor.r, originalColor.g, originalColor.b, currentAlpha);

            yield return null;
        }

        // 最終状態：完全に表示
        messageText.color = new Color(originalColor.r, originalColor.g, originalColor.b, endAlpha);
    }

    private void ShowDeletionMessage()
    {
        if (messagePanel != null && messageText != null)
        {
            messageText.text = deletionMessage;
            messagePanel.SetActive(true);
        }
    }

    private void RestartGame()
    {
        // シーンを再読み込みしてゲームを再起動
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}