using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TitleTextChangerのhasChangedがtrueになったときに
/// 「思い出すボタン」のAlpha値を動的に変動させるコンポーネント
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class RememberButtonAlphaAnimator : MonoBehaviour
{
    [Header("参照設定")]
    [Tooltip("監視するTitleTextChangerコンポーネント")]
    [SerializeField] private TitleTextChanger titleTextChanger;

    [Tooltip("アニメーション対象のCanvasGroup（未設定の場合は自動取得）")]
    [SerializeField] private CanvasGroup targetCanvasGroup;

    [Header("アニメーション設定")]
    [Tooltip("アニメーションの種類")]
    [SerializeField] private AnimationType animationType = AnimationType.Pulse;

    [Tooltip("最小Alpha値")]
    [SerializeField][Range(0f, 1f)] private float minAlpha = 0.3f;

    [Tooltip("最大Alpha値")]
    [SerializeField][Range(0f, 1f)] private float maxAlpha = 1.0f;

    [Tooltip("アニメーション速度")]
    [SerializeField] private float animationSpeed = 2.0f;

    [Tooltip("パルスアニメーションの滑らかさ")]
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("フェードイン設定")]
    [Tooltip("フェードインを使用するか")]
    [SerializeField] private bool useFadeIn = true;

    [Tooltip("フェードイン時間（秒）")]
    [SerializeField] private float fadeInDuration = 1.0f;

    [Tooltip("フェードイン開始前の遅延（秒）")]
    [SerializeField] private float fadeInDelay = 0.5f;

    [Header("フラグ監視設定")]
    [Tooltip("フラグ変更をチェックする間隔（秒）")]
    [SerializeField] private float flagCheckInterval = 0.1f;

    [Tooltip("TitleTextChangerの変更完了も監視するか")]
    [SerializeField] private bool monitorTextChangerCompletion = true;

    [Tooltip("シーン遷移時の特別処理を有効にするか")]
    [SerializeField] private bool enableSceneTransitionDetection = true;

    [Header("ランダム設定")]
    [Tooltip("ランダムな変動を追加するか")]
    [SerializeField] private bool useRandomVariation = false;

    [Tooltip("ランダム変動の強度")]
    [SerializeField][Range(0f, 0.5f)] private float randomVariationStrength = 0.1f;

    [Header("エフェクト設定")]
    [Tooltip("グロー効果を追加するか")]
    [SerializeField] private bool useGlowEffect = false;

    [Tooltip("グロー効果の対象Image")]
    [SerializeField] private Image glowImage;

    [Tooltip("グロー効果の色")]
    [SerializeField] private Color glowColor = new Color(1f, 1f, 1f, 0.5f);

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceStart = false; // テスト用の強制開始

    // アニメーションタイプ
    public enum AnimationType
    {
        Pulse,          // 脈動
        Breathe,        // 呼吸のような動き
        Flicker,        // 点滅
        Wave,           // 波のような動き
        Heartbeat       // 心拍のような動き
    }

    // プライベート変数
    private bool isAnimating = false;
    private Coroutine animationCoroutine;
    private Coroutine flagMonitorCoroutine;
    private float currentAlpha;
    private Button targetButton;
    private TMP_Text buttonText;

    // フラグ監視用
    private bool lastAfterChangeFlag = false;
    private bool lastTextChangerFlag = false;
    private bool hasStartedAnimation = false;

    private void Awake()
    {
        // CanvasGroupの取得または作成
        if (targetCanvasGroup == null)
        {
            targetCanvasGroup = GetComponent<CanvasGroup>();
            if (targetCanvasGroup == null)
            {
                targetCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // TitleTextChangerの自動検索
        if (titleTextChanger == null)
        {
            titleTextChanger = FindFirstObjectByType<TitleTextChanger>();
            if (titleTextChanger == null && !forceStart)
            {
                Debug.LogError("RememberButtonAlphaAnimator: TitleTextChangerが見つかりません。");
                enabled = false;
                return;
            }
        }

        // Buttonコンポーネントの取得
        targetButton = GetComponent<Button>();

        // TextMeshProコンポーネントの取得
        buttonText = GetComponentInChildren<TMP_Text>();

        // 初期Alpha値を設定
        currentAlpha = targetCanvasGroup.alpha;

        if (debugMode)
        {
            Debug.Log("RememberButtonAlphaAnimator: 初期化完了");
        }
    }

    private void Start()
    {
        // 初期フラグ状態を記録
        lastAfterChangeFlag = GetAfterChangeToHerMemoryFlag();
        lastTextChangerFlag = GetTitleTextChangerFlag();

        if (debugMode)
        {
            Debug.Log($"RememberButtonAlphaAnimator: 初期フラグ状態 - AfterChange: {lastAfterChangeFlag}, TextChanger: {lastTextChangerFlag}");
        }

        // 強制開始またはすでにフラグがtrueの場合はアニメーション開始
        if (forceStart || lastAfterChangeFlag || lastTextChangerFlag)
        {
            if (debugMode)
            {
                string reason = forceStart ? "強制開始" :
                               lastAfterChangeFlag ? "AfterChangeフラグがtrue" : "TextChangerフラグがtrue";
                Debug.Log($"RememberButtonAlphaAnimator: {reason}のためアニメーション開始");
            }

            StartAnimation();
        }

        // フラグ監視を開始
        StartFlagMonitoring();
    }

    /// <summary>
    /// フラグの変更を継続的に監視する
    /// </summary>
    private void StartFlagMonitoring()
    {
        if (flagMonitorCoroutine != null)
        {
            StopCoroutine(flagMonitorCoroutine);
        }

        flagMonitorCoroutine = StartCoroutine(MonitorFlags());
    }

    /// <summary>
    /// フラグ監視用コルーチン
    /// </summary>
    private IEnumerator MonitorFlags()
    {
        while (enabled)
        {
            // AfterChangeToHerMemoryフラグをチェック
            bool currentAfterChangeFlag = GetAfterChangeToHerMemoryFlag();
            bool currentTextChangerFlag = GetTitleTextChangerFlag();

            // フラグが false から true に変わった場合
            if (!hasStartedAnimation && !isAnimating)
            {
                bool shouldStart = false;
                string reason = "";

                if (!lastAfterChangeFlag && currentAfterChangeFlag)
                {
                    shouldStart = true;
                    reason = "AfterChangeフラグがfalseからtrueに変更";
                }
                else if (monitorTextChangerCompletion && !lastTextChangerFlag && currentTextChangerFlag)
                {
                    shouldStart = true;
                    reason = "TextChangerフラグがfalseからtrueに変更";
                }
                else if (currentAfterChangeFlag || currentTextChangerFlag)
                {
                    shouldStart = true;
                    reason = "フラグがtrueの状態を検出";
                }

                if (shouldStart)
                {
                    if (debugMode)
                    {
                        Debug.Log($"RememberButtonAlphaAnimator: {reason} - アニメーション開始");
                    }

                    hasStartedAnimation = true;
                    StartAnimation();
                }
            }

            // 前回の状態を更新
            lastAfterChangeFlag = currentAfterChangeFlag;
            lastTextChangerFlag = currentTextChangerFlag;

            yield return new WaitForSeconds(flagCheckInterval);
        }
    }

    /// <summary>
    /// AfterChangeToHerMemoryフラグの状態を取得
    /// </summary>
    private bool GetAfterChangeToHerMemoryFlag()
    {
        // 強制開始フラグ（テスト用）
        if (forceStart)
        {
            return true;
        }

        // GameSaveManagerから取得（最優先）
        if (GameSaveManager.Instance != null)
        {
            return GameSaveManager.Instance.GetAfterChangeToHerMemoryFlag();
        }

        // GameSaveManagerが存在しない場合はfalse
        return false;
    }

    /// <summary>
    /// TitleTextChangerのhasChangedフラグを取得
    /// </summary>
    private bool GetTitleTextChangerFlag()
    {
        if (!monitorTextChangerCompletion || titleTextChanger == null)
        {
            return false;
        }

        return titleTextChanger.HasChanged;
    }

    /// <summary>
    /// アニメーションを開始
    /// </summary>
    public void StartAnimation()
    {
        if (isAnimating) return;

        if (debugMode)
        {
            Debug.Log("RememberButtonAlphaAnimator: アニメーション開始");
        }

        isAnimating = true;
        hasStartedAnimation = true;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(AnimateAlpha());
    }

    /// <summary>
    /// アニメーションを停止
    /// </summary>
    public void StopAnimation()
    {
        if (!isAnimating) return;

        if (debugMode)
        {
            Debug.Log("RememberButtonAlphaAnimator: アニメーション停止");
        }

        isAnimating = false;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        // Alpha値を最大値に戻す
        targetCanvasGroup.alpha = maxAlpha;
    }

    /// <summary>
    /// Alpha値アニメーションコルーチン
    /// </summary>
    private IEnumerator AnimateAlpha()
    {
        // フェードイン処理
        if (useFadeIn)
        {
            yield return StartCoroutine(FadeIn());
        }

        // メインアニメーションループ
        float time = 0f;

        while (isAnimating)
        {
            time += Time.deltaTime * animationSpeed;

            // アニメーションタイプに応じてAlpha値を計算
            float alpha = CalculateAlpha(time);

            // ランダム変動を追加
            if (useRandomVariation)
            {
                alpha += Random.Range(-randomVariationStrength, randomVariationStrength);
                alpha = Mathf.Clamp(alpha, minAlpha, maxAlpha);
            }

            // Alpha値を適用
            targetCanvasGroup.alpha = alpha;
            currentAlpha = alpha;

            // グロー効果の更新
            if (useGlowEffect && glowImage != null)
            {
                UpdateGlowEffect(alpha);
            }

            yield return null;
        }
    }

    /// <summary>
    /// フェードイン処理
    /// </summary>
    private IEnumerator FadeIn()
    {
        if (fadeInDelay > 0)
        {
            yield return new WaitForSeconds(fadeInDelay);
        }

        targetCanvasGroup.alpha = 0f;
        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeInDuration;
            targetCanvasGroup.alpha = Mathf.Lerp(0f, maxAlpha, t);

            yield return null;
        }

        targetCanvasGroup.alpha = maxAlpha;
    }

    /// <summary>
    /// アニメーションタイプに応じたAlpha値を計算
    /// </summary>
    private float CalculateAlpha(float time)
    {
        float normalizedValue = 0f;

        switch (animationType)
        {
            case AnimationType.Pulse:
                // サイン波を使った脈動
                normalizedValue = (Mathf.Sin(time) + 1f) * 0.5f;
                normalizedValue = pulseCurve.Evaluate(normalizedValue);
                break;

            case AnimationType.Breathe:
                // より自然な呼吸のような動き
                normalizedValue = Mathf.Sin(time * 0.5f) * 0.5f + 0.5f;
                normalizedValue = Mathf.Pow(normalizedValue, 2.2f); // ガンマ補正風
                break;

            case AnimationType.Flicker:
                // 点滅効果
                normalizedValue = Mathf.PingPong(time * 2f, 1f);
                if (normalizedValue > 0.9f) normalizedValue = 1f;
                else if (normalizedValue < 0.1f) normalizedValue = 0f;
                break;

            case AnimationType.Wave:
                // 波のような動き
                float wave1 = Mathf.Sin(time) * 0.5f;
                float wave2 = Mathf.Sin(time * 1.5f) * 0.3f;
                float wave3 = Mathf.Sin(time * 2.1f) * 0.2f;
                normalizedValue = (wave1 + wave2 + wave3 + 1f) * 0.5f;
                break;

            case AnimationType.Heartbeat:
                // 心拍のような動き
                float beat = time % 2f;
                if (beat < 0.1f)
                    normalizedValue = 1f;
                else if (beat < 0.3f)
                    normalizedValue = 0.7f;
                else if (beat < 0.4f)
                    normalizedValue = 1f;
                else
                    normalizedValue = 0.5f;
                break;
        }

        // 最小値と最大値の間で補間
        return Mathf.Lerp(minAlpha, maxAlpha, normalizedValue);
    }

    /// <summary>
    /// グロー効果を更新
    /// </summary>
    private void UpdateGlowEffect(float alpha)
    {
        if (glowImage == null) return;

        // Alpha値に応じてグローの強度を変更
        Color color = glowColor;
        color.a = glowColor.a * (alpha - minAlpha) / (maxAlpha - minAlpha);
        glowImage.color = color;
    }

    /// <summary>
    /// 外部からアニメーション開始を強制する（デバッグ用）
    /// </summary>
    [ContextMenu("Debug: Force Start Animation")]
    public void ForceStartAnimation()
    {
        hasStartedAnimation = false;
        StartAnimation();
    }

    /// <summary>
    /// 現在のフラグ状態を表示（デバッグ用）
    /// </summary>
    [ContextMenu("Debug: Show Flag Status")]
    public void ShowFlagStatus()
    {
        bool afterChangeFlag = GetAfterChangeToHerMemoryFlag();
        bool textChangerFlag = GetTitleTextChangerFlag();

        Debug.Log($"=== RememberButtonAlphaAnimator フラグ状態 ===");
        Debug.Log($"AfterChangeToHerMemory: {afterChangeFlag}");
        Debug.Log($"TitleTextChanger HasChanged: {textChangerFlag}");
        Debug.Log($"アニメーション中: {isAnimating}");
        Debug.Log($"開始済み: {hasStartedAnimation}");
        Debug.Log($"=====================================");
    }

    /// <summary>
    /// エディタ用：アニメーションタイプを変更
    /// </summary>
    public void SetAnimationType(AnimationType type)
    {
        animationType = type;
    }

    /// <summary>
    /// エディタ用：速度を変更
    /// </summary>
    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = Mathf.Max(0.1f, speed);
    }

    /// <summary>
    /// 現在のアニメーション状態を取得
    /// </summary>
    public bool IsAnimating => isAnimating;

    private void OnDestroy()
    {
        StopAnimation();
        if (flagMonitorCoroutine != null)
        {
            StopCoroutine(flagMonitorCoroutine);
        }
    }

    private void OnDisable()
    {
        StopAnimation();
        if (flagMonitorCoroutine != null)
        {
            StopCoroutine(flagMonitorCoroutine);
        }
    }

    private void OnEnable()
    {
        // 有効化時にフラグをチェック
        if (!hasStartedAnimation)
        {
            bool shouldAnimate = GetAfterChangeToHerMemoryFlag() || GetTitleTextChangerFlag();

            if (shouldAnimate && !isAnimating)
            {
                if (debugMode)
                    Debug.Log("RememberButtonAlphaAnimator: OnEnable - フラグがtrueのためアニメーション開始");

                StartAnimation();
            }
        }

        // フラグ監視を再開
        StartFlagMonitoring();
    }
}