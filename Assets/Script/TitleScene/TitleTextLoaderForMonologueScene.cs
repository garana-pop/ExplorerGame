using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Localization.Components;
using ExplorerGame.Localization;

/// <summary>
/// ゲームロード時にafterChangeToHerMemory、afterChangeToHisFuture、afterChangeToLastフラグをチェックして
/// すべてtrueの場合、タイトルを"Thanks for playing the game."に表示するクラス
/// </summary>
public class TitleTextLoaderForMonologueScene : MonoBehaviour
{
    [Header("テキスト設定")]
    [Tooltip("表示対象のTextMeshProコンポーネント")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("すべてのフラグがtrue時のタイトルテキスト")]
    [SerializeField] private string finalTitleText = "Thanks for playing the game.";

    [Header("ローカライズ設定")]
    [Tooltip("英語での最終タイトルテキスト")]
    [SerializeField] private string finalTitleTextEnglish = "Thanks for playing the game.";

    [Header("TitleTextChangerForMonologueScene参照")]
    [Tooltip("TitleTextChangerForMonologueSceneへの参照（オプション）")]
    [SerializeField] private TitleTextChangerForMonologueScene titleTextChangerForMonologue;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceAllFlagsTrue = false; // テスト用の強制変更

    // コンポーネント参照
    private LocalizeStringEvent localizeStringEvent;

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
            Debug.LogError("TitleTextLoaderForMonologueScene: TextMeshProコンポーネントが見つかりません。インスペクターで設定してください。");
            enabled = false;
            return;
        }

        // Localize String Eventコンポーネントの取得
        localizeStringEvent = titleText.GetComponent<LocalizeStringEvent>();

        // TitleTextChangerForMonologueSceneの自動検索
        if (titleTextChangerForMonologue == null)
        {
            titleTextChangerForMonologue = FindFirstObjectByType<TitleTextChangerForMonologueScene>();
        }

        // 効果音を確実に無効化
        if (titleTextChangerForMonologue != null)
        {
            // 即座に無効化
            titleTextChangerForMonologue.SetSoundEnabled(false);

            // コルーチンで遅延実行も追加（念のため）
            StartCoroutine(DisableSoundDelayed());
        }
    }

    private IEnumerator DisableSoundDelayed()
    {
        yield return null; // 1フレーム待機

        if (titleTextChangerForMonologue != null)
        {
            titleTextChangerForMonologue.SetSoundEnabled(false);
            if (debugMode) DebugLogger.Log("TitleTextLoaderForMonologueScene: 遅延実行で効果音を無効化しました");
        }
    }

    private void Start()
    {
        // 少し遅延させて確実にGameSaveManagerが初期化されてから実行
        StartCoroutine(LoadAndApplyTitleDelayed());

        // 言語変更イベントの購読
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }
    }

    private void OnDestroy()
    {
        // イベントの購読解除
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    /// <summary>
    /// 言語変更時のコールバック
    /// </summary>
    /// <param name="newLocale">新しいロケール</param>
    private void OnLanguageChanged(UnityEngine.Localization.Locale newLocale)
    {
        LoadAndApplyTitle();
    }

    /// <summary>
    /// 遅延後にタイトルテキストを読み込んで適用
    /// </summary>
    private IEnumerator LoadAndApplyTitleDelayed()
    {
        // GameSaveManagerの初期化を待つ
        yield return new WaitForSeconds(0.1f);

        LoadAndApplyTitle();

    }

    /// <summary>
    /// セーブデータからフラグを読み込んでタイトルテキストを適用
    /// </summary>
    private void LoadAndApplyTitle()
    {
        // Localize String Eventを無効化（手動制御のため）
        if (localizeStringEvent != null)
        {
            localizeStringEvent.enabled = false;
        }

        bool shouldChangeFinal = false;

        // デバッグモードでの強制変更
        if (debugMode && forceAllFlagsTrue)
        {
            shouldChangeFinal = true;
            if (debugMode) DebugLogger.Log("TitleTextLoaderForMonologueScene: デバッグモードで強制的にタイトルを変更");
        }
        else
        {
            // 3つのフラグをチェック
            bool herMemoryFlag = GetAfterChangeToHerMemoryFlag();
            bool hisFutureFlag = GetAfterChangeToHisFutureFlag();
            bool lastFlag = GetAfterChangeToLastFlag();

            // すべてのフラグがtrueの場合のみタイトルを変更
            shouldChangeFinal = herMemoryFlag && hisFutureFlag && lastFlag;

            if (debugMode)
            {
                DebugLogger.Log($"TitleTextLoaderForMonologueScene: afterChangeToHerMemory = {herMemoryFlag}");
                DebugLogger.Log($"TitleTextLoaderForMonologueScene: afterChangeToHisFuture = {hisFutureFlag}");
                DebugLogger.Log($"TitleTextLoaderForMonologueScene: afterChangeToLast = {lastFlag}");
                DebugLogger.Log($"TitleTextLoaderForMonologueScene: 全フラグ条件 = {shouldChangeFinal}");
            }

        }

        // すべてのフラグがtrueの場合のみタイトルを変更 ※それ以外は何もしない
        if (shouldChangeFinal)
        {
            // 現在の言語設定を取得
            string currentLanguageCode = GetCurrentLanguageCode();
            bool isEnglish = currentLanguageCode == "en";
            //bool isChinese = currentLanguageCode == "zh"; 中国語対応は不要のためコメントアウト

            // 言語コードが英語の場合は、英語テキストを適用
            string textToApply = isEnglish ? finalTitleTextEnglish : finalTitleText;
            //string textToApply = isChinese ? finalTitleTextChinese : finalTitleText; 中国語対応は不要のためコメントアウト

            titleText.text = textToApply;


            if (debugMode)
            {
                DebugLogger.Log("TitleTextLoaderForMonologueScene: 現在の言語コード" + currentLanguageCode);
                DebugLogger.Log($"TitleTextLoaderForMonologueScene: タイトルを '{textToApply}' に設定しました");
            }
        }
    }

    /// <summary>
    /// 現在の言語コードを取得
    /// </summary>
    private string GetCurrentLanguageCode()
    {
        if (LocalizationManager.Instance != null)
        {
            return LocalizationManager.Instance.GetCurrentLanguageCode();
        }

        // LocalizationManagerが存在しない場合は日本語をデフォルトとする
        return "ja";
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
        if (debugMode) Debug.LogWarning("TitleTextLoaderForMonologueScene: GameSaveManagerが存在しないため、afterChangeToHerMemoryフラグを取得できませんでした");
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
        if (debugMode) Debug.LogWarning("TitleTextLoaderForMonologueScene: GameSaveManagerが存在しないため、afterChangeToHisFutureフラグを取得できませんでした");
        return false;
    }

    /// <summary>
    /// afterChangeToLastフラグを取得
    /// </summary>
    private bool GetAfterChangeToLastFlag()
    {
        // まずTitleTextChangerForMonologueSceneの静的フラグをチェック
        if (TitleTextChangerForMonologueScene.IsTitleChanged())
        {
            return true;
        }

        // GameSaveManagerから取得
        if (GameSaveManager.Instance != null)
        {
            return GameSaveManager.Instance.GetAfterChangeToLastFlag();
        }

        // フラグが取得できない場合はfalseを返す
        if (debugMode) Debug.LogWarning("TitleTextLoaderForMonologueScene: GameSaveManagerが存在しないため、afterChangeToLastフラグを取得できませんでした");
        return false;
    }
}