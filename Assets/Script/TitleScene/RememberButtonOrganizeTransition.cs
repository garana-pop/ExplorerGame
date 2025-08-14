using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 思い出すボタンをOrganizeMainSceneへの遷移ボタンに変更するクラス
/// </summary>
public class RememberButtonOrganizeTransition : MonoBehaviour
{
    [Header("ボタン参照")]
    [SerializeField] private Button targetButton; // 対象のボタン

    [Header("遷移設定")]
    [SerializeField] private string targetSceneName = "OrganizeMainScene"; // 遷移先シーン名

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false; // デバッグモード

    // フラグ管理
    private bool isOrganizeMode = false;
    private ConversationTransitionController conversationController; // 追加
    private bool isSetupCompleted = false;

    private void Awake()
    {
        // 自身のGameObjectからButtonコンポーネントを取得
        targetButton = GetComponent<Button>();

        if (targetButton == null)
        {
            Debug.LogError($"{nameof(RememberButtonOrganizeTransition)}: Buttonコンポーネントが見つかりません");
        }

        // ConversationTransitionControllerの取得（追加）
        conversationController = GetComponent<ConversationTransitionController>();
    }

    private void Start()
    {
        CheckAndActivateOrganizeMode();
    }

    private void OnEnable()
    {
        // 有効化時に自動的にセットアップ
        SetupOrganizeTransition();
    }

    /// <summary>
    /// OrganizeMainSceneモードをチェックして有効化
    /// </summary>
    private void CheckAndActivateOrganizeMode()
    {
        GameSaveManager saveManager = GameSaveManager.Instance;
        if (saveManager == null) return;

        // afterChangeToLastフラグがtrueの場合
        if (saveManager.GetAfterChangeToLastFlag())
        {
            HandleOrganizeMainSceneActivated();
        }
    }

    /// <summary>
    /// OrganizeMainSceneモードが有効化された時の処理
    /// </summary>
    private void HandleOrganizeMainSceneActivated()
    {
        if (targetButton == null) return;

        isOrganizeMode = true;

        // ConversationTransitionControllerを無効化（重要）
        if (conversationController != null)
        {
            conversationController.enabled = false;
            if (debugMode)
            {
                Debug.Log($"{nameof(RememberButtonOrganizeTransition)}: ConversationTransitionControllerを無効化しました");
            }
        }

        // 既存のリスナーを削除して新しいリスナーを追加
        targetButton.onClick.RemoveAllListeners();
        targetButton.onClick.AddListener(MoveScene);

        // 少し遅延させて確実に設定（追加）
        StartCoroutine(DelayedListenerSetup());

        if (debugMode)
        {
            Debug.Log($"{nameof(RememberButtonOrganizeTransition)}: OrganizeMainSceneへの遷移ボタンを設定しました");
        }
    }

    /// <summary>
    /// 遅延してリスナーを再設定（追加）
    /// </summary>
    private IEnumerator DelayedListenerSetup()
    {
        yield return null; // 1フレーム待機

        if (isOrganizeMode && targetButton != null)
        {
            // 再度リスナーを設定して確実に有効化
            targetButton.onClick.RemoveAllListeners();
            targetButton.onClick.AddListener(MoveScene);

            if (debugMode)
            {
                Debug.Log($"{nameof(RememberButtonOrganizeTransition)}: リスナーを再設定しました");
            }
        }
    }

    /// <summary>
    /// OrganizeMainSceneへ遷移
    /// </summary>
    private void MoveScene()
    {
        if (debugMode)
        {
            Debug.Log($"{nameof(RememberButtonOrganizeTransition)}: {targetSceneName}への遷移を開始します");
        }

        // 効果音再生
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlayClickSound();
        }

        // シーン遷移
        StartCoroutine(LoadSceneWithDelay());
    }

    /// <summary>
    /// 遅延してシーンをロード
    /// </summary>
    private IEnumerator LoadSceneWithDelay()
    {
        yield return new WaitForSeconds(0.1f);

        try
        {
            SceneManager.LoadScene(targetSceneName);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"{nameof(RememberButtonOrganizeTransition)}: シーン遷移エラー: {ex.Message}");
            targetButton.interactable = true;
        }
    }

    /// <summary>
    /// デバッグ用：強制的にOrganizeモードを有効化
    /// </summary>
    [ContextMenu("Force Enable Organize Mode")]
    private void ForceEnableOrganizeMode()
    {
        HandleOrganizeMainSceneActivated();
    }

    /// <summary>
    /// ボタンクリック時の処理
    /// </summary>
    private void OnButtonClicked()
    {
        if (debugMode)
        {
            Debug.Log($"{nameof(RememberButtonOrganizeTransition)}: ボタンがクリックされました");
        }

        // 効果音再生
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlayClickSound();
        }

        // シーン遷移
        TransitionToOrganizeScene();
    }

    /// <summary>
    /// OrganizeMainSceneへ遷移
    /// </summary>
    private void TransitionToOrganizeScene()
    {
        if (debugMode)
        {
            Debug.Log($"{nameof(RememberButtonOrganizeTransition)}: {targetSceneName}への遷移を開始");
        }

        // 二重遷移防止
        targetButton.interactable = false;

        StartCoroutine(LoadSceneWithDelay());
    }

    /// <summary>
    /// OrganizeMainSceneへの遷移を設定（外部から呼び出し可能）
    /// </summary>
    public void SetupOrganizeTransition()
    {
        if (targetButton == null || isSetupCompleted) return;

        // GameSaveManagerでフラグを確認
        GameSaveManager saveManager = GameSaveManager.Instance;
        if (saveManager == null) return;

        // afterChangeToLastフラグが設定されている場合のみ処理
        if (!saveManager.GetAfterChangeToLastFlag())
        {
            if (debugMode)
            {
                Debug.Log($"{nameof(RememberButtonOrganizeTransition)}: afterChangeToLastフラグが設定されていません");
            }
            return;
        }

        // リスナーをクリアして新規追加
        targetButton.onClick.RemoveAllListeners();
        targetButton.onClick.AddListener(OnButtonClicked);

        isSetupCompleted = true;

        if (debugMode)
        {
            Debug.Log($"{nameof(RememberButtonOrganizeTransition)}: OrganizeMainSceneへの遷移を設定しました");
        }
    }
}