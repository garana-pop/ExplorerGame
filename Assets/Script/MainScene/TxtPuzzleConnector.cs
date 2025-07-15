using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TXTパズルとPNG画像表示をつなげるスクリプト
/// パズル完了時に直接オリジナル画像を表示します
/// </summary>
public class TxtPuzzleConnector : MonoBehaviour
{
    [Header("基本設定")]
    [Tooltip("TXTパズルの管理スクリプト")]
    [SerializeField] private TxtPuzzleManager txtPuzzleManager;

    [Header("画像設定")]
    [Tooltip("モザイク画像のコンテナ（非表示にするため）")]
    [SerializeField] private GameObject mosaicContainer;

    [Tooltip("完成時に表示する元の画像")]
    [SerializeField] private GameObject originalImage;

    [Tooltip("パズル完了で解放されるフォルダー")]
    [SerializeField] private GameObject nextFolderOrFile;

    [Header("効果音")]
    [Tooltip("画像表示時の効果音")]
    [SerializeField] private AudioClip completionSound;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;

    // パズル完了状態
    [SerializeField] private bool isPuzzleSolved = false;

    // プライベート変数
    private AudioSource audioSource;

    private void Awake()
    {
        // オーディオソースの取得・初期化
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        InitializeTxtPuzzleManager();
        InitializeImageState();

        if (debugMode)
        {
            LogDebug("初期化完了");
        }
    }

    private void OnEnable()
    {
        InitializeImageState();
    }

    /// <summary>
    /// TxtPuzzleManagerを初期化
    /// </summary>
    private void InitializeTxtPuzzleManager()
    {
        if (txtPuzzleManager == null)
        {
            txtPuzzleManager = FindFirstObjectByType<TxtPuzzleManager>();
            if (txtPuzzleManager == null)
            {
                LogDebug("TxtPuzzleManagerが見つかりません");
            }
        }
    }

    /// <summary>
    /// 画像表示状態の初期設定
    /// </summary>
    private void InitializeImageState()
    {
        // パズルがすでに解かれている場合はオリジナル画像を表示
        if (isPuzzleSolved)
        {
            ShowCompletedImage();
            return;
        }

        // モザイクコンテナを表示、オリジナル画像を非表示
        if (mosaicContainer != null)
        {
            mosaicContainer.SetActive(true);
        }

        if (originalImage != null)
        {
            originalImage.SetActive(false);
        }

        // 次のフォルダーを非表示
        if (nextFolderOrFile != null)
        {
            nextFolderOrFile.SetActive(false);
        }
    }

    /// <summary>
    /// TXTパズルの完了時に呼ばれるメソッド
    /// </summary>
    public void OnTxtPuzzleSolved()
    {
        LogDebug("TXTパズル完了通知を受信");

        // 完了済みなら何もしない
        if (isPuzzleSolved)
        {
            LogDebug("すでに完了しているため処理をスキップします");
            return;
        }

        // モザイク処理をTxtPuzzleManagerに任せるか確認
        if (txtPuzzleManager != null && txtPuzzleManager.GetComponent<TxtPuzzleManager>() != null)
        {
            LogDebug($"TxtPuzzleManagerにモザイク処理を任せます: {txtPuzzleManager.name}");
            // ここでは画像表示のみを行い、モザイク非表示はTxtPuzzleManagerに任せる
        }
        else
        {
            // TxtPuzzleManagerが見つからない場合は自分で処理
            LogDebug("TxtPuzzleManagerが見つからないため、このコンポーネントでモザイク処理を行います");
        }

        // 直接完了状態に移行
        CompleteRevealing();
    }

    /// <summary>
    /// 完成画像を表示する
    /// </summary>
    private void CompleteRevealing()
    {
        // モザイクコンテナを非表示
        if (mosaicContainer != null)
        {
            mosaicContainer.SetActive(false);
        }

        // オリジナル画像を表示
        if (originalImage != null)
        {
            originalImage.SetActive(true);
            LogDebug("オリジナル画像を表示しました");
        }

        // 次のフォルダーを解放
        if (nextFolderOrFile != null)
        {
            nextFolderOrFile.SetActive(true);

            // FolderButtonScriptとFolderActivationGuardの設定
            FolderButtonScript folderScript = nextFolderOrFile.GetComponent<FolderButtonScript>();
            if (folderScript != null)
            {
                folderScript.SetActivatedState(true);
                folderScript.SetVisible(true);
                LogDebug($"次のフォルダー {folderScript.GetFolderName()} を解放しました");
            }

            FolderActivationGuard guard = nextFolderOrFile.GetComponent<FolderActivationGuard>();
            if (guard != null)
            {
                guard.SetActivated(true);
            }
        }

        // 完了効果音
        PlaySound(completionSound);

        // パズル完了フラグを設定
        isPuzzleSolved = true;

        // 完了状態を保存
        SaveCompletionState();
    }

    /// <summary>
    /// 完了状態を表示する（再表示用）
    /// </summary>
    public void ShowCompletedImage()
    {
        // モザイクコンテナを非表示
        if (mosaicContainer != null)
        {
            mosaicContainer.SetActive(false);
        }

        // 完成画像を表示
        if (originalImage != null)
        {
            originalImage.SetActive(true);
        }

        // 次のフォルダーを表示
        if (nextFolderOrFile != null)
        {
            nextFolderOrFile.SetActive(true);
        }
    }

    /// <summary>
    /// 完了状態を保存
    /// </summary>
    private void SaveCompletionState()
    {
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.SaveGame();
            LogDebug("ゲーム状態を保存しました");
        }
    }

    /// <summary>
    /// 効果音再生
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        // SoundEffectManagerを優先
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlayCompletionSound();
            return;
        }

        // 直接AudioSourceで再生
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// デバッグログ出力
    /// </summary>
    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[TxtPuzzleConnector] {message}");
        }
    }
}