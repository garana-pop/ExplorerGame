using System.Collections.Generic;
using UnityEngine;
using ExplorerGame.Localization;

/// <summary>
/// MonologueSceneのセリフデータを読み込むクラス
/// </summary>
public class MonologueDataLoader : MonoBehaviour
{
    [Header("ファイル設定")]
    [SerializeField] private string fileName = "MonologueScene_セリフ";

    [Header("ローカライズ設定")]
    [SerializeField] private string englishFile = "MonologueScene_英語セリフ";

    [Header("デバッグ")]
    [SerializeField] private bool debugMode = false;

    /// <summary>
    /// 初期化処理
    /// </summary>
    private void Start()
    {
        // LocalizationManagerから現在の言語設定を取得して適用
        UpdateFileNameByLanguage();

        // 言語変更イベントに登録
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;

            if (debugMode)
            {
                DebugLogger.Log($"MonologueDataLoader: 言語変更イベントに登録しました");
            }
        }
        else if (debugMode)
        {
            DebugLogger.LogWarning("MonologueDataLoader: LocalizationManagerが見つかりません");
        }
    }

    /// <summary>
    /// 破棄時の処理
    /// </summary>
    private void OnDestroy()
    {
        // イベントの登録解除
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    /// <summary>
    /// 言語変更時のコールバック
    /// </summary>
    /// <param name="newLocale">新しいロケール</param>
    private void OnLanguageChanged(UnityEngine.Localization.Locale newLocale)
    {
        UpdateFileNameByLanguage();

        if (debugMode)
        {
            DebugLogger.Log($"MonologueDataLoader: 言語が {newLocale.Identifier.Code} に変更されました");
        }
    }

    /// <summary>
    /// 現在の言語設定に基づいてファイル名を更新
    /// </summary>
    private void UpdateFileNameByLanguage()
    {
        // LocalizationManagerが存在しない場合は日本語をデフォルトとする
        if (LocalizationManager.Instance == null)
        {
            fileName = "MonologueScene_セリフ";

            if (debugMode)
            {
                DebugLogger.LogWarning("MonologueDataLoader: LocalizationManagerが見つかりません。日本語ファイルを使用します");
            }
            return;
        }

        // 現在の言語コードを取得
        string currentLanguageCode = LocalizationManager.Instance.GetCurrentLanguageCode();

        // 言語コードに応じてファイル名を設定
        if (currentLanguageCode == "en")
        {
            // 英語の場合
            fileName = englishFile;

            if (debugMode)
            {
                DebugLogger.Log($"MonologueDataLoader: 英語ファイル '{fileName}' を使用します");
            }
        }
        else
        {
            // 日本語の場合（デフォルト）
            fileName = "MonologueScene_セリフ";

            if (debugMode)
            {
                DebugLogger.Log($"MonologueDataLoader: 日本語ファイル '{fileName}' を使用します");
            }
        }
    }

    /// <summary>
    /// セリフデータを読み込む
    /// </summary>
    /// <returns>セリフのリスト</returns>
    public List<string> LoadDialogueData()
    {
        List<string> dialogues = new List<string>();

        try
        {
            // Resourcesフォルダからテキストファイルを読み込む
            TextAsset textAsset = Resources.Load<TextAsset>(fileName);

            if (textAsset == null)
            {
                DebugLogger.LogError($"セリフファイル '{fileName}' が見つかりません。");
                return dialogues;
            }

            // 改行で分割してリストに追加
            string[] lines = textAsset.text.Split('\n');

            foreach (string line in lines)
            {
                // 空行を無視しない（「・・・」も有効なセリフとして扱う）
                string trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine))
                {
                    dialogues.Add(trimmedLine);

                    if (debugMode)
                    {
                        DebugLogger.Log($"読み込んだセリフ: {trimmedLine}");
                    }
                }
            }

            if (debugMode)
            {
                DebugLogger.Log($"合計 {dialogues.Count} のセリフを読み込みました。");
            }
        }
        catch (System.Exception e)
        {
            DebugLogger.LogError($"セリフデータの読み込み中にエラーが発生しました: {e.Message}");
        }

        return dialogues;
    }
}