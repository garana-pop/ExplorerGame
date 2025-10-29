using System;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

/// <summary>
/// Steam実績の管理を行うクラス
/// </summary>
public class SteamAchievementManager : MonoBehaviour
{
    #region Singleton Pattern

    private static SteamAchievementManager instance;

    /// <summary>
    /// SteamAchievementManagerのシングルトンインスタンス
    /// </summary>
    public static SteamAchievementManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<SteamAchievementManager>();

                if (instance == null)
                {
                    GameObject go = new GameObject("SteamAchievementManager");
                    instance = go.AddComponent<SteamAchievementManager>();
                }
            }
            return instance;
        }
    }

    #endregion

    #region Fields

    [Header("設定")]
    [Tooltip("デバッグログを表示するか")]
    [SerializeField] private bool debugMode = true;

    [Tooltip("実績解除をローカルキャッシュに保存するか")]
    [SerializeField] private bool cacheAchievements = true;

    // 実績解除済みのキャッシュ
    private HashSet<string> unlockedAchievements = new HashSet<string>();

    // Steam初期化状態
    private bool isInitialized = false;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // シングルトンパターン実装
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Startで初期化することで、SteamManagerの初期化完了を待つ
        Initialize();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// 実績システムの初期化
    /// </summary>
    private void Initialize()
    {
        // SteamManagerの存在確認
        if (SteamManager.Instance == null)
        {
            LogWarning("SteamManagerが見つかりません。実績機能は動作しません。");
            return;
        }

        // Steam APIが初期化されているか確認(複数回チェック)
        if (!SteamManager.Instance.IsInitialized())
        {
            // 初期化が完了していない場合は、遅延して再試行
            StartCoroutine(RetryInitialization());
            return;
        }

        CompleteInitialization();
    }

    /// <summary>
    /// 初期化のリトライ処理
    /// </summary>
    private System.Collections.IEnumerator RetryInitialization()
    {
        int retryCount = 0;
        const int maxRetries = 10;
        const float retryInterval = 0.5f;

        while (retryCount < maxRetries)
        {
            yield return new WaitForSeconds(retryInterval);

            if (SteamManager.Instance != null && SteamManager.Instance.IsInitialized())
            {
                CompleteInitialization();
                yield break;
            }

            retryCount++;
        }

        LogWarning("Steam APIの初期化を待機しましたが、タイムアウトしました。実績機能は動作しません。");
    }

    /// <summary>
    /// 初期化の完了処理
    /// </summary>
    private void CompleteInitialization()
    {
        isInitialized = true;

        // キャッシュが有効な場合、既存の実績を読み込む
        // ※ユーザー統計情報の受信を待ってから読み込む
        if (cacheAchievements)
        {
            StartCoroutine(WaitForUserStatsAndLoad());
        }
        else
        {
            LogDebug("SteamAchievementManagerが初期化されました");
        }
    }

    /// <summary>
    /// ユーザー統計情報の受信を待って実績を読み込む
    /// </summary>
    private System.Collections.IEnumerator WaitForUserStatsAndLoad()
    {
        int waitCount = 0;
        const int maxWait = 20; // 最大10秒待機(0.5秒 × 20回)

        while (!SteamManager.Instance.IsUserStatsReceived() && waitCount < maxWait)
        {
            yield return new WaitForSeconds(0.5f);
            waitCount++;
        }

        if (SteamManager.Instance.IsUserStatsReceived())
        {
            LoadUnlockedAchievements();
            LogDebug("SteamAchievementManagerが初期化されました");
        }
        else
        {
            LogWarning("ユーザー統計情報の受信がタイムアウトしました。実績機能が正常に動作しない可能性があります。");
        }
    }

    #endregion

    #region Achievement Management

    /// <summary>
    /// 実績を解除する
    /// </summary>
    /// <param name="achievementApiName">実績のAPI名</param>
    /// <returns>解除に成功した場合true</returns>
    public bool UnlockAchievement(string achievementApiName)
    {
        if (string.IsNullOrEmpty(achievementApiName))
        {
            LogError("実績API名が無効です");
            return false;
        }

        // Steam APIが初期化されていない場合
        if (!isInitialized || !SteamManager.Instance.IsInitialized())
        {
            LogWarning($"Steam APIが初期化されていないため、実績 '{achievementApiName}' を解除できません");
            return false;
        }

        // ユーザー統計情報が受信されていない場合
        if (!SteamManager.Instance.IsUserStatsReceived())
        {
            LogWarning($"ユーザー統計情報が受信されていないため、実績 '{achievementApiName}' を解除できません");
            return false;
        }

        // キャッシュをチェック（既に解除済みの場合はスキップ）
        if (cacheAchievements && unlockedAchievements.Contains(achievementApiName))
        {
            LogDebug($"実績 '{achievementApiName}' は既に解除済みです");
            return true;
        }

        try
        {
            // 実績が存在するかチェック
            bool currentState = false;
            if (!SteamUserStats.GetAchievement(achievementApiName, out currentState))
            {
                LogError($"実績 '{achievementApiName}' が見つかりません。API名が正しいか確認してください。");
                return false;
            }

            // 既に解除済みの場合
            if (currentState)
            {
                LogDebug($"実績 '{achievementApiName}' は既に解除済みです(Steam側で確認)");
                if (cacheAchievements)
                {
                    unlockedAchievements.Add(achievementApiName);
                }
                return true;
            }

            // 実績を解除
            bool success = SteamUserStats.SetAchievement(achievementApiName);

            if (success)
            {
                // Steamサーバーに送信
                bool stored = SteamUserStats.StoreStats();

                if (stored)
                {
                    // キャッシュに追加
                    if (cacheAchievements)
                    {
                        unlockedAchievements.Add(achievementApiName);
                    }

                    LogDebug($"実績 '{achievementApiName}' を解除しました");
                    return true;
                }
                else
                {
                    LogWarning($"実績 '{achievementApiName}' の保存に失敗しました");
                }
            }
            else
            {
                LogError($"実績 '{achievementApiName}' の解除に失敗しました");
            }
        }
        catch (Exception e)
        {
            LogError($"実績解除中にエラーが発生しました: {e.Message}");
        }

        return false;
    }

    /// <summary>
    /// 実績が解除済みかチェック
    /// </summary>
    /// <param name="achievementApiName">実績のAPI名</param>
    /// <returns>解除済みの場合true</returns>
    public bool IsAchievementUnlocked(string achievementApiName)
    {
        if (string.IsNullOrEmpty(achievementApiName))
        {
            return false;
        }

        // キャッシュをチェック
        if (cacheAchievements && unlockedAchievements.Contains(achievementApiName))
        {
            return true;
        }

        // Steam APIが初期化されていない場合
        if (!isInitialized || !SteamManager.Instance.IsInitialized())
        {
            return false;
        }

        // Steam APIから直接チェック
        bool achieved = false;
        if (SteamUserStats.GetAchievement(achievementApiName, out achieved))
        {
            // キャッシュを更新
            if (achieved && cacheAchievements)
            {
                unlockedAchievements.Add(achievementApiName);
            }
            return achieved;
        }

        return false;
    }

    /// <summary>
    /// 解除済みの実績をキャッシュに読み込み
    /// </summary>
    private void LoadUnlockedAchievements()
    {
        if (!isInitialized || !SteamManager.Instance.IsInitialized())
        {
            return;
        }

        unlockedAchievements.Clear();

        // Steamから実績の数を取得
        uint numAchievements = SteamUserStats.GetNumAchievements();

        for (uint i = 0; i < numAchievements; i++)
        {
            string achievementName = SteamUserStats.GetAchievementName(i);
            bool achieved = false;

            if (SteamUserStats.GetAchievement(achievementName, out achieved) && achieved)
            {
                unlockedAchievements.Add(achievementName);
            }
        }

        LogDebug($"{unlockedAchievements.Count}個の実績が解除済みです");
    }

    #endregion

    #region Debug Methods

