using UnityEngine;
using System.Runtime.InteropServices;
using System;

/// <summary>
/// ウィンドウのサイズ変更を制御するマネージャー
/// </summary>
public class WindowResizeManager : MonoBehaviour
{
    private static WindowResizeManager instance;

    // Windows API
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    // ウィンドウスタイルの定数
    private const int GWL_STYLE = -16;
    private const int WS_SIZEBOX = 0x00040000;
    private const int WS_MAXIMIZEBOX = 0x00010000;
    private const int WS_MINIMIZEBOX = 0x00020000;
    private const int WS_CAPTION = 0x00C00000;

    // リサイズ無効化フラグ（常にfalse）
    private const bool IS_RESIZABLE = false;

    public static WindowResizeManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("WindowResizeManager");
                instance = go.AddComponent<WindowResizeManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // 起動時にリサイズを無効化
            DisableWindowResize();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // エディタでない場合のみ実行
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        DisableWindowResize();
#endif
    }

    /// <summary>
    /// ウィンドウのリサイズを無効化
    /// </summary>
    private void DisableWindowResize()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        IntPtr windowHandle = GetActiveWindow();
        if (windowHandle != IntPtr.Zero)
        {
            int style = GetWindowLong(windowHandle, GWL_STYLE);
            
            // リサイズボーダーと最大化ボタンを削除
            style &= ~(WS_SIZEBOX | WS_MAXIMIZEBOX);
            
            // 最小化ボタンとタイトルバーは維持
            style |= WS_MINIMIZEBOX | WS_CAPTION;
            
            SetWindowLong(windowHandle, GWL_STYLE, style);
            
            Debug.Log("ウィンドウのリサイズを無効化しました");
        }
#endif
    }

    /// <summary>
    /// ウィンドウサイズを設定（リサイズ不可の状態でも変更可能）
    /// </summary>
    /// <param name="width">幅</param>
    /// <param name="height">高さ</param>
    public void SetWindowSize(int width, int height)
    {
        Screen.SetResolution(width, height, false);
        Debug.Log($"ウィンドウサイズを {width}x{height} に変更しました");

        // 解像度変更後にリサイズ無効化を再適用
        StartCoroutine(ReapplyResizeDisableAfterDelay());
    }

    /// <summary>
    /// 解像度変更後、少し遅延してからリサイズ無効化を再適用
    /// </summary>
    private System.Collections.IEnumerator ReapplyResizeDisableAfterDelay()
    {
        // 解像度変更が完了するまで少し待機
        yield return new WaitForSeconds(0.1f);

        // リサイズ無効化を再適用
        DisableWindowResize();
    }

    /// <summary>
    /// 現在のリサイズ可能状態を取得（常にfalseを返す）
    /// </summary>
    public bool IsResizable()
    {
        return false;
    }

    // 以下のメソッドは互換性のために残すが、実際には何もしない
    /// <summary>
    /// ウィンドウのリサイズ可否を設定（このバージョンでは無効）
    /// </summary>
    /// <param name="resizable">リサイズ可能にする場合はtrue（無視される）</param>
    [System.Obsolete("このメソッドは使用されません。ウィンドウリサイズは常に無効です。")]
    public void SetWindowResizable(bool resizable)
    {
        Debug.LogWarning("SetWindowResizable は無効化されています。ウィンドウリサイズは常に無効です。");
    }
}