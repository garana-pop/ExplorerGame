using UnityEngine;
using ExplorerGame.Localization;
using System.Collections.Generic;
using UnityEngine.Localization;

/// <summary>
/// 言語設定に応じてPDFで隠されていたキーワードのサイズを調整するクラス
/// HiddenkeywordPositionAdjustmentByLanguageクラスから呼び出される
/// </summary>
public class HiddenkeywordSizeAdjustmentByLanguage : MonoBehaviour
{
    /// <summary>
    /// 英語用のサイズ調整設定
    /// </summary>
    [System.Serializable]
    public class EnglishSizeAdjustment
    {
        [Header("対象設定")]
        [Tooltip("サイズ調整する親オブジェクト（Line1Textなど）")]
        public RectTransform targetParentObject;

        [Header("親オブジェクトのサイズ調整")]
        [Tooltip("英語時の親オブジェクトのサイズ（日本語の場合は変更しない）")]
        public Vector2 englishParentSize = new Vector2(200f, 50f);

        [Tooltip("親オブジェクトのサイズを調整するか")]
        public bool adjustParentSize = true;

        [Header("子HiddenKeywordのサイズ調整")]
        [Tooltip("子のHiddenKeywordコンポーネントも調整するか")]
        public bool adjustChildHiddenKeyword = true;

        [Tooltip("英語時の子HiddenKeywordのサイズ")]
        public Vector2 englishChildSize = new Vector2(100f, 30f);

        // 内部参照
        [HideInInspector]
        public HiddenKeyword childHiddenKeyword;

        // 元のサイズを保存
        [HideInInspector]
        public Vector2 originalParentSize;

        [HideInInspector]
        public Vector2 originalChildSize;
    }

    [Header("言語コード定数")]
    private const string JAPANESE_CODE = "ja";
    private const string ENGLISH_CODE = "en";

    [Header("サイズ調整設定")]
    [Tooltip("英語用のサイズ調整リスト")]
    [SerializeField] private List<EnglishSizeAdjustment> sizeAdjustments = new List<EnglishSizeAdjustment>();

    [Header("デバッグ設定")]
    [Tooltip("デバッグログを表示するか")]
    [SerializeField] private bool debugMode = false;

    // 現在の言語コード
    private string currentLanguageCode = JAPANESE_CODE;

    // HiddenkeywordPositionAdjustmentByLanguageへの参照
    private HiddenkeywordPositionAdjustmentByLanguage positionAdjustment;

    private void Awake()
    {
        // 同じゲームオブジェクトのHiddenkeywordPositionAdjustmentByLanguageを取得
        positionAdjustment = GetComponent<HiddenkeywordPositionAdjustmentByLanguage>();

        // 各親オブジェクトの子からHiddenKeywordコンポーネントを検索
        FindChildHiddenKeywords();

        // 元のサイズを保存
        SaveOriginalSizes();
    }

