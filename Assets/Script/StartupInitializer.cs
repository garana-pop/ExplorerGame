using UnityEngine;

/// <summary>
/// ゲーム起動時の初期化処理を行うクラス
/// </summary>
[DefaultExecutionOrder(-1000)] // 他のスクリプトより先に実行
public class StartupInitializer : MonoBehaviour
{
    void Awake()
    {
        // WindowResizeManagerの初期化
        InitializeWindowResizeManager();
    }

    /// <summary>
    /// WindowResizeManagerを初期化
    /// </summary>
    private void InitializeWindowResizeManager()
    {
        // WindowResizeManagerのインスタンスを作成（存在しない場合）
        if (WindowResizeManager.Instance == null)
        {
            Debug.Log("WindowResizeManagerを初期化しています...");
        }

        // ウィンドウのリサイズは自動的に無効化される
        Debug.Log("ウィンドウのリサイズ無効化が完了しました");
    }
}