using UnityEngine;
using TMPro;
using ExplorerGame.Localization;

namespace ExplorerGame.UI
{
    /// <summary>
    /// PDFFilePanel内のテキスト位置をローカライズに応じて調整するコンポーネント
    /// </summary>
    public class PDFTextLocalizationAdjuster : MonoBehaviour
    {
        // 英語用のテキスト位置設定
        [Header("English Text Position Settings")]
        [SerializeField] private Vector3 englishPosition = Vector3.zero;
        [SerializeField] private float englishFontSize = 14f;
        [SerializeField] private bool englishAutoSize = false;

        // 英語用の追加設定
        [Header("English Additional Settings (Optional)")]
        [SerializeField] private bool adjustTextAlignment = false;
        [SerializeField] private TextAlignmentOptions englishTextAlignment = TextAlignmentOptions.Left;

        // デバッグ設定
        [Header("Debug Settings")]
        [SerializeField] private bool debugMode = false;

        // コンポーネント参照
        private TextMeshProUGUI textComponent;
        private RectTransform rectTransform;

        // 元の値を保存
        private Vector3 originalPosition;
        private float originalFontSize;
        private bool originalAutoSize;
        private TextAlignmentOptions originalAlignment;

        // 現在の言語コード
        private string currentLanguageCode;

        private void Awake()
        {
            // コンポーネント取得
            textComponent = GetComponent<TextMeshProUGUI>();
            rectTransform = GetComponent<RectTransform>();

            if (textComponent == null)
            {
                DebugLogger.LogError($"{nameof(PDFTextLocalizationAdjuster)}: TextMeshProUGUIコンポーネントが見つかりません");
                enabled = false;
                return;
            }

            // 元の値を保存
            SaveOriginalValues();
        }

        private void Start()
        {
            // LocalizationManagerから現在の言語設定を取得して適用
            ApplyLocalizationSettings();

            // 言語変更イベントに登録
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
            }
            else
            {
                DebugLogger.LogWarning($"{nameof(PDFTextLocalizationAdjuster)}: LocalizationManagerが見つかりません");
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
        /// 元の値を保存
        /// </summary>
        private void SaveOriginalValues()
        {
            if (rectTransform != null)
            {
                originalPosition = rectTransform.anchoredPosition;
            }

            if (textComponent != null)
            {
                originalFontSize = textComponent.fontSize;
                originalAutoSize = textComponent.enableAutoSizing;
                originalAlignment = textComponent.alignment;
            }

            if (debugMode)
            {
                DebugLogger.Log($"{nameof(PDFTextLocalizationAdjuster)}: 元の値を保存しました - " +
                    $"Position: {originalPosition}, FontSize: {originalFontSize}");
            }
        }

        /// <summary>
        /// 言語変更時のコールバック
        /// </summary>
        private void OnLanguageChanged(UnityEngine.Localization.Locale newLocale)
        {
            ApplyLocalizationSettings();
        }

        /// <summary>
        /// ローカライズ設定を適用
        /// </summary>
        private void ApplyLocalizationSettings()
        {
            if (LocalizationManager.Instance == null)
            {
                // LocalizationManagerが存在しない場合は日本語設定を適用
                ApplyJapaneseSettings();
                return;
            }

            // 現在の言語コードを取得
            currentLanguageCode = LocalizationManager.Instance.GetCurrentLanguageCode();

            if (currentLanguageCode == "en")
            {
                ApplyEnglishSettings();
            }
            else
            {
                ApplyJapaneseSettings();
            }
        }

        /// <summary>
        /// 日本語設定を適用（元の値に戻す）
        /// </summary>
        private void ApplyJapaneseSettings()
        {
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = originalPosition;
            }

            if (textComponent != null)
            {
                textComponent.fontSize = originalFontSize;
                textComponent.enableAutoSizing = originalAutoSize;
                textComponent.alignment = originalAlignment;
            }

            if (debugMode)
            {
                DebugLogger.Log($"{nameof(PDFTextLocalizationAdjuster)}: 日本語設定を適用しました");
            }
        }

        /// <summary>
        /// 英語設定を適用
        /// </summary>
        private void ApplyEnglishSettings()
        {
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = englishPosition;
            }

            if (textComponent != null)
            {
                textComponent.fontSize = englishFontSize;
                textComponent.enableAutoSizing = englishAutoSize;

                // テキストアラインメントの調整（オプション）
                if (adjustTextAlignment)
                {
                    textComponent.alignment = englishTextAlignment;
                }
            }

            if (debugMode)
            {
                DebugLogger.Log($"{nameof(PDFTextLocalizationAdjuster)}: 英語設定を適用しました - " +
                    $"Position: {englishPosition}, FontSize: {englishFontSize}");
            }
        }

        /// <summary>
        /// 設定をリセット（エディタ用）
        /// </summary>
        [ContextMenu("Reset to Original Values")]
        private void ResetToOriginalValues()
        {
            ApplyJapaneseSettings();
            DebugLogger.Log($"{nameof(PDFTextLocalizationAdjuster)}: 元の値にリセットしました");
        }

        /// <summary>
        /// 現在の値を英語設定として保存（エディタ用）
        /// </summary>
        [ContextMenu("Save Current Values as English Settings")]
        private void SaveCurrentAsEnglishSettings()
        {
            if (rectTransform != null)
            {
                englishPosition = rectTransform.anchoredPosition;
            }

            if (textComponent != null)
            {
                englishFontSize = textComponent.fontSize;
                englishAutoSize = textComponent.enableAutoSizing;

                if (adjustTextAlignment)
                {
                    englishTextAlignment = textComponent.alignment;
                }
            }

            DebugLogger.Log($"{nameof(PDFTextLocalizationAdjuster)}: 現在の値を英語設定として保存しました");
        }

#if UNITY_EDITOR
        /// <summary>
        /// インスペクターで値が変更された時の処理
        /// </summary>
        private void OnValidate()
        {
            // エディタでプレイ中の場合、即座に設定を反映
            if (Application.isPlaying && textComponent != null)
            {
                ApplyLocalizationSettings();
            }
        }

        /// <summary>
        /// テスト用：言語を切り替える
        /// </summary>
        [ContextMenu("Test - Switch to Japanese")]
        private void TestSwitchToJapanese()
        {
            currentLanguageCode = "ja";
            ApplyJapaneseSettings();
        }

        [ContextMenu("Test - Switch to English")]
        private void TestSwitchToEnglish()
        {
            currentLanguageCode = "en";
            ApplyEnglishSettings();
        }
#endif
    }
}