using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BlinkEffect : MonoBehaviour
{
    [Header("まぶたの設定")]
    [SerializeField] private RectTransform topEyelid;    // 上まぶた
    [SerializeField] private RectTransform bottomEyelid; // 下まぶた
    [SerializeField] private float closedPosition = 0f;  // 目を閉じた時の位置（画面中央）

    [Header("まばたきのタイミング")]
    [SerializeField] private float minBlinkInterval = 5f;    // 最小まばたき間隔
    [SerializeField] private float maxBlinkInterval = 10f;   // 最大まばたき間隔
    [SerializeField] private float blinkDuration = 0.2f;     // まばたき1回の所要時間
    [SerializeField] private float initialBlinkDelay = 1f;   // 初期遅延

    [Header("起動時の設定")]
    [SerializeField] private bool startWithClosed = true;    // 目を閉じた状態から開始するか
    [SerializeField] private float openingDuration = 1.0f;   // 起動時に目を開く時間

    // プライベート変数
    private float topEyelidOpenPosition;    // 上まぶたの開いた状態の位置
    private float bottomEyelidOpenPosition; // 下まぶたの開いた状態の位置
    private bool isBlinking = false;        // まばたき中かどうか

    private void Start()
    {
        // まぶたが設定されていなければログを表示
        if (topEyelid == null || bottomEyelid == null)
        {
            Debug.LogError("BlinkEffect: 上下のまぶたのRectTransformが設定されていません。インスペクターで設定してください。");
            enabled = false;
            return;
        }

        // 開いた状態の位置を記録
        topEyelidOpenPosition = topEyelid.anchoredPosition.y;
        bottomEyelidOpenPosition = bottomEyelid.anchoredPosition.y;

        // 起動時に目を閉じているか開いているかの設定
        if (startWithClosed)
        {
            // 完全に閉じた位置に設定
            SetEyelidsPosition(1.0f);
        }
        else
        {
            // 完全に開いた位置に設定
            SetEyelidsPosition(0.0f);
        }

        // まばたきコルーチンを開始
        StartCoroutine(BlinkCoroutine());
    }

    private IEnumerator BlinkCoroutine()
    {
        // 初期遅延
        yield return new WaitForSeconds(initialBlinkDelay);

        // 起動時に目を閉じている場合は、徐々に開く
        if (startWithClosed)
        {
            yield return StartCoroutine(OpenEyes());
        }

        // 通常のまばたきサイクル
        while (true)
        {
            // 次のまばたきまでの時間を設定
            float nextBlinkTime = Random.Range(minBlinkInterval, maxBlinkInterval);
            yield return new WaitForSeconds(nextBlinkTime);

            // まばたき実行
            if (!isBlinking)
            {
                yield return StartCoroutine(PerformBlink());
            }
        }
    }

    private IEnumerator OpenEyes()
    {
        float timer = 0;

        while (timer < openingDuration)
        {
            timer += Time.deltaTime;
            float t = timer / openingDuration;
            float openRatio = 1.0f - t; // 1.0（閉）から0.0（開）へ

            SetEyelidsPosition(openRatio);

            yield return null;
        }

        // 完全に開いた状態に設定
        SetEyelidsPosition(0);
    }

    private IEnumerator PerformBlink()
    {
        isBlinking = true;

        // まぶたを閉じる
        float timer = 0;
        float halfDuration = blinkDuration / 2;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;

            SetEyelidsPosition(t);

            yield return null;
        }

        // 完全に閉じた状態を確保
        SetEyelidsPosition(1.0f);

        // まぶたを開く
        timer = 0;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;

            SetEyelidsPosition(1.0f - t);

            yield return null;
        }

        // 完全に開いた状態を確保
        SetEyelidsPosition(0);

        isBlinking = false;
    }

    // まぶたの位置を設定するヘルパーメソッド
    // blinkProgress: 0.0 = 完全に開いた状態、1.0 = 完全に閉じた状態
    private void SetEyelidsPosition(float blinkProgress)
    {
        if (topEyelid == null || bottomEyelid == null) return;

        // 上まぶたは下に移動、下まぶたは上に移動
        Vector2 topPos = topEyelid.anchoredPosition;
        Vector2 bottomPos = bottomEyelid.anchoredPosition;

        // 上まぶたの位置を計算（開いた位置から閉じた位置へ線形に移動）
        topPos.y = Mathf.Lerp(topEyelidOpenPosition, closedPosition, blinkProgress);

        // 下まぶたの位置を計算（開いた位置から閉じた位置へ線形に移動）
        bottomPos.y = Mathf.Lerp(bottomEyelidOpenPosition, closedPosition, blinkProgress);

        // 位置を適用
        topEyelid.anchoredPosition = topPos;
        bottomEyelid.anchoredPosition = bottomPos;
    }

    // まばたきを手動でトリガーするパブリックメソッド
    public void TriggerBlink()
    {
        if (!isBlinking)
        {
            StartCoroutine(PerformBlink());
        }
    }
}