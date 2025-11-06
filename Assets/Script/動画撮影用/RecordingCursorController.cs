using UnityEngine;

/// <summary>
/// Unity Recorderでの撮影時にマウスカーソルを表示するためのスクリプト
/// CursorMode.ForceSoftwareを使用してカーソルを録画可能にする
/// </summary>
public class RecordingCursorController : MonoBehaviour
{
    [Header("カーソル表示設定")]
    [SerializeField] private bool forceShowCursor = true; // 強制的にカーソルを表示するか

    [Header("カーソル画像設定")]
    [SerializeField] private Texture2D cursorTexture; // カスタムカーソル画像（nullの場合はデフォルト）
    [SerializeField] private Vector2 hotspot = Vector2.zero; // カーソルのホットスポット位置

    [Header("エディタ専用設定")]
    [SerializeField] private bool onlyInEditor = false; // エディタでのみ有効にするか

    private void Awake()
    {
        // エディタ専用設定がtrueの場合、ビルド版では無効化
        if (onlyInEditor && !Application.isEditor)
        {
            enabled = false;
            return;
        }

        ApplyCursorSettings();
    }

    private void Start()
    {
        ApplyCursorSettings();
    }

    private void OnEnable()
    {
        ApplyCursorSettings();
    }

    /// <summary>
    /// カーソルの表示設定を適用
    /// Unity Recorder用にCursorMode.ForceSoftwareを使用
    /// </summary>
    private void ApplyCursorSettings()
    {
        if (forceShowCursor)
        {
            // Unity Recorderでカーソルを録画するにはCursorMode.ForceSoftwareが必須
            if (cursorTexture != null)
            {
                // カスタムカーソルを使用
                Cursor.SetCursor(cursorTexture, hotspot, CursorMode.ForceSoftware);
            }
            else
            {
                // デフォルトカーソルを使用
                Cursor.SetCursor(null, Vector2.zero, CursorMode.ForceSoftware);
            }

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            if (Application.isEditor)
            {
                DebugLogger.Log($"{nameof(RecordingCursorController)}: カーソルを録画可能モード(ForceSoftware)に設定しました");
            }
        }
    }

    private void Update()
    {
        // 他のスクリプトがカーソルを非表示にしようとしても、強制的に表示を維持
        if (forceShowCursor)
        {
            if (!Cursor.visible)
            {
                Cursor.visible = true;

                // カーソルモードも再設定
                if (cursorTexture != null)
                {
                    Cursor.SetCursor(cursorTexture, hotspot, CursorMode.ForceSoftware);
                }
                else
                {
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.ForceSoftware);
                }
            }

            if (Cursor.lockState != CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.None;
            }
        }
    }

    /// <summary>
    /// 録画終了後、通常のカーソル設定に戻す
    /// </summary>
    public void ResetCursorSettings()
    {
        forceShowCursor = false;

        // 通常モードに戻す
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        if (Application.isEditor)
        {
            DebugLogger.Log($"{nameof(RecordingCursorController)}: カーソル設定を通常モード(Auto)に戻しました");
        }
    }

    private void OnDisable()
    {
        // スクリプトが無効化されたら通常モードに戻す
        if (Application.isEditor)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }
}