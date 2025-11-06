using UnityEngine;

/// <summary>
/// Steam実績「真実の片鱗」(HintsOfTruth_2_ACHIEVEMENTS_UNLOCK)の解除を管理するコンポーネント
/// </summary>
/// <remarks>
/// 「思い出ファイル-偶然の再会.txt」オブジェクトにアタッチし、
/// FileIconChangeクラスのパズル完了イベントを受け取って実績を解除する
/// </remarks>
public class SteamAchievementsUnlock2_HintsOfTruth : MonoBehaviour
{
    #region Inspector設定

    [Header("参照設定")]
    [Tooltip("パズル完了を監視するFileIconChangeコンポーネント（自動取得可）")]
    [SerializeField] private FileIconChange fileIconChange;

    [Header("Steam設定")]
    [Tooltip("解除するSteam実績のAPI名")]
    [SerializeField] private string achievementApiName = "HintsOfTruth_2_ACHIEVEMENTS_UNLOCK";

    [Header("デバッグ")]
    [Tooltip("デバッグモードの有効化")]
    [SerializeField] private bool debugMode = false;

    #endregion

    #region Private変数

    private bool isAchievementUnlocked = false; // 実績解除済みフラグ

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// コンポーネント初期化時の処理
    /// </summary>
    private void Awake()
    {
        // FileIconChangeの参照が未設定の場合、同じGameObjectから取得
        if (fileIconChange == null)
        {
            fileIconChange = GetComponent<FileIconChange>();

            if (fileIconChange == null)
            {
                LogError("FileIconChangeコンポーネントが見つかりません。同じGameObjectにアタッチされているか確認してください。");
            }
        }
    }

    /// <summary>
    /// オブジェクトが有効化された時の処理
    /// </summary>
    private void OnEnable()
    {
        // イベントリスナーを登録
        if (fileIconChange != null && fileIconChange.onPuzzleCompleted != null)
        {
            fileIconChange.onPuzzleCompleted.AddListener(OnPuzzleCompleted);
            LogDebug("パズル完了イベントリスナーを登録しました");
        }
    }

    /// <summary>
    /// オブジェクトが無効化された時の処理
    /// </summary>
    private void OnDisable()
    {
        // イベントリスナーを解除
        if (fileIconChange != null && fileIconChange.onPuzzleCompleted != null)
        {
            fileIconChange.onPuzzleCompleted.RemoveListener(OnPuzzleCompleted);
            LogDebug("パズル完了イベントリスナーを解除しました");
        }
    }

    #endregion

    #region イベントハンドラー

    /// <summary>
    /// パズル完了時に呼び出されるイベントハンドラー
    /// </summary>
    private void OnPuzzleCompleted()
    {
        // 既に解除済みの場合は処理をスキップ
        if (isAchievementUnlocked)
        {
            LogDebug("実績は既に解除済みです");
            return;
        }

        // Steam実績を解除
        UnlockSteamAchievement();
    }

    #endregion

    #region Steam実績解除処理

    /// <summary>
    /// Steam実績を解除する
    /// </summary>
    private void UnlockSteamAchievement()
    {
        // SteamAchievementManagerが存在するか確認
        if (SteamAchievementManager.Instance == null)
        {
            LogError("SteamAchievementManagerが存在しません。実績を解除できませんでした。");
            return;
        }

        // 実績を解除
        bool success = SteamAchievementManager.Instance.UnlockAchievement(achievementApiName);

        if (success)
        {
            isAchievementUnlocked = true;
            LogDebug($"Steam実績「真実の片鱗」({achievementApiName})を解除しました");
        }
        else
        {
            LogError($"Steam実績の解除に失敗しました: {achievementApiName}");
        }
    }

    #endregion

    #region ログ出力

    /// <summary>
    /// デバッグログを出力
    /// </summary>
    /// <param name="message">ログメッセージ</param>
    private void LogDebug(string message)
    {
        if (debugMode)
        {
            DebugLogger.Log($"[{nameof(SteamAchievementsUnlock2_HintsOfTruth)}] {message}");
        }
    }

    /// <summary>
    /// エラーログを出力
    /// </summary>
    /// <param name="message">エラーメッセージ</param>
    private void LogError(string message)
    {
        Debug.LogError($"[{nameof(SteamAchievementsUnlock2_HintsOfTruth)}] {message}");
    }

    #endregion
}