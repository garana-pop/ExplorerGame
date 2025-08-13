using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// MonologueScene完了後、TitleSceneで「整理する」ボタンに変化した際の
/// OrganizeMainSceneへの遷移を管理するコンポーネント
/// </summary>
public class RememberButtonOrganizeTransition : MonoBehaviour
{
    [Header("ボタン参照")]
    [Tooltip("思い出す/整理するボタンへの参照")]
    [SerializeField] private Button targetButton;

    [Tooltip("ボタンのテキストコンポーネント")]
    [SerializeField] private TMP_Text buttonText;

    [Header("遷移設定")]
    [Tooltip("遷移先シーン名")]
    [SerializeField] private string targetSceneName = "OrganizeMainScene";

    [Tooltip("遷移前の待機時間")]
    [SerializeField] private float transitionDelay = 0f;

    [Header("フェード設定")]
    [Tooltip("フェードパネル")]
    [SerializeField] private Image fadePanel;

    [Tooltip("フェード時間")]
    [SerializeField] private float fadeDuration = 1.0f;

    [Header("音声設定")]
    [Tooltip("クリック時に特別な効果音を再生するか")]
    [SerializeField] private bool useSpecialSound = true;

    [Tooltip("特別な効果音クリップ")]
    [SerializeField] private AudioClip specialClickSound;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;

    // 内部状態
    private bool isTransitioning = false;
    private bool isOrganizeMode = false;

    private void Awake()
    {
        // ボタン参照の自動取得
        if (targetButton == null)
        {
            targetButton = GetComponent<Button>();
            if (targetButton == null && transform.parent != null)
            {
                // MenuContainer内の思い出すボタンを探す
                if (transform.parent.name == "MenuContainer")
                {
                    Transform buttonTransform = transform.parent.Find("思い出すボタン");
                    if (buttonTransform != null)
                    {
                        targetButton = buttonTransform.GetComponent<Button>();
                    }
                }
            }
        }

        // テキストコンポーネントの自動取得
        if (buttonText == null && targetButton != null)
        {
            buttonText = targetButton.GetComponentInChildren<TMP_Text>();
        }

        // フェードパネルの自動取得
        if (fadePanel == null)
        {
            GameObject fadePanelObj = GameObject.Find("FadePanel");
            if (fadePanelObj != null)
            {
                fadePanel = fadePanelObj.GetComponent<Image>();
            }
        }
    }

    private void OnEnable()
    {
        // シーンがアクティブになった時に状態を再チェック
        //CheckButtonState();
        // イベント購読
        RememberButtonTextChangerForHer.OnOrganizeMainSceneActivated += HandleOrganizeMainSceneActivated;

    }

    void OnDisable()
    {
        // イベント購読解除
        RememberButtonTextChangerForHer.OnOrganizeMainSceneActivated -= HandleOrganizeMainSceneActivated;
    }

    private void HandleOrganizeMainSceneActivated()
    {
        Debug.Log("RememberButtonOrganizeTransition：イベントを受信しました！ OrganizeMainScene がアクティブになりました。");

        // 既存の OnButtonClick を削除
        targetButton.onClick.RemoveListener(OnButtonClick);

        // 新しい MoveScene を追加
        targetButton.onClick.AddListener(MoveScene);
    }

    private void MoveScene()
    {
        Debug.Log("ボタンが押されたので OrganizeMainScene に移動します");
        UnityEngine.SceneManagement.SceneManager.LoadScene("OrganizeMainScene");
    }


    /// <summary>
    /// ボタンクリック時の処理
    /// </summary>
    private void OnButtonClick()
    {
        // 既に遷移中の場合は処理しない
        if (isTransitioning) return;

        // ボタンの状態を再チェック
        //CheckButtonState();

        if (isOrganizeMode)
        {
            // 整理モードの場合、OrganizeMainSceneへ遷移
            StartOrganizeSceneTransition();
        }
        else
        {
            // 通常モードの場合は何もしない（他のコンポーネントが処理）
            if (debugMode)
            {
                Debug.Log($"{nameof(RememberButtonOrganizeTransition)}: 通常モードのため処理をスキップ");
            }
        }
    }

    /// <summary>
    /// OrganizeMainSceneへの遷移を開始
    /// </summary>
    private void StartOrganizeSceneTransition()
    {
        if (isTransitioning) return;

        isTransitioning = true;


        // ボタンを無効化
        if (targetButton != null)
        {
            targetButton.interactable = false;
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(RememberButtonOrganizeTransition)}: OrganizeMainSceneへの遷移を開始");
        }

        // フェード処理を開始
        StartCoroutine(TransitionWithFade());
    }

    /// <summary>
    /// フェード付きシーン遷移
    /// </summary>
    private IEnumerator TransitionWithFade()
    {
        // 遷移前の待機
        yield return new WaitForSeconds(transitionDelay);

        // GameSaveManagerに遷移を通知
        GameSaveManager saveManager = GameSaveManager.Instance;
        if (saveManager != null)
        {
            // OrganizeSceneへの遷移フラグを設定（必要に応じて）
            saveManager.SaveGame();

            if (debugMode)
            {
                Debug.Log($"{nameof(RememberButtonOrganizeTransition)}: セーブデータを保存しました");
            }
        }

        // シーン遷移
        SceneManager.LoadScene(targetSceneName);
    }

    /// <summary>
    /// 外部から整理モードを強制設定
    /// </summary>
    public void SetOrganizeMode(bool enabled)
    {
        isOrganizeMode = enabled;

        if (debugMode)
        {
            Debug.Log($"{nameof(RememberButtonOrganizeTransition)}: 整理モードを{enabled}に設定");
        }
    }

    /// <summary>
    /// 現在整理モードかどうかを取得
    /// </summary>
    public bool IsOrganizeMode()
    {
        return isOrganizeMode;
    }
}