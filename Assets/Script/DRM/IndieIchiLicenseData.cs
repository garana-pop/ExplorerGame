using System;

/// <summary>
/// IndieIchiライセンスデータ
/// </summary>
[Serializable]
public class IndieIchiLicenseData
{
    public string UserId; // ユーザーID
    public string GameId; // ゲームID
    public string LicenseId; // ライセンスID
}

/// <summary>
/// IndieIchi認証リクエストデータ
/// </summary>
[Serializable]
public class IndieIchiVerifyRequest
{
    public string userId; // ユーザーID
    public string gameId; // ゲームID
    public string licenseId; // ライセンスID
}

/// <summary>
/// IndieIchi認証レスポンスデータ
/// </summary>
[Serializable]
public class IndieIchiVerifyResponse
{
    public bool valid; // 認証結果（true: 成功, false: 失敗）
    public string error; // エラーメッセージ（認証失敗時）
}