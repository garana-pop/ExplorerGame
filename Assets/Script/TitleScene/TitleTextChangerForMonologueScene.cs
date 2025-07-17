using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// MonologueSceneから遷移してきた際にタイトルを"Thanks for playing the game."に変更するコンポーネント
/// タイトルテキストの表示管理はTitleTextLoaderForMonologueSceneが行う
/// </summary>
public class TitleTextChangerForMonologueScene : MonoBehaviour
{
    [Header("テキスト設定")]
    [Tooltip("変更対象のTextMeshProコンポーネント")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("変更後のテキスト")]
    [SerializeField] private string newTitleText = "Thanks for playing the game.";

    [Header("アニメーション設定")]
    [Tooltip("1文字変更にかかる時間（秒）")]
    [SerializeField] private float changeInterval = 0.2f;

    [Tooltip("変更開始までの遅延時間（秒）")]
    [SerializeField] private float startDelay = 1.0f;

    [Tooltip("文字変更時のエフェクト（フェード、グリッチなど）")]
    [SerializeField] private bool useGlitchEffect = true;

    [Tooltip("グリッチエフェクトの持続時間（秒）")]
    [SerializeField] private float glitchDuration = 0.1f;

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

    private string currentText;
    private bool isChanging = false;

    // 静的変数による状態管理
    private static bool shouldExecuteOnNextLoad = false;
    private static bool titleChangedToLast = false; // 完了フラグの代替
    private bool soundEnabled = true; // 効果音の有効/無効フラグ

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
        currentText = titleText.text;

        if (ShouldExecuteTitleChange())
        {
            if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: タイトル変更を開始します");
            // StartCoroutineの前に、もう一度soundEnabledの状態を確認
            if (debugMode) Debug.Log($"TitleTextChangerForMonologueScene: soundEnabled = {soundEnabled}");
            StartCoroutine(StartTitleChange());
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
            if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: MonologueSceneからの遷移を検出");
            return true;
        }

        // MonologueDisplayManagerのallDialoguesCompletedフラグをチェック
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

        // 最終的にテキストを完全に置き換え
        titleText.text = newTitleText;

        isChanging = false;

        //SoundEffectManagerを使用した完了音再生
        if (soundEnabled && SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlayCompletionSound();
        }

        if (setCompletionFlag)
        {
            titleChangedToLast = true;

            GameSaveManager saveManager = GameSaveManager.Instance;
            if (saveManager != null)
            {
                saveManager.SetAfterChangeToLastFlag(true);
                saveManager.SaveGame();
            }

            if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: 完了フラグを設定しました");
        }

        // 変更完了時にボタンを有効化
        SetMenuButtonsInteractable(true);

    }

    /// <summary>
    /// MenuContainer内のボタンの有効/無効を切り替える
    /// </summary>
    private void SetMenuButtonsInteractable(bool interactable)
    {
        if (!disableButtonsDuringChange) return;

        // MenuContainerの取得
        if (menuContainer == null)
        {
            menuContainer = GameObject.Find("MenuContainer");
        }

        if (menuContainer == null)
        {
            if (debugMode) Debug.LogWarning($"{GetType().Name}: MenuContainerが見つかりません");
            return;
        }

        // 全てのButtonコンポーネントを取得して制御
        Button[] buttons = menuContainer.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            button.interactable = interactable;
        }

        if (debugMode)
        {
            Debug.Log($"{GetType().Name}: MenuContainerのボタンを{(interactable ? "有効" : "無効")}化しました");
        }
    }

    private IEnumerator ChangeCharacter(int index)
    {
        char[] chars = titleText.text.ToCharArray();

        // 文字数が増える場合の対応
        if (index >= chars.Length)
        {
            string newText = titleText.text;
            while (newText.Length <= index)
            {
                newText += " ";
            }
            chars = newText.ToCharArray();
        }

        if (index < newTitleText.Length)
        {
            chars[index] = newTitleText[index];
        }
        else if (index < chars.Length)
        {
            chars[index] = ' '; // 余分な文字を空白に
        }

        titleText.text = new string(chars);
        yield return null;
    }

    private IEnumerator ChangeCharacterWithGlitch(int index)
    {
        char[] chars = titleText.text.ToCharArray();

        // 文字数が増える場合の対応
        if (index >= chars.Length)
        {
            string newText = titleText.text;
            while (newText.Length <= index)
            {
                newText += " ";
            }
            chars = newText.ToCharArray();
        }

        char targetChar = index < newTitleText.Length ? newTitleText[index] : ' ';

        float glitchTimer = 0f;

        while (glitchTimer < glitchDuration)
        {
            chars[index] = glitchChars[Random.Range(0, glitchChars.Length)];
            titleText.text = new string(chars);

            glitchTimer += Time.deltaTime;
            yield return null;
        }

        chars[index] = targetChar;
        titleText.text = new string(chars);
    }

    [ContextMenu("Execute Title Change")]
    public void ExecuteTitleChange()
    {
        if (!isChanging)
        {
            StartCoroutine(StartTitleChange());
        }
    }

    [ContextMenu("Reset Completion Flag")]
    public void ResetCompletionFlag()
    {
        titleChangedToLast = false;

        GameSaveManager saveManager = GameSaveManager.Instance;
        if (saveManager != null)
        {
            saveManager.SetAfterChangeToLastFlag(false);
            saveManager.SaveGame();
        }

        if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: 完了フラグをリセットしました");
    }

    /// <summary>
    /// 完了状態を取得
    /// </summary>
    public static bool IsTitleChanged()
    {
        return titleChangedToLast;
    }

    /// <summary>
    /// 効果音の有効/無効を設定
    /// </summary>
    /// <param name="enabled">有効にする場合はtrue</param>
    public void SetSoundEnabled(bool enabled)
    {
        soundEnabled = enabled;
        if (debugMode) Debug.Log($"TitleTextChangerForMonologueScene: 効果音を{(enabled ? "有効" : "無効")}にしました");
    }
}