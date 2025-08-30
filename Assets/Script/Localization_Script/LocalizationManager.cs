using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

namespace ExplorerGame.Localization
{
    /// <summary>
    /// ローカライズ設定を管理するシングルトンクラス
    /// Unity Localizationパッケージと連携して言語切り替えを実装
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        // シングルトンインスタンス
        public static LocalizationManager Instance { get; private set; }

        // 言語コード定数
        private const string JAPANESE_CODE = "ja";
        private const string ENGLISH_CODE = "en";

        // 一時保存用言語コード（TitleScene用）
        private string preparedLanguageCode;

        // 言語変更時のイベント
        public event Action<Locale> OnLanguageChanged;

        // デバッグモード
        [Header("Debug Settings")]
        [SerializeField] private bool debugMode = false;

        // テストコントロール
        [Header("Test Controls")]
        [SerializeField] private bool showTestButtons = false;

        /// <summary>
        /// 初期化処理（シングルトン設定）
        /// </summary>
        private void Awake()
        {
            // シングルトン処理
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (debugMode)
            {
                Debug.Log($"{nameof(LocalizationManager)}: インスタンス初期化完了");
            }
        }

        /// <summary>
        /// LocalizationSettings初期化待機
        /// </summary>
        private IEnumerator Start()
        {
            // LocalizationSettingsの初期化を待機
            var handle = LocalizationSettings.InitializationOperation;
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (debugMode)
                {
                    Debug.Log($"{nameof(LocalizationManager)}: LocalizationSettings初期化完了");
                    Debug.Log($"現在の言語: {GetCurrentLanguageCode()}");
                }

                // 保存された言語設定を読み込み
                LoadLanguageSetting();
            }
            else
            {
                Debug.LogError($"{nameof(LocalizationManager)}: LocalizationSettings初期化失敗");
            }
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

        // 修正箇所: ChangeLanguageメソッド内の言語切り替え処理
        public IEnumerator ChangeLanguage(string languageCode)
        {
            // 言語コードの妥当性チェック
            if (languageCode != JAPANESE_CODE && languageCode != ENGLISH_CODE)
            {
                Debug.LogWarning($"{nameof(LocalizationManager)}: 無効な言語コード: {languageCode}");
                yield break;
            }

            // Locale取得
            Locale newLocale = GetLocaleByCode(languageCode);
            if (newLocale == null)
            {
                Debug.LogError($"{nameof(LocalizationManager)}: 言語コード '{languageCode}' のLocaleが取得できません");
                yield break;
            }

            // 現在の言語と同じ場合はスキップ
            if (LocalizationSettings.SelectedLocale == newLocale)
            {
                if (debugMode)
                {
                    Debug.Log($"{nameof(LocalizationManager)}: 既に言語 '{languageCode}' が選択されています");
                }
                yield break;
            }

            // 言語切り替え（非同期）
            var handle = LocalizationSettings.Instance.GetSelectedLocaleAsync();
            LocalizationSettings.SelectedLocale = newLocale;
            yield return handle;

            // 設定を保存
            SaveLanguageSetting(languageCode);

            // イベント発火
            OnLanguageChanged?.Invoke(newLocale);

            if (debugMode)
            {
                Debug.Log($"{nameof(LocalizationManager)}: 言語を{languageCode}に変更しました");
            }
        }

        /// <summary>
        /// 言語選択を一時保存（SaveButton押下まで適用しない）
        /// TitleScene用メソッド
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
                Debug.Log($"{nameof(LocalizationManager)}: 言語{languageCode}を一時保存");
            }
        }

        /// <summary>
        /// 一時保存された言語設定を適用
        /// TitleScene用メソッド
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

        // 修正案: GameSaveManager に saveData プロパティが存在しないため、GetCurrentSaveData() を使って取得するよう修正
        private void LoadLanguageSetting()
        {
            // GameSaveManagerが存在する場合、セーブデータから言語を読み込み
            var saveManager = FindObjectOfType<GameSaveManager>();
            var saveData = saveManager != null ? saveManager.GetCurrentSaveData() : null;
            if (saveManager != null && saveData != null)
            {
                string savedLanguageCode = saveData.languageCode;
                if (!string.IsNullOrEmpty(savedLanguageCode))
                {
                    StartCoroutine(ChangeLanguage(savedLanguageCode));

                    if (debugMode)
                    {
                        Debug.Log($"{nameof(LocalizationManager)}: セーブデータから言語 '{savedLanguageCode}' を読み込みました");
                    }
                }
            }
        }

        /// <summary>
        /// 言語設定をセーブデータに保存
        /// </summary>
        /// <param name="languageCode">保存する言語コード</param>
        private void SaveLanguageSetting(string languageCode)
        {
            // GameSaveManagerが存在する場合、言語設定を保存
            var saveManager = FindObjectOfType<GameSaveManager>();
            var saveData = saveManager != null ? saveManager.GetCurrentSaveData() : null;
            if (saveManager != null && saveData != null)
            {
                saveData.languageCode = languageCode;
                saveManager.SaveGame();

                if (debugMode)
                {
                    Debug.Log($"{nameof(LocalizationManager)}: 言語設定 '{languageCode}' を保存しました");
                }
            }
        }

        /// <summary>
        /// 動的にローカライズテキストを取得（非同期）
        /// </summary>
        /// <param name="key">ローカライゼーションキー</param>
        /// <param name="callback">取得したテキストを受け取るコールバック</param>
        /// <returns>テキスト取得のコルーチン</returns>
        public IEnumerator GetLocalizedString(string key, System.Action<string> callback)
        {
            var localizedString = new LocalizedString
            {
                TableReference = "SceneStringTable",
                TableEntryReference = key
            };

            var handle = localizedString.GetLocalizedStringAsync();
            yield return handle;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                callback?.Invoke(handle.Result);

                if (debugMode)
                {
                    Debug.Log($"{nameof(LocalizationManager)}: キー '{key}' のテキストを取得: {handle.Result}");
                }
            }
            else
            {
                Debug.LogError($"{nameof(LocalizationManager)}: キー '{key}' のテキスト取得に失敗");
                callback?.Invoke(key); // フォールバック
            }
        }

        // エディタ用デバッグメソッド
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

        [ContextMenu("Show Current Language")]
        private void ShowCurrentLanguage()
        {
            Debug.Log($"現在の言語コード: {GetCurrentLanguageCode()}");

            if (LocalizationSettings.SelectedLocale != null)
            {
                Debug.Log($"Locale名: {LocalizationSettings.SelectedLocale.name}");
                Debug.Log($"Locale識別子: {LocalizationSettings.SelectedLocale.Identifier}");
            }
        }

        [ContextMenu("Show Available Locales")]
        private void ShowAvailableLocales()
        {
            var locales = LocalizationSettings.AvailableLocales.Locales;
            Debug.Log($"利用可能なLocale数: {locales.Count}");

            foreach (var locale in locales)
            {
                Debug.Log($"- {locale.Identifier.Code}: {locale.name}");
            }
        }
        #endif
    }
}