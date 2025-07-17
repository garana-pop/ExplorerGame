using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// HerMainSceneから遷移してきた際に「思い出す」ボタンのテキストを「決意する」に変更するコンポーネント
/// テキストの表示管理はRememberButtonTextLoaderForHerが行う
/// </summary>
public class RememberButtonTextChangerForHer : MonoBehaviour
{
    [Header("テキスト設定")]
    [Tooltip("変更対象のTextMeshProコンポーネント")]
    [SerializeField] private TMP_Text buttonText;

    [Tooltip("変更後のテキスト")]
    [SerializeField] private string newButtonText = "決意する";

    [Header("ボタン制御設定")]
    [Tooltip("思い出すボタンのButton コンポーネント")]
    [SerializeField] private Button rememberButton;

    [Header("シーン遷移設定")]
    [Tooltip("遷移先のシーン名")]
    [SerializeField] private string targetSceneName = "MonologueScene";

    [Header("逆変換設定")]
    [Tooltip("逆変換時のターゲットテキスト")]
    [SerializeField] private string reverseTargetText = "思い出す";

    [Tooltip("シーン遷移時のフェード時間（秒）")]
    [SerializeField] private float transitionDelay = 0.5f;

    [Header("アニメーション設定")]
    [Tooltip("1文字変更にかかる時間（秒）")]
    [SerializeField] private float changeInterval = 0.25f;

    [Tooltip("変更開始までの遅延時間（秒）")]
    [SerializeField] private float startDelay = 0.8f;

    [Tooltip("文字変更時のエフェクト（フェード、グリッチなど）")]
    [SerializeField] private bool useGlitchEffect = true;

    [Tooltip("グリッチエフェクトの持続時間（秒）")]
    [SerializeField] private float glitchDuration = 0.08f;

    [Header("効果音設定")]
    [Tooltip("文字変更時の効果音")]
    [SerializeField] private AudioClip changeSound;

    [Tooltip("完了時の効果音")]
    [SerializeField] private AudioClip completeSound;

    [Tooltip("AudioSource（null の場合は自動取得）")]
    [SerializeField] private AudioSource audioSource;

    [Header("フラグ管理設定")]
    [Tooltip("テキスト変更後にフラグを設定するか")]
    [SerializeField] private bool setCompletionFlag = true;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceExecute = false;

    private string currentText;
    private bool isChanging = false;

    // 静的変数による状態管理
    private static bool shouldExecuteOnNextLoad = false;
    private static bool buttonTextChangedForHer = false; // 完了フラグ
    private static bool shouldExecuteReverseChangeOnNextLoad = false;
    private static bool isReverseMode = false; // 逆変換モードかどうかを管理

    public bool DataResetPanelControllerBoot = false; // DataResetPanelControllerクラスの起動管理フラグ

    private readonly string glitchChars = "!#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`{|}~";

    private void Awake()
    {

        if (buttonText == null)
        {
            // MenuContainerの思い出すボタンを探す
            GameObject menuContainer = GameObject.Find("MenuContainer");
            if (menuContainer != null)
            {
                Transform rememberButton = menuContainer.transform.Find("思い出すボタン");
                if (rememberButton != null)
                {
                    buttonText = rememberButton.GetComponentInChildren<TMP_Text>();
                }
            }
        }

        if (buttonText == null)
        {
            Debug.LogError("RememberButtonTextChangerForHer: 思い出すボタンのTextMeshProコンポーネントが見つかりません");
            enabled = false;
            return;
        }

        // StartTextChangeコルーチンの最後、完了フラグ設定後に追加
        if (rememberButton != null)
        {
            // RemoveAllListenersを使わず、自分のリスナーのみ管理
            rememberButton.onClick.RemoveListener(OnRememberButtonClicked);
            rememberButton.onClick.AddListener(OnRememberButtonClicked);

        }

        // AudioSource
        if (audioSource == null && (changeSound != null || completeSound != null))
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
    }

    private void Start()
    {
        currentText = buttonText.text;

        if (ShouldExecuteTextChange())
        {
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: ボタンテキスト変更を開始します");
            StartCoroutine(StartTextChange());
        }
    }

