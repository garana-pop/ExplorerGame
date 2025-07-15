using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TextTyper完了後にクリックでシーン遷移を行うコンポーネント
/// </summary>
public class ClickToTransitionScene : MonoBehaviour, IPointerClickHandler
{
    [Header("シーン遷移設定")]
    [Tooltip("遷移先のシーン名")]
    [SerializeField] private string targetSceneName = "TitleScene";

    [Header("参照設定")]
    [Tooltip("監視するTextTyperコンポーネント（未設定の場合は自動検索）")]
    [SerializeField] private TextTyper textTyper;

    [Header("クリック待機UI設定")]
    [Tooltip("タイピング完了後に表示するクリック待機UI")]
    [SerializeField] private GameObject clickPromptUI;

    [Tooltip("クリック待機時に表示するテキスト")]
    [SerializeField] private string promptText = "画面をクリックして続行...";

    [Tooltip("プロンプトテキストを表示するTextMeshProコンポーネント")]
    [SerializeField] private TMP_Text promptTextComponent;

    [Header("効果音設定")]
    [Tooltip("クリック時に効果音を再生するか")]
    [SerializeField] private bool playClickSound = true;

    [Tooltip("カスタム効果音（未設定時はデフォルト音を使用）")]
    [SerializeField] private AudioClip customClickSound;

    [Header("遅延設定")]
    [Tooltip("タイピング完了からクリック受付開始までの遅延時間（秒）")]
    [SerializeField] private float clickDelayAfterTyping = 0.5f;

    [Header("デバッグ設定")]
    [Tooltip("デバッグログを表示するか")]
    [SerializeField] private bool debugMode = false;

    [Header("特殊遷移設定")]
    [Tooltip("DaughterRequestSceneからTitleSceneへの遷移時にTitleTextChangerを実行するか")]
    [SerializeField] private bool triggerTitleTextChange = true;

    // 内部状態
    private bool isTypingCompleted = false;
    private bool canClick = false;
    private AudioSource audioSource;

    private void Awake()
    {
        // TextTyperコンポーネントの自動検索
        if (textTyper == null)
        {
            textTyper = FindFirstObjectByType<TextTyper>();
            if (textTyper == null)
            {
                textTyper = GetComponent<TextTyper>();
            }
        }

        // AudioSourceコンポーネントの取得/追加
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (playClickSound || customClickSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // プロンプトUIの初期状態設定
        if (clickPromptUI != null)
        {
            clickPromptUI.SetActive(false);
        }

        // プロンプトテキストの設定
        if (promptTextComponent != null && !string.IsNullOrEmpty(promptText))
        {
            promptTextComponent.text = promptText;
        }
    }

    private void Start()
    {
        // TextTyperが見つからない場合の警告
        if (textTyper == null)
        {
            Debug.LogError("ClickToTransitionScene: TextTyperコンポーネントが見つかりません。インスペクターで設定するか、同じGameObjectにアタッチしてください。");
            enabled = false;
            return;
        }

        // 遷移先シーン名の検証
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("ClickToTransitionScene: 遷移先シーン名が設定されていません。");
        }

        // TextTyperの完了イベントに登録
        textTyper.OnTypingCompleted += OnTypingCompleted;

        if (debugMode)
        {
            Debug.Log("ClickToTransitionScene: 初期化完了。TextTyperの完了を待機中...");
        }
    }

    private void OnDestroy()
    {
        // イベントリスナーの解除
        if (textTyper != null)
        {
            textTyper.OnTypingCompleted -= OnTypingCompleted;
        }
    }

    /// <summary>
    /// TextTyper完了時に呼ばれるコールバック
    /// </summary>
    private void OnTypingCompleted()
    {
        if (debugMode)
        {
            Debug.Log("ClickToTransitionScene: タイピング完了を検知しました。");
        }

        isTypingCompleted = true;

        // 遅延後にクリック受付を開始
        StartCoroutine(EnableClickAfterDelay());
    }

