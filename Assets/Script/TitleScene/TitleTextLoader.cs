using UnityEngine;
using TMPro;

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

    [Header("TitleTextChanger参照")]
    [Tooltip("TitleTextChangerへの直接参照（オプション）")]
    [SerializeField] private TitleTextChanger titleTextChanger;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceChangedTitle = false; // テスト用の強制変更

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
    }

    private void Start()
    {
        // 少し遅延させて確実にGameSaveManagerが初期化されてから実行
        Invoke("LoadAndApplyTitle", 0.1f);
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

        string targetText = useChangedTitle ? changedTitleText : normalTitleText;

        // テキストが空でないことを確認
        if (string.IsNullOrEmpty(targetText))
        {
            Debug.LogWarning($"TitleTextLoader: 設定するテキストが空です (useChangedTitle: {useChangedTitle})");
            targetText = useChangedTitle ? "「彼女」の記憶" : "「彼」の記憶"; // フォールバック
        }

        titleText.text = targetText;

        if (debugMode)
        {
            Debug.Log($"TitleTextLoader: タイトルテキストを設定しました: '{targetText}' (変更後: {useChangedTitle})");
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

        Debug.Log($"=== TitleTextLoader フラグ状態 ===");
        Debug.Log($"GameSaveManager: {gameSaveFlag}");
        Debug.Log($"TitleTextChanger: {titleChangerFlag}");
        Debug.Log($"現在のタイトル: '{titleText?.text}'");
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