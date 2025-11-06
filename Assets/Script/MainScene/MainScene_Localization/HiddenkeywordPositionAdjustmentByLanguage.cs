using ExplorerGame.Localization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// 言語設定に応じてHiddenKeywordの親オブジェクトとその子要素の位置を調整するクラス
/// </summary>
public class HiddenkeywordPositionAdjustmentByLanguage : MonoBehaviour
{
    // HiddenKeywordのサイズを調整するクラスを参照
    private HiddenkeywordSizeAdjustmentByLanguage sizeAdjustment;

    // 英語用の位置調整設定
    [System.Serializable]
    public class EnglishPositionAdjustment
    {
        [Tooltip("調整対象の親オブジェクト（Line1Text等）")]
        public Transform targetParentObject;

        [Tooltip("英語での親オブジェクトの新しい位置")]
        public Vector3 englishParentPosition;

        [Tooltip("子のHiddenKeywordも個別に位置調整する")]
        public bool adjustChildHiddenKeyword = false;

        [Tooltip("英語での子HiddenKeywordの新しい位置（相対位置）")]
        public Vector3 englishChildOffset = Vector3.zero;

        [Tooltip("英語での新しいローカル位置を使用する")]
        public bool useLocalPosition = true;

        [Tooltip("元の親オブジェクトの位置（日本語）を保存")]
        [HideInInspector]
        public Vector3 originalParentPosition;

        [Tooltip("元の子HiddenKeywordの相対位置（日本語）を保存")]
        [HideInInspector]
        public Vector3 originalChildOffset;

        // 内部で管理する子HiddenKeywordへの参照
        [HideInInspector]
        public HiddenKeyword childHiddenKeyword;
    }

    [Header("位置調整設定")]
    [SerializeField] private List<EnglishPositionAdjustment> positionAdjustments = new List<EnglishPositionAdjustment>();

    // 言語コード定数
    private const string JAPANESE_CODE = "ja";
    private const string ENGLISH_CODE = "en";

    // 現在の言語コード
    private string currentLanguageCode = JAPANESE_CODE;

    // 2. Awakeメソッドに以下を追加
    private void Awake()
    {
        // サイズ調整コンポーネントへの参照を取得
        sizeAdjustment = GetComponent<HiddenkeywordSizeAdjustmentByLanguage>();
    }

    private void Start()
    {
        // 子のHiddenKeywordコンポーネントを検索して保存
        FindChildHiddenKeywords();

        // 元の位置を保存
        SaveOriginalPositions();

        // LocalizationManagerから現在の言語設定を取得して適用
        ApplyLanguagePositionAdjustment();

        // 言語変更イベントに登録
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }
        else
        {
            Debug.LogWarning($"{nameof(HiddenkeywordPositionAdjustmentByLanguage)}: LocalizationManagerが見つかりません");
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
        foreach (var adjustment in positionAdjustments)
        {
            if (adjustment.targetParentObject != null)
            {
                // 子オブジェクトからHiddenKeywordコンポーネントを検索
                adjustment.childHiddenKeyword = adjustment.targetParentObject.GetComponentInChildren<HiddenKeyword>();
            }
        }
    }

    /// <summary>
    /// 元の位置（日本語での位置）を保存
    /// </summary>
    private void SaveOriginalPositions()
    {
        foreach (var adjustment in positionAdjustments)
        {
            if (adjustment.targetParentObject != null)
            {
                // 親オブジェクトの位置を保存
                if (adjustment.useLocalPosition)
                {
                    adjustment.originalParentPosition = adjustment.targetParentObject.localPosition;
                }
                else
                {
                    adjustment.originalParentPosition = adjustment.targetParentObject.position;
                }

                // 子HiddenKeywordの相対位置を保存
                if (adjustment.adjustChildHiddenKeyword && adjustment.childHiddenKeyword != null)
                {
                    adjustment.originalChildOffset = adjustment.childHiddenKeyword.transform.localPosition;
                }
            }
        }
    }

    /// <summary>
    /// 言語変更時のコールバック
    /// </summary>
    private void OnLanguageChanged(Locale newLocale)
    {
        ApplyLanguagePositionAdjustment();
    }

