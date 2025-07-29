using UnityEngine;
using System.Collections;

/// <summary>
/// 16:9のアスペクト比を維持してウィンドウサイズを管理するマネージャー
/// </summary>
public class AspectRatioManager : MonoBehaviour
{
    private bool isResizing = false;
    private float resizeDelay = 0.1f; // リサイズ処理の遅延時間
    private float lastResizeTime = 0f;

    // シングルトンインスタンス
    private static AspectRatioManager instance;
    public static AspectRatioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AspectRatioManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("AspectRatioManager");
                    instance = go.AddComponent<AspectRatioManager>();
                }
            }
            return instance;
        }
    }

    [Header("アスペクト比設定")]
    [SerializeField] private float targetAspectRatio = 16f / 9f; // 16:9固定

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;

    // 前フレームのウィンドウサイズ
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        // シングルトンパターンの実装
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // 初期ウィンドウサイズを記録
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        if (debugMode)
        {
            Debug.Log($"{nameof(AspectRatioManager)}: 初期化完了 - ターゲットアスペクト比: {targetAspectRatio:F2}");
        }
    }

    /// <summary>
    /// 現在のアスペクト比を取得
    /// </summary>
    public float GetCurrentAspectRatio()
    {
        return (float)Screen.width / Screen.height;
    }

    /// <summary>
    /// 指定された幅に対する16:9の高さを計算
    /// </summary>
    public int CalculateHeightForWidth(int width)
    {
        return Mathf.RoundToInt(width / targetAspectRatio);
    }

    /// <summary>
    /// 指定された高さに対する16:9の幅を計算
    /// </summary>
    public int CalculateWidthForHeight(int height)
    {
        return Mathf.RoundToInt(height * targetAspectRatio);
    }

    /// <summary>
    /// 現在のウィンドウサイズが16:9かどうかをチェック
    /// </summary>
    public bool IsCorrectAspectRatio()
    {
        float currentRatio = GetCurrentAspectRatio();
        float difference = Mathf.Abs(currentRatio - targetAspectRatio);
        return difference < 0.01f; // 許容誤差
    }

    /// <summary>
    /// ウィンドウサイズ変更を検出
    /// </summary>
    public bool HasWindowSizeChanged()
    {
        return Screen.width != lastScreenWidth || Screen.height != lastScreenHeight;
    }

    /// <summary>
    /// 最後のウィンドウサイズを更新
    /// </summary>
    public void UpdateLastWindowSize()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    private void Update()
    {
        // リサイズ中の場合は処理をスキップ
        if (isResizing)
        {
            return;
        }

        // ウィンドウサイズ変更を監視
        if (HasWindowSizeChanged())
        {
            // 最後のリサイズから一定時間経過していない場合はスキップ
            if (Time.time - lastResizeTime < resizeDelay)
            {
                return;
            }

            // アスペクト比が16:9でない場合のみ修正
            if (!IsCorrectAspectRatio())
            {
                // コルーチンでアスペクト比を修正
                StartCoroutine(EnforceAspectRatio());
            }
            else
            {
                // アスペクト比が正しい場合は、サイズだけ更新
                UpdateLastWindowSize();
            }
        }
    }

    /// <summary>
    /// 16:9のアスペクト比を強制的に維持する
    /// </summary>
    private IEnumerator EnforceAspectRatio()
    {
        isResizing = true;
        lastResizeTime = Time.time;

        // 1フレーム待機
        yield return null;

        int currentWidth = Screen.width;
        int currentHeight = Screen.height;
        float currentRatio = GetCurrentAspectRatio();

        // 現在のアスペクト比と目標アスペクト比の差分を計算
        float ratioDifference = currentRatio - targetAspectRatio;

        int newWidth = currentWidth;
        int newHeight = currentHeight;

        // アスペクト比の補正方法を決定
        if (Mathf.Abs(ratioDifference) > 0.01f)
        {
            // 変更量が少ない方を選択
            int heightBasedOnWidth = CalculateHeightForWidth(currentWidth);
            int widthBasedOnHeight = CalculateWidthForHeight(currentHeight);

            // 前回のサイズとの差分を考慮
            int deltaHeightFromLast = Mathf.Abs(currentHeight - lastScreenHeight);
            int deltaWidthFromLast = Mathf.Abs(currentWidth - lastScreenWidth);

            // より変更が大きかった方向を基準にする
            if (deltaWidthFromLast > deltaHeightFromLast)
            {
                // 幅の変更が大きい場合、幅を基準に高さを調整
                newHeight = heightBasedOnWidth;
            }
            else
            {
                // 高さの変更が大きい場合、高さを基準に幅を調整
                newWidth = widthBasedOnHeight;
            }

            // 最小解像度の確保
            if (newWidth < 960 || newHeight < 540)
            {
                newWidth = 960;
                newHeight = 540;
            }

            // 解像度を設定（ウィンドウモード固定）
            Screen.SetResolution(newWidth, newHeight, false);

            if (debugMode)
            {
                Debug.Log($"{nameof(AspectRatioManager)}: アスペクト比を修正しました - {currentWidth}×{currentHeight} → {newWidth}×{newHeight}");
            }

            // 解像度変更後、少し待機
            yield return new WaitForSeconds(0.1f);
        }

        // 最後のウィンドウサイズを更新
        UpdateLastWindowSize();

        isResizing = false;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}