using UnityEngine;

/// <summary>
/// Steam実績「真実の重み」(TheWeightOfTruth_7_ACHIEVEMENTS_UNLOCK)の解除を管理するコンポーネント
/// </summary>
/// <remarks>
/// DaughterRequestSceneでTextTyperが全てのテキストを表示したときに実績を解除
/// TextTyperコンポーネントのOnTypingCompletedイベントを監視
/// </remarks>
public class Steam7AchievementsUnlock_TheWeightOfTruth : MonoBehaviour
{
    #region Inspector設定

    [Header("参照設定")]
    [Tooltip("監視対象のTextTyperコンポーネント")]
    [SerializeField] private TextTyper textTyper;

    [Header("Steam設定")]
    [Tooltip("解除するSteam実績のAPI名")]
    [SerializeField] private string achievementApiName = "TheWeightOfTruth_7_ACHIEVEMENTS_UNLOCK";

    [Header("デバッグ")]
    [Tooltip("デバッグモードの有効化")]
    [SerializeField] private bool debugMode = false;

    #endregion

    #region Private変数

    private bool isAchievementUnlocked = false; // 実績解除済みフラグ
    private SteamAchievementManager steamAchievementManager; // Steam実績マネージャーの参照

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// コンポーネント初期化時の処理
    /// </summary>
    private void Awake()
    {
        // TextTyperコンポーネントの取得
        if (textTyper == null)
        {
            textTyper = GetComponent<TextTyper>();

            if (textTyper == null)
            {
                LogError("TextTyperコンポーネントが見つかりません。同じGameObjectにアタッチするか、Inspectorで設定してください。");
                enabled = false;
                return;
            }
        }

        // SteamAchievementManagerの取得
        steamAchievementManager = SteamAchievementManager.Instance;
        if (steamAchievementManager == null)
        {
            LogError("SteamAchievementManagerが見つかりません。シーンに存在することを確認してください。");
        }
    }

    /// <summary>
    /// オブジェクトが有効化された時の処理
    /// </summary>
    private void OnEnable()
    {
        // イベントリスナーを登録
        if (textTyper != null)
        {
            textTyper.OnTypingCompleted += OnTypingCompleted;
            LogDebug("TextTyperのタイピング完了イベントリスナーを登録しました");

            // 既に完了している場合は即時処理（タイミングによる取りこぼし対策）
            if (textTyper.IsCompleted && !isAchievementUnlocked)
            {
                LogDebug("TextTyperは既に完了済みです。実績解除を試行します。");
                UnlockSteamAchievement();
            }
        }
    }

    /// <summary>
    /// オブジェクトが無効化された時の処理
    /// </summary>
    private void OnDisable()
    {
        // イベントリスナーを解除
        if (textTyper != null)
        {
            textTyper.OnTypingCompleted -= OnTypingCompleted;
            LogDebug("TextTyperのタイピング完了イベントリスナーを解除しました");
        }
    }

    #endregion

    #region イベントハンドラー

    /// <summary>
    /// タイピング完了時に呼び出されるイベントハンドラー
    /// TextTyperが全てのテキストを表示したら実績を解除する（言語不問）
    /// </summary>
    private void OnTypingCompleted()
    {
        // 既に解除済みの場合は処理をスキップ
        if (isAchievementUnlocked)
        {
            LogDebug("実績は既に解除済みです");
            return;
        }

        // TextTyperの参照確認
        if (textTyper == null)
        {
            LogError("TextTyperの参照が失われています");
            return;
        }

        LogDebug("TextTyperのタイピングが完了しました — 実績を解除します");
        UnlockSteamAchievement();
    }

    #endregion

    #region Steam実績処理

    /// <summary>
    /// Steam実績を解除する
    /// </summary>
    private void UnlockSteamAchievement()
    {
        if (steamAchievementManager == null)
        {
            LogError("SteamAchievementManagerが利用できません");
            return;
        }

        // Steam実績を解除
        bool success = steamAchievementManager.UnlockAchievement(achievementApiName);

        if (success)
        {
            LogDebug($"Steam実績「{achievementApiName}」を正常に解除しました");
            isAchievementUnlocked = true;

            // 実績解除後の追加処理（必要に応じて）
            OnAchievementUnlocked();
        }
        else
        {
            LogError($"Steam実績「{achievementApiName}」の解除に失敗しました");
        }
    }

    /// <summary>
    /// 実績解除後の追加処理
    /// </summary>
    private void OnAchievementUnlocked()
    {
        // 必要に応じて、実績解除後のエフェクトや音楽の変更などを実装
        LogDebug("実績解除後の処理を実行");
    }

    #endregion

    #region デバッグ

    /// <summary>
    /// デバッグログ出力
    /// </summary>
    /// <param name="message">ログメッセージ</param>
    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"{nameof(Steam7AchievementsUnlock_TheWeightOfTruth)}: {message}");
        }
    }

    /// <summary>
    /// エラーログ出力
    /// </summary>
    /// <param name="message">エラーメッセージ</param>
    private void LogError(string message)
    {
        Debug.LogError($"{nameof(Steam7AchievementsUnlock_TheWeightOfTruth)}: {message}");
    }

    #endregion

    #region Public メソッド

    /// <summary>
    /// 手動で実績を解除（テスト用）
    /// </summary>
    [ContextMenu("手動で実績を解除")]
    public void ManualUnlockAchievement()
    {
        if (Application.isEditor && debugMode)
        {
            LogDebug("手動で実績解除を実行");
            UnlockSteamAchievement();
        }
    }

    /// <summary>
    /// 実績解除状態をリセット（テスト用）
    /// </summary>
    [ContextMenu("実績解除状態をリセット")]
    public void ResetAchievementState()
    {
        if (Application.isEditor && debugMode)
        {
            isAchievementUnlocked = false;
            LogDebug("実績解除状態をリセットしました");
        }
    }

    #endregion
}