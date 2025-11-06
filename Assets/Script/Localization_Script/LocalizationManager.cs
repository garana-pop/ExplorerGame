using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

namespace ExplorerGame.Localization
{
    /// <summary>
    /// ローカライズ設定を管理するシングルトンクラス
    /// Unity Localizationパッケージと連携して言語切り替えを制御
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        // シングルトンインスタンス
        public static LocalizationManager Instance { get; private set; }

        // 言語コード定数
        private const string JAPANESE_CODE = "ja";
        private const string ENGLISH_CODE = "en";

        // String Table名の定数
        private const string SCENE_STRING_TABLE_NAME = "SceneStringTable";

        // 一時保存用言語コード（TitleScene用）
        private string preparedLanguageCode;

        // 言語変更完了イベント
        public event Action<Locale> OnLanguageChanged;

        // デバッグモード
        [Header("Debug Settings")]
        [SerializeField] private bool debugMode = true;

        // テストコントロール
        [Header("Test Controls")]
        [SerializeField] private bool showTestButtons = false;

        /// <summary>
        /// 初期化処理（シングルトン設定）
        /// </summary>
        private void Awake()
        {
            // シングルトン実装
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
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
                // return JAPANESE_CODE; // デフォルト: "ja"
                return ENGLISH_CODE; // デフォルト: "en"
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

            // Locale取得
            Locale newLocale = GetLocaleByCode(languageCode);
            if (newLocale == null)
            {
                Debug.LogError($"{nameof(LocalizationManager)}: 言語コード '{languageCode}' のLocaleを取得できません");
                yield break;
            }

            // 現在の言語と同じ場合はスキップ
            if (LocalizationSettings.SelectedLocale == newLocale)
            {
                if (debugMode)
                {
                    DebugLogger.Log($"{nameof(LocalizationManager)}: 既に言語 '{languageCode}' が選択されています");
                }
                yield break;
            }

            // 言語切り替え（非同期）
            LocalizationSettings.SelectedLocale = newLocale;

            // 変更が完了するまで待機
            yield return new WaitUntil(() => LocalizationSettings.SelectedLocale == newLocale);

            // 設定を保存
            SaveLanguageSetting(languageCode);

            // イベント発火
            OnLanguageChanged?.Invoke(newLocale);
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
                DebugLogger.Log($"{nameof(LocalizationManager)}: 言語{languageCode}を一時保存");
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

        /// <summary>
        /// キーから動的にローカライズテキストを取得（非同期）
        /// </summary>
        /// <param name="key">ローカライゼーションキー</param>
        /// <param name="callback">テキスト取得後のコールバック</param>
        /// <returns>ローカライズされたテキスト取得のコルーチン</returns>
        public IEnumerator GetLocalizedString(string key, System.Action<string> callback)
        {
            // 引数チェック
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError($"{nameof(LocalizationManager)}: キーが空またはnullです");
                callback?.Invoke(key);
                yield break;
            }

            if (callback == null)
            {
                Debug.LogWarning($"{nameof(LocalizationManager)}: コールバックがnullです");
                yield break;
            }

            // LocalizedString作成
            var localizedString = new LocalizedString
            {
                TableReference = SCENE_STRING_TABLE_NAME,
                TableEntryReference = key
            };

            // デバッグログ
            if (debugMode)
            {
                DebugLogger.Log($"{nameof(LocalizationManager)}: キー '{key}' のテキスト取得を開始");
            }

            // 非同期でテキスト取得
            var handle = localizedString.GetLocalizedStringAsync();
            yield return handle;

            // 結果処理
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (debugMode)
                {
                    DebugLogger.Log($"{nameof(LocalizationManager)}: キー '{key}' → '{handle.Result}'");
                }
                callback.Invoke(handle.Result);
            }
            else
            {
                Debug.LogError($"{nameof(LocalizationManager)}: キー '{key}' のテキスト取得に失敗");
                if (handle.OperationException != null)
                {
                    Debug.LogError($"エラー詳細: {handle.OperationException.Message}");
                }

                // フォールバック処理（キーをそのまま返す）
                callback.Invoke(key);
            }
        }

        /// <summary>
        /// 指定テーブルからキーに対応するローカライズテキストを取得（非同期）
        /// </summary>
        /// <param name="tableName">String Tableの名前</param>
        /// <param name="key">ローカライゼーションキー</param>
        /// <param name="callback">テキスト取得後のコールバック</param>
        /// <returns>ローカライズされたテキスト取得のコルーチン</returns>
        public IEnumerator GetLocalizedString(string tableName, string key, System.Action<string> callback)
        {
            // 引数チェック
            if (string.IsNullOrEmpty(tableName))
            {
                Debug.LogError($"{nameof(LocalizationManager)}: テーブル名が空またはnullです");
                callback?.Invoke(key);
                yield break;
            }

            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError($"{nameof(LocalizationManager)}: キーが空またはnullです");
                callback?.Invoke(key);
                yield break;
            }

            if (callback == null)
            {
                Debug.LogWarning($"{nameof(LocalizationManager)}: コールバックがnullです");
                yield break;
            }

            // LocalizedString作成
            var localizedString = new LocalizedString
            {
                TableReference = tableName,
                TableEntryReference = key
            };

            // デバッグログ
            if (debugMode)
            {
                DebugLogger.Log($"{nameof(LocalizationManager)}: テーブル '{tableName}' からキー '{key}' のテキスト取得を開始");
            }

            // 非同期でテキスト取得
            var handle = localizedString.GetLocalizedStringAsync();
            yield return handle;

            // 結果処理
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                if (debugMode)
                {
                    DebugLogger.Log($"{nameof(LocalizationManager)}: キー '{key}' → '{handle.Result}'");
                }
                callback.Invoke(handle.Result);
            }
            else
            {
                Debug.LogError($"{nameof(LocalizationManager)}: テーブル '{tableName}' のキー '{key}' のテキスト取得に失敗");
                if (handle.OperationException != null)
                {
                    Debug.LogError($"エラー詳細: {handle.OperationException.Message}");
                }

                // フォールバック処理
                callback.Invoke(key);
            }
        }

        /// <summary>
        /// 保存された言語設定を読み込んで適用
        /// </summary>
        private void LoadLanguageSetting()
        {
            // GameSaveManagerが存在する場合、セーブデータから言語を読み込み
            var saveManager = FindObjectOfType<GameSaveManager>();
            var saveData = saveManager != null ? saveManager.GetCurrentSaveData() : null;

            string languageCodeToApply = ENGLISH_CODE; // デフォルトは英語

            if (saveManager != null && saveData != null)
            {
                string savedLanguageCode = saveData.languageCode;
                if (!string.IsNullOrEmpty(savedLanguageCode))
                {
                    StartCoroutine(ChangeLanguage(savedLanguageCode));

                    if (debugMode)
                    {
                        DebugLogger.Log($"{nameof(LocalizationManager)}: セーブデータから言語 '{savedLanguageCode}' を読み込みました");
                    }
                }
                else
                {
                    // セーブデータはあるが言語設定がない場合（初回起動）
                    languageCodeToApply = ENGLISH_CODE;
                    saveData.languageCode = ENGLISH_CODE;
                    saveManager.SaveGame(); // 英語設定を保存

                    if (debugMode)
                    {
                        Debug.Log($"{nameof(LocalizationManager)}: 初回起動のため英語を設定して保存");
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
                    DebugLogger.Log($"{nameof(LocalizationManager)}: 言語 '{languageCode}' をセーブデータに保存しました");
                }
            }
        }

//#if UNITY_EDITOR
//        // エディタ用デバッグメソッド
//        [ContextMenu("Switch to Japanese")]
//        void TestSwitchToJapanese()
//        {
//            StartCoroutine(ChangeLanguage("ja"));
//        }

//        [ContextMenu("Switch to English")]
//        void TestSwitchToEnglish()
//        {
//            StartCoroutine(ChangeLanguage("en"));
//        }

//        [ContextMenu("Test Get Localized String")]
//        void TestGetLocalizedString()
//        {
//            StartCoroutine(GetLocalizedString("Test-Key-001", (result) =>
//            {
//                DebugLogger.Log($"GetLocalizedString結果: '{result}'");
//            }));
//        }

//        [ContextMenu("Show Current Language")]
//        void ShowCurrentLanguage()
//        {
//            DebugLogger.Log($"現在の言語コード: {GetCurrentLanguageCode()}");
//            if (LocalizationSettings.SelectedLocale != null)
//            {
//                DebugLogger.Log($"現在のLocale: {LocalizationSettings.SelectedLocale.name}");
//            }
//        }

//        [ContextMenu("List Available Locales")]
//        void ListAvailableLocales()
//        {
//            var locales = LocalizationSettings.AvailableLocales.Locales;
//            DebugLogger.Log($"利用可能なLocale数: {locales.Count}");
//            foreach (var locale in locales)
//            {
//                DebugLogger.Log($"- {locale.Identifier.Code}: {locale.name}");
//            }
//        }
//#endif
    }
}