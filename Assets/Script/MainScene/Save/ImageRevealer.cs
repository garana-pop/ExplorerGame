using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PNGファイルの表示を制御するスクリプト
/// TXTパズル完了時に元の画像を表示します
/// </summary>
public class ImageRevealer : MonoBehaviour
{
    [Header("基本設定")]
    [Tooltip("モザイク画像のコンテナ")]
    [SerializeField] private GameObject mosaicContainer;

    [Tooltip("パズル完了時に表示する元の画像")]
    [SerializeField] private GameObject originalImage;

    [Tooltip("次に解放されるフォルダー/ファイル")]
    [SerializeField] private GameObject nextFolderOrFile;

    [Header("TXTパズル連携設定")]
    [Tooltip("関連するTXTパズルマネージャー（インスペクターで設定）")]
    [SerializeField] private TxtPuzzleManager linkedTxtPuzzleManager;

    [Header("セーブ用設定")]
    [Tooltip("このPNGファイルの識別名（必ず設定してください）")]
    [SerializeField] private string fileName = "image.png";

    [Header("進捗表示")]
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("デバッグ")]
    [SerializeField] private bool debugMode = false;

    // 画像表示状態
    private bool isRevealed = false;

    // モザイクが永続的に非アクティブになったかのフラグ
    private bool mosaicPermanentlyDisabled = false;

    // パズル完了チェック済みフラグ
    private bool hasCheckedPuzzleCompletion = false;

    private AudioSource audioSource;

    // 確実に一度だけパズル完了をチェックするための処理カウンター
    private int initializationAttempts = 0;
    private const int MAX_INITIALIZATION_ATTEMPTS = 3;

    private void Awake()
    {
        // オーディオソースの取得
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        // 初期状態を確実に設定
        InitializeState();

        // 初回のパズル完了チェックを開始
        if (!hasCheckedPuzzleCompletion)
        {
            // 遅延チェックの開始
            StartCoroutine(DelayedCompletionCheck());
        }
    }

    private void OnEnable()
    {
        // 画面表示時に状態を更新
        UpdateVisualState();

        // 初期化処理をリトライする仕組み
        if (initializationAttempts < MAX_INITIALIZATION_ATTEMPTS)
        {
            Invoke("DelayedStateCheck", 0.5f);
            initializationAttempts++;
        }
    }

    // 親オブジェクト変更時の処理を追加
    private void OnTransformParentChanged()
    {
        if (debugMode)
            Debug.Log($"ImageRevealer '{fileName}': 親オブジェクトが変更されました");

        // 親が変わった時も状態を再評価
        UpdateVisualState();

        // DraggingCanvasへ移動した場合の特別処理
        if (transform.parent != null && transform.parent.name.Contains("DraggingCanvas"))
        {
            if (debugMode)
                Debug.Log($"ImageRevealer '{fileName}': DraggingCanvasに移動しました");

            // パズル完了チェックを強制的に実行
            ForceCheckTxtPuzzleCompletion();
        }
    }

    // 遅延してパズル完了をチェックするコルーチン
    private System.Collections.IEnumerator DelayedCompletionCheck()
    {
        // 複数フレーム待機して確実にTXTパズルの状態を取得
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(0.2f);
            CheckTxtPuzzleCompletion();
        }

        hasCheckedPuzzleCompletion = true;
    }

    // 遅延して状態チェックを行う（Invokeから呼ばれる）
    private void DelayedStateCheck()
    {
        // パズル完了状態を再チェック
        CheckTxtPuzzleCompletion();

        // 視覚的な状態を更新
        UpdateVisualState();
    }

    /// <summary>
    /// 画像表示状態の初期化
    /// </summary>
    public void InitializeState()
    {
        // ゲームの開始時または再読み込み時に一度だけ呼ばれる
        if (isRevealed || mosaicPermanentlyDisabled)
        {
            // 永続的な表示状態の場合
            DisableMosaicPermanently();
        }
        else
        {
            // まだ表示されていない初期状態
            if (mosaicContainer != null)
                mosaicContainer.SetActive(true);

            if (originalImage != null)
                originalImage.SetActive(false);

            if (nextFolderOrFile != null)
                nextFolderOrFile.SetActive(false);
        }

        // 進捗表示の更新
        UpdateProgressDisplay();
    }

    /// <summary>
    /// 視覚的な状態を更新（内部状態に基づいて表示を調整）
    /// </summary>
    private void UpdateVisualState()
    {
        if (isRevealed || mosaicPermanentlyDisabled)
        {
            // 表示済み状態
            ShowRevealedImage();
        }
        else
        {
            // 初期状態
            if (mosaicContainer != null)
                mosaicContainer.SetActive(true);

            if (originalImage != null)
                originalImage.SetActive(false);
        }

        // 進捗表示の更新
        UpdateProgressDisplay();
    }

    /// <summary>
    /// モザイクを永続的に非表示にする
    /// </summary>
    private void DisableMosaicPermanently()
    {
        // モザイクを永続的に非表示に
        mosaicPermanentlyDisabled = true;

        // 視覚要素の更新
        if (mosaicContainer != null)
            mosaicContainer.SetActive(false);

        if (originalImage != null)
            originalImage.SetActive(true);
    }

    /// <summary>
    /// 完成画像を表示する
    /// </summary>
    public void RevealImage()
    {
        // すでに表示済みなら何もしない
        if (isRevealed) return;

        // 状態フラグを設定
        isRevealed = true;
        mosaicPermanentlyDisabled = true;

        // モザイクを非表示
        if (mosaicContainer != null)
        {
            mosaicContainer.SetActive(false);

            if (debugMode)
                Debug.Log($"ImageRevealer '{fileName}': モザイクを永続的に非表示にしました");
        }

        // 完成画像を表示
        if (originalImage != null)
        {
            originalImage.SetActive(true);

            if (debugMode)
                Debug.Log($"ImageRevealer '{fileName}': オリジナル画像を表示しました");
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
            }

            FolderActivationGuard guard = nextFolderOrFile.GetComponent<FolderActivationGuard>();
            if (guard != null)
            {
                guard.SetActivated(true);
            }

            if (debugMode)
                Debug.Log($"ImageRevealer '{fileName}': 次のフォルダーを解放しました");
        }

        // 効果音再生
        SoundEffectManager.Instance?.PlayCompletionSound();

        // 進捗表示の更新
        UpdateProgressDisplay();

        // 完了時にゲーム状態を保存
        SaveGameState();
    }

    /// <summary>
    /// 表示済み画像を再表示する
    /// </summary>
    private void ShowRevealedImage()
    {
        // モザイクを非表示
        if (mosaicContainer != null)
            mosaicContainer.SetActive(false);

        // モザイクを永続的に非アクティブに設定
        mosaicPermanentlyDisabled = true;

        // 完成画像を表示
        if (originalImage != null)
            originalImage.SetActive(true);

        // 次のフォルダーを表示
        if (nextFolderOrFile != null)
            nextFolderOrFile.SetActive(true);

        // 進捗表示の更新
        UpdateProgressDisplay();
    }

    /// <summary>
    /// TXTパズルの完了状態を強制的にチェック
    /// </summary>
    public void ForceCheckTxtPuzzleCompletion()
    {
        if (debugMode)
            Debug.Log($"ImageRevealer '{fileName}': TXTパズル完了を強制チェックします");

        // TXTパズルが完了していれば強制的に表示
        if (linkedTxtPuzzleManager != null && linkedTxtPuzzleManager.IsPuzzleCompleted())
        {
            if (debugMode)
                Debug.Log($"ImageRevealer '{fileName}': TXTパズルが完了しています - 画像表示を強制します");

            RevealImage();
        }
        else if (debugMode)
        {
            // TXTパズルマネージャーの状態を詳細ログ
            if (linkedTxtPuzzleManager == null)
                Debug.Log($"ImageRevealer '{fileName}': リンクされたTXTパズルマネージャーがありません");
            else
                Debug.Log($"ImageRevealer '{fileName}': TXTパズルの完了状態: {linkedTxtPuzzleManager.IsPuzzleCompleted()}");
        }
    }

    /// <summary>
    /// TXTパズルの完了状態をチェックし、完了していれば画像を表示
    /// </summary>
    public void CheckTxtPuzzleCompletion()
    {
        // すでに表示済みなら何もしない
        if (isRevealed || mosaicPermanentlyDisabled) return;

        // TxtPuzzleManagerが設定されておらず、他の場所にあるかもしれない場合は検索
        if (linkedTxtPuzzleManager == null)
        {
            // 親を遡ってTxtPuzzleManagerを検索
            linkedTxtPuzzleManager = GetComponentInParent<TxtPuzzleManager>();

            // 親に見つからなければシーン内で検索
            if (linkedTxtPuzzleManager == null)
            {
                linkedTxtPuzzleManager = FindFirstObjectByType<TxtPuzzleManager>();

                if (linkedTxtPuzzleManager != null && debugMode)
                    Debug.Log($"ImageRevealer '{fileName}': シーン内でTxtPuzzleManagerを見つけました: {linkedTxtPuzzleManager.name}");
            }
        }

        // TxtPuzzleManagerが設定されており、パズルが完了している場合
        if (linkedTxtPuzzleManager != null && linkedTxtPuzzleManager.IsPuzzleCompleted())
        {
            if (debugMode)
                Debug.Log($"ImageRevealer '{fileName}': TXTパズルが完了しているため画像を表示します");

            // 画像を表示
            RevealImage();
        }
    }

    /// <summary>
    /// 進捗表示を更新
    /// </summary>
    private void UpdateProgressDisplay()
    {
        if (progressText != null)
        {
            if (isRevealed || mosaicPermanentlyDisabled)
            {
                progressText.text = "画像復元完了!";
            }
            else if (linkedTxtPuzzleManager != null)
            {
                progressText.text = "TXTパズルをクリアして画像を表示";
            }
            else
            {
                progressText.text = "関連パズルを完了させてください";
            }
        }
    }

    /// <summary>
    /// ゲーム状態を保存
    /// </summary>
    private void SaveGameState()
    {
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.SaveGame();

            if (debugMode)
                Debug.Log($"画像表示完了: {fileName} - ゲーム状態を保存しました");
        }
    }

    /// <summary>
    /// この画像のPNG進捗を取得
    /// </summary>
    public PngFileData GetImageProgress()
    {
        return new PngFileData
        {
            fileName = fileName,
            currentLevel = isRevealed ? 1 : 0,
            maxLevel = 1,
            isRevealed = isRevealed || mosaicPermanentlyDisabled // モザイク永続無効化フラグも反映
        };
    }

    /// <summary>
    /// 画像ファイル名を取得
    /// </summary>
    public string GetImageFileName() => fileName;

    /// <summary>
    /// 画像の進捗を適用
    /// </summary>
    public void ApplyImageProgress(PngFileData progressData)
    {
        if (progressData == null) return;

        // セーブデータから状態を復元
        isRevealed = progressData.isRevealed;

        // isRevealedがtrueならモザイク永続無効化も設定
        if (isRevealed)
            mosaicPermanentlyDisabled = true;

        if (isRevealed || mosaicPermanentlyDisabled)
        {
            ShowRevealedImage();

            if (debugMode)
                Debug.Log($"ImageRevealer '{fileName}': セーブデータから復元 - 画像表示状態");
        }
        else
        {
            InitializeState();

            // TXTパズルの状態も確認（セーブデータ適用後）
            Invoke("CheckTxtPuzzleCompletion", 0.2f);
        }
    }

    /// <summary>
    /// 画像が表示されているか取得
    /// </summary>
    public bool IsImageRevealed() => isRevealed || mosaicPermanentlyDisabled;
}