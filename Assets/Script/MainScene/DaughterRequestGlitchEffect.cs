using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 「願いファイル-娘からのお願い」用の不穏なグリッチエフェクト
/// ファイルアイコンに対して点滅、ノイズ、色収差などのエフェクトを適用
/// </summary>
[RequireComponent(typeof(Image))]
public class DaughterRequestGlitchEffect : MonoBehaviour
{
    #region インスペクター設定

    [Header("=== グリッチエフェクト基本設定 ===")]
    [SerializeField, Tooltip("エフェクトの有効/無効")]
    private bool isEffectEnabled = true;

    [Header("点滅エフェクト")]
    [SerializeField, Range(0.05f, 1.0f), Tooltip("点滅の最小間隔（秒）")]
    private float flickerMinInterval = 0.1f;

    [SerializeField, Range(0.05f, 1.0f), Tooltip("点滅の最大間隔（秒）")]
    private float flickerMaxInterval = 0.3f;

    [SerializeField, Range(0.01f, 0.2f), Tooltip("点滅の持続時間（秒）")]
    private float flickerDuration = 0.05f;

    [Header("デジタルノイズエフェクト")]
    [SerializeField, Tooltip("ノイズラインの表示確率（0-1）")]
    [Range(0.0f, 1.0f)]
    private float noiseLineProbability = 0.3f;

    [SerializeField, Tooltip("ノイズラインの高さ")]
    [Range(1f, 10f)]
    private float noiseLineHeight = 3f;

