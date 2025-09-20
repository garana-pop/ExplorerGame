using ExplorerGame.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

/// <summary>
/// ゲームロード時にafterChangeToHerMemoryフラグに基づいてタイトルテキストを設定するクラス
/// TitleContainerオブジェクトに配置して使用
/// </summary>
public class TitleTextLoader : MonoBehaviour
{
    [Header("テキスト設定")]
    [Tooltip("変更対象のTextMeshProコンポーネント")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("通常時のタイトルテキスト")]
    [SerializeField] private string normalTitleText = "「彼」の記憶";

    [Tooltip("afterChangeToHerMemory=true時のタイトルテキスト")]
    [SerializeField] private string changedTitleText = "「彼女」の記憶";

    [Header("ローカライズ設定")]
    [Tooltip("英語版のタイトルテキスト（通常）")]
    [SerializeField] private string normalTitleTextEnglish = "Memories of 'Wish'";

    [Tooltip("英語版のタイトルテキスト（変更後）")]
    [SerializeField] private string changedTitleTextEnglish = "Memories of 'Her'";

    [Header("TitleTextChanger参照")]
    [Tooltip("TitleTextChangerへの直接参照（オプション）")]
    [SerializeField] private TitleTextChanger titleTextChanger;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceChangedTitle = false; // テスト用の強制変更

    // コンポーネント参照
    private LocalizationManager localizationManager;
    private LocalizeStringEvent localizeStringEvent;

    // 現在の状態を記録
    private bool isUsingChangedTitle = false;

    private void Awake()
    {
        // TextMeshProコンポーネントの自動取得
        if (titleText == null)
        {
            titleText = GetComponent<TMP_Text>();
            if (titleText == null)
            {
                titleText = GetComponentInChildren<TMP_Text>();
            }
        }

        if (titleText == null)
        {
            Debug.LogError("TitleTextLoader: TextMeshProコンポーネントが見つかりません。インスペクターで設定してください。");
            enabled = false;
            return;
        }

        // Localize String Eventコンポーネントの取得
        localizeStringEvent = titleText.GetComponent<LocalizeStringEvent>();

        // TitleTextChangerの自動検索
        if (titleTextChanger == null)
        {
            titleTextChanger = FindFirstObjectByType<TitleTextChanger>();
        }

        // TitleTextChangerから設定値を取得
        if (titleTextChanger != null)
        {
            // 通常テキストとして元のテキストを取得
            if (string.IsNullOrEmpty(normalTitleText))
            {
                normalTitleText = titleTextChanger.OriginalTitleText;
            }

            // 変更後テキストを取得
            if (string.IsNullOrEmpty(changedTitleText))
            {
                changedTitleText = titleTextChanger.NewTitleText;
            }
            // ロード時は効果音を無効化
            titleTextChanger.SetSoundEnabled(false);
        }

        // LocalizationManagerの取得を追加
        localizationManager = FindFirstObjectByType<LocalizationManager>();
        if (localizationManager == null && debugMode)
        {
            Debug.LogWarning("TitleTextLoader: LocalizationManagerが見つかりません");
        }
    }

    private void Start()
    {
        // 少し遅延させて確実にGameSaveManagerが初期化されてから実行
        Invoke("LoadAndApplyTitle", 0.1f);

        // 言語変更イベントの購読
        if (localizationManager != null)
        {
            localizationManager.OnLanguageChanged += OnLanguageChanged;
        }
    }

    // イベントの購読解除
    private void OnDestroy()
    {
        // 言語変更イベントの購読解除
        if (localizationManager != null)
        {
            localizationManager.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    // 言語変更時のコールバックメソッドを追加
    private void OnLanguageChanged(UnityEngine.Localization.Locale newLocale)
    {
        if (debugMode)
        {
            Debug.Log($"TitleTextLoader: 言語が変更されました: {newLocale.Identifier.Code}");
        }

        // 現在の設定で再適用
        RefreshTitleText();
    }

    /// <summary>
    /// afterChangeToHerMemoryフラグを取得（共通処理）
    /// </summary>
    private bool GetAfterChangeFlag()
    {
        // GameSaveManagerからフラグを取得
        if (GameSaveManager.Instance != null)
        {
            return GameSaveManager.Instance.GetAfterChangeToHerMemoryFlag();
        }

        if (debugMode)
            Debug.Log("TitleTextLoader: GameSaveManagerが存在しないため、false を返します");
        return false;
    }

    /// <summary>
    /// セーブデータからフラグを読み込み、タイトルテキストを設定
    /// afterChangeToHerMemory=falseの場合は何もしない
    /// </summary>
    private void LoadAndApplyTitle()
    {
        // afterChangeToLastフラグがtrueの場合は処理をスキップ
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.GetAfterChangeToLastFlag())
        {
            if (debugMode) Debug.Log("TitleTextLoader: afterChangeToLastがtrueのため処理をスキップします");
            return;
        }

        try
        {
            bool afterChangeFlag = GetAfterChangeFlag();

            if (debugMode)
            {
                Debug.Log($"TitleTextLoader: afterChangeToHerMemoryフラグ = {afterChangeFlag}");
                Debug.Log($"TitleTextLoader: 現在のタイトルテキスト = '{titleText?.text}'");
            }

            // フラグに基づいてテキストを設定
            ApplyTitleText(afterChangeFlag);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"TitleTextLoader: タイトルテキスト設定中にエラー: {ex.Message}");
        }
    }


