using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// 似顔絵ファイルの削除機能を管理するクラス
/// 削除確認ダイアログの表示、削除処理、GameSaveManagerとの連携、シーン遷移を担当
/// </summary>
public class PortraitDeletionManager : MonoBehaviour
{
    [Header("UI参照")]
    [Tooltip("削除確認パネル（DeleteSelectionPanel）")]
    [SerializeField] private GameObject deletionConfirmationPanel;

    [Tooltip("削除確認メッセージテキスト")]
    [SerializeField] private TextMeshProUGUI confirmationMessageText;

    [Tooltip("デフォルトの確認メッセージ")]
    [SerializeField] private string defaultConfirmationMessage = "似顔絵を削除しますか？\n※この操作は取り消せません";

    [Header("ボタン参照")]
    [Tooltip("削除を実行する「はい」ボタン")]
    [SerializeField] private Button confirmButton;

    [Tooltip("削除をキャンセルする「いいえ」ボタン")]
    [SerializeField] private Button cancelButton;

    [Tooltip("削除確認パネルを表示するトリガーボタン（似顔絵画像など）")]
    [SerializeField] private Button deleteButton;

    [Header("削除対象")]
    [Tooltip("削除対象の似顔絵オブジェクト")]
    [SerializeField] private GameObject portraitObject;

    [Tooltip("削除対象のファイル名")]
    [SerializeField] private string portraitFileName = "似顔絵.png";

    [Header("エフェクト設定")]
    [Tooltip("削除時のフェード演出時間")]
    [SerializeField] private float fadeDuration = 1.0f;

    [Tooltip("削除エフェクト用のCanvasGroup（オプション）")]
    [SerializeField] private CanvasGroup portraitCanvasGroup;

    [Header("サウンド設定")]
    [Tooltip("削除実行時の効果音")]
    [SerializeField] private AudioClip deletionSound;

    [Tooltip("キャンセル時の効果音")]
    [SerializeField] private AudioClip cancelSound;

    [Header("シーン遷移設定")]
    [Tooltip("削除後に遷移するシーン名")]
    [SerializeField] private string nextSceneName = "TitleScene";

    [Tooltip("削除後、シーン遷移までの待機時間")]
    [SerializeField] private float sceneTransitionDelay = 2.0f;

    [Header("オーバーレイ設定")]
    [Tooltip("処理中に表示するオーバーレイ（オプション）")]
    [SerializeField] private GameObject processingOverlay;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;

    // 内部状態
    private bool isProcessingDeletion = false;
    private AudioSource audioSource;
    private GameSaveManager saveManager;


    private void Awake()
    {
        // AudioSourceの取得または作成
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // GameSaveManagerの参照取得
        saveManager = GameSaveManager.Instance;
        if (saveManager == null && debugMode)
        {
            Debug.LogWarning("PortraitDeletionManager: GameSaveManagerが見つかりません");
        }
    }

    private void Start()
    {
        // ボタンイベントの設定
        SetupButtonListeners();

        // 初期状態の設定
        if (deletionConfirmationPanel != null)
        {
            deletionConfirmationPanel.SetActive(false);
        }

        if (processingOverlay != null)
        {
            processingOverlay.SetActive(false);
        }

        // 確認メッセージの設定
        if (confirmationMessageText != null && string.IsNullOrEmpty(confirmationMessageText.text))
        {
            confirmationMessageText.text = defaultConfirmationMessage;
        }
    }

    /// <summary>
    /// ボタンリスナーの設定
    /// </summary>
    private void SetupButtonListeners()
    {
        // 削除ボタン（似顔絵をクリックなど）
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(ShowDeletionConfirmation);
        }

