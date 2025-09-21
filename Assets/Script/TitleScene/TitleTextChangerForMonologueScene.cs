using ExplorerGame.Localization;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

/// <summary>
/// MonologueSceneから遷移してきた際にタイトルを最終メッセージに変更するコンポーネント
/// </summary>
public class TitleTextChangerForMonologueScene : MonoBehaviour
{
    [Header("テキスト設定")]
    [Tooltip("変更対象のTextMeshProコンポーネント")]
    [SerializeField] private TMP_Text titleText;

    [Header("日本語テキスト設定")]
    [Tooltip("変更後のテキスト（日本語）")]
    [SerializeField] private string newTitleText_Japanese = "Thanks for playing the game.";

    [Header("英語テキスト設定")]
    [Tooltip("変更後のテキスト（英語）")]
    [SerializeField] private string newTitleText_English = "Thanks for playing the game.";

    [Header("アニメーション設定")]
    [Tooltip("1文字変更にかかる時間（秒）")]
    [SerializeField] private float changeInterval = 0.15f;

    [Tooltip("変更開始までの遅延時間（秒）")]
    [SerializeField] private float startDelay = 0.5f;

    [Tooltip("文字変更時のエフェクト（フェード、グリッチなど）")]
    [SerializeField] private bool useGlitchEffect = true;

    [Tooltip("グリッチエフェクトの持続時間（秒）")]
    [SerializeField] private float glitchDuration = 0.05f;

    [Header("ボタン制御設定")]
    [Tooltip("タイトル変更中にMenuContainerのボタンを無効化するか")]
    [SerializeField] private bool disableButtonsDuringChange = true;

    [Tooltip("MenuContainerへの参照（未設定の場合は自動検索）")]
    [SerializeField] private GameObject menuContainer;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceExecute = false;

    // ランタイムで使用する変数（言語設定に応じて切り替え）
    private string newTitleText;
    private string currentText;
    private bool isChanging = false;

    // 静的変数による状態管理
    private static bool shouldExecuteOnNextLoad = false;
    private bool soundEnabled = true;
    private static bool titleChangedToLast = false;

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
            if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: タイトル変更を開始します");
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
            if (saveManager != null && saveManager.GetAfterChangeToLastFlag())
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
            // LocalizationManagerが存在しない場合は日本語をデフォルトとする
            newTitleText = newTitleText_Japanese;
            if (debugMode) Debug.LogWarning("TitleTextChangerForMonologueScene: LocalizationManagerが見つかりません。日本語をデフォルトとして使用します。");
            return;
        }

        // 現在の言語コードを取得
        string currentLanguageCode = LocalizationManager.Instance.GetCurrentLanguageCode();

        // 言語コードに応じてテキストを設定
        if (currentLanguageCode == "en")
        {
            newTitleText = newTitleText_English;
            if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: 英語テキストを適用");
        }
        else
        {
            newTitleText = newTitleText_Japanese;
            if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: 日本語テキストを適用");
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
                if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: LocalizeStringEventを無効化しました");
            }
        }
    }

    private bool ShouldExecuteTitleChange()
    {
        if (debugMode && forceExecute)
        {
            Debug.Log("TitleTextChangerForMonologueScene: 強制実行モードでタイトル変更を実行");
            return true;
        }

        if (titleChangedToLast)
        {
            if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: 既にタイトル変更済みです");
            return false;
        }

        if (shouldExecuteOnNextLoad)
        {
            shouldExecuteOnNextLoad = false;
            if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: shouldExecuteOnNextLoadフラグを検出");
            return true;
        }

        // 全ダイアログ完了フラグをチェック
        GameSaveManager saveManager = GameSaveManager.Instance;
        if (saveManager != null && saveManager.GetAllDialoguesCompletedFlag())
        {
            if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: 全ダイアログ完了フラグを検出");
            return true;
        }

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

            // デバッグログを追加
            if (debugMode)
            {
                Debug.Log($"TitleTextChangerForMonologueScene: 効果音チェック - soundEnabled={soundEnabled}");
            }

            // SoundEffectManagerを使用した効果音再生
            if (soundEnabled && SoundEffectManager.Instance != null)
            {
                if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: 効果音を再生します");
                SoundEffectManager.Instance.PlayTypeSound();
            }

            yield return new WaitForSeconds(changeInterval);
        }

        // 最終的にテキストを完全に置き換える
        titleText.text = newTitleText;

        isChanging = false;

        titleChangedToLast = true;

        // 変更完了フラグを設定
        GameSaveManager saveManager = GameSaveManager.Instance;
        if (saveManager != null)
        {
            saveManager.SetAfterChangeToLastFlag(true);
            saveManager.SaveGame();
            if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: タイトル変更フラグを保存しました");
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
        else if (index < currentText.Length)
        {
            // 新しいテキストが短い場合、古いテキストの残りを削除
            titleText.text = newTitleText;
        }
        yield return null;
    }

    private IEnumerator ChangeCharacterWithGlitch(int index)
    {
        if (index < newTitleText.Length)
        {
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
        else if (index < currentText.Length)
        {
            // 新しいテキストが短い場合
            titleText.text = newTitleText;
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
                Debug.Log($"TitleTextChangerForMonologueScene: MenuContainerのボタンを{state}にしました");
            }
        }
    }

    public void SetSoundEnabled(bool enabled)
    {
        soundEnabled = enabled;
    }

    /// <summary>
    /// 完了状態を取得
    /// </summary>
    public static bool IsTitleChanged()
    {
        return titleChangedToLast;
    }
}