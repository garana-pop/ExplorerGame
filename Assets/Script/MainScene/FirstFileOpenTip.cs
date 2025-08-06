using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// ファイルをダブルクリックで開くことを示唆するヒント表示コンポーネント
/// FirstFileClickTipクラスと連携して動作
/// </summary>
public class FirstFileOpenTip : MonoBehaviour, IPointerClickHandler
{
    [Header("ヒント表示設定")]
    [SerializeField] private bool enableTip = true; // ヒント表示の有効/無効
    [SerializeField] private GameObject tipObject; // DraggingCanvasに移動するヒントオブジェクト
    [SerializeField] private Transform draggingCanvas; // DraggingCanvasへの参照
    [SerializeField] private Vector3 tipOffset = new Vector3(50f, -50f, 0f); // ファイルからの相対位置

    [Header("表示タイミング設定")]
    [SerializeField] private float displayDelay = 0.5f; // シングルクリック後の表示遅延
    [SerializeField] private float autoHideTime = 5.0f; // 自動非表示までの時間（0で無効）

    [Header("視覚設定")]
    [SerializeField] private bool fadeInEffect = true; // フェードイン効果の使用
    [SerializeField] private float fadeInDuration = 0.3f; // フェードイン時間
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false; // デバッグログの表示

    // 内部状態管理
    private bool isShowingTip = false; // ヒントが表示中か
    private bool hasBeenDoubleClicked = false; // ダブルクリック済みか
    private float lastClickTime = 0f; // 最後のクリック時刻
    private Coroutine showTipCoroutine; // ヒント表示コルーチン
    private Coroutine autoHideCoroutine; // 自動非表示コルーチン
    private FirstFileClickTip clickTipComponent; // FirstFileClickTipコンポーネントへの参照
    private CanvasGroup tipCanvasGroup; // ヒントのCanvasGroup
    private Vector3 originalTipPosition; // ヒントの元の位置
    private Transform originalTipParent; // ヒントの元の親

    // 定数定義
    private const float DOUBLE_CLICK_TIME = 0.3f; // ダブルクリック判定時間
    private const string DRAGGING_CANVAS_NAME = "DraggingCanvas"; // DraggingCanvasの名前

    /// <summary>
    /// 初期化処理
    /// </summary>
    private void Awake()
    {
        // FirstFileClickTipコンポーネントの取得
        clickTipComponent = GetComponent<FirstFileClickTip>();

        // DraggingCanvasの自動検索
        if (draggingCanvas == null)
        {
            GameObject draggingCanvasObj = GameObject.Find(DRAGGING_CANVAS_NAME);
            if (draggingCanvasObj != null)
            {
                draggingCanvas = draggingCanvasObj.transform;
            }
            else
            {
                Debug.LogWarning($"{nameof(FirstFileOpenTip)}: DraggingCanvasが見つかりません");
                enabled = false;
                return;
            }
        }

        // ヒントオブジェクトの設定
        if (tipObject != null)
        {
            // 元の位置と親を保存
            originalTipPosition = tipObject.transform.position;
            originalTipParent = tipObject.transform.parent;

            // CanvasGroup設定
            tipCanvasGroup = tipObject.GetComponent<CanvasGroup>();
            if (tipCanvasGroup == null && fadeInEffect)
            {
                tipCanvasGroup = tipObject.AddComponent<CanvasGroup>();
            }

            // 初期状態で非表示
            HideTip(true);
        }
        else
        {
            Debug.LogWarning($"{nameof(FirstFileOpenTip)}: ヒントオブジェクトが設定されていません");
            enabled = false;
        }
    }

