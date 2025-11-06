using UnityEngine;

/// <summary>
/// アスペクト比管理クラス（ウィンドウリサイズ無効化により機能停止）
/// </summary>
[System.Obsolete("ウィンドウリサイズが無効化されたため、このクラスは使用されません")]
public class AspectRatioManager : MonoBehaviour
{
    private static AspectRatioManager instance;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;

    // 16:9のアスペクト比
    private const float TARGET_ASPECT_RATIO = 16f / 9f;

    public static AspectRatioManager Instance
    {
        get { return instance; }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (debugMode)
        {
            Debug.LogWarning($"{nameof(AspectRatioManager)}: このクラスは無効化されています");
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
        return Mathf.RoundToInt(width / TARGET_ASPECT_RATIO);
    }

    /// <summary>
    /// 指定された高さに対する16:9の幅を計算
    /// </summary>
    public int CalculateWidthForHeight(int height)
    {
        return Mathf.RoundToInt(height * TARGET_ASPECT_RATIO);
    }

    /// <summary>
    /// 現在のウィンドウサイズが16:9かどうかをチェック
    /// </summary>
    public bool IsCorrectAspectRatio()
    {
        float currentRatio = GetCurrentAspectRatio();
        float difference = Mathf.Abs(currentRatio - TARGET_ASPECT_RATIO);
        return difference < 0.01f;
    }

    // Update処理は無効化
    void Update()
    {
        // 何もしない
    }
}