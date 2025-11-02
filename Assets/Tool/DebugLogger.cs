using UnityEngine;

/// <summary>
/// デバッグログを管理するユーティリティクラス
/// リリースビルドでログを無効化する
/// </summary>
public static class DebugLogger
{
    /// <summary>
    /// 通常のログを出力
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message)
    {
        Debug.Log(message);
    }

    /// <summary>
    /// 警告ログを出力
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object message)
    {
        Debug.LogWarning(message);
    }

    /// <summary>
    /// エラーログを出力
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(object message)
    {
        Debug.LogError(message);
    }

    /// <summary>
    /// コンテキスト付きログを出力
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message, Object context)
    {
        Debug.Log(message, context);
    }

    /// <summary>
    /// コンテキスト付き警告ログを出力
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object message, Object context)
    {
        Debug.LogWarning(message, context);
    }

    /// <summary>
    /// コンテキスト付きエラーログを出力
    /// </summary>
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(object message, Object context)
    {
        Debug.LogError(message, context);
    }
}