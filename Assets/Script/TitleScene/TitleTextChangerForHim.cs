using ExplorerGame.Localization;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>
/// HerMainSceneから遷移してきた際にタイトルを「彼」の未来に変更するコンポーネント
/// タイトルテキストの表示管理はTitleTextLoaderForHimが行う
/// </summary>
public class TitleTextChangerForHim : MonoBehaviour
{
    [Header("テキスト設定")]
    [Tooltip("変更対象のTextMeshProコンポーネント")]
    [SerializeField] private TMP_Text titleText;

    [Header("日本語テキスト設定")]
    [Tooltip("変更後のテキスト（日本語）")]
    [SerializeField] private string newTitleText_Japanese = "「彼」の未来";

    [Header("英語テキスト設定")]
    [Tooltip("変更後のテキスト（英語）")]
    [SerializeField] private string newTitleText_English = "\"His\" Future";

    [Header("アニメーション設定")]
    [Tooltip("1文字変更にかかる時間（秒）")]
    [SerializeField] private float changeInterval = 0.25f;

    [Tooltip("変更開始までの遅延時間（秒）")]
    [SerializeField] private float startDelay = 0.8f;

    [Tooltip("文字変更時のエフェクト（フェード、グリッチなど）")]
    [SerializeField] private bool useGlitchEffect = true;

    [Tooltip("グリッチエフェクトの持続時間（秒）")]
    [SerializeField] private float glitchDuration = 0.08f;

    [Header("ボタン制御設定")]
    [Tooltip("タイトル変更中にMenuContainerのボタンを無効化するか")]
    [SerializeField] private bool disableButtonsDuringChange = true;

    [Tooltip("MenuContainerへの参照（未設定の場合は自動検索）")]
    [SerializeField] private GameObject menuContainer;

    [Header("フラグ管理設定")]
    [Tooltip("タイトル変更後にフラグを設定するか")]
    [SerializeField] private bool setCompletionFlag = true;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceExecute = false;

    // ランタイムで使用する変数（言語設定に応じて切り替え）
    private string newTitleText;
    private string currentText;
    private bool isChanging = false;

    // 静的変数による状態管理
    private static bool shouldExecuteOnNextLoad = false;
    private static bool titleChangedToHisFuture = false;
    private bool soundEnabled = true;

    private readonly string glitchChars = "!#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`{|}~";

