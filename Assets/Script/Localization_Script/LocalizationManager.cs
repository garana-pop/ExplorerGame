using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ExplorerGame.Localization
{
    /// <summary>
    /// ローカライゼーション管理クラス
    /// Unity Localizationを使用した言語切り替え機能を提供
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        // シングルトンインスタンス
        public static LocalizationManager Instance { get; private set; }

        // 言語コード定数
        private const string JAPANESE_CODE = "ja";
        private const string ENGLISH_CODE = "en";

        // 一時保存用の言語コード（TitleSceneで使用）
        private string preparedLanguageCode;

        // 言語変更時のイベント
        public System.Action<Locale> OnLanguageChanged;

        // デバッグモード（インスペクターで設定可能）
        [SerializeField] private bool debugMode = false;

        /// <summary>
        /// 初期化処理
        /// </summary>
        private void Awake()
        {
            // シングルトンの実装
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (debugMode)
            {
                Debug.Log($"{nameof(LocalizationManager)}: インスタンスを初期化しました");
            }
        }

        /// <summary>
        /// Start時の処理
        /// </summary>
        private IEnumerator Start()
        {
            if (debugMode)
            {
                Debug.Log($"{nameof(LocalizationManager)}: LocalizationSettings初期化開始");
            }

            // LocalizationSettingsの初期化を待機
            yield return LocalizationSettings.InitializationOperation;

            if (debugMode)
            {
                Debug.Log($"{nameof(LocalizationManager)}: LocalizationSettings初期化完了");
            }

            // 保存された言語設定を読み込む（Step 6で実装）
            // LoadLanguageSetting();
        }

        /// <summary>
        /// 現在の言語コードを取得
        /// </summary>
        /// <returns>現在の言語コード</returns>
        public string GetCurrentLanguageCode()
        {
            if (LocalizationSettings.SelectedLocale == null)
            {
                if (debugMode)
                {
                    Debug.Log($"{nameof(LocalizationManager)}: Localeが未選択のため、デフォルト（日本語）を返却");
                }
                return JAPANESE_CODE; // デフォルト
            }

            return LocalizationSettings.SelectedLocale.Identifier.Code;
        }

        /// <summary>
        /// 言語コードからLocaleを取得
        /// </summary>
        /// <param name="languageCode">言語コード</param>
        /// <returns>対応するLocale</returns>
        private Locale GetLocaleByCode(string languageCode)
        {
            var locales = LocalizationSettings.AvailableLocales.Locales;
            foreach (var locale in locales)
            {
                if (locale.Identifier.Code == languageCode)
                {
                    return locale;
                }
            }

            if (debugMode)
            {
                Debug.LogWarning($"{nameof(LocalizationManager)}: 言語コード '{languageCode}' に対応するLocaleが見つかりません");
            }

            return null;
        }

        /// <summary>
        /// 言語を即座に切り替える
        /// </summary>
        /// <param name="languageCode">言語コード（"ja" または "en"）</param>
        /// <returns>切り替え処理のコルーチン</returns>
        public IEnumerator ChangeLanguage(string languageCode)
        {
            // 言語コードの妥当性チェック
            if (languageCode != JAPANESE_CODE && languageCode != ENGLISH_CODE)
            {
                Debug.LogWarning($"{nameof(LocalizationManager)}: 無効な言語コード: {languageCode}");
                yield break;
            }

            // Localeを取得
            Locale newLocale = GetLocaleByCode(languageCode);
            if (newLocale == null)
            {
                Debug.LogError($"{nameof(LocalizationManager)}: Localeが見つかりません: {languageCode}");
                yield break;
            }

            // 現在の言語と同じ場合はスキップ
            if (LocalizationSettings.SelectedLocale == newLocale)
            {
                if (debugMode)
                {
                    Debug.Log($"{nameof(LocalizationManager)}: 既に {languageCode} に設定されています");
                }
                yield break;
            }

            // 言語変更
            LocalizationSettings.SelectedLocale = newLocale;

            // 変更が完了するまで待機
            yield return LocalizationSettings.SelectedLocaleAsync;

            // 設定を保存（Step 6で実装）
            // SaveLanguageSetting(languageCode);

            // イベント発火
            OnLanguageChanged?.Invoke(newLocale);

            if (debugMode)
            {
                Debug.Log($"{nameof(LocalizationManager)}: 言語を {languageCode} に変更しました");
            }
        }

        /// <summary>
        /// 言語選択を一時保存（SaveButton押下まで適用しない）
        /// TitleSceneの言語選択用
        /// </summary>
        /// <param name="languageCode">選択された言語コード</param>
        public void PrepareLanguageChange(string languageCode)
        {
            if (languageCode != JAPANESE_CODE && languageCode != ENGLISH_CODE)
            {
                Debug.LogWarning($"{nameof(LocalizationManager)}: 無効な言語コード: {languageCode}");
                return;
            }

            preparedLanguageCode = languageCode;

            if (debugMode)
            {
                Debug.Log($"{nameof(LocalizationManager)}: 言語 {languageCode} を一時保存");
            }
        }

        /// <summary>
        /// 一時保存された言語設定を適用
        /// </summary>
        public void ApplyPreparedLanguageChange()
        {
            if (string.IsNullOrEmpty(preparedLanguageCode))
            {
                Debug.LogWarning($"{nameof(LocalizationManager)}: 適用する言語設定がありません");
                return;
            }

            // コルーチンを開始して言語を変更
            StartCoroutine(ChangeLanguage(preparedLanguageCode));

            // 一時保存をクリア
            preparedLanguageCode = null;
        }

#if UNITY_EDITOR
        // エディタ用デバッグ機能
        [ContextMenu("Switch to Japanese")]
        private void SwitchToJapanese()
        {
            StartCoroutine(ChangeLanguage(JAPANESE_CODE));
        }

        [ContextMenu("Switch to English")]
        private void SwitchToEnglish()
        {
            StartCoroutine(ChangeLanguage(ENGLISH_CODE));
        }

        [ContextMenu("Log Current Language")]
        private void LogCurrentLanguage()
        {
            Debug.Log($"現在の言語: {GetCurrentLanguageCode()}");
        }

        [ContextMenu("Log Available Locales")]
        private void LogAvailableLocales()
        {
            if (LocalizationSettings.AvailableLocales == null)
            {
                Debug.LogWarning("AvailableLocalesがnullです");
                return;
            }

            var locales = LocalizationSettings.AvailableLocales.Locales;
            Debug.Log($"利用可能なLocale数: {locales.Count}");
            foreach (var locale in locales)
            {
                Debug.Log($"- {locale.Identifier.Code}: {locale.LocaleName}");
            }
        }
#endif
    }
}