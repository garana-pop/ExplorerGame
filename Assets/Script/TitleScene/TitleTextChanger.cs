using ExplorerGame.Localization;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>
/// タイトルテキストを「願い」の記憶から「彼女」の記憶に変更するコンポーネント
/// 変更開始のトリガーはSaveManager経由
/// タイトルテキストの表示管理はTitleTextLoaderが行う
/// </summary>
public class TitleTextChanger : MonoBehaviour
{
    [Header("テキスト設定")]
    [Tooltip("変更対象のTextMeshProコンポーネント")]
    [SerializeField] private TMP_Text titleText;

    [Header("日本語テキスト設定")]
    [Tooltip("元のテキスト（日本語）")]
    [SerializeField] private string originalTitleText_Japanese = "「願い」の記憶";

    [Tooltip("変更後のテキスト（日本語）")]
    [SerializeField] private string newTitleText_Japanese = "「彼女」の記憶";

    [Header("英語テキスト設定")]
    [Tooltip("元のテキスト（英語）")]
    [SerializeField] private string originalTitleText_English = "Memory of \"Wishes\"";

    [Tooltip("変更後のテキスト（英語）")]
    [SerializeField] private string newTitleText_English = "Memory of \"Her\"";

    [Header("アニメーション設定")]
    [Tooltip("1文字変更にかかる時間（秒）")]
    [SerializeField] private float changeInterval = 0.3f;

    [Tooltip("変更開始までの遅延時間（秒）")]
    [SerializeField] private float startDelay = 1.5f;

    [Tooltip("文字変更時のエフェクト（フェード、グリッチなど）")]
    [SerializeField] private bool useGlitchEffect = true;

    [Tooltip("グリッチエフェクトの持続時間（秒）")]
    [SerializeField] private float glitchDuration = 0.1f;

    [Header("ボタン制御設定")]
    [Tooltip("タイトル変更中にMenuContainerのボタンを無効化するか")]
    [SerializeField] private bool disableButtonsDuringChange = true;

    [Tooltip("MenuContainerへの参照（未設定の場合は自動検索）")]
    [SerializeField] private GameObject menuContainer;

    [Tooltip("遷移フラグを保持する時間（秒）")]
    [SerializeField] private float transitionFlagDuration = 1.0f;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceExecute = false;

    // ランタイムで使用する変数（言語設定に応じて切り替え）
    private string originalTitleText;
    private string newTitleText;
    private string currentText;
    private bool isChanging = false;
    private bool soundEnabled = true;
    private float transitionFlagTimer = 0f;

    // プロパティとして公開（TitleTextLoaderから参照）
    public string OriginalTitleText => originalTitleText;
    public string NewTitleText => newTitleText;

    // タイトルテキストが変更されたかどうかを示すプロパティ
    public bool HasChanged => isChanging;

    // 静的変数による状態管理
    private static bool shouldExecuteOnNextLoad = false;

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

        // 遷移フラグのチェック
        if (shouldExecuteOnNextLoad)
        {
            transitionFlagTimer = transitionFlagDuration;
            if (debugMode)
                DebugLogger.Log("TitleTextChanger: 遷移フラグが設定されています。");
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
            if (debugMode) DebugLogger.Log("TitleTextChanger: タイトル変更を開始します");
            StartCoroutine(StartTitleChange());
        }

