using System;
using UnityEngine;

/// <summary>
/// IndieIchiライセンスファイルを解析するユーティリティクラス
/// </summary>
public static class IndieIchiLicenseParser
{
    private const string USER_ID_KEY = "USER ID:"; // ユーザーIDのキー
    private const string GAME_ID_KEY = "GAME ID:"; // ゲームIDのキー
    private const string LICENSE_ID_KEY = "LICENSE ID:"; // ライセンスIDのキー

    /// <summary>
    /// ライセンスファイルの内容を解析してIndieIchiLicenseDataを返す
    /// </summary>
    /// <param name="fileContent">ライセンスファイルの内容</param>
    /// <returns>解析されたライセンスデータ（失敗時はnull）</returns>
    public static IndieIchiLicenseData Parse(string fileContent)
    {
        if (string.IsNullOrEmpty(fileContent))
        {
            Debug.LogError($"{nameof(IndieIchiLicenseParser)}: ファイル内容が空です");
            return null;
        }

        IndieIchiLicenseData data = new IndieIchiLicenseData();
        string[] lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();

            // USER IDを抽出
            if (trimmedLine.StartsWith(USER_ID_KEY, StringComparison.OrdinalIgnoreCase))
            {
                data.UserId = ExtractValue(trimmedLine, USER_ID_KEY);
            }
            // GAME IDを抽出
            else if (trimmedLine.StartsWith(GAME_ID_KEY, StringComparison.OrdinalIgnoreCase))
            {
                data.GameId = ExtractValue(trimmedLine, GAME_ID_KEY);
            }
            // LICENSE IDを抽出
            else if (trimmedLine.StartsWith(LICENSE_ID_KEY, StringComparison.OrdinalIgnoreCase))
            {
                data.LicenseId = ExtractValue(trimmedLine, LICENSE_ID_KEY);
            }
        }

        // 必須項目のチェック
        if (string.IsNullOrEmpty(data.UserId) ||
            string.IsNullOrEmpty(data.GameId) ||
            string.IsNullOrEmpty(data.LicenseId))
        {
            Debug.LogError($"{nameof(IndieIchiLicenseParser)}: 必須項目が不足しています");
            Debug.LogError($"  USER ID: {(string.IsNullOrEmpty(data.UserId) ? "未設定" : "OK")}");
            Debug.LogError($"  GAME ID: {(string.IsNullOrEmpty(data.GameId) ? "未設定" : "OK")}");
            Debug.LogError($"  LICENSE ID: {(string.IsNullOrEmpty(data.LicenseId) ? "未設定" : "OK")}");
            return null;
        }

        return data;
    }

    /// <summary>
    /// 行からキーに続く値を抽出
    /// </summary>
    private static string ExtractValue(string line, string key)
    {
        int keyIndex = line.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (keyIndex == -1)
        {
            return string.Empty;
        }

        string value = line.Substring(keyIndex + key.Length).Trim();
        return value;
    }
}