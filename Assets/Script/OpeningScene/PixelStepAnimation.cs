using UnityEngine;

/// <summary>
/// カクカクとステップ状に上下移動するアニメーションを提供するコンポーネント
/// ContinueIndicatorなどのUI要素に使用します
/// </summary>
[AddComponentMenu("UI/Animations/Pixel Step Animation")]
public class PixelStepAnimation : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("上下の移動量（ピクセル単位）")]
    [SerializeField] private float amplitude = 10f;

    [Tooltip("アニメーションの速度")]
    [SerializeField] private float speed = 2f;

    [Header("カクカク設定")]
    [Tooltip("位置更新の時間間隔（秒）- 大きいほどカクカク")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float updateInterval = 0.1f;

    [Tooltip("ピクセル単位で位置を丸める（カクカク感が増します）")]
    [SerializeField] private bool snapToPixel = true;

    [Tooltip("ステップ数 - 少ないほどカクカク")]
    [Range(2, 20)]
    [SerializeField] private int steps = 4;

    [Header("詳細設定")]
    [Tooltip("アニメーションを開始時に自動的に開始するか")]
    [SerializeField] private bool playOnStart = true;

    // プライベート変数
    private RectTransform rectTransform;
    private Vector2 startPosition;
    private float currentTime;
    private float lastUpdateTime;
    private bool isPlaying = false;

    private void Awake()
    {
        // RectTransformの取得
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("PixelStepAnimation: RectTransformが見つかりません。UI要素にアタッチしてください。");
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        // 初期位置を保存
        startPosition = rectTransform.anchoredPosition;

        // 自動開始の場合
        if (playOnStart)
        {
            Play();
        }
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        // 時間の更新
        currentTime += Time.deltaTime * speed;

        // 更新間隔に達していない場合はスキップ
        if (Time.time - lastUpdateTime < updateInterval)
            return;

        lastUpdateTime = Time.time;

        // ステップ状の値を計算（0からsteps-1の範囲で循環）
        int currentStep = Mathf.FloorToInt(Mathf.Repeat(currentTime, steps)) % steps;

        // ステップを正規化（0-1の範囲）
        float normalizedStep = (float)currentStep / (steps - 1);

        // 三角波パターンを作成（0→1→0のパターン）
        float triangleWave;
        if (normalizedStep < 0.5f)
            triangleWave = normalizedStep * 2;
        else
            triangleWave = 1 - ((normalizedStep - 0.5f) * 2);

        // Y方向のオフセットを計算
        float yOffset = (triangleWave * 2 - 1) * amplitude;

        // ピクセルにスナップする場合
        if (snapToPixel)
            yOffset = Mathf.Round(yOffset);

        // 位置を更新
        rectTransform.anchoredPosition = new Vector2(
            startPosition.x,
            startPosition.y + yOffset
        );
    }

    /// <summary>
    /// アニメーションを開始します
    /// </summary>
    public void Play()
    {
        isPlaying = true;
        lastUpdateTime = Time.time;
    }

    /// <summary>
    /// アニメーションを一時停止します
    /// </summary>
    public void Pause()
    {
        isPlaying = false;
    }

    /// <summary>
    /// アニメーションを停止し、初期位置に戻します
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
        rectTransform.anchoredPosition = startPosition;
    }

    /// <summary>
    /// カクカク感を調整します
    /// </summary>
    /// <param name="pixelSnap">ピクセルスナップの有効/無効</param>
    /// <param name="intervalTime">更新間隔（秒）</param>
    /// <param name="stepCount">ステップ数</param>
    public void SetPixelation(bool pixelSnap, float intervalTime, int stepCount)
    {
        snapToPixel = pixelSnap;
        updateInterval = Mathf.Clamp(intervalTime, 0.01f, 0.5f);
        steps = Mathf.Clamp(stepCount, 2, 20);
    }
}