using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// MainScene移行後、最初にクリックすべきファイルに視覚的ヒントを提供するコンポーネント
/// ファイルに点滅効果やグロー効果を追加し、ダブルクリック後に自動的に効果を終了
/// </summary>
public class FirstFileClickTip : MonoBehaviour
{
    // インスペクター設定用フィールド
    [Header("ヒント表示設定")]
    [SerializeField] private bool enableTip = true; // ヒント表示の有効/無効
    [SerializeField] private float blinkInterval = 2.5f; // 点滅間隔（秒）
    [SerializeField] private float blinkDuration = 1.0f; // 点滅アニメーション時間（秒）

    [Header("視覚効果設定")]
    [SerializeField] private Color glowColor = new Color(1f, 1f, 0.7f, 1f); // グロー色（薄い黄色）
    [SerializeField] private float minAlpha = 0.3f; // 最小透明度
    [SerializeField] private float maxAlpha = 0.8f; // 最大透明度
    [SerializeField] private AnimationCurve blinkCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 点滅カーブ

    [Header("グロー効果設定")]
    [SerializeField] private bool useOutlineEffect = true; // アウトライン効果の使用
    [SerializeField] private float outlineWidth = 3f; // アウトライン幅

    // 内部状態管理
    private bool isEffectActive = false; // エフェクトがアクティブか
    private bool hasBeenClicked_FirstFileClickTip = false; // 一度でもダブルクリックされたか
    private Coroutine blinkCoroutine; // 点滅コルーチンの参照
    private Image targetImage; // 対象の画像コンポーネント
    private Outline outlineComponent; // アウトラインコンポーネント
    private Color originalColor; // 元の色を保存
    private FileOpen fileOpenComponent; // FileOpenコンポーネント

    // クリック検出用
    private float lastClickTime = 0f; // 最後のクリック時刻
    private const float DOUBLE_CLICK_TIME = 0.3f; // ダブルクリック判定時間

    // 定数定義
    private const string FIRST_FILE_NAME = "初めて見かけた日.txt"; // 最初にクリックすべきファイル名

    /// <summary>
    /// 初期化処理
    /// </summary>
    private void Awake()
    {
        // 必要なコンポーネントを取得
        targetImage = GetComponent<Image>();
        fileOpenComponent = GetComponent<FileOpen>();

        if (targetImage == null)
        {
            Debug.LogWarning($"{nameof(FirstFileClickTip)}: Image コンポーネントが見つかりません");
            enabled = false;
            return;
        }

        // 元の色を保存
        originalColor = targetImage.color;

        // アウトラインコンポーネントの設定
        if (useOutlineEffect)
        {
            SetupOutlineEffect();
        }
    }

    /// <summary>
    /// 開始処理
    /// </summary>
    private void Start()
    {
        // ヒント表示条件をチェック
        if (!ShouldShowTip())
        {
            enabled = false;
            return;
        }

        // 効果を開始
        StartEffect();
    }

    /// <summary>
    /// 更新処理（クリック検出用）
    /// </summary>
    private void Update()
    {
        if (!isEffectActive || hasBeenClicked_FirstFileClickTip) return;

        // マウスクリックを検出
        if (Input.GetMouseButtonDown(0))
        {
            // このオブジェクト上でクリックされたかチェック
            if (IsPointerOverGameObject())
            {
                CheckForDoubleClick();
            }
        }
    }

    /// <summary>
    /// マウスポインタがこのオブジェクト上にあるかチェック
    /// </summary>
    private bool IsPointerOverGameObject()
    {
        // EventSystemを使用してレイキャストを実行
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == gameObject)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// ダブルクリックをチェック
    /// </summary>
    private void CheckForDoubleClick()
    {
        float currentTime = Time.time;

        // 前回のクリックからの経過時間をチェック
        if (currentTime - lastClickTime < DOUBLE_CLICK_TIME)
        {
            // ダブルクリック検出
            OnFileDoubleClicked();
        }

        lastClickTime = currentTime;
    }

    /// <summary>
    /// ヒントを表示すべきか判定
    /// </summary>
    private bool ShouldShowTip()
    {
        // ヒントが無効の場合
        if (!enableTip) return false;

        // 対象ファイル名でない場合
        if (!gameObject.name.Contains(FIRST_FILE_NAME)) return false;

        // すでにヒントを表示済みの場合（セーブデータで管理）
        if (GameSaveManager.Instance != null)
        {
            var saveData = GameSaveManager.Instance.GetCurrentSaveData();
            if (saveData != null)
            {
                // firstFileTipShownフィールドが存在する場合のチェック
                // 注: GameSaveDataクラスにこのフィールドを追加する必要があります
                try
                {
                    var field = saveData.GetType().GetField("firstFileTipShown");
                    if (field != null && field.GetValue(saveData) is bool shown && shown)
                    {
                        return false;
                    }
                }
                catch
                {
                    // フィールドが存在しない場合は表示する
                }
            }
        }

        return true;
    }

