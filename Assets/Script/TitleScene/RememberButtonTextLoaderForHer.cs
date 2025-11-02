using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using ExplorerGame.Localization;
using UnityEngine.Localization.Components;

/// <summary>
/// 思い出すボタンのテキスト表示を管理するクラス
/// RememberButtonTextChangerForHerと連携して動作
/// </summary>
public class RememberButtonTextLoaderForHer : MonoBehaviour
{
    [Header("テキスト設定")]
    [Tooltip("表示対象のTextMeshProコンポーネント")]
    [SerializeField] private TMP_Text buttonText;

    [Header("ローカライズ設定")]
    [Tooltip("英語版の通常時のボタンテキスト")]
    [SerializeField] private string normalButtonTextEnglish = "Remember";

    [Tooltip("英語版の変更後のボタンテキスト")]
    [SerializeField] private string changedButtonTextEnglish = "Accept";

    [Tooltip("通常時のボタンテキスト")]
    [SerializeField] private string normalButtonText = "思い出す";

    [Tooltip("変更後のボタンテキスト")]
    [SerializeField] private string changedButtonText = "決意する";

    [Header("RememberButtonTextChangerForHer参照")]
    [Tooltip("RememberButtonTextChangerForHerへの参照（オプション）")]
    [SerializeField] private RememberButtonTextChangerForHer buttonTextChanger;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceChangedText = false; // テスト用の強制変更

    // LocalizeStringEventコンポーネントの参照
    private LocalizeStringEvent localizeStringEvent;

    private void Awake()
    {
        // TextMeshProコンポーネントの自動取得
        if (buttonText == null)
        {
            // MenuContainerの思い出すボタンを探す
            GameObject menuContainer = GameObject.Find("MenuContainer");
            if (menuContainer != null)
            {
                Transform rememberButton = menuContainer.transform.Find("思い出すボタン");
                if (rememberButton != null)
                {
                    buttonText = rememberButton.GetComponentInChildren<TMP_Text>();
                }
            }
        }

        if (buttonText == null)
        {
            DebugLogger.LogError("RememberButtonTextLoaderForHer: 思い出すボタンのTextMeshProコンポーネントが見つかりません。インスペクターで設定してください。");
            enabled = false;
            return;
        }

        // RememberButtonTextChangerForHerの自動検索
        if (buttonTextChanger == null)
        {
            buttonTextChanger = FindFirstObjectByType<RememberButtonTextChangerForHer>();
        }

        // RememberButtonTextChangerForHerから設定値を取得
        if (buttonTextChanger != null)
        {
            // 変更後テキストを取得（リフレクションを使用）
            var newTextField = buttonTextChanger.GetType().GetField("newButtonText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (newTextField != null)
            {
                string newTextValue = newTextField.GetValue(buttonTextChanger) as string;
                if (!string.IsNullOrEmpty(newTextValue))
                {
                    changedButtonText = newTextValue;
                }
            }
        }

        // Localize String Eventコンポーネントの取得を追加
        if (buttonText != null)
        {
            localizeStringEvent = buttonText.GetComponent<LocalizeStringEvent>();
        }
    }

    private void Start()
    {
        // 少し遅延させて確実に状態を確認
        StartCoroutine(LoadAndApplyTextDelayed());

        // 言語変更イベントへの登録
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }
    }