    /// <summary>
    /// IPointerClickHandlerの実装
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!enableTip || hasBeenDoubleClicked) return;

        float currentTime = Time.time;

        // ダブルクリック判定
        if (currentTime - lastClickTime < DOUBLE_CLICK_TIME)
        {
            OnDoubleClick();
        }
        else
        {
            // シングルクリックの処理
            OnSingleClick();
        }

        lastClickTime = currentTime;
    }

    /// <summary>
    /// シングルクリック時の処理
    /// </summary>
    private void OnSingleClick()
    {
        // FirstFileClickTipのhasBeenClicked_FirstFileClickTipフラグをチェック
        if (clickTipComponent == null || ShouldShowTip())
        {
            // 既にヒント表示中の場合はキャンセル
            if (showTipCoroutine != null)
            {
                StopCoroutine(showTipCoroutine);
            }

            // ヒント表示コルーチンを開始
            showTipCoroutine = StartCoroutine(ShowTipAfterDelay());
        }
    }

    /// <summary>
    /// ダブルクリック時の処理
    /// </summary>
    private void OnDoubleClick()
    {
        hasBeenDoubleClicked = true;

        // ヒント表示をキャンセル
        if (showTipCoroutine != null)
        {
            StopCoroutine(showTipCoroutine);
            showTipCoroutine = null;
        }

        // ヒントが表示中なら非表示にする
        if (isShowingTip)
        {
            HideTip(false);
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(FirstFileOpenTip)}: ダブルクリックを検出 - {gameObject.name}");
        }
    }

    /// <summary>
    /// ヒント表示条件の判定
    /// </summary>
    private bool ShouldShowTip()
    {
        // FirstFileClickTipのhasBeenClicked_FirstFileClickTipフラグがfalseの場合のみ表示
        if (clickTipComponent != null)
        {
            // リフレクションを使用してprivateフィールドにアクセス
            var fieldInfo = clickTipComponent.GetType().GetField("hasBeenClicked_FirstFileClickTip",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fieldInfo != null)
            {
                bool hasBeenClicked_FirstFileClickTip = (bool)fieldInfo.GetValue(clickTipComponent);
                return !hasBeenClicked_FirstFileClickTip;
            }
        }

        return true; // FirstFileClickTipがない場合はデフォルトで表示
    }

    /// <summary>
    /// 遅延後にヒントを表示するコルーチン
    /// </summary>
    private IEnumerator ShowTipAfterDelay()
    {
        yield return new WaitForSeconds(displayDelay);

        // 再度条件をチェック
        if (!hasBeenDoubleClicked && ShouldShowTip())
        {
            ShowTip();
        }
    }

    /// <summary>
    /// ヒントを表示
    /// </summary>
    private void ShowTip()
    {
        if (tipObject == null || isShowingTip) return;

        isShowingTip = true;

        // DraggingCanvasに移動
        tipObject.transform.SetParent(draggingCanvas);

        // 位置を設定（ファイルの近くに配置）
        Vector3 worldPos = transform.position + tipOffset;
        tipObject.transform.position = worldPos;

        // アクティブ化
        tipObject.SetActive(true);

        // フェードイン効果
        if (fadeInEffect && tipCanvasGroup != null)
        {
            StartCoroutine(FadeIn());
        }

        // 自動非表示の設定
        if (autoHideTime > 0)
        {
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
            }
            autoHideCoroutine = StartCoroutine(AutoHide());
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(FirstFileOpenTip)}: ヒントを表示しました - {gameObject.name}");
        }
    }

    /// <summary>
    /// ヒントを非表示
    /// </summary>
    private void HideTip(bool immediate)
    {
        if (tipObject == null) return;

        isShowingTip = false;

        // コルーチンの停止
        if (showTipCoroutine != null)
        {
            StopCoroutine(showTipCoroutine);
            showTipCoroutine = null;
        }

        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        if (immediate)
        {
            // 即座に非表示
            tipObject.SetActive(false);

            // 元の親に戻す
            tipObject.transform.SetParent(originalTipParent);
            tipObject.transform.position = originalTipPosition;
        }
        else
        {
            // フェードアウト効果
            if (fadeInEffect && tipCanvasGroup != null)
            {
                StartCoroutine(FadeOut());
            }
            else
            {
                tipObject.SetActive(false);
                tipObject.transform.SetParent(originalTipParent);
                tipObject.transform.position = originalTipPosition;
            }
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(FirstFileOpenTip)}: ヒントを非表示にしました - {gameObject.name}");
        }
    }

    /// <summary>
    /// フェードイン効果
    /// </summary>
    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        tipCanvasGroup.alpha = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / fadeInDuration;
            tipCanvasGroup.alpha = fadeInCurve.Evaluate(normalizedTime);
            yield return null;
        }

        tipCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// フェードアウト効果
    /// </summary>
    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        float startAlpha = tipCanvasGroup.alpha;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / fadeInDuration;
            tipCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, normalizedTime);
            yield return null;
        }

        tipCanvasGroup.alpha = 0f;
        tipObject.SetActive(false);

        // 元の親に戻す
        tipObject.transform.SetParent(originalTipParent);
        tipObject.transform.position = originalTipPosition;
    }

    /// <summary>
    /// 自動非表示コルーチン
    /// </summary>
    private IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(autoHideTime);
        HideTip(false);
    }

    /// <summary>
    /// FirstFileClickTipのhasBeenClicked_FirstFileClickTipフラグを監視
    /// </summary>
    private void Update()
    {
        if (!isShowingTip || clickTipComponent == null) return;

        // FirstFileClickTipのhasBeenClicked_FirstFileClickTipがtrueになったら非表示
        var fieldInfo = clickTipComponent.GetType().GetField("hasBeenClicked_FirstFileClickTip",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (fieldInfo != null)
        {
            bool hasBeenClicked_FirstFileClickTip = (bool)fieldInfo.GetValue(clickTipComponent);
            if (hasBeenClicked_FirstFileClickTip)
            {
                HideTip(false);
            }
        }
    }

    /// <summary>
    /// コンポーネント無効化時の処理
    /// </summary>
    private void OnDisable()
    {
        // クリーンアップ
        if (showTipCoroutine != null)
        {
            StopCoroutine(showTipCoroutine);
            showTipCoroutine = null;
        }

        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        // ヒントを非表示
        if (isShowingTip)
        {
            HideTip(true);
        }
    }

    /// <summary>
    /// エディタ用：ヒント表示をテスト
    /// </summary>
    [ContextMenu("Test Show Tip")]
    private void TestShowTip()
    {
        if (Application.isPlaying)
        {
            if (isShowingTip)
            {
                HideTip(false);
            }
            else
            {
                ShowTip();
            }
        }
    }
}