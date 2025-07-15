using UnityEngine;

/// <summary>
/// Filesystemテスト用のクラス
/// ファイル作成動作確認のためのサンプルスクリプト
/// </summary>
public class FilesystemTestClass : MonoBehaviour
{
    [SerializeField] private string testMessage = "Filesystem動作テスト用スクリプト"; // テストメッセージ
    private const float SHOW_LOG_INTERVAL = 5.0f; // ログ表示間隔

    /// <summary>
    /// 初期化処理
    /// </summary>
    void Start()
    {
        // Filesystemテスト用のログ出力
        Debug.Log($"{nameof(FilesystemTestClass)}: Filesystemテストクラスが正常に作成されました");
        Debug.Log($"{nameof(FilesystemTestClass)}: テストメッセージ: {testMessage}");
    }

    /// <summary>
    /// Filesystemテスト用の公開メソッド
    /// インスペクターから呼び出し可能
    /// </summary>
    [ContextMenu("ファイルシステムテスト実行")]
    public void RunFilesystemTest()
    {
        Debug.Log($"{nameof(FilesystemTestClass)}: ファイルシステムテストを実行中...");
        Debug.Log($"{nameof(FilesystemTestClass)}: ファイル作成テスト成功");
        Debug.Log($"{nameof(FilesystemTestClass)}: ConversationFatherAndDaughterフォルダへの配置完了");
    }

    /// <summary>
    /// テストメッセージを変更する
    /// </summary>
    /// <param name="newMessage">新しいテストメッセージ</param>
    public void SetTestMessage(string newMessage)
    {
        if (!string.IsNullOrEmpty(newMessage))
        {
            testMessage = newMessage;
            Debug.Log($"{nameof(FilesystemTestClass)}: テストメッセージが変更されました: {testMessage}");
        }
    }

    /// <summary>
    /// 定期ログ出力用コルーチン（必要に応じて使用）
    /// </summary>
    private void Update()
    {
        // 一定間隔でテストログを出力
        if (Time.time % SHOW_LOG_INTERVAL < Time.deltaTime)
        {
            Debug.Log($"{nameof(FilesystemTestClass)}: 定期テストログ - 現在時刻: {Time.time:F2}秒");
        }
    }
}
