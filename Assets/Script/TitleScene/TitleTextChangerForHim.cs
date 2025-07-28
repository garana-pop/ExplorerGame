using System.Collections;
using UnityEngine;
using TMPro;
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

    [Tooltip("変更後のテキスト")]
    [SerializeField] private string newTitleText = "「彼」の未来";

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

    private string currentText;
    private bool isChanging = false;

    // 静的変数による状態管理
    private static bool shouldExecuteOnNextLoad = false;
    private static bool titleChangedToHisFuture = false; // 完了フラグの代替
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
        currentText = titleText.text;

        if (ShouldExecuteTitleChange())
        {
            if (debugMode) Debug.Log("TitleTextChangerForHim: タイトル変更を開始します");
            StartCoroutine(StartTitleChange());
        }
    }

    private bool ShouldExecuteTitleChange()
    {
        // MonologueSceneからの遷移フラグがtrueの場合は実行しない（新規追加）
        if (IsFromMonologueScene())
        {
            if (debugMode) Debug.Log("TitleTextChangerForHim: MonologueSceneからの遷移のため、処理をスキップします");
            return false;
        }

        // afterChangeToLastフラグがtrueの場合は実行しない（追加）
        GameSaveManager saveManager = GameSaveManager.Instance;
        if (saveManager != null && saveManager.GetAfterChangeToLastFlag())
        {
            if (debugMode) Debug.Log("TitleTextChangerForHim: afterChangeToLastがtrueのため、処理をスキップします");
            return false;
        }

        if (debugMode && forceExecute)
        {
            Debug.Log("TitleTextChangerForHim: 強制実行モードでタイトル変更を実行");
            return true;
        }

        if (titleChangedToHisFuture)
        {
            if (debugMode) Debug.Log("TitleTextChangerForHim: 既にタイトル変更済みです");
            return false;
        }

        if (shouldExecuteOnNextLoad)
        {
            shouldExecuteOnNextLoad = false;
            if (debugMode) Debug.Log("TitleTextChangerForHim: HerMainSceneからの遷移を検出");
            return true;
        }

        // HerMainSceneで特定の状態をクリアした場合のフラグチェック
        GameSaveManager saveManager2 = GameSaveManager.Instance;
        if (saveManager2 != null && saveManager2.GetAfterChangeToHisFutureFlag())
        {
            if (debugMode) Debug.Log("TitleTextChangerForHim: AfterChangeToHisFutureFlagフラグを検出");
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

        // SoundEffectManagerを使用した完了音再生
        if (soundEnabled && SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlayCompletionSound();
        }

        if (setCompletionFlag)
        {
            titleChangedToHisFuture = true;

            GameSaveManager saveManager = GameSaveManager.Instance;
            if (saveManager != null)
            {
                saveManager.SetAfterChangeToHisFutureFlag(true);
                saveManager.SaveGame();
            }

            if (debugMode) Debug.Log("TitleTextChangerForHim: 完了フラグを設定しました");
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
        if (index < newTitleText.Length)
        {
            chars[index] = newTitleText[index];
            titleText.text = new string(chars);
        }
        yield return null;
    }

    private IEnumerator ChangeCharacterWithGlitch(int index)
    {
        char[] chars = titleText.text.ToCharArray();
        char targetChar = index < newTitleText.Length ? newTitleText[index] : chars[index];

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
        titleChangedToHisFuture = false;

        GameSaveManager saveManager = GameSaveManager.Instance;
        if (saveManager != null)
        {
            saveManager.SetAfterChangeToHisFutureFlag(false);
            saveManager.SaveGame();
        }

        if (debugMode) Debug.Log("TitleTextChangerForHim: 完了フラグをリセットしました");
    }

    /// <summary>
    /// 完了状態を取得（TitleTextLoaderForHimから参照可能）
    /// </summary>
    public static bool IsTitleChanged()
    {
        return titleChangedToHisFuture;
    }

    /// <summary>
    /// 効果音の有効/無効を設定
    /// </summary>
    /// <param name="enabled">有効にする場合はtrue</param>
    public void SetSoundEnabled(bool enabled)
    {
        soundEnabled = enabled;
    }
}