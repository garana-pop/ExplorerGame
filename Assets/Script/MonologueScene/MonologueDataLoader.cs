using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MonologueSceneのセリフデータを読み込むクラス
/// </summary>
public class MonologueDataLoader : MonoBehaviour
{
    [Header("ファイル設定")]
    [SerializeField] private string fileName = "MonologueScene_セリフ";

    [Header("デバッグ")]
    [SerializeField] private bool debugMode = false;

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
                Debug.LogError($"セリフファイル '{fileName}' が見つかりません。");
                return dialogues;
            }

            // 改行で分割してリストに追加
            string[] lines = textAsset.text.Split('\n');

            foreach (string line in lines)
            {
                // 空行を除外しない（「・・・」も有効なセリフとして扱う）
                string trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine))
                {
                    dialogues.Add(trimmedLine);

                    if (debugMode)
                    {
                        Debug.Log($"読み込んだセリフ: {trimmedLine}");
                    }
                }
            }

            if (debugMode)
            {
                Debug.Log($"合計 {dialogues.Count} 個のセリフを読み込みました。");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"セリフデータの読み込み中にエラーが発生しました: {e.Message}");
        }

        return dialogues;
    }
}