        // 遷移フラグをチェック
        if (shouldExecuteOnNextLoad)
        {
            shouldExecuteOnNextLoad = false; // フラグをリセット

            if (debugMode)
                DebugLogger.Log("TitleTextChanger: DaughterRequestSceneからの遷移を検出。テキスト変更を開始します。");

            StartCoroutine(StartTitleChange());
        }
    }

    private void Update()
    {
        // 遷移フラグのタイマー処理
        if (transitionFlagTimer > 0)
        {
            transitionFlagTimer -= Time.deltaTime;
            if (transitionFlagTimer <= 0)
            {
                shouldExecuteOnNextLoad = false;
                if (debugMode)
                    DebugLogger.Log("TitleTextChanger: 遷移フラグがタイムアウトしました。");
            }
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

            // 適切なテキストを表示
            GameSaveManager saveManager = GameSaveManager.Instance;
            if (saveManager != null && saveManager.GetAfterChangeToHerMemoryFlag())
            {
                titleText.text = newTitleText;
            }
            else
            {
                titleText.text = originalTitleText;
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
            // LocalizationManagerが存在しない場合は日本語をデフォルトとする
            originalTitleText = originalTitleText_Japanese;
            newTitleText = newTitleText_Japanese;
            if (debugMode) Debug.LogWarning("TitleTextChanger: LocalizationManagerが見つかりません。日本語をデフォルトとして使用します。");
            return;
        }

        // 現在の言語コードを取得
        string currentLanguageCode = LocalizationManager.Instance.GetCurrentLanguageCode();

        // 言語コードに応じてテキストを設定
        if (currentLanguageCode == "en")
        {
            originalTitleText = originalTitleText_English;
            newTitleText = newTitleText_English;
            if (debugMode) DebugLogger.Log("TitleTextChanger: 英語テキストを適用");
        }
        else
        {
            originalTitleText = originalTitleText_Japanese;
            newTitleText = newTitleText_Japanese;
            if (debugMode) DebugLogger.Log("TitleTextChanger: 日本語テキストを適用");
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
                if (debugMode) DebugLogger.Log("TitleTextChanger: LocalizeStringEventを無効化しました");
            }
        }
    }

    private bool ShouldExecuteTitleChange()
    {
        if (debugMode && forceExecute)
        {
            DebugLogger.Log("TitleTextChanger: 強制実行モードでタイトル変更を実行");
            return true;
        }

        GameSaveManager saveManager = GameSaveManager.Instance;
        if (saveManager == null)
        {
            if (debugMode) Debug.LogWarning("TitleTextChanger: GameSaveManagerが見つかりません");
            return false;
        }

        // afterChangeToHerMemoryフラグをチェック（既に変更済みの場合）
        if (saveManager.GetAfterChangeToHerMemoryFlag())
        {
            if (debugMode) DebugLogger.Log("TitleTextChanger: 既にタイトル変更済みです");
            return false;
        }

        if (shouldExecuteOnNextLoad)
        {
            shouldExecuteOnNextLoad = false;
            if (debugMode) DebugLogger.Log("TitleTextChanger: shouldExecuteOnNextLoadフラグを検出");
            return true;
        }

        // OpeningSceneからの遷移判定（具体的な条件に応じて調整が必要）
        // ここではshouldExecuteOnNextLoadフラグで代用

        return false;
    }

    public static void SetTransitionFlag()
    {
        shouldExecuteOnNextLoad = true;
    }

    private IEnumerator StartTitleChange()
    {
        yield return new WaitForSeconds(startDelay);

        // Localize String Eventコンポーネントを無効化
        DisableLocalizeStringEvent();

        // 変更開始時にボタンを無効化
        SetMenuButtonsInteractable(false);

        isChanging = true;

        // 現在のテキストと新しいテキストの長さを調整
        int maxLength = Mathf.Max(currentText.Length, newTitleText.Length);

        for (int i = 0; i < maxLength; i++)
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

        // 最終的にテキストを完全に置き換える
        titleText.text = newTitleText;

        isChanging = false;

        // 変更完了フラグを設定
        GameSaveManager saveManager = GameSaveManager.Instance;
        if (saveManager != null)
        {
            saveManager.SetAfterChangeToHerMemoryFlag(true);
            if (debugMode) DebugLogger.Log("TitleTextChanger: タイトル変更フラグを保存しました");
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
            }
            else
            {
                System.Array.Resize(ref textArray, index + 1);
                textArray[index] = newTitleText[index];
            }
            titleText.text = new string(textArray);
        }
        yield return null;
    }

    private IEnumerator ChangeCharacterWithGlitch(int index)
    {
        if (index < newTitleText.Length)
        {
            char originalChar = index < currentText.Length ? currentText[index] : ' ';
            char targetChar = newTitleText[index];

            float elapsed = 0;
            while (elapsed < glitchDuration)
            {
                char[] textArray = titleText.text.ToCharArray();
                if (index < textArray.Length)
                {
                    textArray[index] = glitchChars[Random.Range(0, glitchChars.Length)];
                }
                else
                {
                    System.Array.Resize(ref textArray, index + 1);
                    textArray[index] = glitchChars[Random.Range(0, glitchChars.Length)];
                }
                titleText.text = new string(textArray);

                elapsed += Time.deltaTime;
                yield return null;
            }

            char[] finalArray = titleText.text.ToCharArray();
            if (index < finalArray.Length)
            {
                finalArray[index] = targetChar;
            }
            else
            {
                System.Array.Resize(ref finalArray, index + 1);
                finalArray[index] = targetChar;
            }
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
                DebugLogger.Log($"TitleTextChanger: MenuContainerのボタンを{state}にしました");
            }
        }
    }

    public void SetSoundEnabled(bool enabled)
    {
        soundEnabled = enabled;
    }

    /// <summary>
    /// 次回TitleScene読み込み時にテキスト変更を実行するフラグを設定
    /// </summary>
    public static void SetExecuteOnNextLoad()
    {
        shouldExecuteOnNextLoad = true;
        DebugLogger.Log("TitleTextChanger: 次回読み込み時の実行フラグを設定しました。");
    }

    /// <summary>
    /// テキストを元に戻す（デバッグ用）
    /// </summary>
    public void ResetText()
    {
        if (!isChanging)
        {
            titleText.text = originalTitleText;
            currentText = originalTitleText;
            isChanging = false;

            if (debugMode)
            {
                DebugLogger.Log("TitleTextChanger: テキストをリセットしました。");
            }
        }
    }
}