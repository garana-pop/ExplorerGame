using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ゴミ箱でのファイル削除管理を行うクラス
/// ファイルドロップ時の削除処理と状態管理を制御します
/// </summary>
public class TrashBoxDeletionManagement : MonoBehaviour, IDropHandler
{
    #region インスペクター設定

    [Header("削除アニメーション設定")]
    [Tooltip("ファイル削除時のフェードアウト時間（秒）")]
    [SerializeField] private float fileDeleteFadeTime = 0.5f;

    [Tooltip("ファイル削除時のスケールアニメーション")]
    [SerializeField] private bool useScaleAnimation = true;

    [Tooltip("削除アニメーション終了時のスケール")]
    [SerializeField] private float deleteAnimationEndScale = 0.3f;

    [Header("削除確認設定")]
    [Tooltip("全ファイル削除時の確認メッセージ")]
    [SerializeField] private string allFilesDeleteMessage = "すべてのファイルを完全に削除しますか？";

    [Header("ファイル管理設定")]
    [Tooltip("削除されたファイルを復元可能にするか")]
    [SerializeField] private bool enableFileRestore = false;

    [Tooltip("削除ファイルの最大保持数")]
    [SerializeField] private int maxDeletedFilesCount = 50;

    [Header("デバッグ設定")]
    [Tooltip("デバッグログを表示するか")]
    [SerializeField] private bool debugMode = false;

    #endregion

    #region プライベート変数

    // 削除管理
    private List<string> deletedFileNames; // 削除されたファイル名のリスト
    private List<GameObject> deletedFileObjects; // 削除されたファイルオブジェクトのリスト（復元用）
    private Dictionary<string, FileDeleteInfo> fileDeleteHistory; // ファイル削除履歴

    // 現在表示中のファイル数
    private int currentVisibleFileCount = 0;

    // アニメーション管理
    private List<Coroutine> activeDeleteAnimations;

    // 他のコンポーネント参照
    private TrashBoxSoundSetting soundSetting;
    private TrashBoxTips tips;
    private OrganizeMainSceneController sceneController;
    private FileManager fileManager;

    // 定数
    private const float MIN_FADE_TIME = 0.1f;
    private const float MAX_FADE_TIME = 3.0f;
    private const float MIN_SCALE = 0.0f;
    private const float MAX_SCALE = 2.0f;

    #endregion

    #region 内部クラス

    /// <summary>
    /// ファイル削除情報を格納するクラス
    /// </summary>
    [System.Serializable]
    private class FileDeleteInfo
    {
        public string fileName; // ファイル名
        public System.DateTime deleteTime; // 削除時刻
        public Vector3 originalPosition; // 元の位置
        public Transform originalParent; // 元の親
        public bool isImportantFile; // 重要ファイルかどうか

        public FileDeleteInfo(string name, Vector3 position, Transform parent, bool important)
        {
            fileName = name;
            deleteTime = System.DateTime.Now;
            originalPosition = position;
            originalParent = parent;
            isImportantFile = important;
        }
    }

    #endregion

    #region Unity ライフサイクル

    /// <summary>
    /// Awakeメソッド - 初期化処理
    /// </summary>
    private void Awake()
    {
        InitializeLists();
        ValidateSettings();
    }

    /// <summary>
    /// Startメソッド - シーン開始後の処理
    /// </summary>
    private void Start()
    {
        InitializeComponents();
        CountVisibleFiles();
    }

    #endregion

    #region 初期化処理

    /// <summary>
    /// リストの初期化
    /// </summary>
    private void InitializeLists()
    {
        deletedFileNames = new List<string>();
        deletedFileObjects = new List<GameObject>();
        fileDeleteHistory = new Dictionary<string, FileDeleteInfo>();
        activeDeleteAnimations = new List<Coroutine>();
    }

    /// <summary>
    /// 設定値の検証
    /// </summary>
    private void ValidateSettings()
    {
        fileDeleteFadeTime = Mathf.Clamp(fileDeleteFadeTime, MIN_FADE_TIME, MAX_FADE_TIME);
        deleteAnimationEndScale = Mathf.Clamp(deleteAnimationEndScale, MIN_SCALE, MAX_SCALE);
        maxDeletedFilesCount = Mathf.Max(1, maxDeletedFilesCount);
    }