    private bool ShouldExecuteTextChange()
    {
        if (debugMode)
        {
            Debug.Log($"RememberButtonTextChangerForHer：ShouldExecuteTextChangeメソッドは呼ばれてる");
            Debug.Log($"RememberButtonTextChangerForHer: shouldExecuteOnNextLoadフラグ:{shouldExecuteOnNextLoad}");
            Debug.Log($"RememberButtonTextChangerForHer: shouldExecuteReverseChangeOnNextLoadフラグ:{shouldExecuteReverseChangeOnNextLoad}");
            Debug.Log($"RememberButtonTextChangerForHer: buttonTextChangedForHerフラグ:{buttonTextChangedForHer}");
            Debug.Log($"RememberButtonTextChangerForHer: isReverseModeフラグ:{isReverseMode}");
        }

        // デバッグモード強制実行
        if (debugMode && forceExecute)
        {
            Debug.Log("RememberButtonTextChangerForHer: 強制実行モードでテキスト変更を実行");
            return true;
        }

        // 逆変換の優先チェック（MonologueScene → TitleScene）
        if (shouldExecuteReverseChangeOnNextLoad)
        {
            shouldExecuteReverseChangeOnNextLoad = false;
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: 逆変換（MonologueScene → TitleScene）を実行");
            return true;
        }

        // 通常変換（HerMainScene → TitleScene）
        if (shouldExecuteOnNextLoad)
        {
            shouldExecuteOnNextLoad = false;
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: 通常変換（HerMainScene → TitleScene）を実行");
            return true;
        }

        // すでに変更済みの場合はスキップ
        if (buttonTextChangedForHer && !isReverseMode)
        {
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: すでにテキスト変更済みです");
            return false;
        }

        return false;
    }


    public static void SetTransitionFlag()
    {
        shouldExecuteOnNextLoad = true;
        isReverseMode = false; // 通常変換モードに設定
        Debug.Log("RememberButtonTextChangerForHer: 通常変換フラグを設定しました");
    }

    private IEnumerator StartTextChange()
    {
        yield return new WaitForSeconds(startDelay);

        isChanging = true;

        // 変換方向とターゲットテキストを決定
        string targetText = DetermineTargetText();

        // 現在のテキストとターゲットテキストの長さを調整
        int maxLength = Mathf.Max(currentText.Length, targetText.Length);

        for (int i = 0; i < maxLength; i++)
        {
            if (useGlitchEffect)
            {
                yield return StartCoroutine(ChangeCharacterWithGlitch(i, targetText));
            }
            else
            {
                yield return StartCoroutine(ChangeCharacter(i, targetText));
            }

            yield return new WaitForSeconds(changeInterval);
        }

        // 最終的にテキストを完全に置き換え
        buttonText.text = targetText;
        currentText = targetText;

        isChanging = false;

        // 完了フラグの設定（修正版）
        if (targetText == reverseTargetText)
        {
            // 逆変換の場合はフラグをリセット
            buttonTextChangedForHer = false;
            isReverseMode = false; // 逆変換モードを解除
            DataResetPanelControllerBoot = true; //
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: 逆変換により完了フラグとモードをリセットしました");
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: DataResetPanelControllerBootフラグ" + DataResetPanelControllerBoot + "に変更");
        }
        else if (setCompletionFlag && targetText == newButtonText)
        {
            // 通常変換の場合はフラグを設定
            buttonTextChangedForHer = true;
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: 完了フラグを設定しました");
        }

        if (debugMode) Debug.Log("RememberButtonTextChangerForHer: テキスト変更が完了しました");
    }

    private IEnumerator ChangeCharacter(int index, string targetText)
    {
        char[] chars = currentText.ToCharArray();

        // 配列のサイズを調整
        if (chars.Length < targetText.Length)
        {
            char[] newChars = new char[targetText.Length];
            for (int i = 0; i < chars.Length; i++)
            {
                newChars[i] = chars[i];
            }
            for (int i = chars.Length; i < targetText.Length; i++)
            {
                newChars[i] = ' ';
            }
            chars = newChars;
        }

        if (index < chars.Length && index < targetText.Length)
        {
            chars[index] = targetText[index];
            buttonText.text = new string(chars);
            currentText = new string(chars);
        }

        yield return null;
    }


    private IEnumerator ChangeCharacterWithGlitch(int index, string targetText)
    {
        char[] chars = currentText.ToCharArray();

        // 配列のサイズを調整
        if (chars.Length < targetText.Length)
        {
            char[] newChars = new char[targetText.Length];
            for (int i = 0; i < chars.Length; i++)
            {
                newChars[i] = chars[i];
            }
            for (int i = chars.Length; i < targetText.Length; i++)
            {
                newChars[i] = ' ';
            }
            chars = newChars;
        }

        char targetChar = index < targetText.Length ? targetText[index] : ' ';

        float glitchTimer = 0f;

        while (glitchTimer < glitchDuration)
        {
            if (index < chars.Length)
            {
                chars[index] = glitchChars[Random.Range(0, glitchChars.Length)];
                buttonText.text = new string(chars);
            }

            glitchTimer += Time.deltaTime;
            yield return null;
        }

        if (index < chars.Length)
        {
            chars[index] = targetChar;
            buttonText.text = new string(chars);
            currentText = new string(chars);
        }
    }


