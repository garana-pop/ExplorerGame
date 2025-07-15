using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FlagTestController : MonoBehaviour
{
    [Header("テストボタン")]
    [SerializeField] private Button setTrueButton;
    [SerializeField] private Button setFalseButton;
    [SerializeField] private Button getFlagButton;

    [Header("結果表示用テキスト")]
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("デバッグ情報表示")]
    [SerializeField] private bool showDebugInfo = true;

    void Start()
    {
        // ボタンイベントの設定
        if (setTrueButton != null)
            setTrueButton.onClick.AddListener(() => SetFlag(true));

        if (setFalseButton != null)
            setFalseButton.onClick.AddListener(() => SetFlag(false));

        if (getFlagButton != null)
            getFlagButton.onClick.AddListener(GetFlag);

        // 初期状態の表示
        UpdateResultText("テスト準備完了");

        if (showDebugInfo)
        {
            Debug.Log("FlagTestController: テスト開始準備完了");
            Debug.Log($"GameSaveManager存在確認: {GameSaveManager.Instance != null}");
        }
    }

    /// <summary>
    /// フラグを設定するテスト
    /// </summary>
    /// <param name="value">設定する値</param>
    public void SetFlag(bool value)
    {
        if (GameSaveManager.Instance == null)
        {
            UpdateResultText("エラー: GameSaveManagerが見つかりません");
            Debug.LogError("FlagTestController: GameSaveManagerが存在しません");
            return;
        }

        try
        {
            GameSaveManager.Instance.SetEndOpeningSceneFlag(value);
            UpdateResultText($"フラグを{value}に設定しました");
            Debug.Log($"FlagTestController: SetEndOpeningSceneFlag({value})を実行");
        }
        catch (System.Exception e)
        {
            UpdateResultText($"エラー: {e.Message}");
            Debug.LogError($"FlagTestController: SetFlag({value})でエラー - {e.Message}");
        }
    }

    /// <summary>
    /// フラグを取得するテスト
    /// </summary>
    public void GetFlag()
    {
        if (GameSaveManager.Instance == null)
        {
            UpdateResultText("エラー: GameSaveManagerが見つかりません");
            Debug.LogError("FlagTestController: GameSaveManagerが存在しません");
            return;
        }

        try
        {
            bool flagValue = GameSaveManager.Instance.GetEndOpeningSceneFlag();
            UpdateResultText($"現在のフラグ値: {flagValue}");
            Debug.Log($"FlagTestController: GetEndOpeningSceneFlag()の結果 = {flagValue}");
        }
        catch (System.Exception e)
        {
            UpdateResultText($"エラー: {e.Message}");
            Debug.LogError($"FlagTestController: GetFlag()でエラー - {e.Message}");
        }
    }

    /// <summary>
    /// セーブデータ全体の状況を確認するテスト
    /// </summary>
    [ContextMenu("セーブデータ状況確認")]
    public void CheckSaveDataStatus()
    {
        if (GameSaveManager.Instance == null)
        {
            Debug.LogError("FlagTestController: GameSaveManagerが存在しません");
            return;
        }

        Debug.Log("=== セーブデータ状況確認 ===");
        Debug.Log($"セーブデータ存在: {GameSaveManager.Instance.SaveDataExists()}");

        if (GameSaveManager.Instance.SaveDataExists())
        {
            GameSaveManager.Instance.LoadGame();
            bool flagValue = GameSaveManager.Instance.GetEndOpeningSceneFlag();
            Debug.Log($"endOpeningSceneフラグ: {flagValue}");
        }
        else
        {
            Debug.Log("セーブデータが存在しないため、フラグ値は確認できません");
        }
        Debug.Log("=========================");
    }

    /// <summary>
    /// 結果表示テキストを更新
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    private void UpdateResultText(string message)
    {
        if (resultText != null)
        {
            resultText.text = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
        }
    }

    /// <summary>
    /// セーブデータを削除してテストをリセット
    /// </summary>
    [ContextMenu("セーブデータ削除（テストリセット）")]
    public void ResetTestData()
    {
        if (GameSaveManager.Instance != null)
        {
            // セーブファイルを削除
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, "game_save.json");
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                Debug.Log("FlagTestController: セーブファイルを削除しました");
                UpdateResultText("セーブデータをリセットしました");
            }
            else
            {
                Debug.Log("FlagTestController: セーブファイルが存在しませんでした");
                UpdateResultText("削除対象のセーブファイルがありません");
            }
        }
    }
}