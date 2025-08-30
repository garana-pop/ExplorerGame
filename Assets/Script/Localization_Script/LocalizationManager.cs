using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ExplorerGame.Localization
{
    /// <summary>
    /// Unity Localizationパッケージを使用したローカライゼーション管理クラス
    /// 日本語/英語の切り替えとテキスト管理を統括
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        // シングルトンインスタンス
        public static LocalizationManager Instance { get; private set; }

        // 言語コード定数
        private const string JAPANESE_CODE = "ja";
        private const string ENGLISH_CODE = "en";

        // 一時保存用の言語コード（設定画面用）
        private string preparedLanguageCode;

        // 言語変更完了イベント
        public event System.Action<Locale> OnLanguageChanged;

        // デバッグ設定
        [SerializeField] private bool debugMode = false;

        /// <summary>
        /// シングルトンの初期化とDontDestroyOnLoad設定
        /// </summary>
        private void Awake()
        {
            // シングルトン実装
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);

                if (debugMode)
                {
                    Debug.Log($"{nameof(LocalizationManager)}: インスタンスを初期化しました");
                }
            }
            else
            {
                // 重複インスタンスの削除
                if (debugMode)
                {
                    Debug.LogWarning($"{nameof(LocalizationManager)}: 重複インスタンスを削除します");
                }
                Destroy(gameObject);
                return;
            }
        }

        /// <summary>
        /// Localization Settingsの初期化と保存された言語設定の復元
        /// </summary>
        private IEnumerator Start()
        {
            if (debugMode)
            {
                Debug.Log($"{nameof(LocalizationManager)}: LocalizationSettings初期化開始");
            }

            // LocalizationSettingsの初期化を待つ
            yield return LocalizationSettings.InitializationOperation;

            if (debugMode)
            {
                Debug.Log($"{nameof(LocalizationManager)}: LocalizationSettings初期化完了");
            }

            // 保存された言語設定を読み込む（後のステップで実装）
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

            // 言語を変更
            LocalizationSettings.SelectedLocale = newLocale;

            // 変更が完了するまで待機
            yield return LocalizationSettings.SelectedLocaleAsync;

            // 設定を保存（後のステップで実装）
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

            if (debugMode)
            {
                Debug.Log($"{nameof(LocalizationManager)}: 一時保存された言語 {preparedLanguageCode} を適用");
            }

            // コルーチンを開始して言語を変更
            StartCoroutine(ChangeLanguage(preparedLanguageCode));

            // 一時保存をクリア
            preparedLanguageCode = null;
        }

        /// <summary>
        /// キーから動的にローカライズテキストを取得（非同期）
        /// </summary>
        /// <param name="key">ローカライゼーションキー</param>
        /// <param name="callback">取得したテキストを受け取るコールバック</param>
        /// <returns>ローカライズされたテキスト取得のコルーチン</returns>
        public IEnumerator GetLocalizedString(string key, System.Action<string> callback)
        {
            var localizedString = new LocalizedString
            {
                TableReference = "SceneStringTable",
                TableEntryReference = key
            };

            var handle = localizedString.GetLocalizedStringAsync();
            yield return handle;

            if (handle.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
            {
                callback?.Invoke(handle.Result);

                if (debugMode)
                {
                    Debug.Log($"{nameof(LocalizationManager)}: キー '{key}' のテキスト取得成功: {handle.Result}");
                }
            }
            else
            {
                Debug.LogError($"{nameof(LocalizationManager)}: キー '{key}' のテキスト取得に失敗");
                callback?.Invoke(key); // フォールバック
            }
        }

        // デバッグ機能
#if UNITY_EDITOR
        [ContextMenu("Switch to Japanese")]
        private void TestSwitchToJapanese()
        {
            StartCoroutine(ChangeLanguage(JAPANESE_CODE));
        }

        [ContextMenu("Switch to English")]
        private void TestSwitchToEnglish()
        {
            StartCoroutine(ChangeLanguage(ENGLISH_CODE));
        }

        [ContextMenu("Print Current Language")]
        private void PrintCurrentLanguage()
        {
            Debug.Log($"{nameof(LocalizationManager)}: 現在の言語コード: {GetCurrentLanguageCode()}");
        }

        [ContextMenu("Print Available Locales")]
        private void PrintAvailableLocales()
        {
            if (LocalizationSettings.AvailableLocales == null)
            {
                Debug.LogWarning($"{nameof(LocalizationManager)}: AvailableLocalesが初期化されていません");
                return;
            }

            var locales = LocalizationSettings.AvailableLocales.Locales;
            Debug.Log($"{nameof(LocalizationManager)}: 利用可能なLocale数: {locales.Count}");

            foreach (var locale in locales)
            {
                Debug.Log($"  - {locale.LocaleName} ({locale.Identifier.Code})");
            }
        }
#endif

        /// <summary>
        /// インスタンス破棄時の処理
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                if (debugMode)
                {
                    Debug.Log($"{nameof(LocalizationManager)}: インスタンスを破棄しました");
                }
                Instance = null;
            }
        }
    }
}