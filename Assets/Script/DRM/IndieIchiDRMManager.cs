using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

/// <summary>
/// IndieIchi DRMライセンス認証を管理するマネージャー
/// ゲーム起動時にライセンスファイルを読み込み、サーバーで認証を行う
/// </summary>
public class IndieIchiDRMManager : MonoBehaviour
{
    // エラーメッセージ定数
    private const string ERROR_NO_LICENSE_FILE =
        "ライセンスファイルが見つかりません。\n\n" +
        "LICENSE-INDIEICHI-***.txt ファイルを\n" +
        "ゲームの実行ファイルと同じフォルダに配置してください。\n\n" +
        "ゲームは5秒後に終了します。";

    private const string ERROR_LICENSE_PARSE_FAILED =
        "ライセンスファイルの読み込みに失敗しました。\n\n" +
        "ファイルが破損している可能性があります。\n" +
        "正しいライセンスファイルを配置してください。\n\n" +
        "ゲームは5秒後に終了します。";

    private const string ERROR_VERIFICATION_FAILED =
        "ライセンス認証に失敗しました。\n\n" +
        "理由: {0}\n\n" +
        "購入元のストアページでライセンスをご確認ください。\n" +
        "問題が解決しない場合は、サポートにお問い合わせください。\n\n" +
        "ゲームは5秒後に終了します。";

    private const string ERROR_NETWORK_FAILED =
        "サーバーとの通信に失敗しました。\n\n" +
        "インターネット接続を確認してください。\n\n" +
        "【詳細情報】\n" +
        "{0}\n\n" +
        "問題が解決しない場合は、サポートにお問い合わせください。\n" +
        "ゲームは5秒後に終了します。";

    private const string ERROR_SERVER_RESPONSE_INVALID =
        "サーバーからの応答が不正です。\n\n" +
        "一時的なサーバーエラーの可能性があります。\n" +
        "しばらく待ってから再度お試しください。\n\n" +
        "問題が解決しない場合は、サポートにお問い合わせください。\n" +
        "ゲームは5秒後に終了します。";

    // DRM設定
    private const string LICENSE_FILE_PREFIX = "LICENSE-INDIEICHI-"; // ライセンスファイルの接頭辞
    private const string LICENSE_FILE_EXTENSION = ".txt"; // ライセンスファイルの拡張子
    private const string VERIFY_URL = "https://indieichi-backend.onrender.com/api/game-auth/verify-license"; // 認証API URL
    private const float REQUEST_TIMEOUT = 30f; // リクエストタイムアウト時間（秒）

    [Header("DRM設定")]
    [SerializeField] private bool enableDRM = true; // DRMを有効にするかどうか
    [SerializeField] private string firstSceneName = "TitleScene"; // 認証成功時に遷移するシーン名

    [Header("UI参照")]
    [SerializeField] private GameObject errorPanel; // エラーメッセージ表示パネル
    [SerializeField] private TMPro.TextMeshProUGUI errorMessageText; // エラーメッセージテキスト

    // ライセンス情報
    private IndieIchiLicenseData licenseData; // ライセンスデータ
    private bool isVerifying = false; // 認証処理中フラグ

    private void Awake()
    {
        // DRMが無効の場合は即座にゲーム開始
        if (!enableDRM)
        {
            Debug.Log($"{nameof(IndieIchiDRMManager)}: DRM無効 - ゲーム開始");
            StartGame();
            return;
        }

        // ライセンス認証開始
        StartCoroutine(VerifyLicenseCoroutine());
    }

    /// <summary>
    /// ライセンス認証処理のコルーチン
    /// </summary>
    private IEnumerator VerifyLicenseCoroutine()
    {
        if (isVerifying)
        {
            yield break;
        }

        isVerifying = true;
        Debug.Log($"{nameof(IndieIchiDRMManager)}: ライセンス認証開始");

        // ステップ1: ライセンスファイル読み込み
        yield return StartCoroutine(LoadLicenseFile());

        if (licenseData == null)
        {
            ShowError("ライセンスファイルが見つかりません。\nゲームを終了します。");
            yield break;
        }

        // ステップ2: サーバーで認証
        yield return StartCoroutine(VerifyLicenseWithServer());

        isVerifying = false;
    }