    /// <summary>
    /// 現在の言語設定に基づいて位置を調整
    /// </summary>
    private void ApplyLanguagePositionAdjustment()
    {
        // LocalizationManagerから現在の言語コードを取得
        if (LocalizationManager.Instance == null)
        {
            Debug.LogWarning($"{nameof(HiddenkeywordPositionAdjustmentByLanguage)}: LocalizationManagerが存在しません。日本語設定として処理します");
            currentLanguageCode = JAPANESE_CODE;
        }
        else
        {
            currentLanguageCode = LocalizationManager.Instance.GetCurrentLanguageCode();
        }

        // 言語に応じて位置を調整
        foreach (var adjustment in positionAdjustments)
        {
            if (adjustment.targetParentObject == null)
            {
                continue;
            }

            if (currentLanguageCode == ENGLISH_CODE)
            {
                // 英語の場合は指定された位置に移動
                ApplyEnglishPosition(adjustment);
            }
            else
            {
                // 日本語（またはその他の言語）の場合は元の位置に戻す
                ApplyOriginalPosition(adjustment);
            }
        }

        // サイズも調整
        if (sizeAdjustment != null)
        {
            sizeAdjustment.ApplyLanguageSizeAdjustment();
        }

    }

    /// <summary>
    /// 英語の位置を適用
    /// </summary>
    private void ApplyEnglishPosition(EnglishPositionAdjustment adjustment)
    {
        // 親オブジェクトの位置を変更
        if (adjustment.useLocalPosition)
        {
            adjustment.targetParentObject.localPosition = adjustment.englishParentPosition;
        }
        else
        {
            adjustment.targetParentObject.position = adjustment.englishParentPosition;
        }

        // 子HiddenKeywordの相対位置も調整（必要な場合）
        if (adjustment.adjustChildHiddenKeyword && adjustment.childHiddenKeyword != null)
        {
            adjustment.childHiddenKeyword.transform.localPosition = adjustment.englishChildOffset;
        }
    }

    /// <summary>
    /// 元の位置を適用
    /// </summary>
    private void ApplyOriginalPosition(EnglishPositionAdjustment adjustment)
    {
        // 親オブジェクトを元の位置に戻す
        if (adjustment.useLocalPosition)
        {
            adjustment.targetParentObject.localPosition = adjustment.originalParentPosition;
        }
        else
        {
            adjustment.targetParentObject.position = adjustment.originalParentPosition;
        }

        // 子HiddenKeywordも元の相対位置に戻す（必要な場合）
        if (adjustment.adjustChildHiddenKeyword && adjustment.childHiddenKeyword != null)
        {
            adjustment.childHiddenKeyword.transform.localPosition = adjustment.originalChildOffset;
        }
    }

    /// <summary>
    /// エディタでの位置調整テスト用メソッド
    /// </summary>
    [ContextMenu("Test English Position")]
    private void TestEnglishPosition()
    {
        currentLanguageCode = ENGLISH_CODE;
        ApplyLanguagePositionAdjustment();
        DebugLogger.Log($"{nameof(HiddenkeywordPositionAdjustmentByLanguage)}: 英語位置をテスト適用しました");
    }

    /// <summary>
    /// エディタでの位置調整テスト用メソッド
    /// </summary>
    [ContextMenu("Test Japanese Position")]
    private void TestJapanesePosition()
    {
        currentLanguageCode = JAPANESE_CODE;
        ApplyLanguagePositionAdjustment();
        DebugLogger.Log($"{nameof(HiddenkeywordPositionAdjustmentByLanguage)}: 日本語位置をテスト適用しました");
    }

    /// <summary>
    /// 現在の位置を英語位置として保存（エディタ用）
    /// </summary>
    [ContextMenu("Save Current Positions as English")]
    private void SaveCurrentPositionsAsEnglish()
    {
        foreach (var adjustment in positionAdjustments)
        {
            if (adjustment.targetParentObject != null)
            {
                // 親オブジェクトの現在位置を保存
                if (adjustment.useLocalPosition)
                {
                    adjustment.englishParentPosition = adjustment.targetParentObject.localPosition;
                }
                else
                {
                    adjustment.englishParentPosition = adjustment.targetParentObject.position;
                }
                // 子HiddenKeywordの現在の相対位置も保存
                if (adjustment.adjustChildHiddenKeyword && adjustment.childHiddenKeyword != null)
                {
                    adjustment.englishChildOffset = adjustment.childHiddenKeyword.transform.localPosition;
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
    }
}