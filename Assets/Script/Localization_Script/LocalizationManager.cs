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
            LoadLanguageSetting();
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
            SaveLanguageSetting(languageCode);

            // イベント発火
            OnLanguageChanged?.Invoke(newLocale);

            if (debugMode)
            {
                Debug.Log($"{nameof(LocalizationManager)}: 言語を {languageCode} に変更しました");
            }
        }

        /// <summary>
        /// 言語設定をセーブデータに保存
        /// </summary>
        /// <param name="languageCode">保存する言語コード</param>
        private void SaveLanguageSetting(string languageCode)
        {
            try
            {
                // GameSaveManagerのインスタンスを取得
                if (GameSaveManager.Instance == null)
                {
                    Debug.LogWarning($"{nameof(LocalizationManager)}: GameSaveManagerが見つかりません");
                    return;
                }

                // 現在のセーブデータを取得
                GameSaveData saveData = GameSaveManager.Instance.GetCurrentSaveData();

                if (saveData == null)
                {
                    Debug.LogWarning($"{nameof(LocalizationManager)}: セーブデータが取得できません");
                    return;
                }

                // 言語コードを更新
                saveData.languageCode = languageCode;

                // セーブデータを保存
                GameSaveManager.Instance.SaveGame();

                if (debugMode)
                {
                    Debug.Log($"{nameof(LocalizationManager)}: 言語設定 '{languageCode}' をセーブデータに保存しました");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{nameof(LocalizationManager)}: 言語設定の保存に失敗しました: {e.Message}");
            }
        }

        /// <summary>
        /// セーブデータから言語設定を読み込んで適用
        /// </summary>
        private void LoadLanguageSetting()
        {
            try
            {
                // GameSaveManagerのインスタンスを取得
                if (GameSaveManager.Instance == null)
                {
                    Debug.LogWarning($"{nameof(LocalizationManager)}: GameSaveManagerが見つかりません");
                    return;
                }

                // 現在のセーブデータを取得
                GameSaveData saveData = GameSaveManager.Instance.GetCurrentSaveData();

                if (saveData == null)
                {
                    if (debugMode)
                    {
                        Debug.Log($"{nameof(LocalizationManager)}: セーブデータが存在しません。デフォルト言語を使用");
                    }
                    return;
                }

                // 保存された言語コードを取得
                string savedLanguageCode = string.IsNullOrEmpty(saveData.languageCode)
                    ? JAPANESE_CODE
                    : saveData.languageCode;

                // 保存された言語を適用
                StartCoroutine(ChangeLanguage(savedLanguageCode));

                if (debugMode)
                {
                    Debug.Log($"{nameof(LocalizationManager)}: セーブデータから言語設定 '{savedLanguageCode}' を読み込みました");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"{nameof(LocalizationManager)}: 言語設定の読み込みに失敗しました: {e.Message}");
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
        [ContextMenu("Test Save Language Setting")]
        private void TestSaveLanguageSetting()
        {
            string currentCode = GetCurrentLanguageCode();
            SaveLanguageSetting(currentCode);
            Debug.Log($"言語設定 '{currentCode}' を保存しました");
        }

        [ContextMenu("Test Load Language Setting")]
        private void TestLoadLanguageSetting()
        {
            LoadLanguageSetting();
            Debug.Log("言語設定を読み込みました");
        }

        [ContextMenu("Show Current Save Data Language")]
        private void ShowCurrentSaveDataLanguage()
        {
            if (GameSaveManager.Instance != null)
            {
                GameSaveData saveData = GameSaveManager.Instance.GetCurrentSaveData();
                if (saveData != null)
                {
                    Debug.Log($"セーブデータの言語設定: {saveData.languageCode}");
                }
                else
                {
                    Debug.Log("セーブデータが存在しません");
                }
            }
        }
#endif
    }
}