    /// <summary>
    /// ライセンスファイルを読み込む
    /// </summary>
    private IEnumerator LoadLicenseFile()
    {
        Debug.Log($"{nameof(IndieIchiDRMManager)}: ライセンスファイル検索中");

        // 実行ファイルのディレクトリを取得
        string executablePath = Application.dataPath;

        // ビルド時は1階層上がる必要がある
        if (!Application.isEditor)
        {
            executablePath = Directory.GetParent(Application.dataPath).FullName;
        }

        // ライセンスファイルを検索
        string[] licenseFiles = Directory.GetFiles(executablePath,
            LICENSE_FILE_PREFIX + "*" + LICENSE_FILE_EXTENSION,
            SearchOption.TopDirectoryOnly);

        if (licenseFiles.Length == 0)
        {
            Debug.LogError($"{nameof(IndieIchiDRMManager)}: ライセンスファイルが見つかりません");
            ShowError(ERROR_NO_LICENSE_FILE);
            licenseData = null;
            yield break;
        }

        if (licenseFiles.Length > 1)
        {
            Debug.LogWarning($"{nameof(IndieIchiDRMManager)}: 複数のライセンスファイルが見つかりました。最初のファイルを使用します。");
        }

        // ライセンスファイルを読み込み
        string licenseFilePath = licenseFiles[0];
        Debug.Log($"{nameof(IndieIchiDRMManager)}: ライセンスファイル読み込み: {Path.GetFileName(licenseFilePath)}");

        try
        {
            string fileContent = File.ReadAllText(licenseFilePath);
            licenseData = IndieIchiLicenseParser.Parse(fileContent);

            if (licenseData != null)
            {
                Debug.Log($"{nameof(IndieIchiDRMManager)}: ライセンス情報読み込み成功");
                Debug.Log($"  USER ID: {licenseData.UserId}");
                Debug.Log($"  GAME ID: {licenseData.GameId}");
                Debug.Log($"  LICENSE ID: {licenseData.LicenseId}");
            }
            else
            {
                Debug.LogError($"{nameof(IndieIchiDRMManager)}: ライセンスファイルの解析に失敗しました");
                ShowError(ERROR_LICENSE_PARSE_FAILED);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"{nameof(IndieIchiDRMManager)}: ライセンスファイル読み込みエラー: {e.Message}");
            ShowError(ERROR_LICENSE_PARSE_FAILED);
            licenseData = null;
        }

        yield return null;
    }

    /// <summary>
    /// サーバーでライセンスを認証する
    /// </summary>
    private IEnumerator VerifyLicenseWithServer()
    {
        Debug.Log($"{nameof(IndieIchiDRMManager)}: サーバー認証開始");

        // リクエストボディを作成
        IndieIchiVerifyRequest requestData = new IndieIchiVerifyRequest
        {
            userId = licenseData.UserId,
            gameId = licenseData.GameId,
            licenseId = licenseData.LicenseId
        };

        string jsonData = JsonUtility.ToJson(requestData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        // UnityWebRequestを作成
        using (UnityWebRequest request = new UnityWebRequest(VERIFY_URL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = (int)REQUEST_TIMEOUT;

            // リクエスト送信
            yield return request.SendWebRequest();

            // レスポンス処理
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string responseText = request.downloadHandler.text;
                    IndieIchiVerifyResponse response = JsonUtility.FromJson<IndieIchiVerifyResponse>(responseText);

                    if (response != null && response.valid)
                    {
                        Debug.Log($"{nameof(IndieIchiDRMManager)}: ライセンス認証成功");
                        StartGame();
                    }
                    else
                    {
                        string errorMsg = response != null ? response.error : "不明なエラー";
                        Debug.LogError($"{nameof(IndieIchiDRMManager)}: ライセンス認証失敗: {errorMsg}");
                        ShowError(string.Format(ERROR_VERIFICATION_FAILED, errorMsg));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"{nameof(IndieIchiDRMManager)}: レスポンス解析エラー: {e.Message}");
                    ShowError(ERROR_SERVER_RESPONSE_INVALID);
                }
            }
            else
            {
                string errorDetails = $"エラー: {request.error}\nHTTPコード: {request.responseCode}";
                Debug.LogError($"{nameof(IndieIchiDRMManager)}: 通信エラー: {errorDetails}");
                ShowError(string.Format(ERROR_NETWORK_FAILED, errorDetails));
            }
        }
    }


    /// <summary>
    /// エラーメッセージを表示してゲームを終了
    /// </summary>
    private void ShowError(string message)
    {
        Debug.LogError($"{nameof(IndieIchiDRMManager)}: {message}");

        if (errorPanel != null)
        {
            errorPanel.SetActive(true);
        }

        if (errorMessageText != null)
        {
            errorMessageText.text = message;
        }

        // 5秒後にゲーム終了
        StartCoroutine(QuitGameAfterDelay(5f));
    }

    /// <summary>
    /// 指定秒数後にゲームを終了
    /// </summary>
    private IEnumerator QuitGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        QuitGame();
    }

    /// <summary>
    /// ゲームを終了
    /// </summary>
    private void QuitGame()
    {
        Debug.Log($"{nameof(IndieIchiDRMManager)}: ゲーム終了");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// 認証成功後にゲームを開始
    /// </summary>
    private void StartGame()
    {
        Debug.Log($"{nameof(IndieIchiDRMManager)}: ゲーム開始 - {firstSceneName}シーンへ遷移");
        SceneManager.LoadScene(firstSceneName);
    }
}