#if UNITY_EDITOR
    /// <summary>
    /// テスト用：全実績をリセット（エディタのみ）
    /// </summary>
    [ContextMenu("全実績をリセット（テスト用）")]
    private void ResetAllAchievements()
    {
        if (!isInitialized || !SteamManager.Instance.IsInitialized())
        {
            LogWarning("Steam APIが初期化されていません");
            return;
        }

        if (SteamUserStats.ResetAllStats(true))
        {
            unlockedAchievements.Clear();
            LogDebug("全実績をリセットしました");
        }
        else
        {
            LogError("実績のリセットに失敗しました");
        }
    }

    /// <summary>
    /// テスト用：実績一覧を表示（エディタのみ）
    /// </summary>
    [ContextMenu("実績一覧を表示")]
    private void ShowAllAchievements()
    {
        if (!isInitialized || !SteamManager.Instance.IsInitialized())
        {
            LogWarning("Steam APIが初期化されていません");
            return;
        }

        uint numAchievements = SteamUserStats.GetNumAchievements();
        Debug.Log($"===== 実績一覧 ({numAchievements}個) =====");

        for (uint i = 0; i < numAchievements; i++)
        {
            string achievementName = SteamUserStats.GetAchievementName(i);
            bool achieved = false;
            SteamUserStats.GetAchievement(achievementName, out achieved);

            Debug.Log($"{i + 1}. {achievementName}: {(achieved ? "解除済み" : "未解除")}");
        }
    }
#endif

    #endregion

    #region Logging

    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[SteamAchievementManager] {message}");
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[SteamAchievementManager] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[SteamAchievementManager] {message}");
    }

    #endregion
}