    /// <summary>
    /// 他のコンポーネントの初期化
    /// </summary>
    private void InitializeComponents()
    {
        // 同じゲームオブジェクト上のコンポーネントを取得
        soundSetting = GetComponent<TrashBoxSoundSetting>();
        tips = GetComponent<TrashBoxTips>();

        // シーンコントローラーを取得
        sceneController = OrganizeMainSceneController.Instance;
        if (sceneController == null && debugMode)
        {
            Debug.LogWarning($"{nameof(TrashBoxDeletionManagement)}: OrganizeMainSceneControllerが見つかりません");
        }

        // FileManagerを取得
        fileManager = FindFirstObjectByType<FileManager>();
        if (fileManager == null && debugMode)
        {
            Debug.LogWarning($"{nameof(TrashBoxDeletionManagement)}: FileManagerが見つかりません");
        }
    }

    /// <summary>
    /// 現在表示中のファイル数をカウント
    /// </summary>
    private void CountVisibleFiles()
    {
        DraggableFile[] allFiles = FindObjectsByType<DraggableFile>(FindObjectsSortMode.None);
        currentVisibleFileCount = 0;

        foreach (DraggableFile file in allFiles)
        {
            if (file.gameObject.activeInHierarchy)
            {
                currentVisibleFileCount++;
            }
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDeletionManagement)}: 現在の表示ファイル数 - {currentVisibleFileCount}");
        }
    }

    #endregion

    #region ドロップ処理

    /// <summary>
    /// ファイルがドロップされた時の処理
    /// </summary>
    /// <param name="eventData">ポインタイベントデータ</param>
    public void OnDrop(PointerEventData eventData)
    {
        // ドロップされたオブジェクトを取得
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject == null)
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(TrashBoxDeletionManagement)}: ドロップオブジェクトがnullです");
            }
            return;
        }

        // DraggableFileコンポーネントを確認
        DraggableFile draggableFile = droppedObject.GetComponent<DraggableFile>();
        if (draggableFile == null)
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(TrashBoxDeletionManagement)}: ドロップされたオブジェクトはDraggableFileではありません");
            }
            return;
        }

        // ドロップ位置を取得してファイル削除処理を実行
        Vector2 dropPosition = Input.mousePosition;

        // DraggableFileの削除処理中フラグを設定
        draggableFile.SetDeleting(true);

        StartCoroutine(DeleteFileCoroutine(draggableFile, dropPosition));
    }

    #endregion

    #region ファイル削除処理

    /// <summary>
    /// ファイル削除コルーチン
    /// </summary>
    /// <param name="draggableFile">削除対象のファイル</param>
    /// <param name="dropPosition">ドロップされた位置</param>
    /// <returns>コルーチン</returns>
    private IEnumerator DeleteFileCoroutine(DraggableFile draggableFile, Vector2 dropPosition)
    {
        if (draggableFile == null) yield break;

        string fileName = draggableFile.name;
        bool isImportantFile = IsImportantFile(draggableFile);

        // GridLayoutGroupの影響を回避するためにファイルを別の親に移動
        GameObject fileObject = draggableFile.gameObject;
        Transform originalParent = fileObject.transform.parent;

        // DraggingCanvasまたは適切な親を探して移動
        Canvas draggingCanvas = GameObject.Find("DraggingCanvas")?.GetComponent<Canvas>();
        if (draggingCanvas == null)
        {
            // DraggingCanvasが見つからない場合は最上位のCanvasを使用
            draggingCanvas = fileObject.GetComponentInParent<Canvas>();
        }

        if (draggingCanvas != null)
        {
            // GridLayoutGroupの影響を受けないように親を変更
            fileObject.transform.SetParent(draggingCanvas.transform, true);

            // ドロップ位置にファイルを移動
            if (draggingCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // スクリーンスペースオーバーレイの場合
                fileObject.transform.position = dropPosition;
            }
            else if (draggingCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                // スクリーンスペースカメラの場合
                Vector3 worldPosition;
                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    draggingCanvas.transform as RectTransform,
                    dropPosition,
                    draggingCanvas.worldCamera,
                    out worldPosition);
                fileObject.transform.position = worldPosition;
            }
        }

        // 削除履歴に追加（元の位置情報を保存）
        FileDeleteInfo deleteInfo = new FileDeleteInfo(
            fileName,
            originalParent.position, // 元の親の位置を保存
            originalParent,
            isImportantFile
        );

        AddToDeleteHistory(fileName, deleteInfo);

        // 削除アニメーション実行（ドロップ位置から開始）
        yield return StartCoroutine(PlayDeleteAnimation(draggableFile));

        // ファイルオブジェクトを非表示/削除する前に削除フラグをリセット
        draggableFile.SetDeleting(false);

        // ファイルオブジェクトを非表示/削除
        ProcessFileRemoval(draggableFile);

        // 表示ファイル数を更新
        currentVisibleFileCount--;

        // 全ファイル削除チェック
        CheckAllFilesDeleted();

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDeletionManagement)}: ファイル削除完了 - {fileName}");
        }
    }

    /// <summary>
    /// ファイルが重要ファイルかどうかを判定
    /// </summary>
    /// <param name="draggableFile">チェック対象のファイル</param>
    /// <returns>重要ファイルの場合はtrue</returns>
    private bool IsImportantFile(DraggableFile draggableFile)
    {
        // ファイル名や拡張子から重要度を判定
        string fileName = draggableFile.name.ToLower();

        string[] importantKeywords = { "記録", "証拠", "警察", "報告書" };
        foreach (string keyword in importantKeywords)
        {
            if (fileName.Contains(keyword))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 削除履歴に追加
    /// </summary>
    /// <param name="fileName">ファイル名</param>
    /// <param name="deleteInfo">削除情報</param>
    private void AddToDeleteHistory(string fileName, FileDeleteInfo deleteInfo)
    {
        // 既存の履歴があれば更新、なければ追加
        if (fileDeleteHistory.ContainsKey(fileName))
        {
            fileDeleteHistory[fileName] = deleteInfo;
        }
        else
        {
            fileDeleteHistory.Add(fileName, deleteInfo);
        }

        // 削除ファイル名リストに追加
        if (!deletedFileNames.Contains(fileName))
        {
            deletedFileNames.Add(fileName);
        }

        // 最大保持数を超えた場合は古いものから削除
        if (deletedFileNames.Count > maxDeletedFilesCount)
        {
            string oldestFile = deletedFileNames[0];
            deletedFileNames.RemoveAt(0);
            fileDeleteHistory.Remove(oldestFile);
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDeletionManagement)}: 削除履歴に追加 - {fileName}");
        }
    }

    /// <summary>
    /// 指定時間後にエフェクトオブジェクトを破棄
    /// </summary>
    /// <param name="effectObject">エフェクトオブジェクト</param>
    /// <param name="delay">待機時間</param>
    /// <returns>コルーチン</returns>
    private IEnumerator DestroyEffectAfterTime(GameObject effectObject, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (effectObject != null)
        {
            Destroy(effectObject);
        }
    }

    /// <summary>
    /// 削除アニメーションを再生
    /// </summary>
    /// <param name="draggableFile">削除対象のファイル</param>
    /// <returns>コルーチン</returns>
    private IEnumerator PlayDeleteAnimation(DraggableFile draggableFile)
    {
        if (draggableFile == null) yield break;

        GameObject fileObject = draggableFile.gameObject;
        CanvasGroup canvasGroup = fileObject.GetComponent<CanvasGroup>();

        // CanvasGroupが無い場合は追加
        if (canvasGroup == null)
        {
            canvasGroup = fileObject.AddComponent<CanvasGroup>();
        }

        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        Vector3 startScale = fileObject.transform.localScale;
        Vector3 targetScale = startScale * deleteAnimationEndScale;

        while (elapsedTime < fileDeleteFadeTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fileDeleteFadeTime;

            // フェードアウト
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);

            // スケールアニメーション
            if (useScaleAnimation)
            {
                fileObject.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            }

            yield return null;
        }

        // 最終値を設定
        canvasGroup.alpha = 0f;
        if (useScaleAnimation)
        {
            fileObject.transform.localScale = targetScale;
        }
    }

    /// <summary>
    /// ファイルオブジェクトの削除処理
    /// </summary>
    /// <param name="draggableFile">削除対象のファイル</param>
    private void ProcessFileRemoval(DraggableFile draggableFile)
    {
        if (draggableFile == null) return;

        GameObject fileObject = draggableFile.gameObject;

        if (enableFileRestore)
        {
            // 復元可能にする場合は非表示にするだけ
            fileObject.SetActive(false);
            deletedFileObjects.Add(fileObject);
        }
        else
        {
            // 復元不可の場合は完全に削除
            Destroy(fileObject);
        }

        // FileManagerに削除を通知
        if (fileManager != null)
        {
            // TODO: fileManager.OnFileDeleted(draggableFile.name);
        }
    }

    /// <summary>
    /// 全ファイル削除チェック
    /// </summary>
    private void CheckAllFilesDeleted()
    {
        if (currentVisibleFileCount <= 0)
        {
            if (debugMode)
            {
                Debug.Log($"{nameof(TrashBoxDeletionManagement)}: 全ファイルが削除されました");
            }

            // 全ファイル削除確認ダイアログ表示
            StartCoroutine(ShowAllFilesDeletedConfirmation());
        }
    }

    /// <summary>
    /// 全ファイル削除確認ダイアログ表示
    /// </summary>
    /// <returns>コルーチン</returns>
    private IEnumerator ShowAllFilesDeletedConfirmation()
    {
        // メッセージ表示
        if (tips != null)
        {
            tips.ShowMessage(allFilesDeleteMessage);
        }

        // TODO: 確認ダイアログの実装
        // bool userConfirmed = yield return ShowConfirmationDialog(allFilesDeleteMessage);
        // if (userConfirmed)
        // {
        //     ExecuteCompleteFileDeletion();
        // }

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDeletionManagement)}: 全ファイル削除確認ダイアログ表示");
        }

        yield return null;
    }

    #endregion

    #region ファイル復元機能

    /// <summary>
    /// 削除されたファイルを復元
    /// </summary>
    /// <param name="fileName">復元するファイル名</param>
    /// <returns>復元成功時はtrue</returns>
    public bool RestoreFile(string fileName)
    {
        if (!enableFileRestore)
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(TrashBoxDeletionManagement)}: ファイル復元機能が無効です");
            }
            return false;
        }

        if (!fileDeleteHistory.ContainsKey(fileName))
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(TrashBoxDeletionManagement)}: 復元対象ファイルが見つかりません - {fileName}");
            }
            return false;
        }

        // 削除されたファイルオブジェクトを検索
        GameObject fileObject = deletedFileObjects.Find(obj => obj != null && obj.name == fileName);
        if (fileObject == null)
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(TrashBoxDeletionManagement)}: 復元対象オブジェクトが見つかりません - {fileName}");
            }
            return false;
        }

        try
        {
            // 削除情報を取得
            FileDeleteInfo deleteInfo = fileDeleteHistory[fileName];

            // ファイルオブジェクトを復元
            fileObject.SetActive(true);
            fileObject.transform.position = deleteInfo.originalPosition;
            fileObject.transform.SetParent(deleteInfo.originalParent);

            // CanvasGroupの設定をリセット
            CanvasGroup canvasGroup = fileObject.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            // スケールをリセット
            fileObject.transform.localScale = Vector3.one;

            // リストから削除
            deletedFileObjects.Remove(fileObject);
            deletedFileNames.Remove(fileName);
            fileDeleteHistory.Remove(fileName);

            // 表示ファイル数を更新
            currentVisibleFileCount++;

            if (debugMode)
            {
                Debug.Log($"{nameof(TrashBoxDeletionManagement)}: ファイル復元完了 - {fileName}");
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{nameof(TrashBoxDeletionManagement)}: ファイル復元エラー - {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 全ての削除ファイルを復元
    /// </summary>
    /// <returns>復元されたファイル数</returns>
    public int RestoreAllFiles()
    {
        if (!enableFileRestore)
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(TrashBoxDeletionManagement)}: ファイル復元機能が無効です");
            }
            return 0;
        }

        int restoredCount = 0;
        List<string> filesToRestore = new List<string>(deletedFileNames);

        foreach (string fileName in filesToRestore)
        {
            if (RestoreFile(fileName))
            {
                restoredCount++;
            }
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDeletionManagement)}: 全ファイル復元完了 - {restoredCount}個復元");
        }

        return restoredCount;
    }

    #endregion

    #region パブリックメソッド

    /// <summary>
    /// 削除されたファイル名のリストを取得
    /// </summary>
    /// <returns>削除ファイル名リスト</returns>
    public List<string> GetDeletedFileNames()
    {
        return new List<string>(deletedFileNames);
    }

    /// <summary>
    /// 削除ファイル数を取得
    /// </summary>
    /// <returns>削除されたファイル数</returns>
    public int GetDeletedFileCount()
    {
        return deletedFileNames.Count;
    }

    /// <summary>
    /// 現在表示中のファイル数を取得
    /// </summary>
    /// <returns>表示中のファイル数</returns>
    public int GetVisibleFileCount()
    {
        return currentVisibleFileCount;
    }

    /// <summary>
    /// 特定のファイルが削除されているかチェック
    /// </summary>
    /// <param name="fileName">チェックするファイル名</param>
    /// <returns>削除されている場合はtrue</returns>
    public bool IsFileDeleted(string fileName)
    {
        return deletedFileNames.Contains(fileName);
    }

    /// <summary>
    /// 削除履歴をクリア
    /// </summary>
    public void ClearDeleteHistory()
    {
        deletedFileNames.Clear();
        deletedFileObjects.Clear();
        fileDeleteHistory.Clear();

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDeletionManagement)}: 削除履歴をクリアしました");
        }
    }

    /// <summary>
    /// 削除アニメーション設定を更新
    /// </summary>
    /// <param name="newFadeTime">新しいフェード時間</param>
    /// <param name="newEndScale">新しい終了スケール</param>
    /// <param name="enableScale">スケールアニメーションを有効にするか</param>
    public void UpdateDeleteAnimationSettings(float newFadeTime, float newEndScale, bool enableScale)
    {
        fileDeleteFadeTime = Mathf.Clamp(newFadeTime, MIN_FADE_TIME, MAX_FADE_TIME);
        deleteAnimationEndScale = Mathf.Clamp(newEndScale, MIN_SCALE, MAX_SCALE);
        useScaleAnimation = enableScale;
    }

    /// <summary>
    /// ファイル復元機能の有効/無効を設定
    /// </summary>
    /// <param name="enable">復元機能を有効にするか</param>
    public void SetFileRestoreEnabled(bool enable)
    {
        enableFileRestore = enable;

        if (!enable)
        {
            // 復元機能を無効にする場合、既存の削除ファイルを完全削除
            foreach (GameObject fileObject in deletedFileObjects)
            {
                if (fileObject != null)
                {
                    Destroy(fileObject);
                }
            }
            ClearDeleteHistory();
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDeletionManagement)}: ファイル復元機能を{(enable ? "有効" : "無効")}にしました");
        }
    }

    /// <summary>
    /// 表示ファイル数を手動で更新
    /// </summary>
    public void RefreshVisibleFileCount()
    {
        CountVisibleFiles();
    }

    /// <summary>
    /// 完全なファイル削除を実行（復元不可）
    /// </summary>
    public void ExecuteCompleteFileDeletion()
    {
        // 全ての削除ファイルオブジェクトを破棄
        foreach (GameObject fileObject in deletedFileObjects)
        {
            if (fileObject != null)
            {
                Destroy(fileObject);
            }
        }

        // 削除履歴をクリア
        ClearDeleteHistory();

        // シーンコントローラーに完了を通知
        if (sceneController != null)
        {
            // TODO: sceneController.OnAllFilesCompletelyDeleted();
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDeletionManagement)}: 完全ファイル削除を実行しました");
        }
    }

    #endregion
}