    /// <summary>
    /// フラグに基づいてタイトルテキストを適用
    /// </summary>
    /// <param name="useChangedTitle">変更後テキストを使用するかどうか</param>
    private void ApplyTitleText(bool useChangedTitle)
    {
        if (titleText == null)
        {
            Debug.LogError("TitleTextLoader: titleTextがnullです");
            return;
        }

        // 状態を記録
        isUsingChangedTitle = useChangedTitle;

        // Localize String Eventコンポーネントの制御
        if (useChangedTitle && localizeStringEvent != null)
        {
            // afterChangeToHerMemory = true の場合、Localize String Eventを無効化
            localizeStringEvent.enabled = false;
            if (debugMode)
            {
                Debug.Log("TitleTextLoader: Localize String Eventを無効化しました");
            }
        }
        else if (!useChangedTitle && localizeStringEvent != null)
        {
            // 通常タイトルの場合は有効化（ローカライズシステムに任せる）
            localizeStringEvent.enabled = true;
            if (debugMode)
            {
                Debug.Log("TitleTextLoader: Localize String Eventを有効化しました");
            }
            return; // Localize String Eventに処理を任せる
        }

        // 手動でテキストを設定（afterChangeToHerMemory = true の場合のみ）
        if (useChangedTitle)
        {
            // 現在の言語を確認
            bool isEnglish = false;
            if (localizationManager != null)
            {
                string currentLanguage = localizationManager.GetCurrentLanguageCode();
                isEnglish = (currentLanguage == "en");
                if (debugMode)
                {
                    Debug.Log($"TitleTextLoader: 現在の言語 = {currentLanguage}");
                }
            }

            // 言語に応じてテキストを選択
            string targetText = isEnglish ? changedTitleTextEnglish : changedTitleText;

            // テキストが空でないことを確認
            if (string.IsNullOrEmpty(targetText))
            {
                Debug.LogWarning($"TitleTextLoader: 設定するテキストが空です (isEnglish: {isEnglish})");
                // フォールバック
                targetText = isEnglish ? "Memories of 'Her'" : "「彼女」の記憶";
            }

            titleText.text = targetText;

            if (debugMode)
            {
                Debug.Log($"TitleTextLoader: タイトルテキストを設定しました: '{targetText}' (変更後: {useChangedTitle}, 英語: {isEnglish})");
            }
        }
    }

    /// <summary>
    /// 外部から手動でタイトルテキストを更新
    /// afterChangeToHerMemory=falseの場合は何もしない
    /// </summary>
    public void RefreshTitleText()
    {
        LoadAndApplyTitle();
    }

    /// <summary>
    /// 通常タイトルを強制設定（デバッグ用）
    /// </summary>
    [ContextMenu("Debug: Set Normal Title")]
    public void SetNormalTitle()
    {
        ApplyTitleText(false);
    }

    /// <summary>
    /// 変更後タイトルを強制設定（デバッグ用）
    /// </summary>
    [ContextMenu("Debug: Set Changed Title")]
    public void SetChangedTitle()
    {
        ApplyTitleText(true);
    }

    /// <summary>
    /// 現在のフラグ状態を確認（デバッグ用）
    /// </summary>
    [ContextMenu("Debug: Check Flag Status")]
    public void CheckFlagStatus()
    {
        bool gameSaveFlag = GameSaveManager.Instance?.GetAfterChangeToHerMemoryFlag() ?? false;
        bool titleChangerFlag = titleTextChanger?.GetAfterChangeToHerMemoryFlag() ?? false;
        bool localizeEnabled = localizeStringEvent?.enabled ?? false;

        Debug.Log($"=== TitleTextLoader フラグ状態 ===");
        Debug.Log($"GameSaveManager: {gameSaveFlag}");
        Debug.Log($"TitleTextChanger: {titleChangerFlag}");
        Debug.Log($"LocalizeStringEvent有効: {localizeEnabled}");
        Debug.Log($"現在のタイトル: '{titleText?.text}'");
        Debug.Log($"変更後タイトル使用中: {isUsingChangedTitle}");
        Debug.Log($"==============================");
    }

    /// <summary>
    /// TitleTextChangerから設定を再取得
    /// </summary>
    public void RefreshFromTitleTextChanger()
    {
        if (titleTextChanger == null)
        {
            titleTextChanger = FindFirstObjectByType<TitleTextChanger>();
        }

        if (titleTextChanger != null)
        {
            normalTitleText = titleTextChanger.OriginalTitleText;
            changedTitleText = titleTextChanger.NewTitleText;

            if (debugMode)
            {
                Debug.Log($"TitleTextLoader: TitleTextChangerから設定を更新しました");
                Debug.Log($"通常テキスト: '{normalTitleText}'");
                Debug.Log($"変更後テキスト: '{changedTitleText}'");
            }
        }
    }

    // プロパティでアクセス可能にする
    public string NormalTitleText => normalTitleText;
    public string ChangedTitleText => changedTitleText;
    public bool IsShowingChangedTitle => titleText?.text == changedTitleText;
}