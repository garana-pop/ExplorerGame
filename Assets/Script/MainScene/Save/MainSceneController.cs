using System;
using UnityEngine;

/// <summary>
/// MainSceneロード時にセーブデータを読み込むコントローラー
/// </summary>
public class MainSceneController : MonoBehaviour
{
    [Tooltip("起動時にセーブデータを自動で読み込むかどうか")]
    [SerializeField] private bool loadSaveDataOnStart = true;

    [Tooltip("デバッグログを表示するかどうか")]
    [SerializeField] private bool debugMode = false;

    // GameSaveManagerへの参照（オプションでインスペクタから設定可能）
    [SerializeField] private GameSaveManager saveManager;

    /// <summary>
    /// 起動時の処理
    /// </summary>
    private void Start()
    {
        InitializeSaveManager();

        if (loadSaveDataOnStart)
        {
            LoadSaveData();

            // セーブデータ読み込み後にフォルダー解放実績をチェック
            CheckAndUnlockFolderAchievements();
        }

        // Steam実績「記憶の扉」を解除
        UnlockDoorOfMemoryAchievement();
    }

    /// <summary>
    /// SaveManagerの初期化
    /// </summary>
    private void InitializeSaveManager()
    {
        // すでに設定されている場合は何もしない
        if (saveManager != null) return;

        // GameSaveManagerを取得
        saveManager = GameSaveManager.Instance;

        // インスタンスが見つからない場合は新規作成
        if (saveManager == null)
        {
            LogDebug("GameSaveManagerが見つかりません。新しく作成します。");
            GameObject saveManagerObj = new GameObject("GameSaveManager");
            saveManager = saveManagerObj.AddComponent<GameSaveManager>();
        }
    }

    /// <summary>
    /// セーブデータを読み込む
    /// </summary>
    private void LoadSaveData()
    {
        try
        {
            if (saveManager == null)
            {
                LogWarning("GameSaveManagerが見つかりません。セーブデータの読み込みをスキップします。");
                return;
            }

            // セーブデータを読み込んで適用
            bool loadSuccess = saveManager.LoadGameAndApply();

            if (debugMode)
            {
                if (loadSuccess)
                {
                    LogDebug($"セーブデータを読み込みました。保存日時: {saveManager.GetLastSaveTimestamp()}");
                }
                else
                {
                    LogDebug("セーブデータがないか、読み込みに失敗しました。新規ゲームとして開始します。");
                }
            }
        }
        catch (Exception ex)
        {
            LogError($"セーブデータの読み込み中にエラーが発生しました: {ex.Message}");
        }
    }

    /// <summary>
    /// 「記憶の扉」実績を解除
    /// OpeningSceneを完了してMainSceneに移行した際に呼び出される
    /// </summary>
    private void UnlockDoorOfMemoryAchievement()
    {
        // GameSaveManagerで OpeningScene完了フラグを確認
        if (saveManager != null && saveManager.GetEndOpeningSceneFlag())
        {
            // 既にOpeningSceneをクリアしているが、実績が未解除の可能性がある場合
            // SteamAchievementManagerを通じて実績を解除
            if (SteamAchievementManager.Instance != null)
            {
                bool unlocked = SteamAchievementManager.Instance.UnlockAchievement("DoorOfMemory_1_ACHIEVEMENTS_UNLOCK");

                if (debugMode && unlocked)
                {
                    LogDebug("Steam実績「記憶の扉」を解除しました");
                }
            }
            else
            {
                if (debugMode)
                {
                    LogWarning("SteamAchievementManagerが見つからないため、実績を解除できませんでした");
                }
            }
        }
    }

    // デバッグ用のログメソッド
    private void LogDebug(string message)
    {
        if (debugMode) DebugLogger.Log($"[MainSceneController] {message}");
    }

    private void LogWarning(string message)
    {
        DebugLogger.LogWarning($"[MainSceneController] {message}");
    }

    private void LogError(string message)
    {
        DebugLogger.LogError($"[MainSceneController] {message}");
    }

    /// <summary>
    /// セーブデータからフォルダー解放状態を確認し、対応するSteam実績を解除
    /// </summary>
    private void CheckAndUnlockFolderAchievements()
    {
        // SaveManagerが存在しない場合は処理をスキップ
        if (saveManager == null)
        {
            if (debugMode)
            {
                LogWarning("GameSaveManagerが見つからないため、フォルダー実績チェックをスキップします");
            }
            return;
        }

        // セーブデータを取得
        GameSaveData saveData = saveManager.GetCurrentSaveData();
        if (saveData == null || saveData.folderState == null)
        {
            if (debugMode)
            {
                LogDebug("セーブデータまたはフォルダー状態が存在しないため、フォルダー実績チェックをスキップします");
            }
            return;
        }

        // SteamAchievementManagerが存在しない場合は処理をスキップ
        if (SteamAchievementManager.Instance == null)
        {
            if (debugMode)
            {
                LogWarning("SteamAchievementManagerが見つからないため、フォルダー実績チェックをスキップします");
            }
            return;
        }

        // 解放されているフォルダーを確認
        string[] activatedFolders = saveData.folderState.activatedFolders;
        if (activatedFolders == null || activatedFolders.Length == 0)
        {
            if (debugMode)
            {
                LogDebug("解放されているフォルダーがありません");
            }
            return;
        }

        // 各フォルダーに対応する実績をチェック
        int unlockedCount = 0;
        foreach (string folderName in activatedFolders)
        {
            string achievementApiName = GetFolderAchievementApiName(folderName);

            // 対応する実績がない場合はスキップ
            if (string.IsNullOrEmpty(achievementApiName))
            {
                continue;
            }

            // 実績が未解除の場合のみ解除
            if (!SteamAchievementManager.Instance.IsAchievementUnlocked(achievementApiName))
            {
                bool unlocked = SteamAchievementManager.Instance.UnlockAchievement(achievementApiName);

                if (unlocked)
                {
                    unlockedCount++;
                    if (debugMode)
                    {
                        LogDebug($"ロード時にSteam実績を解除: {folderName}フォルダー（API: {achievementApiName}）");
                    }
                }
            }
            else
            {
                if (debugMode)
                {
                    LogDebug($"フォルダー「{folderName}」の実績は既に解除済みです");
                }
            }
        }

        if (debugMode && unlockedCount > 0)
        {
            LogDebug($"ロード時に{unlockedCount}個のフォルダー実績を解除しました");
        }
    }

    /// <summary>
    /// フォルダー名から対応するSteam実績API名を取得
    /// </summary>
    /// <param name="folderName">フォルダー名</param>
    /// <returns>対応するAPI名。該当なしの場合はnull</returns>
    private string GetFolderAchievementApiName(string folderName)
    {
        switch (folderName)
        {
            case "恋人":
                return "MemoryOfPain_3_ACHIEVEMENTS_UNLOCK";
            case "友人":
                return "Rejection_4_ACHIEVEMENTS_UNLOCK";
            case "記録":
                return "ConfrontingReality_5_ACHIEVEMENTS_UNLOCK";
            case "願い":
                return "Acceptance_6_ACHIEVEMENTS_UNLOCK";
            default:
                return null;
        }
    }
}