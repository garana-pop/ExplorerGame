using UnityEngine;
using Steamworks;

/// <summary>
/// Steam APIの初期化と管理を行うシングルトンクラス
/// </summary>
public class SteamManager : MonoBehaviour
{
    #region Singleton

    private static SteamManager _instance;

    public static SteamManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SteamManager>();

                if (_instance == null)
                {
                    GameObject go = new GameObject("SteamManager");
                    _instance = go.AddComponent<SteamManager>();
                }
            }
            return _instance;
        }
    }

    #endregion

    #region Fields

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = true;

    private bool isInitialized = false;
    private bool isSteamRunning = false;
    private bool userStatsReceived = false;

    private Callback<UserStatsReceived_t> m_UserStatsReceived;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        // シングルトンの重複チェック
        if (_instance != null && _instance != this)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
            return;
        }

        _instance = this;

        // シーン間で保持
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }

        // Steam APIの初期化
        InitializeSteam();
    }

    private void Update()
    {
        // Steam APIのコールバックを処理
        if (isSteamRunning)
        {
            SteamAPI.RunCallbacks();
        }
    }

    private void OnDestroy()
    {
        // Steam APIのシャットダウン
        if (isSteamRunning)
        {
            SteamAPI.Shutdown();

            if (debugMode)
            {
                DebugLogger.Log($"{nameof(SteamManager)}: Steam APIをシャットダウンしました");
            }
        }
    }

    #endregion

    #region Steam Initialization

    /// <summary>
    /// Steam APIを初期化
    /// </summary>
    private void InitializeSteam()
    {
        try
        {
            // Steam APIの初期化
            if (SteamAPI.Init())
            {
                isSteamRunning = true;
                isInitialized = true;

                DebugLogger.Log($"[Steamworks.NET] SteamAPI_Init() success");

                // ユーザー統計情報受信コールバックの登録
                m_UserStatsReceived = Callback<UserStatsReceived_t>.Create(OnUserStatsReceived);

                // 現在のユーザーの統計情報をリクエスト
                RequestCurrentUserStats();

                if (debugMode)
                {
                    DebugLogger.Log($"{nameof(SteamManager)}: Steam API初期化成功");
                    DebugLogger.Log($"{nameof(SteamManager)}: App ID: {SteamUtils.GetAppID()}");
                    DebugLogger.Log($"{nameof(SteamManager)}: ユーザー名: {SteamFriends.GetPersonaName()}");
                }
            }
            else
            {
                DebugLogger.LogWarning($"{nameof(SteamManager)}: Steam API初期化失敗 - Steamクライアントが起動していない可能性があります");
                isInitialized = false;
                isSteamRunning = false;
            }
        }
        catch (System.Exception e)
        {
            DebugLogger.LogError($"{nameof(SteamManager)}: Steam API初期化中にエラーが発生: {e.Message}");
            isInitialized = false;
            isSteamRunning = false;
        }
    }

    /// <summary>
    /// ユーザー統計情報受信時のコールバック
    /// </summary>
    private void OnUserStatsReceived(UserStatsReceived_t pCallback)
    {
        if (pCallback.m_eResult == EResult.k_EResultOK)
        {
            userStatsReceived = true;

            if (debugMode)
            {
                DebugLogger.Log($"{nameof(SteamManager)}: ユーザー統計情報を受信しました");
            }
        }
        else
        {
            DebugLogger.LogWarning($"{nameof(SteamManager)}: ユーザー統計情報の受信に失敗: {pCallback.m_eResult}");
        }
    }

    /// <summary>
    /// 現在のユーザーの統計情報をリクエスト
    /// </summary>
    private void RequestCurrentUserStats()
    {
        if (isSteamRunning)
        {
            // 現在のユーザーのSteamIDを取得
            CSteamID steamID = SteamUser.GetSteamID();

            // ユーザー統計情報をリクエスト
            SteamAPICall_t handle = SteamUserStats.RequestUserStats(steamID);

            if (debugMode)
            {
                if (handle != SteamAPICall_t.Invalid)
                {
                    DebugLogger.Log($"{nameof(SteamManager)}: ユーザー統計情報のリクエストを送信しました");
                    DebugLogger.Log($"{nameof(SteamManager)}: SteamID: {steamID}");
                }
                else
                {
                    DebugLogger.LogWarning($"{nameof(SteamManager)}: ユーザー統計情報のリクエストに失敗しました");
                }
            }
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Steamが正常に初期化されているか確認
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized && isSteamRunning;
    }

    /// <summary>
    /// ユーザー統計情報が受信済みか確認
    /// </summary>
    public bool IsUserStatsReceived()
    {
        return userStatsReceived;
    }

    /// <summary>
    /// 現在のユーザー名を取得
    /// </summary>
    public string GetUserName()
    {
        if (!IsInitialized())
        {
            return "Unknown";
        }

        return SteamFriends.GetPersonaName();
    }

    /// <summary>
    /// 現在のApp IDを取得
    /// </summary>
    public uint GetAppID()
    {
        if (!IsInitialized())
        {
            return 0;
        }

        return SteamUtils.GetAppID().m_AppId;
    }

    /// <summary>
    /// ユーザー統計情報を再リクエスト（公開メソッド）
    /// </summary>
    public void RetryRequestUserStats()
    {
        if (isSteamRunning && !userStatsReceived)
        {
            RequestCurrentUserStats();
        }
    }

    #endregion

    #region Debug Methods

#if UNITY_EDITOR
    [ContextMenu("Steam初期化状態を表示")]
    private void ShowSteamStatus()
    {
        if (IsInitialized())
        {
            DebugLogger.Log($"Steam初期化: 成功");
            DebugLogger.Log($"App ID: {GetAppID()}");
            DebugLogger.Log($"ユーザー名: {GetUserName()}");
            DebugLogger.Log($"ユーザー統計情報: {(userStatsReceived ? "受信済み" : "未受信")}");
        }
        else
        {
            DebugLogger.Log($"Steam初期化: 失敗");
        }
    }
#endif

    #endregion
}