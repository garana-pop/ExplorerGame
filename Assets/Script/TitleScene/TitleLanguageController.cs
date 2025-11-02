using UnityEngine;
using UnityEngine.UI;
using ExplorerGame.Localization;

namespace ExplorerGame.UI
{
    /// <summary>
    /// TitleSceneの言語選択機能を制御するクラス
    /// LanguagePanelの日本語/Englishボタンと連携
    /// </summary>
    public class TitleLanguageController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button japaneseButton;     // 日本語_Button
        [SerializeField] private Button englishButton;      // English_Button
        [SerializeField] private Button saveButton;         // SaveButton
        [SerializeField] private GameObject languagePanel;  // LanguagePanel

        [Header("Visual Feedback")]
        [SerializeField] private Color selectedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color normalColor = Color.white;

        // 一時選択された言語コード
        private string selectedLanguageCode;

        // デバッグモード
        [Header("Debug Settings")]
        [SerializeField] private bool debugMode = false;

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Start()
        {
            // ボタンのイベントリスナー設定
            SetupButtonListeners();

            // 現在の言語に基づいて初期選択状態を設定
            InitializeSelection();

            if (debugMode)
            {
                DebugLogger.Log($"{nameof(TitleLanguageController)}: 初期化完了");
            }
        }

        /// <summary>
        /// ボタンのイベントリスナーを設定
        /// </summary>
        private void SetupButtonListeners()
        {
            // 日本語ボタン
            if (japaneseButton != null)
            {
                japaneseButton.onClick.RemoveAllListeners();
                japaneseButton.onClick.AddListener(() => OnLanguageButtonClicked("ja"));
            }
            else
            {
                DebugLogger.LogWarning($"{nameof(TitleLanguageController)}: 日本語_Buttonが設定されていません");
            }

            // Englishボタン
            if (englishButton != null)
            {
                englishButton.onClick.RemoveAllListeners();
                englishButton.onClick.AddListener(() => OnLanguageButtonClicked("en"));
            }
            else
            {
                DebugLogger.LogWarning($"{nameof(TitleLanguageController)}: English_Buttonが設定されていません");
            }

            // SaveButton
            if (saveButton != null)
            {
                saveButton.onClick.RemoveAllListeners();
                saveButton.onClick.AddListener(OnSaveButtonClicked);
            }
            else
            {
                DebugLogger.LogWarning($"{nameof(TitleLanguageController)}: SaveButtonが設定されていません");
            }
        }

        /// <summary>
        /// 現在の言語に基づいて初期選択状態を設定
        /// </summary>
        private void InitializeSelection()
        {
            if (LocalizationManager.Instance != null)
            {
                string currentLanguageCode = LocalizationManager.Instance.GetCurrentLanguageCode();
                selectedLanguageCode = currentLanguageCode;
                UpdateButtonVisuals(currentLanguageCode);

                if (debugMode)
                {
                    DebugLogger.Log($"{nameof(TitleLanguageController)}: 初期言語設定: {currentLanguageCode}");
                }
            }
        }

        /// <summary>
        /// 言語ボタンクリック時の処理
        /// </summary>
        /// <param name="languageCode">選択された言語コード</param>
        private void OnLanguageButtonClicked(string languageCode)
        {
            if (LocalizationManager.Instance == null)
            {
                DebugLogger.LogError($"{nameof(TitleLanguageController)}: LocalizationManagerが見つかりません");
                return;
            }

            // 一時保存
            selectedLanguageCode = languageCode;
            LocalizationManager.Instance.PrepareLanguageChange(languageCode);

            // ビジュアルフィードバック更新
            UpdateButtonVisuals(languageCode);

            // 効果音再生（SEManagerが存在する場合）
            PlayClickSound();

            if (debugMode)
            {
                DebugLogger.Log($"{nameof(TitleLanguageController)}: 言語 '{languageCode}' を一時選択");
            }
        }

        /// <summary>
        /// SaveButtonクリック時の処理
        /// </summary>
        private void OnSaveButtonClicked()
        {
            if (LocalizationManager.Instance == null)
            {
                DebugLogger.LogError($"{nameof(TitleLanguageController)}: LocalizationManagerが見つかりません");
                return;
            }

            // 一時保存された言語設定を適用
            LocalizationManager.Instance.ApplyPreparedLanguageChange();

            // 効果音再生
            PlayClickSound();

            // 言語パネルを閉じる（必要に応じて）
            if (languagePanel != null)
            {
                StartCoroutine(CloseLanguagePanelWithDelay());
            }

            if (debugMode)
            {
                DebugLogger.Log($"{nameof(TitleLanguageController)}: 言語設定を適用しました");
            }
        }

        /// <summary>
        /// ボタンのビジュアル状態を更新
        /// </summary>
        /// <param name="selectedCode">選択された言語コード</param>
        private void UpdateButtonVisuals(string selectedCode)
        {
            // 日本語ボタン
            if (japaneseButton != null)
            {
                Image japaneseImage = japaneseButton.GetComponent<Image>();
                if (japaneseImage != null)
                {
                    japaneseImage.color = (selectedCode == "ja") ? selectedColor : normalColor;
                }
            }

            // Englishボタン
            if (englishButton != null)
            {
                Image englishImage = englishButton.GetComponent<Image>();
                if (englishImage != null)
                {
                    englishImage.color = (selectedCode == "en") ? selectedColor : normalColor;
                }
            }
        }

        /// <summary>
        /// クリック音を再生
        /// </summary>
        private void PlayClickSound()
        {
            // SoundEffectManagerが存在する場合、効果音を再生
            SoundEffectManager seManager = FindObjectOfType<SoundEffectManager>();
            if (seManager != null)
            {
                seManager.PlayClickSound();
            }
        }

        /// <summary>
        /// 言語パネルを遅延して閉じる
        /// </summary>
        private System.Collections.IEnumerator CloseLanguagePanelWithDelay()
        {
            // 言語変更の適用を待つ
            yield return new WaitForSeconds(0.5f);

            // パネルを非表示にする
            if (languagePanel != null)
            {
                languagePanel.SetActive(false);

                if (debugMode)
                {
                    DebugLogger.Log($"{nameof(TitleLanguageController)}: LanguagePanelを閉じました");
                }
            }
        }

        /// <summary>
        /// オブジェクト破棄時の処理
        /// </summary>
        private void OnDestroy()
        {
            // イベントリスナーのクリーンアップ
            if (japaneseButton != null)
            {
                japaneseButton.onClick.RemoveAllListeners();
            }

            if (englishButton != null)
            {
                englishButton.onClick.RemoveAllListeners();
            }

            if (saveButton != null)
            {
                saveButton.onClick.RemoveAllListeners();
            }
        }

        // エディタ用テストメソッド
#if UNITY_EDITOR
        [ContextMenu("Test Japanese Selection")]
        private void TestJapaneseSelection()
        {
            OnLanguageButtonClicked("ja");
        }

        [ContextMenu("Test English Selection")]
        private void TestEnglishSelection()
        {
            OnLanguageButtonClicked("en");
        }

        [ContextMenu("Test Save")]
        private void TestSave()
        {
            OnSaveButtonClicked();
        }
#endif
    }
}