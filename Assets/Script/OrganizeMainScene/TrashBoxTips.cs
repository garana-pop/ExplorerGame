using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// ゴミ箱のヒントメッセージ表示を管理するクラス
/// クリック時のメッセージ表示機能を制御します
/// </summary>
public class TrashBoxTips : MonoBehaviour
{
    #region インスペクター設定

    [Header("メッセージ設定")]
    [Tooltip("ゴミ箱クリック時のメッセージ")]
    [SerializeField] private string clickMessage = "削除したいファイルをドラッグ&ドロップしてください。";

    [Tooltip("メッセージ表示時間（秒）")]
    [SerializeField] private float messageDisplayTime = 3.0f;

    [Tooltip("フェードイン時間（秒）")]
    [SerializeField] private float fadeInTime = 0.5f;

    [Tooltip("フェードアウト時間（秒）")]
    [SerializeField] private float fadeOutTime = 0.5f;

    [Header("UI参照")]
    [Tooltip("メッセージ表示用パネル")]
    [SerializeField] private GameObject messagePanel;

    [Tooltip("メッセージテキストコンポーネント")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("表示設定")]
    [Tooltip("メッセージ表示時の背景色")]
    [SerializeField] private Color messagePanelColor = new Color(0f, 0f, 0f, 0.7f);

    [Tooltip("メッセージテキストの色")]
    [SerializeField] private Color messageTextColor = Color.white;

    [Tooltip("メッセージテキストのフォントサイズ")]
    [SerializeField] private float messageFontSize = 18f;

    [Header("アニメーション設定")]
    [Tooltip("メッセージ表示時のスケールアニメーション")]
    [SerializeField] private bool useScaleAnimation = true;

    [Tooltip("スケールアニメーションの開始値")]
    [SerializeField] private float scaleAnimationStart = 0.8f;

    [Tooltip("スケールアニメーションの終了値")]
    [SerializeField] private float scaleAnimationEnd = 1.0f;

    [Header("デバッグ設定")]
    [Tooltip("デバッグログを表示するか")]
    [SerializeField] private bool debugMode = false;

    #endregion

    #region プライベート変数

    // シーンコントローラー参照
    private OrganizeMainSceneController sceneController;

    // メッセージ表示状態管理
    private Coroutine messageCoroutine;
    private bool isMessageDisplaying = false;

    // UIコンポーネント参照
    private CanvasGroup messagePanelCanvasGroup;
    private UnityEngine.UI.Image messagePanelImage;

    // アニメーション用
    private Vector3 originalScale;

    // 定数
    private const float MIN_DISPLAY_TIME = 0.1f;
    private const float MAX_DISPLAY_TIME = 10.0f;
    private const float MIN_FADE_TIME = 0.0f;
    private const float MAX_FADE_TIME = 2.0f;

    #endregion

    #region Unity ライフサイクル

