using System.Collections;
using UnityEngine;
using TMPro;

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

    [Header("TitleTextChangerForMonologueScene参照")]
    [Tooltip("TitleTextChangerForMonologueSceneへの参照（オプション）")]
    [SerializeField] private TitleTextChangerForMonologueScene titleTextChangerForMonologue;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceAllFlagsTrue = false; // テスト用の強制変更

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
            if (debugMode) Debug.Log("TitleTextLoaderForMonologueScene: 遅延実行で効果音を無効化しました");
        }
    }

    private void Start()
    {
        // 少し遅延させて確実にGameSaveManagerが初期化されてから実行
        StartCoroutine(LoadAndApplyTitleDelayed());
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
        bool shouldChangeFinal = false;

        // デバッグモードでの強制変更
        if (debugMode && forceAllFlagsTrue)
        {
            shouldChangeFinal = true;
            if (debugMode) Debug.Log("TitleTextLoaderForMonologueScene: デバッグモードで強制的にタイトルを変更");
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
                Debug.Log($"TitleTextLoaderForMonologueScene: afterChangeToHerMemory = {herMemoryFlag}");
                Debug.Log($"TitleTextLoaderForMonologueScene: afterChangeToHisFuture = {hisFutureFlag}");
                Debug.Log($"TitleTextLoaderForMonologueScene: afterChangeToLast = {lastFlag}");
                Debug.Log($"TitleTextLoaderForMonologueScene: 全フラグ条件 = {shouldChangeFinal}");
            }

        }

        // すべてのフラグがtrueの場合のみタイトルを変更 ※それ以外は何もしない
        if (shouldChangeFinal)
        {
            string textToApply = finalTitleText;
            titleText.text = textToApply;

            if (debugMode)
            {
                Debug.Log($"TitleTextLoaderForMonologueScene: タイトルを '{finalTitleText}' に設定しました");
            }
        }
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