    /// <summary>
    /// アウトライン効果の設定
    /// </summary>
    private void SetupOutlineEffect()
    {
        // Outlineコンポーネントを追加または取得
        outlineComponent = GetComponent<Outline>();
        if (outlineComponent == null)
        {
            outlineComponent = gameObject.AddComponent<Outline>();
        }

        // アウトラインの設定
        outlineComponent.effectColor = glowColor;
        outlineComponent.effectDistance = new Vector2(outlineWidth, outlineWidth);
        outlineComponent.useGraphicAlpha = false;
        outlineComponent.enabled = false; // 初期状態では無効
    }

    /// <summary>
    /// 効果を開始
    /// </summary>
    private void StartEffect()
    {
        if (isEffectActive) return;

        isEffectActive = true;
        blinkCoroutine = StartCoroutine(BlinkEffect());

        Debug.Log($"{nameof(FirstFileClickTip)}: ヒント効果を開始しました - {gameObject.name}");
    }

    /// <summary>
    /// 効果を停止
    /// </summary>
    private void StopEffect()
    {
        if (!isEffectActive) return;

        isEffectActive = false;

        // コルーチンを停止
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        // 元の状態に戻す
        if (targetImage != null)
        {
            targetImage.color = originalColor;
        }

        // アウトラインを無効化
        if (outlineComponent != null)
        {
            outlineComponent.enabled = false;
        }

        // セーブデータに記録
        if (GameSaveManager.Instance != null)
        {
            var saveData = GameSaveManager.Instance.GetCurrentSaveData();
            if (saveData != null)
            {
                // firstFileTipShownフィールドに値を設定
                // 注: GameSaveDataクラスにこのフィールドを追加する必要があります
                try
                {
                    var field = saveData.GetType().GetField("firstFileTipShown");
                    if (field != null)
                    {
                        field.SetValue(saveData, true);
                        GameSaveManager.Instance.SaveGame();
                    }
                }
                catch
                {
                    // フィールドが存在しない場合は何もしない
                }
            }
        }

        Debug.Log($"{nameof(FirstFileClickTip)}: ヒント効果を停止しました - {gameObject.name}");

        // コンポーネントを無効化
        enabled = false;
    }

    /// <summary>
    /// 点滅効果のコルーチン
    /// </summary>
    private IEnumerator BlinkEffect()
    {
        while (isEffectActive)
        {
            // 点滅アニメーション
            float elapsedTime = 0f;

            // フェードイン
            while (elapsedTime < blinkDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / blinkDuration;
                float curveValue = blinkCurve.Evaluate(normalizedTime);

                // グロー色との線形補間
                Color currentColor = Color.Lerp(originalColor, glowColor, curveValue * 0.5f);
                currentColor.a = Mathf.Lerp(minAlpha, maxAlpha, curveValue);

                if (targetImage != null)
                {
                    targetImage.color = currentColor;
                }

                // アウトラインの透明度も変更
                if (outlineComponent != null)
                {
                    outlineComponent.enabled = true;
                    Color outlineColor = glowColor;
                    outlineColor.a = curveValue * maxAlpha;
                    outlineComponent.effectColor = outlineColor;
                }

                yield return null;
            }

            // フェードアウト
            elapsedTime = 0f;
            while (elapsedTime < blinkDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / blinkDuration;
                float curveValue = blinkCurve.Evaluate(1f - normalizedTime);

                // 元の色に戻す
                Color currentColor = Color.Lerp(originalColor, glowColor, curveValue * 0.5f);
                currentColor.a = Mathf.Lerp(minAlpha, maxAlpha, curveValue);

                if (targetImage != null)
                {
                    targetImage.color = currentColor;
                }

                // アウトラインの透明度も変更
                if (outlineComponent != null)
                {
                    Color outlineColor = glowColor;
                    outlineColor.a = curveValue * maxAlpha;
                    outlineComponent.effectColor = outlineColor;
                }

                yield return null;
            }

            // 元の状態に戻す
            if (targetImage != null)
            {
                targetImage.color = originalColor;
            }

            if (outlineComponent != null)
            {
                outlineComponent.enabled = false;
            }

            // インターバル待機
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    /// <summary>
    /// ファイルがダブルクリックされた時の処理
    /// </summary>
    private void OnFileDoubleClicked()
    {
        if (hasBeenClicked_FirstFileClickTip) return;

        hasBeenClicked_FirstFileClickTip = true;
        Debug.Log($"{nameof(FirstFileClickTip)}: ファイルがダブルクリックされました - {gameObject.name}");

        // 効果を停止
        StopEffect();
    }

    /// <summary>
    /// コンポーネントが無効化された時の処理
    /// </summary>
    private void OnDisable()
    {
        // クリーンアップ
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        // 元の状態に戻す
        if (targetImage != null)
        {
            targetImage.color = originalColor;
        }

        if (outlineComponent != null)
        {
            outlineComponent.enabled = false;
        }
    }

    /// <summary>
    /// エディタ用：効果をテスト
    /// </summary>
    [ContextMenu("Test Effect")]
    private void TestEffect()
    {
        if (Application.isPlaying)
        {
            if (isEffectActive)
            {
                StopEffect();
            }
            else
            {
                StartEffect();
            }
        }
    }
}