    private void OnDestroy()
    {
        // 言語変更イベントの登録解除
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    // 言語変更時のコールバックメソッドを追加
    /// <summary>
    /// 言語変更時のコールバック
    /// </summary>
    private void OnLanguageChanged(UnityEngine.Localization.Locale newLocale)
    {
        LoadAndApplyText();
    }

    /// <summary>
    /// 遅延後にボタンテキストを読み込んで適用
    /// </summary>
    private IEnumerator LoadAndApplyTextDelayed()
    {
        // 初期化を待つ
        yield return new WaitForSeconds(0.1f);

        LoadAndApplyText();
    }

    /// <summary>
    /// afterChangeToHerMemoryフラグを取得
    /// </summary>
    private bool GetAfterChangeToHerMemoryFlag()
    {
        // GameSaveManagerから取得
        if (GameSaveManager.Instance != null)
        {
            return GameSaveManager.Instance.GetAfterChangeToHerMemoryFlag();
        }

        // フラグが取得できない場合はfalseを返す
        if (debugMode) DebugLogger.LogWarning("RememberButtonTextLoaderForHer: GameSaveManagerが存在しないため、afterChangeToHerMemoryフラグを取得できませんでした");
        return false;
    }

    /// <summary>
    /// afterChangeToHisFutureフラグを取得
    /// </summary>
    private bool GetAfterChangeToHisFutureFlag()
    {
        // GameSaveManagerから取得
        if (GameSaveManager.Instance != null)
        {
            return GameSaveManager.Instance.GetAfterChangeToHisFutureFlag();
        }

        // フラグが取得できない場合はfalseを返す
        if (debugMode) DebugLogger.LogWarning("RememberButtonTextLoaderForHer: GameSaveManagerが存在しないため、afterChangeToHisFutureフラグを取得できませんでした");
        return false;
    }

    /// <summary>
    /// 現在の状態に基づいてボタンテキストを適用
    /// </summary>
    private void LoadAndApplyText()
    {
        // afterChangeToLastフラグがtrueの場合は処理をスキップ
        //if (GameSaveManager.Instance != null && GameSaveManager.Instance.GetAfterChangeToLastFlag())
        //{
        //    if (debugMode) DebugLogger.Log("RememberButtonTextLoaderForHer: afterChangeToLastがtrueのため処理をスキップします");
        //    return;
        //}

        bool shouldChangeText = false;

        // デバッグモードでの強制変更
        if (debugMode && forceChangedText)
        {
            shouldChangeText = true;
            if (debugMode) DebugLogger.Log("RememberButtonTextLoaderForHer: デバッグモードで強制的にテキストを変更");
        }
        else
        {
            // 両方のフラグをチェック
            bool herMemoryFlag = GetAfterChangeToHerMemoryFlag();
            bool hisFutureFlag = GetAfterChangeToHisFutureFlag();

            // 両方のフラグがtrueの場合のみテキストを変更
            shouldChangeText = herMemoryFlag && hisFutureFlag;

            if (debugMode)
            {
                DebugLogger.Log($"RememberButtonTextLoaderForHer: afterChangeToHerMemory = {herMemoryFlag}");
                DebugLogger.Log($"RememberButtonTextLoaderForHer: afterChangeToHisFuture = {hisFutureFlag}");
                DebugLogger.Log($"RememberButtonTextLoaderForHer: 両フラグ条件 = {shouldChangeText}");
            }
        }

        // 現在の言語を取得
        string currentLanguageCode = "ja"; // デフォルト
        if (LocalizationManager.Instance != null)
        {
            currentLanguageCode = LocalizationManager.Instance.GetCurrentLanguageCode();
        }

        // 言語に応じてテキストを選択
        string textToApply;
        if (currentLanguageCode == "en")
        {
            textToApply = shouldChangeText ? changedButtonTextEnglish : normalButtonTextEnglish;
        }
        else
        {
            textToApply = shouldChangeText ? changedButtonText : normalButtonText;
        }

        // Localize String Eventコンポーネントを無効化
        if (localizeStringEvent != null)
        {
            localizeStringEvent.enabled = false;
            if (debugMode) DebugLogger.Log("RememberButtonTextLoaderForHer: LocalizeStringEventを無効化");
        }

        // ボタンテキストを設定
        if (buttonText != null)
        {
            buttonText.text = textToApply;
            if (debugMode)
            {
                DebugLogger.Log($"RememberButtonTextLoaderForHer: ボタンテキストを「{textToApply}」に設定 (言語: {currentLanguageCode})");
            }
        }

        // ボタンにシーン遷移機能を設定
        if (shouldChangeText && buttonText != null && !GameSaveManager.Instance.GetAfterChangeToLastFlag())
        {
            Button button = buttonText.GetComponentInParent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    if (debugMode) DebugLogger.Log("RememberButtonTextLoaderForHer: MonologueSceneへ遷移します");
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MonologueScene");
                });
            }
        }

        // MonologueScene→TitleSceneに遷移済みの場合：思い出すボタンを「整理する」or"Organize"に変更し、OrganizeMainSceneに遷移するように設定
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.GetAfterChangeToLastFlag())
        {
            //言語コードに応じてテキストを設定
            if (currentLanguageCode == "ja")
            {
                buttonText.text = "整理する";
                if (debugMode) DebugLogger.Log("RememberButtonTextLoaderForHer: ボタンテキストを「整理する」に設定");
            }
            else if (currentLanguageCode == "en")
            {
                buttonText.text = "Organize";
                if (debugMode) DebugLogger.Log("RememberButtonTextLoaderForHer: ボタンテキストを「Organize」に設定");
            }
            else
            {
                buttonText.text = "整理する"; // デフォルトは日本語
                if (debugMode) DebugLogger.Log("RememberButtonTextLoaderForHer: 言語コード取得失敗のため、ボタンテキストをデフォルトの「整理する」に設定");
            }

            Button button = buttonText.GetComponentInParent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    if (debugMode) DebugLogger.Log("RememberButtonTextLoaderForHer: OrganizeMainSceneへ遷移します");
                    UnityEngine.SceneManagement.SceneManager.LoadScene("OrganizeMainScene");
                });
            }
        }
    }

    /// <summary>
    /// 手動でテキストを再読み込み（デバッグ用）
    /// </summary>
    [ContextMenu("Reload Button Text")]
    public void ReloadButtonText()
    {
        LoadAndApplyText();
    }

    /// <summary>
    /// フラグの状態を手動で設定（デバッグ用）
    /// </summary>
    [ContextMenu("Toggle Changed Text")]
    public void ToggleChangedText()
    {
        forceChangedText = !forceChangedText;
        LoadAndApplyText();
    }
}