        // 確認ボタン
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmDeletion);
        }

        // キャンセルボタン
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelDeletion);
        }
    }

    /// <summary>
    /// 削除確認ダイアログを表示
    /// </summary>
    public void ShowDeletionConfirmation()
    {
        if (isProcessingDeletion)
        {
            if (debugMode)
                Debug.Log("PortraitDeletionManager: 削除処理中のため、確認ダイアログを表示できません");
            return;
        }

        if (deletionConfirmationPanel != null)
        {
            deletionConfirmationPanel.SetActive(true);

            // サウンド再生
            PlaySound(null); // デフォルトのクリック音

            if (debugMode)
                Debug.Log("PortraitDeletionManager: 削除確認ダイアログを表示しました");
        }
        else
        {
            Debug.LogError("PortraitDeletionManager: 削除確認パネルが設定されていません");
        }
    }

    /// <summary>
    /// 削除確認ダイアログを非表示
    /// </summary>
    private void HideDeletionConfirmation()
    {
        if (deletionConfirmationPanel != null)
        {
            deletionConfirmationPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 削除確認時の処理
    /// </summary>
    private void OnConfirmDeletion()
    {
        if (isProcessingDeletion) return;

        isProcessingDeletion = true;

        // 確認ダイアログを非表示
        HideDeletionConfirmation();

        // 削除音を再生
        PlaySound(deletionSound);

        // 削除処理を開始
        StartCoroutine(ProcessDeletion());

        if (debugMode)
            Debug.Log("PortraitDeletionManager: 削除処理を開始します");
    }

    /// <summary>
    /// 削除キャンセル時の処理
    /// </summary>
    private void OnCancelDeletion()
    {
        // キャンセル音を再生
        PlaySound(cancelSound);

        // 確認ダイアログを非表示
        HideDeletionConfirmation();

        if (debugMode)
            Debug.Log("PortraitDeletionManager: 削除をキャンセルしました");
    }

    /// <summary>
    /// 削除処理のコルーチン
    /// </summary>
    private IEnumerator ProcessDeletion()
    {
        // GameSaveManagerで削除フラグを設定
        if (saveManager != null)
        {
            // 似顔絵削除フラグを設定
            PlayerPrefs.SetInt("PortraitDeleted", 1);
            PlayerPrefs.Save();

            // AfterChangeToHisFutureフラグを設定
            saveManager.SetAfterChangeToHisFutureFlag(true);

            // 現在の状態を保存
            saveManager.SaveGame();

            if (debugMode)
                Debug.Log("PortraitDeletionManager: afterChangeToHisFutureフラグを設定し、セーブデータを更新しました");
        }

        // 少し待機
        yield return new WaitForSeconds(0.2f);

        // シーン遷移前の待機
        yield return new WaitForSeconds(sceneTransitionDelay);

        // 修正箇所: コルーチンを正しく呼び出す
        if (debugMode)
            Debug.Log($"PortraitDeletionManager: {nextSceneName}へ遷移を開始します");

        // TransitionToNextScene()がコルーチンの場合
        yield return StartCoroutine(TransitionToNextScene());
    }

    /// <summary>
    /// シーン遷移前にフラグを設定
    /// </summary>
    private void SetSceneTransitionFlags()
    {
        // GameSaveManagerに削除フラグを設定
        GameSaveManager saveManager = GameSaveManager.Instance;
        if (saveManager != null)
        {
            saveManager.SetPortraitDeleted(true);
            saveManager.SaveGame();
        }

        // TitleTextChangerForHimのフラグを設定
        TitleTextChangerForHim.SetTransitionFlag();

        // RememberButtonTextChangerForHerのフラグを設定（追加）
        RememberButtonTextChangerForHer.SetTransitionFlag();
    }

    /// <summary>
    /// 次のシーンへ遷移
    /// </summary>
    private IEnumerator TransitionToNextScene()
    {
        // デバッグログ追加
        if (debugMode)
            Debug.Log("PortraitDeletionManager: TransitionToNextScene開始");

        // 遅延時間を待つ
        yield return new WaitForSeconds(sceneTransitionDelay);

        // ここに追加：シーン遷移前にフラグを設定
        SetSceneTransitionFlags();

        // デバッグログ追加
        if (debugMode)
            Debug.Log($"PortraitDeletionManager: SceneManager.LoadScene({nextSceneName})を実行");

        // シーン遷移
        SceneManager.LoadScene(nextSceneName);
    }

    /// <summary>
    /// サウンドを再生
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else if (SoundEffectManager.Instance != null)
        {
            // デフォルトのクリック音を再生
            SoundEffectManager.Instance.PlayClickSound();
        }
    }

    /// <summary>
    /// 削除処理中かどうかを取得
    /// </summary>
    public bool IsProcessingDeletion()
    {
        return isProcessingDeletion;
    }

    /// <summary>
    /// 削除確認メッセージを動的に設定
    /// </summary>
    public void SetConfirmationMessage(string message)
    {
        if (confirmationMessageText != null)
        {
            confirmationMessageText.text = message;
        }
    }

    private void OnDestroy()
    {
        // ボタンリスナーのクリーンアップ
        if (deleteButton != null)
            deleteButton.onClick.RemoveAllListeners();

        if (confirmButton != null)
            confirmButton.onClick.RemoveAllListeners();

        if (cancelButton != null)
            cancelButton.onClick.RemoveAllListeners();
    }
}