    private void Start()
    {
        // LocalizationManagerのイベントに登録
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;

            // 初期言語設定を適用
            currentLanguageCode = LocalizationManager.Instance.GetCurrentLanguageCode();
            ApplyLanguageSizeAdjustment();

            if (debugMode)
            {
                Debug.Log($"{nameof(HiddenkeywordSizeAdjustmentByLanguage)}: LocalizationManagerの言語変更イベントに登録しました");
            }
        }
        else
        {
            Debug.LogWarning($"{nameof(HiddenkeywordSizeAdjustmentByLanguage)}: LocalizationManagerが見つかりません");
        }
    }

    private void OnDestroy()
    {
        // イベントの登録解除
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    /// <summary>
    /// 各親オブジェクトの子からHiddenKeywordコンポーネントを検索
    /// </summary>
    private void FindChildHiddenKeywords()
    {
        foreach (var adjustment in sizeAdjustments)
        {
            if (adjustment.targetParentObject != null)
            {
                // 子オブジェクトからHiddenKeywordコンポーネントを検索
                adjustment.childHiddenKeyword = adjustment.targetParentObject.GetComponentInChildren<HiddenKeyword>();

                if (adjustment.childHiddenKeyword != null && debugMode)
                {
                    Debug.Log($"{nameof(HiddenkeywordSizeAdjustmentByLanguage)}: {adjustment.targetParentObject.name}の子から{adjustment.childHiddenKeyword.name}を発見");
                }
            }
        }
    }

    /// <summary>
    /// 元のサイズ（日本語でのサイズ）を保存
    /// </summary>
    private void SaveOriginalSizes()
    {
        foreach (var adjustment in sizeAdjustments)
        {
            if (adjustment.targetParentObject != null)
            {
                // 親オブジェクトのサイズを保存
                adjustment.originalParentSize = adjustment.targetParentObject.sizeDelta;

                if (debugMode)
                {
                    Debug.Log($"{nameof(HiddenkeywordSizeAdjustmentByLanguage)}: {adjustment.targetParentObject.name}の元のサイズを保存: {adjustment.originalParentSize}");
                }

                // 子HiddenKeywordのサイズを保存
                if (adjustment.adjustChildHiddenKeyword && adjustment.childHiddenKeyword != null)
                {
                    RectTransform childRect = adjustment.childHiddenKeyword.GetComponent<RectTransform>();
                    if (childRect != null)
                    {
                        adjustment.originalChildSize = childRect.sizeDelta;

                        if (debugMode)
                        {
                            Debug.Log($"{nameof(HiddenkeywordSizeAdjustmentByLanguage)}: 子HiddenKeyword {adjustment.childHiddenKeyword.name}の元のサイズを保存: {adjustment.originalChildSize}");
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 言語変更時のコールバック
    /// </summary>
    private void OnLanguageChanged(Locale newLocale)
    {
        if (debugMode)
        {
            Debug.Log($"{nameof(HiddenkeywordSizeAdjustmentByLanguage)}: 言語が変更されました: {newLocale.Identifier.Code}");
        }

        // HiddenkeywordPositionAdjustmentByLanguageのメソッドが呼ばれた後に実行
        Invoke(nameof(ApplyLanguageSizeAdjustment), 0.1f);
    }

    /// <summary>
    /// 現在の言語設定に基づいてサイズを調整（public メソッド）
    /// HiddenkeywordPositionAdjustmentByLanguageから呼び出される
    /// </summary>
    public void ApplyLanguageSizeAdjustment()
    {
        // LocalizationManagerから現在の言語コードを取得
        if (LocalizationManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(HiddenkeywordSizeAdjustmentByLanguage)}: LocalizationManagerが存在しません。日本語設定として処理します");
            currentLanguageCode = JAPANESE_CODE;
        }
        else
        {
            currentLanguageCode = LocalizationManager.Instance.GetCurrentLanguageCode();
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(HiddenkeywordSizeAdjustmentByLanguage)}: 現在の言語コード: {currentLanguageCode}");
        }

        // 言語に応じてサイズを調整
        foreach (var adjustment in sizeAdjustments)
        {
            if (adjustment.targetParentObject == null)
            {
                continue;
            }

            if (currentLanguageCode == ENGLISH_CODE)
            {
                // 英語の場合は指定されたサイズに変更
                ApplyEnglishSize(adjustment);
            }
            else
            {
                // 日本語（またはその他の言語）の場合は元のサイズに戻す
                ApplyOriginalSize(adjustment);
            }
        }
    }

    /// <summary>
    /// 英語のサイズを適用
    /// </summary>
    private void ApplyEnglishSize(EnglishSizeAdjustment adjustment)
    {
        // 親オブジェクトのサイズを変更
        if (adjustment.adjustParentSize)
        {
            adjustment.targetParentObject.sizeDelta = adjustment.englishParentSize;

            if (debugMode)
            {
                Debug.Log($"{nameof(HiddenkeywordSizeAdjustmentByLanguage)}: {adjustment.targetParentObject.name}を英語サイズに変更: {adjustment.englishParentSize}");
            }
        }

        // 子HiddenKeywordのサイズを調整（必要な場合）
        if (adjustment.adjustChildHiddenKeyword && adjustment.childHiddenKeyword != null)
        {
            RectTransform childRect = adjustment.childHiddenKeyword.GetComponent<RectTransform>();
            if (childRect != null)
            {
                childRect.sizeDelta = adjustment.englishChildSize;

                if (debugMode)
                {
                    Debug.Log($"{nameof(HiddenkeywordSizeAdjustmentByLanguage)}: 子HiddenKeyword {adjustment.childHiddenKeyword.name}のサイズを調整: {adjustment.englishChildSize}");
                }
            }
        }
    }

    /// <summary>
    /// 元のサイズを適用
    /// </summary>
    private void ApplyOriginalSize(EnglishSizeAdjustment adjustment)
    {
        // 親オブジェクトを元のサイズに戻す
        if (adjustment.adjustParentSize)
        {
            adjustment.targetParentObject.sizeDelta = adjustment.originalParentSize;

            if (debugMode)
            {
                Debug.Log($"{nameof(HiddenkeywordSizeAdjustmentByLanguage)}: {adjustment.targetParentObject.name}を元のサイズに戻す: {adjustment.originalParentSize}");
            }
        }

        // 子HiddenKeywordを元のサイズに戻す（必要な場合）
        if (adjustment.adjustChildHiddenKeyword && adjustment.childHiddenKeyword != null)
        {
            RectTransform childRect = adjustment.childHiddenKeyword.GetComponent<RectTransform>();
            if (childRect != null)
            {
                childRect.sizeDelta = adjustment.originalChildSize;

                if (debugMode)
                {
                    Debug.Log($"{nameof(HiddenkeywordSizeAdjustmentByLanguage)}: 子HiddenKeyword {adjustment.childHiddenKeyword.name}を元のサイズに戻す: {adjustment.originalChildSize}");
                }
            }
        }
    }

    /// <summary>
    /// エディタでのサイズ調整テスト用メソッド
    /// </summary>
    [ContextMenu("Test English Size")]
    private void TestEnglishSize()
    {
        currentLanguageCode = ENGLISH_CODE;
        ApplyLanguageSizeAdjustment();
        Debug.Log($"{nameof(HiddenkeywordSizeAdjustmentByLanguage)}: 英語サイズをテスト適用しました");
    }

    /// <summary>
    /// エディタでのサイズ調整テスト用メソッド
    /// </summary>
    [ContextMenu("Test Japanese Size")]
    private void TestJapaneseSize()
    {
        currentLanguageCode = JAPANESE_CODE;
        ApplyLanguageSizeAdjustment();
        Debug.Log($"{nameof(HiddenkeywordSizeAdjustmentByLanguage)}: 日本語サイズをテスト適用しました");
    }

    /// <summary>
    /// 現在のサイズを英語サイズとして保存（エディタ用）
    /// </summary>
    [ContextMenu("Save Current Sizes as English")]
    private void SaveCurrentSizesAsEnglish()
    {
        foreach (var adjustment in sizeAdjustments)
        {
            if (adjustment.targetParentObject != null)
            {
                // 親オブジェクトの現在サイズを保存
                adjustment.englishParentSize = adjustment.targetParentObject.sizeDelta;

                Debug.Log($"{nameof(HiddenkeywordSizeAdjustmentByLanguage)}: {adjustment.targetParentObject.name}の現在サイズを英語サイズとして保存: {adjustment.englishParentSize}");

                // 子HiddenKeywordの現在のサイズを保存
                if (adjustment.adjustChildHiddenKeyword && adjustment.childHiddenKeyword != null)
                {
                    RectTransform childRect = adjustment.childHiddenKeyword.GetComponent<RectTransform>();
                    if (childRect != null)
                    {
                        adjustment.englishChildSize = childRect.sizeDelta;

                        Debug.Log($"{nameof(HiddenkeywordSizeAdjustmentByLanguage)}: 子HiddenKeyword {adjustment.childHiddenKeyword.name}の現在サイズを英語サイズとして保存: {adjustment.englishChildSize}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 子のHiddenKeywordコンポーネントを再検索（エディタ用）
    /// </summary>
    [ContextMenu("Refresh Child HiddenKeywords")]
    private void RefreshChildHiddenKeywords()
    {
        FindChildHiddenKeywords();
        Debug.Log($"{nameof(HiddenkeywordSizeAdjustmentByLanguage)}: 子HiddenKeywordコンポーネントを再検索しました");
    }
}