    /// <summary>
    /// Awakeメソッド - 初期化処理
    /// </summary>
    private void Awake()
    {
        ValidateSettings();
        InitializeUI();

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxTips)}: 初期化完了");
        }
    }

    /// <summary>
    /// Startメソッド - シーン開始後の処理
    /// </summary>
    private void Start()
    {
        // シーンコントローラーの参照取得
        sceneController = OrganizeMainSceneController.Instance;
        if (sceneController == null && debugMode)
        {
            Debug.LogWarning($"{nameof(TrashBoxTips)}: OrganizeMainSceneControllerが見つかりません");
        }

        // メッセージパネルを初期状態で非表示に設定
        SetMessagePanelVisible(false);
    }

    #endregion

    #region 初期化処理

    /// <summary>
    /// 設定値の検証
    /// </summary>
    private void ValidateSettings()
    {
        // 表示時間の検証
        messageDisplayTime = Mathf.Clamp(messageDisplayTime, MIN_DISPLAY_TIME, MAX_DISPLAY_TIME);
        fadeInTime = Mathf.Clamp(fadeInTime, MIN_FADE_TIME, MAX_FADE_TIME);
        fadeOutTime = Mathf.Clamp(fadeOutTime, MIN_FADE_TIME, MAX_FADE_TIME);

        // スケール値の検証
        scaleAnimationStart = Mathf.Max(0.1f, scaleAnimationStart);
        scaleAnimationEnd = Mathf.Max(0.1f, scaleAnimationEnd);

        // フォントサイズの検証
        messageFontSize = Mathf.Clamp(messageFontSize, 8f, 72f);

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxTips)}: 設定値検証完了");
        }
    }

    /// <summary>
    /// UIコンポーネントの初期化
    /// </summary>
    private void InitializeUI()
    {
        // メッセージパネルのコンポーネント取得
        if (messagePanel != null)
        {
            // CanvasGroupの取得または追加
            messagePanelCanvasGroup = messagePanel.GetComponent<CanvasGroup>();
            if (messagePanelCanvasGroup == null)
            {
                messagePanelCanvasGroup = messagePanel.AddComponent<CanvasGroup>();
            }

            // Imageコンポーネントの取得
            messagePanelImage = messagePanel.GetComponent<UnityEngine.UI.Image>();
            if (messagePanelImage != null)
            {
                messagePanelImage.color = messagePanelColor;
            }

            // 元のスケールを保存
            originalScale = messagePanel.transform.localScale;
        }

        // メッセージテキストの設定
        if (messageText != null)
        {
            messageText.text = "";
            messageText.color = messageTextColor;
            messageText.fontSize = messageFontSize;
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxTips)}: UI初期化完了");
        }
    }

    #endregion

    #region メッセージ表示制御

    /// <summary>
    /// クリック時のメッセージを表示
    /// </summary>
    public void ShowClickMessage()
    {
        Debug.LogWarning($"{nameof(TrashBoxTips)}: Tipsメッセージ表示");
        ShowMessage(clickMessage);
    }

    /// <summary>
    /// カスタムメッセージを表示
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    public void ShowMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(TrashBoxTips)}: 空のメッセージが指定されました");
            }
            return;
        }

        // 既存のメッセージ表示を停止
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        // メッセージ表示コルーチンを開始
        messageCoroutine = StartCoroutine(DisplayMessageCoroutine(message));
    }

    /// <summary>
    /// メッセージを非表示にする
    /// </summary>
    public void HideMessage()
    {
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }

        StartCoroutine(HideMessageCoroutine());
    }

    /// <summary>
    /// メッセージ表示コルーチン
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    /// <returns>コルーチン</returns>
    private IEnumerator DisplayMessageCoroutine(string message)
    {
        isMessageDisplaying = true;

        // メッセージテキストを設定
        if (messageText != null)
        {
            messageText.text = message;
        }

        // メッセージパネルを表示
        SetMessagePanelVisible(true);

        // フェードイン&スケールアニメーション
        yield return StartCoroutine(FadeInAnimation());

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxTips)}: メッセージ表示 - {message}");
        }

        // 指定時間待機
        yield return new WaitForSeconds(messageDisplayTime);

        // フェードアウト&スケールアニメーション
        yield return StartCoroutine(FadeOutAnimation());

        // メッセージパネルを非表示
        SetMessagePanelVisible(false);

        isMessageDisplaying = false;
        messageCoroutine = null;

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxTips)}: メッセージ非表示");
        }
    }

    /// <summary>
    /// メッセージ非表示コルーチン
    /// </summary>
    /// <returns>コルーチン</returns>
    private IEnumerator HideMessageCoroutine()
    {
        if (!isMessageDisplaying) yield break;

        // フェードアウト&スケールアニメーション
        yield return StartCoroutine(FadeOutAnimation());

        // メッセージパネルを非表示
        SetMessagePanelVisible(false);

        isMessageDisplaying = false;

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxTips)}: メッセージを強制非表示にしました");
        }
    }

    #endregion

    #region アニメーション処理

    /// <summary>
    /// フェードインアニメーション
    /// </summary>
    /// <returns>コルーチン</returns>
    private IEnumerator FadeInAnimation()
    {
        if (messagePanelCanvasGroup == null) yield break;

        float elapsedTime = 0f;
        float startAlpha = 0f;
        float targetAlpha = 1f;

        Vector3 startScale = useScaleAnimation ? originalScale * scaleAnimationStart : originalScale;
        Vector3 targetScale = originalScale * scaleAnimationEnd;

        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeInTime;

            // アルファ値の補間
            messagePanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);

            // スケールアニメーション
            if (useScaleAnimation && messagePanel != null)
            {
                messagePanel.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            }

            yield return null;
        }

        // 最終値を設定
        messagePanelCanvasGroup.alpha = targetAlpha;
        if (useScaleAnimation && messagePanel != null)
        {
            messagePanel.transform.localScale = targetScale;
        }
    }

    /// <summary>
    /// フェードアウトアニメーション
    /// </summary>
    /// <returns>コルーチン</returns>
    private IEnumerator FadeOutAnimation()
    {
        if (messagePanelCanvasGroup == null) yield break;

        float elapsedTime = 0f;
        float startAlpha = messagePanelCanvasGroup.alpha;
        float targetAlpha = 0f;

        Vector3 startScale = messagePanel != null ? messagePanel.transform.localScale : originalScale;
        Vector3 targetScale = useScaleAnimation ? originalScale * scaleAnimationStart : originalScale;

        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeOutTime;

            // アルファ値の補間
            messagePanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);

            // スケールアニメーション
            if (useScaleAnimation && messagePanel != null)
            {
                messagePanel.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            }

            yield return null;
        }

        // 最終値を設定
        messagePanelCanvasGroup.alpha = targetAlpha;
        if (useScaleAnimation && messagePanel != null)
        {
            messagePanel.transform.localScale = targetScale;
        }
    }

    #endregion

    #region UI制御

    /// <summary>
    /// メッセージパネルの表示/非表示を設定
    /// </summary>
    /// <param name="visible">表示するかどうか</param>
    private void SetMessagePanelVisible(bool visible)
    {
        if (messagePanel != null)
        {
            messagePanel.SetActive(visible);
        }
    }

    #endregion

    #region パブリックメソッド

    /// <summary>
    /// メッセージが表示中かどうかを取得
    /// </summary>
    /// <returns>表示中の場合はtrue</returns>
    public bool IsMessageDisplaying()
    {
        return isMessageDisplaying;
    }

    /// <summary>
    /// クリックメッセージを設定
    /// </summary>
    /// <param name="newMessage">新しいメッセージ</param>
    public void SetClickMessage(string newMessage)
    {
        if (!string.IsNullOrEmpty(newMessage))
        {
            clickMessage = newMessage;

            if (debugMode)
            {
                Debug.Log($"{nameof(TrashBoxTips)}: クリックメッセージを更新 - {newMessage}");
            }
        }
    }

    /// <summary>
    /// メッセージ表示時間を設定
    /// </summary>
    /// <param name="newDisplayTime">新しい表示時間</param>
    public void SetMessageDisplayTime(float newDisplayTime)
    {
        messageDisplayTime = Mathf.Clamp(newDisplayTime, MIN_DISPLAY_TIME, MAX_DISPLAY_TIME);

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxTips)}: メッセージ表示時間を更新 - {messageDisplayTime}秒");
        }
    }

    /// <summary>
    /// フェード時間を設定
    /// </summary>
    /// <param name="newFadeInTime">新しいフェードイン時間</param>
    /// <param name="newFadeOutTime">新しいフェードアウト時間</param>
    public void SetFadeTimes(float newFadeInTime, float newFadeOutTime)
    {
        fadeInTime = Mathf.Clamp(newFadeInTime, MIN_FADE_TIME, MAX_FADE_TIME);
        fadeOutTime = Mathf.Clamp(newFadeOutTime, MIN_FADE_TIME, MAX_FADE_TIME);

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxTips)}: フェード時間を更新 - In:{fadeInTime}秒, Out:{fadeOutTime}秒");
        }
    }

    /// <summary>
    /// メッセージUIの参照を設定
    /// </summary>
    /// <param name="newMessagePanel">新しいメッセージパネル</param>
    /// <param name="newMessageText">新しいメッセージテキスト</param>
    public void SetMessageUI(GameObject newMessagePanel, TextMeshProUGUI newMessageText)
    {
        messagePanel = newMessagePanel;
        messageText = newMessageText;

        // UIを再初期化
        InitializeUI();

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxTips)}: メッセージUI参照を更新しました");
        }
    }

    #endregion
}