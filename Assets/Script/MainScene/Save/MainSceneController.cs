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
        }
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

    // デバッグ用のログメソッド
    private void LogDebug(string message)
    {
        if (debugMode) Debug.Log($"[MainSceneController] {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[MainSceneController] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[MainSceneController] {message}");
    }
}