    [ContextMenu("Execute Text Change")]
    public void ExecuteTextChange()
    {
        if (!isChanging)
        {
            StartCoroutine(StartTextChange());
        }
    }

    [ContextMenu("Reset Completion Flag")]
    public void ResetCompletionFlag()
    {
        buttonTextChangedForHer = false;

        if (debugMode) Debug.Log("RememberButtonTextChangerForHer: 完了フラグをリセットしました");
    }

    /// <summary>
    /// 完了状態を取得（RememberButtonTextLoaderForHerから参照可能）
    /// </summary>
    public static bool IsTextChanged()
    {
        return buttonTextChangedForHer;
    }

    /// <summary>
    /// 思い出すボタンがクリックされた時の処理
    /// </summary>
    private void OnRememberButtonClicked()
    {
        // DataResetPanelControllerBootフラグチェックを最優先で実行
        if (DataResetPanelControllerBoot)
        {
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: DataResetPanelControllerBootフラグがtrueのため、シーン遷移をスキップします");
            return; // 早期リターンでシーン遷移処理を停止CheckAfterChangeToLastFlagAndProceed
        }

        // 遅延してafterChangeToLastフラグをチェック
        StartCoroutine(CheckAfterChangeToLastFlagAndProceed());
    }

    private IEnumerator CheckAfterChangeToLastFlagAndProceed()
    {
        // GameSaveManagerのロード完了を待つ
        yield return new WaitForSeconds(0.5f);

        // afterChangeToLastフラグをチェック
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.GetAfterChangeToLastFlag())
        {
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: afterChangeToLastフラグがtrueのため、シーン遷移をスキップします");
            yield break; // afterChangeToLastがtrueの場合はシーン遷移を停止
        }

        // フラグがfalseまたは取得できない場合は既存のシーン遷移処理を継続
        if (debugMode) Debug.Log($"RememberButtonTextChangerForHer: {targetSceneName}へ遷移します");
        StartCoroutine(TransitionToMonologue());
    }


    /// <summary>
    /// MonologueSceneへの遷移処理
    /// </summary>
    private IEnumerator TransitionToMonologue()
    {
        // ボタンを無効化（二重クリック防止）
        if (rememberButton != null)
        {
            rememberButton.interactable = false;
        }

        // コルーチン開始時にも再度フラグチェック
        if (DataResetPanelControllerBoot)
        {
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: TransitionToMonologue開始時にDataResetPanelControllerBootフラグがtrueのため、処理を停止します");
            yield break; // コルーチンを終了
        }

        // afterChangeToLastフラグの再チェック
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.GetAfterChangeToLastFlag())
        {
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: TransitionToMonologue開始時にafterChangeToLastフラグがtrueのため、処理を停止します");
            yield break; // コルーチンを終了
        }

        if (debugMode) Debug.Log("RememberButtonTextChangerForHer: シーン遷移処理を開始します");

        // 遷移遅延（必要に応じてフェード処理などを追加可能）
        yield return new WaitForSeconds(transitionDelay);

        // シーン遷移
        SceneManager.LoadScene(targetSceneName);
    }

    /// <summary>
    /// MonologueSceneからの逆変換フラグを設定
    /// </summary>
    public static void SetReverseTransitionFlag()
    {
        shouldExecuteReverseChangeOnNextLoad = true;
        isReverseMode = true; // 逆変換モードに設定
        Debug.Log("RememberButtonTextChangerForHer-SetReverseTransitionFlag():shouldExecuteReverseChangeOnNextLoadフラグ：" + shouldExecuteReverseChangeOnNextLoad);
        Debug.Log("RememberButtonTextChangerForHer: 逆変換モードに設定しました");
    }

    /// <summary>
    /// 変換方向に応じてターゲットテキストを決定
    /// </summary>
    private string DetermineTargetText()
    {
        if (debugMode)
        {
            Debug.Log($"RememberButtonTextChangerForHer: isReverseModeフラグ: {isReverseMode}");
        }

        // フラグベースで判定
        if (isReverseMode)
        {
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: 逆変換モード - ターゲット: " + reverseTargetText);
            return reverseTargetText; // "思い出す"
        }
        else
        {
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: 通常変換モード - ターゲット: " + newButtonText);
            return newButtonText; // "決意する"
        }
    }

}