    /// <summary>
    /// 遅延後にクリック受付を有効にするコルーチン
    /// </summary>
    private IEnumerator EnableClickAfterDelay()
    {
        if (clickDelayAfterTyping > 0)
        {
            yield return new WaitForSeconds(clickDelayAfterTyping);
        }

        canClick = true;

        // クリック待機UIを表示
        if (clickPromptUI != null)
        {
            clickPromptUI.SetActive(true);
        }

        if (debugMode)
        {
            Debug.Log("ClickToTransitionScene: クリック受付開始。画面クリックでシーン遷移します。");
        }
    }

    /// <summary>
    /// クリック検知処理
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!canClick || !isTypingCompleted)
        {
            if (debugMode)
            {
                Debug.Log("ClickToTransitionScene: まだクリック受付していません。");
            }
            return;
        }

        if (debugMode)
        {
            Debug.Log("ClickToTransitionScene: クリックを検知。シーン遷移を開始します。");
        }

        // 重複クリック防止
        canClick = false;

        // クリック音を再生
        if (playClickSound)
        {
            PlayClickSound();
        }

        // プロンプトUIを非表示
        if (clickPromptUI != null)
        {
            clickPromptUI.SetActive(false);
        }

        // シーン遷移を実行
        TransitionToScene();
    }

    /// <summary>
    /// クリック音を再生
    /// </summary>
    private void PlayClickSound()
    {
        // SoundEffectManagerを優先使用
        if (SoundEffectManager.Instance != null)
        {
            if (customClickSound != null)
            {
                SoundEffectManager.Instance.PlaySound(customClickSound);
            }
            else
            {
                SoundEffectManager.Instance.PlayClickSound();
            }
        }
        else if (customClickSound != null && audioSource != null)
        {
            // SoundEffectManagerがない場合は直接再生
            audioSource.PlayOneShot(customClickSound);
        }
    }

    /// <summary>
    /// シーン遷移を実行
    /// </summary>
    private void TransitionToScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("ClickToTransitionScene: 遷移先シーン名が設定されていません。");
            return;
        }

        // DaughterRequestSceneからTitleSceneへの遷移の場合のみ処理
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == "DaughterRequest" && targetSceneName == "TitleScene")
        {
            // 遷移フラグを設定
            TitleTextChanger.SetExecuteOnNextLoad();

            if (debugMode)
            {
                Debug.Log("ClickToTransitionScene: DaughterRequestからTitleSceneへの遷移フラグを設定しました。");
            }
        }

        Debug.Log(targetSceneName + "に移行します。");
        LoadSceneDirectly();
    }

    /// <summary>
    /// 直接シーン遷移
    /// </summary>
    private void LoadSceneDirectly()
    {
        try
        {
            // セーブデータの保存
            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.SaveGame();
            }

            // シーン遷移
            SceneManager.LoadScene(targetSceneName);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ClickToTransitionScene: シーン遷移中にエラーが発生しました: {ex.Message}");
        }
    }

    /// <summary>
    /// 外部からクリック受付状態を強制設定（デバッグ用）
    /// </summary>
    public void ForceEnableClick()
    {
        isTypingCompleted = true;
        canClick = true;

        if (clickPromptUI != null)
        {
            clickPromptUI.SetActive(true);
        }

        if (debugMode)
        {
            Debug.Log("ClickToTransitionScene: クリック受付を強制有効化しました。");
        }
    }

    /// <summary>
    /// 現在のクリック受付状態を取得
    /// </summary>
    public bool CanClick => canClick && isTypingCompleted;

    /// <summary>
    /// 遷移先シーン名を動的に変更
    /// </summary>
    public void SetTargetScene(string sceneName)
    {
        targetSceneName = sceneName;
        if (debugMode)
        {
            Debug.Log($"ClickToTransitionScene: 遷移先シーンを '{sceneName}' に変更しました。");
        }
    }
}