    [SerializeField, Tooltip("ノイズラインの色")]
    private Color noiseLineColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("色収差エフェクト")]
    [SerializeField, Tooltip("色収差の最大オフセット")]
    [Range(1f, 10f)]
    private float chromaticAberrationOffset = 5f;

    [SerializeField, Tooltip("色収差エフェクトの発生間隔（秒）")]
    [Range(0.5f, 3f)]
    private float chromaticAberrationInterval = 1.5f;

    [SerializeField, Tooltip("色収差エフェクトの持続時間（秒）")]
    [Range(0.1f, 0.5f)]
    private float chromaticAberrationDuration = 0.2f;

    [Header("赤色エラーエフェクト")]
    [SerializeField, Tooltip("赤色の混合開始までの遅延時間（秒）")]
    private float redTintDelay = 2f;

    [SerializeField, Tooltip("赤色の混合速度")]
    [Range(0.1f, 2f)]
    private float redTintSpeed = 0.5f;

    [SerializeField, Tooltip("最大赤色混合率")]
    [Range(0.0f, 1.0f)]
    private float maxRedTintAmount = 0.3f;

    [SerializeField, Tooltip("エラー赤色")]
    private Color errorRedColor = new Color(1f, 0f, 0f, 1f);

    [Header("オプション設定")]
    [SerializeField, Tooltip("デバッグログを出力")]
    private bool enableDebugLog = false;

    #endregion

    #region プライベート変数

    private Image targetImage;                  // 対象のImageコンポーネント
    private Color originalColor;                // 元の色を保存
    private Vector3 originalPosition;           // 元の位置を保存
    private GameObject noiseLineObject;         // ノイズライン用のゲームオブジェクト
    private Image noiseLineImage;               // ノイズラインのImageコンポーネント
    private GameObject chromaticCopyObject;     // 色収差用の複製オブジェクト
    private Image chromaticCopyImage;           // 色収差用のImageコンポーネント
    private float redTintTimer = 0f;            // 赤色混合用タイマー
    private Coroutine flickerCoroutine;         // 点滅コルーチン
    private Coroutine noiseCoroutine;           // ノイズコルーチン
    private Coroutine chromaticCoroutine;       // 色収差コルーチン
    private Coroutine redTintCoroutine;         // 赤色混合コルーチン

    #endregion

    #region Unity イベント

    void Awake()
    {
        // Imageコンポーネントを取得
        targetImage = GetComponent<Image>();
        if (targetImage == null)
        {
            DebugLogger.LogError($"[DaughterRequestGlitchEffect] {gameObject.name}にImageコンポーネントが見つかりません");
            enabled = false;
            return;
        }

        // 初期状態を保存
        originalColor = targetImage.color;
        originalPosition = transform.localPosition;

        // エフェクト用オブジェクトを作成
        CreateEffectObjects();
    }

    void OnEnable()
    {
        if (isEffectEnabled && targetImage != null)
        {
            StartGlitchEffects();
        }
    }

    void OnDisable()
    {
        StopGlitchEffects();
    }

    void OnDestroy()
    {
        // エフェクト用オブジェクトを破棄
        if (noiseLineObject != null) Destroy(noiseLineObject);
        if (chromaticCopyObject != null) Destroy(chromaticCopyObject);
    }

    #endregion

    #region パブリックメソッド

    /// <summary>
    /// エフェクトを開始
    /// </summary>
    public void StartEffects()
    {
        isEffectEnabled = true;
        StartGlitchEffects();
    }

    /// <summary>
    /// エフェクトを停止
    /// </summary>
    public void StopEffects()
    {
        isEffectEnabled = false;
        StopGlitchEffects();
        ResetToOriginalState();
    }

    /// <summary>
    /// エフェクトの強度を設定
    /// </summary>
    /// <param name="intensity">強度（0-1）</param>
    public void SetEffectIntensity(float intensity)
    {
        intensity = Mathf.Clamp01(intensity);

        // 各エフェクトパラメータを調整
        flickerMinInterval = Mathf.Lerp(0.3f, 0.1f, intensity);
        flickerMaxInterval = Mathf.Lerp(0.5f, 0.3f, intensity);
        noiseLineProbability = Mathf.Lerp(0.1f, 0.5f, intensity);
        maxRedTintAmount = Mathf.Lerp(0.1f, 0.5f, intensity);

        if (enableDebugLog)
        {
            DebugLogger.Log($"[DaughterRequestGlitchEffect] エフェクト強度を{intensity}に設定");
        }
    }

    #endregion

    #region プライベートメソッド

    /// <summary>
    /// エフェクト用オブジェクトを作成
    /// </summary>
    private void CreateEffectObjects()
    {
        // ノイズライン用オブジェクトを作成
        noiseLineObject = new GameObject("NoiseLineEffect");
        noiseLineObject.transform.SetParent(transform.parent, false);
        noiseLineObject.transform.SetSiblingIndex(transform.GetSiblingIndex() + 1);

        noiseLineImage = noiseLineObject.AddComponent<Image>();
        noiseLineImage.color = noiseLineColor;
        noiseLineImage.raycastTarget = false;

        RectTransform noiseRect = noiseLineObject.GetComponent<RectTransform>();
        RectTransform targetRect = GetComponent<RectTransform>();
        noiseRect.anchorMin = targetRect.anchorMin;
        noiseRect.anchorMax = targetRect.anchorMax;
        noiseRect.anchoredPosition = targetRect.anchoredPosition;
        noiseRect.sizeDelta = new Vector2(targetRect.sizeDelta.x, noiseLineHeight);
        noiseLineObject.SetActive(false);

        // 色収差用の複製オブジェクトを作成
        chromaticCopyObject = new GameObject("ChromaticAberrationEffect");
        chromaticCopyObject.transform.SetParent(transform.parent, false);
        chromaticCopyObject.transform.SetSiblingIndex(transform.GetSiblingIndex());

        chromaticCopyImage = chromaticCopyObject.AddComponent<Image>();
        chromaticCopyImage.sprite = targetImage.sprite;
        chromaticCopyImage.color = new Color(1f, 0f, 0f, 0.5f); // 赤色の半透明
        chromaticCopyImage.raycastTarget = false;

        RectTransform chromaRect = chromaticCopyObject.GetComponent<RectTransform>();
        chromaRect.anchorMin = targetRect.anchorMin;
        chromaRect.anchorMax = targetRect.anchorMax;
        chromaRect.anchoredPosition = targetRect.anchoredPosition;
        chromaRect.sizeDelta = targetRect.sizeDelta;
        chromaticCopyObject.SetActive(false);
    }

    /// <summary>
    /// すべてのグリッチエフェクトを開始
    /// </summary>
    private void StartGlitchEffects()
    {
        StopGlitchEffects(); // 既存のエフェクトを停止

        if (flickerCoroutine == null)
            flickerCoroutine = StartCoroutine(FlickerEffect());

        if (noiseCoroutine == null)
            noiseCoroutine = StartCoroutine(NoiseLineEffect());

        if (chromaticCoroutine == null)
            chromaticCoroutine = StartCoroutine(ChromaticAberrationEffect());

        if (redTintCoroutine == null)
            redTintCoroutine = StartCoroutine(RedTintEffect());

        if (enableDebugLog)
        {
            DebugLogger.Log($"[DaughterRequestGlitchEffect] エフェクト開始: {gameObject.name}");
        }
    }

    /// <summary>
    /// すべてのグリッチエフェクトを停止
    /// </summary>
    private void StopGlitchEffects()
    {
        if (flickerCoroutine != null)
        {
            StopCoroutine(flickerCoroutine);
            flickerCoroutine = null;
        }

        if (noiseCoroutine != null)
        {
            StopCoroutine(noiseCoroutine);
            noiseCoroutine = null;
        }

        if (chromaticCoroutine != null)
        {
            StopCoroutine(chromaticCoroutine);
            chromaticCoroutine = null;
        }

        if (redTintCoroutine != null)
        {
            StopCoroutine(redTintCoroutine);
            redTintCoroutine = null;
        }

        // エフェクトオブジェクトを非表示
        if (noiseLineObject != null) noiseLineObject.SetActive(false);
        if (chromaticCopyObject != null) chromaticCopyObject.SetActive(false);
    }

    /// <summary>
    /// 元の状態にリセット
    /// </summary>
    private void ResetToOriginalState()
    {
        if (targetImage != null)
        {
            targetImage.color = originalColor;
            transform.localPosition = originalPosition;
        }

        redTintTimer = 0f;
    }

    #endregion

    #region コルーチン

    /// <summary>
    /// 点滅エフェクト
    /// </summary>
    private IEnumerator FlickerEffect()
    {
        while (isEffectEnabled)
        {
            // ランダムな間隔で待機
            float waitTime = Random.Range(flickerMinInterval, flickerMaxInterval);
            yield return new WaitForSeconds(waitTime);

            // 点滅
            Color tempColor = targetImage.color;
            targetImage.color = new Color(tempColor.r, tempColor.g, tempColor.b, 0.3f);

            yield return new WaitForSeconds(flickerDuration);

            targetImage.color = tempColor;
        }
    }

    /// <summary>
    /// デジタルノイズラインエフェクト
    /// </summary>
    private IEnumerator NoiseLineEffect()
    {
        while (isEffectEnabled)
        {
            yield return new WaitForSeconds(Random.Range(0.2f, 0.8f));

            if (Random.value < noiseLineProbability && noiseLineObject != null)
            {
                // ノイズラインを表示
                RectTransform noiseRect = noiseLineObject.GetComponent<RectTransform>();
                float randomY = Random.Range(-100f, 100f);
                noiseRect.anchoredPosition = new Vector2(0, randomY);

                noiseLineObject.SetActive(true);

                // 横に高速移動
                float moveTime = 0.1f;
                float elapsedTime = 0f;
                Vector2 startPos = noiseRect.anchoredPosition;
                Vector2 endPos = startPos + new Vector2(Random.Range(-100f, 100f), 0);

                while (elapsedTime < moveTime)
                {
                    elapsedTime += Time.deltaTime;
                    float t = elapsedTime / moveTime;
                    noiseRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
                    yield return null;
                }

                noiseLineObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 色収差エフェクト
    /// </summary>
    private IEnumerator ChromaticAberrationEffect()
    {
        while (isEffectEnabled)
        {
            yield return new WaitForSeconds(chromaticAberrationInterval);

            if (chromaticCopyObject != null)
            {
                chromaticCopyObject.SetActive(true);

                // ランダムなオフセットで表示
                RectTransform chromaRect = chromaticCopyObject.GetComponent<RectTransform>();
                Vector2 offset = new Vector2(
                    Random.Range(-chromaticAberrationOffset, chromaticAberrationOffset),
                    Random.Range(-chromaticAberrationOffset, chromaticAberrationOffset)
                );

                chromaRect.anchoredPosition = GetComponent<RectTransform>().anchoredPosition + offset;

                // フェードイン・アウト
                float elapsedTime = 0f;
                while (elapsedTime < chromaticAberrationDuration)
                {
                    elapsedTime += Time.deltaTime;
                    float alpha = Mathf.PingPong(elapsedTime * 4f, 0.5f);
                    chromaticCopyImage.color = new Color(1f, 0f, 0f, alpha);
                    yield return null;
                }

                chromaticCopyObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 赤色エラーエフェクト
    /// </summary>
    private IEnumerator RedTintEffect()
    {
        // 初期遅延
        yield return new WaitForSeconds(redTintDelay);

        while (isEffectEnabled)
        {
            redTintTimer += Time.deltaTime * redTintSpeed;

            // 赤色を徐々に混合
            float redAmount = Mathf.PingPong(redTintTimer, maxRedTintAmount);
            Color currentColor = Color.Lerp(originalColor, errorRedColor, redAmount);
            targetImage.color = currentColor;

            yield return null;
        }
    }

    #endregion
}