    private void Awake()
    {
        if (titleText == null)
        {
            titleText = GetComponent<TMP_Text>();
            if (titleText == null)
            {
                titleText = GetComponentInChildren<TMP_Text>();
            }
        }

        if (titleText == null)
        {
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        // LocalizationManagerから現在の言語設定を取得して適用
        UpdateTextsByLanguage();

        // 言語変更イベントに登録
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }

        currentText = titleText.text;

        if (ShouldExecuteTitleChange())
        {
            if (debugMode) DebugLogger.Log("TitleTextChangerForHim: タイトル変更を開始します");
            StartCoroutine(StartTitleChange());
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
    /// 言語変更時のコールバック
    /// </summary>
    private void OnLanguageChanged(UnityEngine.Localization.Locale newLocale)
    {
        UpdateTextsByLanguage();

        // 変更中でない場合、現在の表示を更新
        if (!isChanging)
        {
            // Localize String Eventコンポーネントを無効化
            DisableLocalizeStringEvent();

            // 適切なテキストを表示（変更済みの場合）
            GameSaveManager saveManager = GameSaveManager.Instance;
            if (saveManager != null && saveManager.GetAfterChangeToHisFutureFlag())
            {
                titleText.text = newTitleText;
            }
        }
    }

    /// <summary>
    /// 現在の言語設定に基づいてテキストを更新
    /// </summary>
    private void UpdateTextsByLanguage()
    {
        if (LocalizationManager.Instance == null)
        {
            // LocalizationManagerが存在しない場合は英語をデフォルトとする
            newTitleText = newTitleText_English;
            if (debugMode) DebugLogger.LogWarning("TitleTextChangerForHim: LocalizationManagerが見つかりません。英語をデフォルトとして使用します。");
            return;
        }

        // 現在の言語コードを取得
        string currentLanguageCode = LocalizationManager.Instance.GetCurrentLanguageCode();

        // 言語コードに応じてテキストを設定
        if (currentLanguageCode == "en")
        {
            newTitleText = newTitleText_English;
            if (debugMode) DebugLogger.Log("TitleTextChangerForHim: 英語テキストを適用");
        }
        else
        {
            newTitleText = newTitleText_Japanese;
            if (debugMode) DebugLogger.Log("TitleTextChangerForHim: 日本語テキストを適用");
        }
    }

    /// <summary>
    /// Localize String Eventコンポーネントを無効化
    /// </summary>
    private void DisableLocalizeStringEvent()
    {
        if (titleText != null)
        {
            LocalizeStringEvent localizeEvent = titleText.GetComponent<LocalizeStringEvent>();
            if (localizeEvent != null)
            {
                localizeEvent.enabled = false;
                if (debugMode) DebugLogger.Log("TitleTextChangerForHim: LocalizeStringEventを無効化しました");
            }
        }
    }

    private bool ShouldExecuteTitleChange()
    {
        // MonologueSceneからの遷移フラグがtrueの場合は実行しない
        if (IsFromMonologueScene())
        {
            if (debugMode) DebugLogger.Log("TitleTextChangerForHim: MonologueSceneからの遷移のため、処理をスキップします");
            return false;
        }

        // afterChangeToLastフラグがtrueの場合は実行しない
        GameSaveManager saveManager = GameSaveManager.Instance;
        if (saveManager != null && saveManager.GetAfterChangeToLastFlag())
        {
            if (debugMode) DebugLogger.Log("TitleTextChangerForHim: afterChangeToLastがtrueのため、処理をスキップします");
            return false;
        }

        if (debugMode && forceExecute)
        {
            DebugLogger.Log("TitleTextChangerForHim: 強制実行モードでタイトル変更を実行");
            return true;
        }

        if (titleChangedToHisFuture)
        {
            if (debugMode) DebugLogger.Log("TitleTextChangerForHim: 既にタイトル変更済みです");
            return false;
        }

        if (shouldExecuteOnNextLoad)
        {
            shouldExecuteOnNextLoad = false;
            if (debugMode) DebugLogger.Log("TitleTextChangerForHim: shouldExecuteOnNextLoadフラグを検出");
            return true;
        }

        // HerMainSceneクリアした場合のフラグチェック
        GameSaveManager saveManager2 = GameSaveManager.Instance;
        if (saveManager2 != null && saveManager2.GetAfterChangeToHisFutureFlag())
        {
            if (debugMode) DebugLogger.Log("TitleTextChangerForHim: AfterChangeToHisFutureFlagフラグを検出");
            return true;
        }

        return false;
    }

    private bool IsFromMonologueScene()
    {
        // 簡易的なシーン遷移検出
        // MonologueDisplayManagerの完了フラグまたは特定のフラグで判定
        GameSaveManager saveManager = GameSaveManager.Instance;
        if (saveManager != null)
        {
            return saveManager.GetAllDialoguesCompletedFlag();
        }
        return false;
    }

    public static void SetTransitionFlag()
    {
        shouldExecuteOnNextLoad = true;
    }

    private IEnumerator StartTitleChange()
    {
        // Localize String Eventコンポーネントを無効化
        DisableLocalizeStringEvent();

        // 変更開始時にボタンを無効化
        SetMenuButtonsInteractable(false);

        yield return new WaitForSeconds(startDelay);

        isChanging = true;
        currentText = titleText.text;

        for (int i = 0; i < newTitleText.Length && i < currentText.Length; i++)
        {
            if (useGlitchEffect)
            {
                yield return StartCoroutine(ChangeCharacterWithGlitch(i));
            }
            else
            {
                yield return StartCoroutine(ChangeCharacter(i));
            }

            // SoundEffectManagerを使用した効果音再生
            if (soundEnabled && SoundEffectManager.Instance != null)
            {
                SoundEffectManager.Instance.PlayTypeSound();
            }

            yield return new WaitForSeconds(changeInterval);
        }

        if (newTitleText.Length != currentText.Length)
        {
            titleText.text = newTitleText;
        }

        isChanging = false;
        titleChangedToHisFuture = true;

        // 変更完了フラグを設定
        if (setCompletionFlag)
        {
            GameSaveManager saveManager = GameSaveManager.Instance;
            if (saveManager != null)
            {
                saveManager.SetAfterChangeToHisFutureFlag(true);
                saveManager.SaveGame();
                if (debugMode) DebugLogger.Log("TitleTextChangerForHim: タイトル変更フラグを保存しました");
            }
        }

        // ボタンを再度有効化
        SetMenuButtonsInteractable(true);
    }

    private IEnumerator ChangeCharacter(int index)
    {
        if (index < newTitleText.Length)
        {
            char[] textArray = titleText.text.ToCharArray();
            if (index < textArray.Length)
            {
                textArray[index] = newTitleText[index];
                titleText.text = new string(textArray);
            }
        }
        yield return null;
    }

    private IEnumerator ChangeCharacterWithGlitch(int index)
    {
        if (index < newTitleText.Length && index < currentText.Length)
        {
            char targetChar = newTitleText[index];

            float elapsed = 0;
            while (elapsed < glitchDuration)
            {
                char[] textArray = titleText.text.ToCharArray();
                textArray[index] = glitchChars[Random.Range(0, glitchChars.Length)];
                titleText.text = new string(textArray);

                elapsed += Time.deltaTime;
                yield return null;
            }

            char[] finalArray = titleText.text.ToCharArray();
            finalArray[index] = targetChar;
            titleText.text = new string(finalArray);
        }
    }

    private void SetMenuButtonsInteractable(bool interactable)
    {
        if (!disableButtonsDuringChange) return;

        if (menuContainer == null)
        {
            menuContainer = GameObject.Find("MenuContainer");
        }

        if (menuContainer != null)
        {
            Button[] buttons = menuContainer.GetComponentsInChildren<Button>();
            foreach (Button button in buttons)
            {
                button.interactable = interactable;
            }

            if (debugMode)
            {
                string state = interactable ? "有効" : "無効";
                DebugLogger.Log($"TitleTextChangerForHim: MenuContainerのボタンを{state}にしました");
            }
        }
    }

    public void SetSoundEnabled(bool enabled)
    {
        soundEnabled = enabled;
    }
}