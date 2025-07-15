using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲームのセーブデータを管理するシングルトンクラス
/// </summary>
public class GameSaveManager : MonoBehaviour
{
    // シングルトンインスタンス
    private static GameSaveManager _instance;
    public static GameSaveManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Unity 6の新機能を使用 - 非アクティブを含むオブジェクト検索
                _instance = FindFirstObjectByType<GameSaveManager>(FindObjectsInactive.Include);

                if (_instance == null)
                {
                    // インスタンスが存在しない場合のみ新規作成
                    GameObject go = new GameObject("GameSaveManager");
                    _instance = go.AddComponent<GameSaveManager>();

                    // シーン間で保持する設定
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(go);
                    }
                }
            }
            return _instance;
        }
    }

    [Header("設定")]
    [SerializeField] private string saveFileName = "game_save.json";
    [SerializeField] private string gameVersion = "1.0";
    [SerializeField] private bool autoSaveOnQuit = true;
    [SerializeField] private bool autoLoadOnStart = true;
    [SerializeField] private bool debugMode = false;

    [Header("オーディオ参照")]
    [SerializeField] private AudioSource bgmAudioSource; // インスペクターで設定するためのBGM AudioSource

    [Header("フォルダー初期設定")]
    [SerializeField] private string[] initialFolders = { "思い出" }; // 初期表示フォルダー
    [SerializeField] private string defaultActiveFolder = "思い出"; // デフォルトのアクティブフォルダー

    [Header("遅延設定")]
    [SerializeField] private float folderToggleDelay = 0.1f; // フォルダー切り替え遅延（秒）

    [Header("タイトル変更（「彼女」の記憶）フラグ")]
    [Tooltip("タイトルが「彼女の記憶」に変更された後かどうか")]
    [SerializeField] private bool debugAfterChangeFlag = false;

    // セーブデータとマネージャー参照
    private GameSaveData currentSaveData;
    private TxtPuzzleManager txtPuzzleManager;
    private List<ImageRevealer> imageRevealers = new List<ImageRevealer>();
    private List<PdfDocumentManager> pdfManagers = new List<PdfDocumentManager>();
    private List<FolderButtonScript> folderScripts = new List<FolderButtonScript>();
    private bool initialLoadCompleted = false;
    private bool hasAfterChangeFlag = false; // メモリ上でのフラグ保持
    private bool hasAfterChangeFutureFlag = false; // メモリ上でのフラグ保持
    public bool portraitDeleted = false;
    public bool fromMonologueScene = false; // MonologueSceneから遷移したかのフラグ

    private string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);

    private void Awake()
    {
        // 既存のインスタンスとの重複チェックを改善
        if (_instance != null && _instance != this)
        {
            // 重複インスタンスを破棄
            if (Application.isPlaying)
            {
                // Destroyを使用する場合は即時破棄しない
                Destroy(gameObject);
            }
            else
            {
                // エディタモードでの即時破棄
                DestroyImmediate(gameObject);
            }
            return;
        }

        // このインスタンスを保持
        _instance = this;

        // シーン間で保持する設定（エディタモードでは適用しない）
        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }

        // 以降の初期化処理
        InitializeSaveData();
    }

    private void Start()
    {
        if (autoLoadOnStart) LoadGameAndApply();
    }

    /// <summary>
    /// AfterChangeToHisFutureフラグを設定（新規追加）
    /// </summary>
    public void SetAfterChangeToHisFutureFlag(bool value)
    {
        if (currentSaveData == null)
        {
            InitializeSaveData();
        }

        // フラグが変更された場合のみ処理
        if (currentSaveData.afterChangeToHisFuture != value)
        {
            // メモリ上のフラグを更新
            hasAfterChangeFutureFlag = value;
            currentSaveData.afterChangeToHisFuture = value;

            if (debugMode)
            {
                Debug.Log($"GameSaveManager: AfterChangeToHisFutureフラグを {value} に設定しました");
            }

            // 変更があった場合はセーブ
            SaveAfterChangeFutureFlag();
        }
    }

    /// <summary>
    /// AfterChangeToHisFutureフラグのみを含むセーブ（新規追加）
    /// </summary>
    private void SaveAfterChangeFutureFlag()
    {
        try
        {
            if (currentSaveData != null)
            {
                currentSaveData.saveTimestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

                // JSONファイルに保存
                string jsonData = JsonUtility.ToJson(currentSaveData, true);
                File.WriteAllText(SaveFilePath, jsonData);

                if (debugMode)
                {
                    Debug.Log($"GameSaveManager: AfterChangeToHisFutureフラグ({currentSaveData.afterChangeToHisFuture})を保存しました");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"GameSaveManager: AfterChangeToHisFutureフラグ保存中にエラー: {ex.Message}");
        }
    }

    /// <summary>
    /// AfterChangeToHisFutureフラグを取得（新規追加）
    /// </summary>
    public bool GetAfterChangeToHisFutureFlag()
    {
        // メモリ上のフラグを優先
        if (hasAfterChangeFutureFlag)
        {
            return true;
        }

        // セーブデータから取得
        if (currentSaveData != null && currentSaveData.afterChangeToHisFuture)
        {
            return true;
        }

        return false;
    }

    public void SetPortraitDeleted(bool deleted)
    {
        if (currentSaveData != null)
        {
            currentSaveData.portraitDeleted = deleted;
        }
    }

    public bool HasPortraitBeenDeleted()
    {
        if (currentSaveData != null)
        {
            return currentSaveData.portraitDeleted;
        }
        return false;
    }

    private void OnApplicationQuit()
    {
        // AfterChangeToHerMemoryフラグの状態に関係なく、現在の状態を保持
        if (GameSaveManager.Instance != null)
        {
            bool currentFlag = GameSaveManager.Instance.GetAfterChangeToHerMemoryFlag();

            // 現在のフラグ状態でセーブ設定を決定
            GameSaveManager.Instance.SetAutoSaveOnQuit(currentFlag);
        }

        // デバッグログ
        Debug.Log("アプリケーション終了時: AfterChangeToHerMemoryフラグはJSONファイルに保存済み");
    }

    /// <summary>
    /// MonologueSceneからの遷移フラグを設定
    /// </summary>
    public void SetFromMonologueSceneFlag(bool value)
    {
        if (currentSaveData != null)
        {
            currentSaveData.fromMonologueScene = value;
        }
        else
        {
            Debug.LogWarning("GameSaveManager: currentSaveDataがnullのため、fromMonologueSceneフラグを設定できませんでした");
        }
    }

    /// <summary>
    /// MonologueSceneからの遷移フラグを取得
    /// </summary>
    public bool GetFromMonologueSceneFlag()
    {
        if (currentSaveData != null)
        {
            return currentSaveData.fromMonologueScene;
        }

        Debug.LogWarning("GameSaveManager: currentSaveDataがnullのため、fromMonologueSceneフラグを取得できませんでした");
        return false;
    }


    /// <summary>
    /// PDFのファイルパネル状態を確実にアクティブにする（アプリ起動時用）
    /// </summary>
    private void EnsurePdfPanelsActive()
    {
        // 「記録」「願い」フォルダーのパネルを探す
        Transform recordPanel = null;
        Transform wishPanel = null;

        try
        {
            foreach (FolderButtonScript folder in FindObjectsByType<FolderButtonScript>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string folderName = folder.GetFolderName();
                if (folderName == "記録")
                {
                    recordPanel = folder.filePanel?.transform;
                }
                else if (folderName == "願い")
                {
                    wishPanel = folder.filePanel?.transform;
                }
            }

            // PDFファイルの完了状態をチェック
            if (currentSaveData?.fileProgress?.pdf != null)
            {
                // 「記録」フォルダーの処理
                if (recordPanel != null)
                {
                    foreach (Transform child in recordPanel)
                    {
                        // 子オブジェクトが対応するPDFファイルかチェック
                        foreach (var pdfEntry in currentSaveData.fileProgress.pdf)
                        {
                            if (pdfEntry.Value.isCompleted &&
                                (child.name.Contains(pdfEntry.Key) ||
                                 child.name.Contains(Path.GetFileNameWithoutExtension(pdfEntry.Key))))
                            {
                                child.gameObject.SetActive(true);

                                // 対応するPdfDocumentManagerも完了状態に設定
                                var pdfManager = child.GetComponentInChildren<PdfDocumentManager>(true);
                                if (pdfManager != null)
                                {
                                    pdfManager.SetCompletionState(true);
                                }

                                if (debugMode)
                                    Debug.Log($"記録フォルダー内の完了状態PDF '{child.name}' をアクティブ化しました");
                            }
                        }
                    }
                }

                // 「願い」フォルダーの処理
                if (wishPanel != null)
                {
                    foreach (Transform child in wishPanel)
                    {
                        // 子オブジェクトが対応するPDFファイルかチェック
                        foreach (var pdfEntry in currentSaveData.fileProgress.pdf)
                        {
                            if (pdfEntry.Value.isCompleted &&
                                (child.name.Contains(pdfEntry.Key) ||
                                 child.name.Contains(Path.GetFileNameWithoutExtension(pdfEntry.Key))))
                            {
                                child.gameObject.SetActive(true);

                                // 対応するPdfDocumentManagerも完了状態に設定
                                var pdfManager = child.GetComponentInChildren<PdfDocumentManager>(true);
                                if (pdfManager != null)
                                {
                                    pdfManager.SetCompletionState(true);
                                }

                                if (debugMode)
                                    Debug.Log($"願いフォルダー内の完了状態PDF '{child.name}' をアクティブ化しました");
                            }
                        }
                    }
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"PDFファイルのアクティブ化中にエラー: {ex.Message}");
        }
    }


    /// <summary>
    /// 子オブジェクトでFileOpenコンポーネントを持つものをアクティブにする
    /// </summary>
    private void ActivateChildrenWithFileOpen(Transform parent)
    {
        try
        {
            foreach (Transform child in parent)
            {
                if (child.gameObject.activeSelf && child.GetComponent<FileOpen>() != null)
                {
                    // このファイルの対応するPDFパネルを探す
                    string childName = child.name.ToLower();

                    // 1. 保存データから完了状態のPDFファイルを先に確認
                    if (currentSaveData != null &&
                        currentSaveData.fileProgress != null &&
                        currentSaveData.fileProgress.pdf != null)
                    {
                        foreach (var pdfEntry in currentSaveData.fileProgress.pdf)
                        {
                            string pdfFileName = pdfEntry.Key.ToLower();
                            bool isCompleted = pdfEntry.Value.isCompleted;

                            // 完了状態のPDFで、名前が部分一致する場合
                            if (isCompleted && (childName.Contains(pdfFileName) ||
                                             pdfFileName.Contains(Path.GetFileNameWithoutExtension(childName))))
                            {
                                // 該当するすべてのPDFパネルを探して強制アクティブ化
                                foreach (var go in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                                {
                                    if (go.name.Contains("PDFFilePanel") && go.name.Contains(pdfFileName))
                                    {
                                        go.SetActive(true);
                                        if (debugMode)
                                            Debug.Log($"完了状態PDFパネル '{go.name}' を強制的にアクティブ化しました");
                                    }
                                }
                            }
                        }
                    }

                    // 2. 従来の方法でもPDFパネルを探してアクティブ化
                    foreach (var pdfManager in FindObjectsByType<PdfDocumentManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    {
                        string pdfName = pdfManager.GetPdfFileName().ToLower();

                        // ファイル名が含まれていればそのPDFパネルをアクティブに
                        if (childName.Contains(pdfName) || pdfManager.name.ToLower().Contains(childName))
                        {
                            // パネルを強制アクティブ化
                            Transform panelTransform = pdfManager.transform;
                            while (panelTransform != null && !panelTransform.name.Contains("PDFFilePanel"))
                            {
                                panelTransform = panelTransform.parent;
                            }

                            if (panelTransform != null)
                            {
                                panelTransform.gameObject.SetActive(true);
                                if (debugMode)
                                    Debug.Log($"PDFパネル '{panelTransform.name}' をアクティブ化しました");
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"PDFパネルのアクティブ化中にエラー: {ex.Message}");
        }
    }

    public void InitializeSaveData()
    {
        // 初期状態では設定されたフォルダーをアクティブに
        currentSaveData = new GameSaveData
        {
            gameVersion = gameVersion,
            saveTimestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
            folderState = new FolderState
            {
                activeFolder = defaultActiveFolder,
                displayedFolders = initialFolders
            },
            fileProgress = new FileProgressData
            {
                txt = new Dictionary<string, TxtFileData>(),
                png = new Dictionary<string, PngFileData>(),
                pdf = new Dictionary<string, PdfFileData>()
            },
            audioSettings = new AudioSettings
            {
                bgmVolume = 0.5f,
                seVolume = 0.5f
            },
            afterChangeToHerMemory = false,
            endOpeningScene = false
        };
    }

    private void CollectGameState()
    {
        try
        {
            currentSaveData.saveTimestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

            // 修正: 各データ収集メソッドをtry-catchで囲む
            try { CollectTxtPuzzleState(); } catch (Exception ex) { Debug.LogError($"TXTデータ収集中にエラー: {ex.Message}"); }
            try { CollectImageRevealerState(); } catch (Exception ex) { Debug.LogError($"画像データ収集中にエラー: {ex.Message}"); }
            try { CollectPdfDocumentState(); } catch (Exception ex) { Debug.LogError($"PDFデータ収集中にエラー: {ex.Message}"); }
            try { CollectFolderState(); } catch (Exception ex) { Debug.LogError($"フォルダー状態収集中にエラー: {ex.Message}"); }
            try { CollectAudioSettings(); } catch (Exception ex) { Debug.LogError($"音声設定収集中にエラー: {ex.Message}"); }

            // デバッグ用の出力
            if (debugMode)
            {
                int txtCount = currentSaveData.fileProgress.txt?.Count ?? 0;
                int pngCount = currentSaveData.fileProgress.png?.Count ?? 0;
                int pdfCount = currentSaveData.fileProgress.pdf?.Count ?? 0;
                int folderCount = currentSaveData.folderState?.displayedFolders?.Length ?? 0;

                Debug.Log($"収集したデータ - TXT: {txtCount}件, PNG: {pngCount}件, PDF: {pdfCount}件, 表示フォルダー: {folderCount}件");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"ゲーム状態の収集中にエラーが発生しました: {ex.Message}");
        }
    }

    // GameSaveManager.csのCollectTxtPuzzleStateメソッドを修正
    private void CollectTxtPuzzleState()
    {
        // TxtFileDataの初期化を確認
        if (currentSaveData.fileProgress.txt == null)
            currentSaveData.fileProgress.txt = new Dictionary<string, TxtFileData>();
        else
            currentSaveData.fileProgress.txt.Clear();

        // シーン内のすべてのTxtPuzzleManagerを取得
        TxtPuzzleManager[] txtManagers = FindObjectsByType<TxtPuzzleManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (txtManagers != null && txtManagers.Length > 0)
        {
            // 既存のセーブデータがあれば読み込む
            Dictionary<string, TxtFileData> existingData = new Dictionary<string, TxtFileData>();
            string txtProgressPath = Path.Combine(Application.persistentDataPath, "txt_progress.json");

            if (File.Exists(txtProgressPath))
            {
                try
                {
                    string txtJson = File.ReadAllText(txtProgressPath);
                    existingData = JsonHelper.FromJson<string, TxtFileData>(txtJson);
                    if (debugMode)
                        Debug.Log($"既存のTXTデータを読み込みました: {existingData.Count}件");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"既存TXTデータの読み込みエラー: {ex.Message}");
                }
            }

            // すべてのマネージャーから進捗データを収集
            foreach (var manager in txtManagers)
            {
                if (manager == null) continue;

                TxtFileData fileData = manager.GetTxtProgress();
                if (string.IsNullOrEmpty(fileData.fileName)) continue;

                // 既存データが完了状態なら、その状態を維持
                if (existingData.TryGetValue(fileData.fileName, out TxtFileData existing) && existing.isCompleted)
                {
                    fileData.isCompleted = true;
                }

                // データを保存
                currentSaveData.fileProgress.txt[fileData.fileName] = fileData;

                if (debugMode)
                    Debug.Log($"TXTファイル '{fileData.fileName}' の進捗を保存: 完了={fileData.isCompleted}, 解答={fileData.solvedMatches}/{fileData.totalMatches}");
            }

            if (debugMode)
                Debug.Log($"合計 {currentSaveData.fileProgress.txt.Count} 件のTXTデータを保存します");
        }
        else if (debugMode)
        {
            Debug.LogWarning("TxtPuzzleManagerが見つかりません");
        }
    }

    private void CollectImageRevealerState()
    {
        // PngFileDataの初期化を確認
        if (currentSaveData.fileProgress.png == null)
            currentSaveData.fileProgress.png = new Dictionary<string, PngFileData>();
        else
            currentSaveData.fileProgress.png.Clear();

        // ImageRevealerのリストをクリアして再取得
        imageRevealers.Clear();
        imageRevealers.AddRange(FindObjectsByType<ImageRevealer>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        if (imageRevealers.Count > 0)
        {
            foreach (var revealer in imageRevealers)
            {
                if (revealer == null) continue;

                var fileProgress = revealer.GetImageProgress();
                if (fileProgress != null && !string.IsNullOrEmpty(fileProgress.fileName))
                {
                    currentSaveData.fileProgress.png[fileProgress.fileName] = fileProgress;

                    if (debugMode)
                        Debug.Log($"画像データを収集: {fileProgress.fileName}, レベル: {fileProgress.currentLevel}/{fileProgress.maxLevel}");
                }
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning("ImageRevealerが見つかりません");
        }
    }

    /// <summary>
    /// 完了状態のPDFファイルを強制的にアクティブ化するメソッド
    /// </summary>
    private void ForceActivateCompletedPdfPanels()
    {
        if (currentSaveData?.fileProgress?.pdf == null) return;

        try
        {
            // 記録FilePanelと願いFilePanelを探す
            Transform recordFilePanel = null;
            Transform wishFilePanel = null;

            foreach (var folderObj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (folderObj.name == "記録FilePanel")
                {
                    recordFilePanel = folderObj.transform;
                }
                else if (folderObj.name == "願いFilePanel")
                {
                    wishFilePanel = folderObj.transform;
                }
            }

            // 完了状態のPDFファイルを探す
            foreach (var pdfEntry in currentSaveData.fileProgress.pdf)
            {
                if (pdfEntry.Value.isCompleted)
                {
                    string pdfFileName = pdfEntry.Key;
                    bool foundFile = false;

                    // 記録FilePanelの子から探す
                    if (recordFilePanel != null)
                    {
                        foreach (Transform child in recordFilePanel)
                        {
                            if (child.name.Contains(pdfFileName) ||
                                child.name.Contains(Path.GetFileNameWithoutExtension(pdfFileName)))
                            {
                                child.gameObject.SetActive(true);
                                foundFile = true;

                                // 対応するPdfDocumentManagerを完了状態に設定
                                PdfDocumentManager pdfManager = child.GetComponentInChildren<PdfDocumentManager>(true);
                                if (pdfManager != null)
                                {
                                    pdfManager.SetCompletionState(true);
                                }

                                if (debugMode)
                                    Debug.Log($"記録FilePanel内の完了状態PDF '{child.name}' をアクティブ化しました");

                                break;
                            }
                        }
                    }

                    // 願いFilePanelの子から探す
                    if (!foundFile && wishFilePanel != null)
                    {
                        foreach (Transform child in wishFilePanel)
                        {
                            if (child.name.Contains(pdfFileName) ||
                                child.name.Contains(Path.GetFileNameWithoutExtension(pdfFileName)))
                            {
                                child.gameObject.SetActive(true);
                                foundFile = true;

                                // 対応するPdfDocumentManagerを完了状態に設定
                                PdfDocumentManager pdfManager = child.GetComponentInChildren<PdfDocumentManager>(true);
                                if (pdfManager != null)
                                {
                                    pdfManager.SetCompletionState(true);
                                }

                                if (debugMode)
                                    Debug.Log($"願いFilePanel内の完了状態PDF '{child.name}' をアクティブ化しました");

                                break;
                            }
                        }
                    }

                    // より一般的な検索方法も追加
                    if (!foundFile)
                    {
                        foreach (var fileObj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                        {
                            if ((fileObj.name.Contains("ファイル") || fileObj.name.EndsWith(".pdf")) &&
                                (fileObj.name.Contains(pdfFileName) ||
                                 fileObj.name.Contains(Path.GetFileNameWithoutExtension(pdfFileName))))
                            {
                                fileObj.gameObject.SetActive(true);

                                // 対応するPdfDocumentManagerを完了状態に設定
                                PdfDocumentManager pdfManager = fileObj.GetComponentInChildren<PdfDocumentManager>(true);
                                if (pdfManager != null)
                                {
                                    pdfManager.SetCompletionState(true);
                                }

                                if (debugMode)
                                    Debug.Log($"完了状態PDF '{fileObj.name}' をアクティブ化しました");

                                break;
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"完了状態PDFファイルのアクティブ化中にエラー: {ex.Message}");
        }
    }
    private void CollectPdfDocumentState()
    {
        // 現在のPDF完了状態を一時保存（既存データの保持）
        Dictionary<string, bool> existingCompletionState = new Dictionary<string, bool>();
        if (currentSaveData.fileProgress.pdf != null)
        {
            foreach (var entry in currentSaveData.fileProgress.pdf)
            {
                if (entry.Value.isCompleted)
                {
                    existingCompletionState[entry.Key] = true;
                }
            }
        }

        // PDFデータの初期化
        if (currentSaveData.fileProgress.pdf == null)
            currentSaveData.fileProgress.pdf = new Dictionary<string, PdfFileData>();
        else
            currentSaveData.fileProgress.pdf.Clear();

        // PDFマネージャーのリストをクリアして再取得
        pdfManagers.Clear();
        pdfManagers.AddRange(FindObjectsByType<PdfDocumentManager>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        if (pdfManagers.Count > 0)
        {
            foreach (var pdfManager in pdfManagers)
            {
                if (pdfManager == null) continue;

                var fileProgress = pdfManager.GetPdfProgress();
                if (fileProgress != null && !string.IsNullOrEmpty(fileProgress.fileName))
                {
                    // 以前に完了していた場合は必ずtrue状態を保持
                    if (existingCompletionState.TryGetValue(fileProgress.fileName, out bool wasCompleted) && wasCompleted)
                    {
                        fileProgress.isCompleted = true;
                        if (debugMode)
                            Debug.Log($"PDF '{fileProgress.fileName}' は以前完了していたため、完了状態を維持します");
                    }

                    // 現在完了している場合も状態を更新
                    if (pdfManager.IsDocumentCompleted() && !fileProgress.isCompleted)
                    {
                        fileProgress.isCompleted = true;
                        Debug.Log($"PDF '{fileProgress.fileName}' の完了状態を修正しました: true");
                    }

                    currentSaveData.fileProgress.pdf[fileProgress.fileName] = fileProgress;

                    if (debugMode)
                    {
                        int revealedCount = fileProgress.revealedKeywords?.Length ?? 0;
                        Debug.Log($"PDFデータを収集: {fileProgress.fileName}, キーワード: {revealedCount}/{fileProgress.totalKeywords}");
                    }
                }
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning("PdfDocumentManagerが見つかりません");
        }

        // シーン内に存在しないPDFで完了状態だったものも復元
        foreach (var entry in existingCompletionState)
        {
            if (!currentSaveData.fileProgress.pdf.ContainsKey(entry.Key))
            {
                // 存在しないPDFを空のデータで作成し完了状態を復元
                currentSaveData.fileProgress.pdf[entry.Key] = new PdfFileData
                {
                    fileName = entry.Key,
                    isCompleted = true,
                    revealedKeywords = new string[0],
                    totalKeywords = 0
                };

                if (debugMode)
                    Debug.Log($"シーン内に存在しないPDF '{entry.Key}' の完了状態を復元しました");
            }
        }
    }


    private void CollectFolderState()
    {
        // フォルダー状態の初期化
        if (currentSaveData.folderState == null)
            currentSaveData.folderState = new FolderState();

        // フォルダーリストをクリアして再取得
        folderScripts.Clear();
        folderScripts.AddRange(FindObjectsByType<FolderButtonScript>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        List<string> displayedFolders = new List<string>();
        List<string> activatedFolders = new List<string>(); // 追加：アクティブ化履歴
        string activeFolder = "";

        if (folderScripts.Count > 0)
        {
            foreach (var folderScript in folderScripts)
            {
                if (folderScript == null) continue;

                string folderName = folderScript.GetFolderName();
                bool isActive = folderScript.IsActive();

                // 空のフォルダー名を無視
                if (string.IsNullOrEmpty(folderName)) continue;

                // フォルダーボタンが有効で表示されている場合
                if (folderScript.gameObject.activeSelf)
                {
                    displayedFolders.Add(folderName);
                    if (debugMode)
                        Debug.Log($"表示フォルダーを検出: {folderName}");
                }

                // アクティブ化履歴の収集（追加）
                if (folderScript.HasBeenActivated())
                {
                    activatedFolders.Add(folderName);
                    if (debugMode)
                        Debug.Log($"アクティブ化履歴フォルダーを検出: {folderName}");
                }

                // アクティブフォルダーを収集
                if (isActive)
                {
                    activeFolder = folderName;
                    if (debugMode)
                        Debug.Log($"アクティブフォルダーを検出: {folderName}");
                }
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning("FolderButtonScriptが見つかりません");
        }

        // 収集したフォルダー状態を設定
        currentSaveData.folderState.displayedFolders = displayedFolders.ToArray();
        currentSaveData.folderState.activeFolder = activeFolder;
        currentSaveData.folderState.activatedFolders = activatedFolders.ToArray(); // 追加
    }

    private void CollectAudioSettings()
    {
        // 既に読み込まれた音量設定がある場合は、それを優先する
        if (currentSaveData.audioSettings != null &&
            currentSaveData.audioSettings.masterVolume > 0)
        {
            // game_save.jsonから読み込んだ値を保持
            if (debugMode)
            { 
                Debug.Log("game_save.jsonから読み込んだ値を保持"); 
            }
                
            return;
        }

        // 初回のみPlayerPrefsから読み込む
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
        float seVolume = PlayerPrefs.GetFloat("SEVolume", 0.5f);

        // SoundEffectManagerがある場合はそこから直接取得
        SoundEffectManager soundManager = SoundEffectManager.Instance;
        if (soundManager != null)
        {
            seVolume = soundManager.GetVolume();
        }

        if (currentSaveData.audioSettings == null)
            currentSaveData.audioSettings = new AudioSettings();

        currentSaveData.audioSettings.masterVolume = masterVolume;
        currentSaveData.audioSettings.bgmVolume = bgmVolume;
        currentSaveData.audioSettings.seVolume = seVolume;
    }

    public void SaveGame()
    {
        try
        {
            CollectGameState();

            // AfterChangeToHerMemoryフラグを保持
            if (hasAfterChangeFlag || PlayerPrefs.GetInt("AfterChangeToHerMemory", 0) == 1)
            {
                currentSaveData.afterChangeToHerMemory = true;
            }

            // DictionaryデータをJSON形式で保存
            string txtFilePath = Path.Combine(Application.persistentDataPath, "txt_progress.json");
            string pngFilePath = Path.Combine(Application.persistentDataPath, "png_progress.json");
            string pdfFilePath = Path.Combine(Application.persistentDataPath, "pdf_progress.json");

            // ファイル削除ロジックを修正（データが空でも既存ファイルは削除しない）
            if (currentSaveData.fileProgress.txt.Count > 0)
                File.WriteAllText(txtFilePath, JsonHelper.ToJson(currentSaveData.fileProgress.txt));

            if (currentSaveData.fileProgress.png.Count > 0)
                File.WriteAllText(pngFilePath, JsonHelper.ToJson(currentSaveData.fileProgress.png));

            if (currentSaveData.fileProgress.pdf.Count > 0)
                File.WriteAllText(pdfFilePath, JsonHelper.ToJson(currentSaveData.fileProgress.pdf));

            // メインのJSONデータを保存
            File.WriteAllText(SaveFilePath, JsonUtility.ToJson(currentSaveData, true));

        }
        catch (Exception e)
        {
            Debug.LogError($"セーブデータの保存中にエラーが発生しました: {e.Message}\n{e.StackTrace}");
        }
    }

    // 自動保存設定を変更するためのパブリックメソッドを追加
    public void SetAutoSaveOnQuit(bool value)
    {
        autoSaveOnQuit = value;
    }

    public bool LoadGame()
    {
        try
        {
            if (!File.Exists(SaveFilePath))
            {
                if (debugMode)
                    Debug.Log("セーブファイルが見つかりません。新規ゲームとして開始します。");
                return false;
            }

            string jsonData = File.ReadAllText(SaveFilePath);
            GameSaveData loadedData = JsonUtility.FromJson<GameSaveData>(jsonData);

            if (loadedData == null)
            {
                Debug.LogError("セーブデータの読み込みに失敗しました。データの形式が不正です。");
                return false;
            }

            // 現在のPDFの完了状態を一時保存（もし存在するなら）
            Dictionary<string, bool> currentCompletionState = new Dictionary<string, bool>();
            if (currentSaveData?.fileProgress?.pdf != null)
            {
                foreach (var entry in currentSaveData.fileProgress.pdf)
                {
                    if (entry.Value.isCompleted)
                    {
                        currentCompletionState[entry.Key] = true;
                    }
                }
            }

            // ファイル進捗データの初期化
            if (loadedData.fileProgress == null)
                loadedData.fileProgress = new FileProgressData();

            loadedData.fileProgress.txt = new Dictionary<string, TxtFileData>();
            loadedData.fileProgress.png = new Dictionary<string, PngFileData>();
            loadedData.fileProgress.pdf = new Dictionary<string, PdfFileData>();

            // 追加データの読み込み
            string txtFilePath = Path.Combine(Application.persistentDataPath, "txt_progress.json");
            string pngFilePath = Path.Combine(Application.persistentDataPath, "png_progress.json");
            string pdfFilePath = Path.Combine(Application.persistentDataPath, "pdf_progress.json");

            // TXT進捗データ
            if (File.Exists(txtFilePath))
            {
                try
                {
                    string txtJson = File.ReadAllText(txtFilePath);
                    loadedData.fileProgress.txt = JsonHelper.FromJson<string, TxtFileData>(txtJson);

                    if (debugMode)
                        Debug.Log($"TXTデータを読み込みました: {loadedData.fileProgress.txt.Count}件");
                }
                catch (Exception e)
                {
                    Debug.LogError($"TXTデータの読み込みに失敗: {e.Message}");
                }
            }

            // PNG進捗データ
            if (File.Exists(pngFilePath))
            {
                try
                {
                    string pngJson = File.ReadAllText(pngFilePath);
                    loadedData.fileProgress.png = JsonHelper.FromJson<string, PngFileData>(pngJson);

                    if (debugMode)
                        Debug.Log($"PNGデータを読み込みました: {loadedData.fileProgress.png.Count}件");
                }
                catch (Exception e)
                {
                    Debug.LogError($"PNGデータの読み込みに失敗: {e.Message}");
                }
            }

            // PDF進捗データ
            if (File.Exists(pdfFilePath))
            {
                try
                {
                    string pdfJson = File.ReadAllText(pdfFilePath);
                    Dictionary<string, PdfFileData> loadedPdfData = JsonHelper.FromJson<string, PdfFileData>(pdfJson);

                    // 現在のCompletedフラグと統合
                    foreach (var entry in loadedPdfData)
                    {
                        // 現在または以前の状態で完了フラグがtrueなら保持
                        if (currentCompletionState.TryGetValue(entry.Key, out bool wasCompleted) && wasCompleted)
                        {
                            entry.Value.isCompleted = true;
                            if (debugMode)
                                Debug.Log($"PDF '{entry.Key}' は以前完了していたため、完了状態を維持します");
                        }
                    }

                    loadedData.fileProgress.pdf = loadedPdfData;

                    if (debugMode)
                        Debug.Log($"PDFデータを読み込みました: {loadedData.fileProgress.pdf.Count}件");
                }
                catch (Exception e)
                {
                    Debug.LogError($"PDFデータの読み込みに失敗: {e.Message}");
                }
            }

            // 以前完了していたがロードされなかったPDFを追加
            foreach (var entry in currentCompletionState)
            {
                if (!loadedData.fileProgress.pdf.ContainsKey(entry.Key))
                {
                    loadedData.fileProgress.pdf[entry.Key] = new PdfFileData
                    {
                        fileName = entry.Key,
                        isCompleted = true,
                        revealedKeywords = new string[0],
                        totalKeywords = 0
                    };

                    if (debugMode)
                        Debug.Log($"読み込まれなかったPDF '{entry.Key}' の完了状態を復元しました");
                }
            }

            // 他のnullチェックと初期化(既存コード)
            if (loadedData.folderState == null)
                loadedData.folderState = new FolderState
                {
                    activeFolder = defaultActiveFolder,
                    displayedFolders = initialFolders
                };

            if (loadedData.audioSettings == null)
            {
                loadedData.audioSettings = new AudioSettings
                {
                    bgmVolume = 0.5f,
                    seVolume = 0.5f
                };
            }

            // 古いセーブデータでafterChangeToHerMemoryフラグがない場合はfalseとして扱う
            if (loadedData.afterChangeToHerMemory)
            {
                if (debugMode)
                    Debug.Log($"セーブデータからAfterChangeToHerMemoryフラグを読み込み: {loadedData.afterChangeToHerMemory}");
            }

            currentSaveData = loadedData;

            // フラグをメモリにも復元
            hasAfterChangeFlag = currentSaveData.afterChangeToHerMemory;

            if (debugMode)
            {
                Debug.Log($"セーブデータからAfterChangeToHerMemoryフラグを読み込み: {hasAfterChangeFlag}");
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"セーブデータの読み込み中にエラーが発生しました: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    // 対応する閉じ括弧を見つけるヘルパーメソッド
    private int FindMatchingBracket(string text, int openBracketIndex)
    {
        if (openBracketIndex < 0 || openBracketIndex >= text.Length) return -1;

        char openChar = text[openBracketIndex];
        char closeChar = openChar == '[' ? ']' : (openChar == '{' ? '}' : ')');

        int depth = 1;
        for (int i = openBracketIndex + 1; i < text.Length; i++)
        {
            if (text[i] == openChar) depth++;
            else if (text[i] == closeChar)
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    // EnsureTxtNextFoldersActiveメソッドを追加
    private void EnsureTxtNextFoldersActive()
    {
        var txtManagers = FindObjectsByType<TxtPuzzleManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var txtManager in txtManagers)
        {
            if (txtManager != null && txtManager.IsPuzzleCompleted())
            {
                GameObject nextFolder = txtManager.GetNextFolder();
                if (nextFolder != null)
                {
                    nextFolder.SetActive(true);

                    // FolderButtonScript処理
                    FolderButtonScript folderScript = nextFolder.GetComponent<FolderButtonScript>();
                    if (folderScript == null)
                        folderScript = nextFolder.GetComponentInParent<FolderButtonScript>();

                    if (folderScript != null)
                    {
                        folderScript.SetActivatedState(true);
                        folderScript.SetVisible(true);
                    }
                }
            }
        }
    }

    public bool LoadGameAndApply()
    {
        try
        {
            bool loadSuccess = LoadGame();
            if (!loadSuccess) return false;

            // PDFデータを最初に適用
            try { ApplyPdfDocumentState(); }
            catch (Exception ex) { Debug.LogError($"PDFデータ適用中にエラー: {ex.Message}"); }

            // 残りのデータを適用
            try { ApplyTxtPuzzleState(); }
            catch (Exception ex) { Debug.LogError($"TXTデータ適用中にエラー: {ex.Message}"); }

            try { ApplyImageRevealerState(); }
            catch (Exception ex) { Debug.LogError($"画像データ適用中にエラー: {ex.Message}"); }

            try { ApplyFolderState(); }
            catch (Exception ex) { Debug.LogError($"フォルダー状態適用中にエラー: {ex.Message}"); }

            try { ApplyAudioSettings(); }
            catch (Exception ex) { Debug.LogError($"音声設定適用中にエラー: {ex.Message}"); }

            // 完了したPDFの次のフォルダーを確実にアクティブに
            try
            {
                EnsurePdfNextFoldersActive();
                // 少し遅延させて確実に適用
                StartCoroutine(DelayedPdfFolderActivation());
            }
            catch (Exception ex) { Debug.LogError($"PDFフォルダー有効化中にエラー: {ex.Message}"); }

            try { EnsureTxtNextFoldersActive(); }
            catch (Exception ex) { Debug.LogError($"TXTフォルダー有効化中にエラー: {ex.Message}"); }

            // PDFパネルの詳細有効化
            try { EnsurePdfPanelsActive(); }
            catch (Exception ex) { Debug.LogError($"PDFパネル詳細有効化中にエラー: {ex.Message}"); }

            // 追加: 完了状態のPDFパネルを強制的にアクティブ化
            try { ForceActivateCompletedPdfPanels(); }
            catch (Exception ex) { Debug.LogError($"完了状態PDFパネル強制有効化中にエラー: {ex.Message}"); }

            initialLoadCompleted = true;
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"セーブデータの適用中にエラーが発生しました: {e.Message}\n{e.StackTrace}");
            return false;
        }
    }

    // 遅延実行用のコルーチンを追加
    private IEnumerator DelayedPdfFolderActivation()
    {
        yield return new WaitForSeconds(0.5f);

        // PDFマネージャーをもう一度確認
        var pdfManagers = FindObjectsByType<PdfDocumentManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var pdfManager in pdfManagers)
        {
            if (pdfManager != null && pdfManager.IsDocumentCompleted())
            {

                // 完了状態を再設定することですべてのキーワードを表示（自動セーブ無効）
                pdfManager.SetCompletionState(true, false);

                // 次のフォルダーをアクティブ化
                pdfManager.EnsureNextFolderActive();
            }
        }
    }

    // 追加: 完了したPDFの次のフォルダーをアクティブにする
    // EnsurePdfNextFoldersActiveメソッドに以下の修正を加えます
    private void EnsurePdfNextFoldersActive()
    {
        var pdfManagers = FindObjectsByType<PdfDocumentManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var pdfManager in pdfManagers)
        {
            if (pdfManager != null)
            {
                // PDFファイル名をチェックして進捗データと照合
                string fileName = pdfManager.GetPdfFileName();

                // 対応するPDFデータが存在し、完了状態であればアクティブ化
                if (!string.IsNullOrEmpty(fileName) &&
                    currentSaveData.fileProgress.pdf.TryGetValue(fileName, out PdfFileData pdfData) &&
                    pdfData.isCompleted)
                {
                    // PDFオブジェクト自体をアクティブに
                    if (!pdfManager.gameObject.activeSelf)
                    {
                        pdfManager.gameObject.SetActive(true);
                        if (debugMode)
                            Debug.Log($"PDF '{fileName}' オブジェクトを強制的にアクティブ化しました");
                    }

                    // 完了状態を強制設定（自動セーブ無効）
                    pdfManager.SetCompletionState(true, false);

                    // 次のフォルダーを明示的にアクティブ化
                    pdfManager.EnsureNextFolderActive();

                    if (debugMode)
                        Debug.Log($"PDF '{fileName}' は完了状態です。次のフォルダーを確実にアクティブ化しました（自動セーブ無効）。");
                }
            }
        }
    }

    private void ApplyTxtPuzzleState()
    {
        if (currentSaveData.fileProgress.txt == null || currentSaveData.fileProgress.txt.Count == 0)
        {
            if (debugMode)
                Debug.Log("適用するTXTデータがありません");
            return;
        }

        // シーン内のすべてのTxtPuzzleManagerを取得
        TxtPuzzleManager[] txtManagers = FindObjectsByType<TxtPuzzleManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        if (txtManagers != null && txtManagers.Length > 0)
        {
            int appliedCount = 0;

            foreach (var manager in txtManagers)
            {
                if (manager == null) continue;

                // Dictionaryにキーとして使用するためのファイル名を取得
                string fileName = manager.GetTxtProgress().fileName;

                if (!string.IsNullOrEmpty(fileName) &&
                    currentSaveData.fileProgress.txt.TryGetValue(fileName, out TxtFileData fileData))
                {
                    manager.ApplyTxtProgress(new Dictionary<string, TxtFileData> { { fileName, fileData } });
                    appliedCount++;

                    if (debugMode)
                        Debug.Log($"TXTファイル '{fileName}' の進捗を適用しました");
                }
            }

            if (debugMode)
                Debug.Log($"合計 {appliedCount} 件のTXTデータを適用しました");
        }
        else if (debugMode)
        {
            Debug.LogWarning("TxtPuzzleManagerが見つからないためTXTデータを適用できません");
        }
    }

    private void ApplyImageRevealerState()
    {
        if (currentSaveData.fileProgress.png == null || currentSaveData.fileProgress.png.Count == 0)
        {
            if (debugMode)
                Debug.Log("適用するPNGデータがありません");
            return;
        }

        // ImageRevealer のリストが空なら再取得
        if (imageRevealers.Count == 0)
            imageRevealers.AddRange(FindObjectsByType<ImageRevealer>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        if (imageRevealers.Count > 0)
        {
            int appliedCount = 0;
            // コレクションのコピーを作成してから反復処理
            var imageRevealersCopy = new List<ImageRevealer>(imageRevealers);

            foreach (var revealer in imageRevealersCopy)
            {
                if (revealer == null) continue;

                string fileName = revealer.GetImageFileName();
                if (!string.IsNullOrEmpty(fileName) &&
                    currentSaveData.fileProgress.png.TryGetValue(fileName, out PngFileData pngData))
                {
                    revealer.ApplyImageProgress(pngData);
                    appliedCount++;
                }
            }

            if (debugMode)
                Debug.Log($"PNGデータを適用しました: {appliedCount}件");
        }
        else if (debugMode)
        {
            Debug.LogWarning("ImageRevealerが見つからないためPNGデータを適用できません");
        }
    }

    private void ApplyPdfDocumentState()
    {
        if (currentSaveData.fileProgress.pdf == null || currentSaveData.fileProgress.pdf.Count == 0)
        {
            if (debugMode)
                Debug.Log("適用するPDFデータがありません");
            return;
        }

        try
        {
            // すべてのPDFファイルパネルを事前にアクティブ化
            var allPdfPanels = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(go => go.name.EndsWith(".PDFFilePanel") || go.name.Contains("PDFFilePanel"))
                .ToArray();

            foreach (var panel in allPdfPanels)
            {
                if (!panel.activeSelf)
                {
                    panel.SetActive(true);
                    if (debugMode)
                        Debug.Log($"PDFパネル '{panel.name}' を強制的にアクティブ化しました");
                }
            }

            // PdfManager のリストが空ならば取得
            if (pdfManagers.Count == 0)
                pdfManagers.AddRange(FindObjectsByType<PdfDocumentManager>(FindObjectsInactive.Include, FindObjectsSortMode.None));

            if (pdfManagers.Count > 0)
            {
                int appliedCount = 0;

                // コレクションのコピーを作成してから操作（エラー防止のため）
                var managersToProcess = new List<PdfDocumentManager>(pdfManagers);

                // 各PDFマネージャーに状態を適用
                foreach (var pdfManager in managersToProcess)
                {
                    if (pdfManager == null) continue;

                    string fileName = pdfManager.GetPdfFileName();
                    if (!string.IsNullOrEmpty(fileName) &&
                        currentSaveData.fileProgress.pdf.TryGetValue(fileName, out PdfFileData pdfData))
                    {
                        // 完了状態の場合は、親オブジェクトもアクティブに
                        if (pdfData.isCompleted)
                        {
                            // パネル自体をアクティブに
                            Transform parent = pdfManager.transform;
                            while (parent != null && !parent.name.Contains("PDFFilePanel"))
                            {
                                parent = parent.parent;
                            }

                            if (parent != null)
                            {
                                parent.gameObject.SetActive(true);
                                if (debugMode)
                                    Debug.Log($"PDF '{fileName}' の親パネル '{parent.name}' を強制的にアクティブ化しました");
                            }

                            // 強制的に完了状態を設定（自動セーブ無効）
                            pdfManager.SetCompletionState(true, false);
                        }

                        appliedCount++;
                    }
                }

                if (debugMode)
                    Debug.Log($"PDFデータを適用しました: {appliedCount}件（自動セーブ無効）");
            }
            else if (debugMode)
            {
                Debug.LogWarning("PdfDocumentManagerが見つからないためPDFデータを適用できません");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"PDFデータ適用中にエラー: {ex.Message}");
        }
    }

    private void ApplyFolderState()
    {
        if (currentSaveData.folderState == null ||
            currentSaveData.folderState.displayedFolders == null ||
            currentSaveData.folderState.displayedFolders.Length == 0)
        {
            if (debugMode)
                Debug.Log("適用するフォルダー状態がありません");
            return;
        }

        // FolderScript のリストが空ならもう一度取得
        if (folderScripts.Count == 0)
            folderScripts.AddRange(FindObjectsByType<FolderButtonScript>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        if (folderScripts.Count == 0)
        {
            if (debugMode)
                Debug.LogWarning("FolderButtonScriptが見つからないためフォルダー状態を適用できません");
            return;
        }

        // フォルダー名をキーとしたマップを作成
        Dictionary<string, FolderButtonScript> folderMap = new Dictionary<string, FolderButtonScript>();
        foreach (var folderScript in folderScripts)
        {
            if (folderScript == null) continue;

            string folderName = folderScript.GetFolderName();
            if (!string.IsNullOrEmpty(folderName))
            {
                folderMap[folderName] = folderScript;
            }
        }

        // セーブデータに含まれるフォルダーをすべて表示状態に
        HashSet<string> displayedFolderSet = new HashSet<string>(currentSaveData.folderState.displayedFolders);

        // アクティブ化履歴のセットを作成（追加）
        HashSet<string> activatedFolderSet = new HashSet<string>();
        if (currentSaveData.folderState.activatedFolders != null)
        {
            activatedFolderSet = new HashSet<string>(currentSaveData.folderState.activatedFolders);
        }

        // コレクションのコピーを作成してから操作
        var folderScriptsCopy = new List<FolderButtonScript>(folderScripts);

        foreach (var folderScript in folderScriptsCopy)
        {
            if (folderScript == null) continue;

            string folderName = folderScript.GetFolderName();
            if (string.IsNullOrEmpty(folderName)) continue;

            // アクティブ化履歴の適用（追加）
            if (activatedFolderSet.Contains(folderName))
            {
                folderScript.SetActivatedState(true);
                if (debugMode)
                    Debug.Log($"フォルダーのアクティブ化履歴を適用: {folderName}");
            }

            // 表示状態の適用
            if (displayedFolderSet.Contains(folderName) || folderScript.HasBeenActivated())
            {
                folderScript.gameObject.SetActive(true);
                if (debugMode)
                    Debug.Log($"フォルダーボタンを表示: {folderName}");
            }

            // ファイルパネルの初期状態は非表示に設定
            if (folderScript.filePanel != null)
            {
                folderScript.filePanel.SetActive(false);
            }
        }

        // アクティブフォルダーを設定
        string activeFolder = currentSaveData.folderState.activeFolder;
        if (!string.IsNullOrEmpty(activeFolder) &&
            folderMap.TryGetValue(activeFolder, out FolderButtonScript activeFolderScript))
        {
            // 少し遅延させてToggleFolderを呼び出す（他のフォルダー初期化後に呼び出すため）
            StartCoroutine(DelayedToggleFolder(activeFolderScript));

            if (debugMode)
                Debug.Log($"アクティブフォルダーを設定: {activeFolder}");
        }
        else if (folderMap.TryGetValue(defaultActiveFolder, out FolderButtonScript defaultFolderScript))
        {
            // アクティブフォルダーがない場合、デフォルトで設定したフォルダーを開く
            StartCoroutine(DelayedToggleFolder(defaultFolderScript));
        }
    }

    private System.Collections.IEnumerator DelayedToggleFolder(FolderButtonScript folderScript)
    {
        yield return new WaitForSeconds(folderToggleDelay); // 少し遅延させる
        folderScript.ToggleFolder();
    }

    private void ApplyAudioSettings()
    {
        // 音量設定を適用
        if (currentSaveData.audioSettings != null)
        {
            // マスター音量を適用
            AudioListener.volume = currentSaveData.audioSettings.masterVolume;
            PlayerPrefs.SetFloat("MasterVolume", currentSaveData.audioSettings.masterVolume);

            // BGM音量を設定
            PlayerPrefs.SetFloat("BGMVolume", currentSaveData.audioSettings.bgmVolume);

            // SE音量を設定
            PlayerPrefs.SetFloat("SEVolume", currentSaveData.audioSettings.seVolume);

            // SoundEffectManagerがある場合は直接設定
            SoundEffectManager soundManager = SoundEffectManager.Instance;
            if (soundManager != null)
            {
                soundManager.SetVolume(currentSaveData.audioSettings.seVolume);
            }

            // MainSceneSettingsManagerがある場合は音量スライダーも更新
            MainSceneSettingsManager settingsManager = FindFirstObjectByType<MainSceneSettingsManager>(FindObjectsInactive.Include);
            if (settingsManager != null)
            {
                settingsManager.LoadSettings(); // 音量設定を再読み込み
            }

            // インスペクターで設定されたBGM AudioSourceがあれば音量を更新
            if (bgmAudioSource != null)
            {
                bgmAudioSource.volume = currentSaveData.audioSettings.bgmVolume;
            }
            else
            {
                // BGMという名前のオブジェクトがないか検索
                AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (AudioSource source in allAudioSources)
                {
                    if (source.gameObject.name.Contains("BGM") ||
                        source.gameObject.name.Contains("Bgm") ||
                        source.gameObject.name.Contains("bgm"))
                    {
                        source.volume = currentSaveData.audioSettings.bgmVolume;
                        break;
                    }
                }
            }

            PlayerPrefs.Save();
        }
    }

    public void DeleteSaveData()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                Debug.Log("セーブデータを削除しました");
            }

            InitializeSaveData();
        }
        catch (Exception e)
        {
            Debug.LogError($"セーブデータの削除中にエラーが発生しました: {e.Message}");
        }
    }

    // 現在フォルダー表示状態をデバッグログに出力（デバッグ用）
    public void LogCurrentFolders()
    {
        if (!debugMode) return;

        folderScripts.Clear();
        folderScripts.AddRange(FindObjectsByType<FolderButtonScript>(FindObjectsInactive.Include, FindObjectsSortMode.None));

        Debug.Log($"==== 現在のフォルダー状態 ====");
        foreach (var folder in folderScripts)
        {
            if (folder == null) continue;

            string folderName = folder.GetFolderName();
            bool isActive = folder.IsActive();
            bool isDisplayed = folder.gameObject.activeSelf;

            Debug.Log($"フォルダー: {folderName}, 表示中: {isDisplayed}, アクティブ: {isActive}");
        }
        Debug.Log($"==========================");
    }

    public bool SaveDataExists() => File.Exists(SaveFilePath);
    public string GetLastSaveTimestamp() => currentSaveData?.saveTimestamp ?? string.Empty;
    public bool IsInitialLoadCompleted() => initialLoadCompleted;

    public string GetActiveFolder()
    {
        // セーブデータが初期化されていない場合は空文字を返す
        if (currentSaveData == null || currentSaveData.folderState == null)
            return "";

        return currentSaveData.folderState.activeFolder;
    }

    // 音量設定のみを保存するメソッド
    public void SaveAudioSettingsOnly()
    {
        try
        {
            // 既存のセーブデータがあれば読み込む
            if (File.Exists(SaveFilePath))
            {
                string jsonData = File.ReadAllText(SaveFilePath);
                GameSaveData loadedData = JsonUtility.FromJson<GameSaveData>(jsonData);

                if (loadedData != null)
                {
                    // 音量設定のみを更新
                    loadedData.audioSettings = currentSaveData.audioSettings;
                    loadedData.saveTimestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

                    // 更新したデータを保存
                    File.WriteAllText(SaveFilePath, JsonUtility.ToJson(loadedData, true));

                    if (debugMode)
                        Debug.Log($"音量設定のみ保存しました: BGM={loadedData.audioSettings.bgmVolume}, SE={loadedData.audioSettings.seVolume}, Master={loadedData.audioSettings.masterVolume}");

                    return;
                }

            }

            // 既存のセーブデータがない場合は新規作成
            GameSaveData newData = new GameSaveData
            {
                gameVersion = gameVersion,
                saveTimestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                folderState = new FolderState
                {
                    activeFolder = defaultActiveFolder,
                    displayedFolders = initialFolders
                },
                fileProgress = new FileProgressData(),
                audioSettings = currentSaveData.audioSettings
            };

            File.WriteAllText(SaveFilePath, JsonUtility.ToJson(newData, true));
        }
        catch (Exception e)
        {
            Debug.LogError($"音量設定の保存中にエラーが発生しました: {e.Message}\n{e.StackTrace}");
        }
    }

    /// <summary>
    /// すべてのPDFファイルの進捗データを取得
    /// </summary>
    public Dictionary<string, PdfFileData> GetAllPdfProgress()
    {
        if (currentSaveData != null && currentSaveData.fileProgress != null)
        {
            return currentSaveData.fileProgress.pdf;
        }
        return new Dictionary<string, PdfFileData>();
    }

    public void SaveOnMainSceneEntry()
    {
        if (currentSaveData == null)
        {
            Debug.LogWarning("GameSaveManager: セーブデータが存在しません。初期化します");
            InitializeSaveData();
        }

        // endOpeningSceneフラグを確実にtrueに設定
        currentSaveData.endOpeningScene = true;

        // 即座にセーブ
        SaveGame();
        Debug.Log("GameSaveManager: MainScene移行時の自動セーブを実行しました");
    }

    public void SetEndOpeningSceneFlag(bool value)
    {
        if (currentSaveData == null)
        {
            Debug.LogWarning("GameSaveManager: セーブデータが初期化されていません");
            InitializeSaveData();
        }

        currentSaveData.endOpeningScene = value;
        Debug.Log($"GameSaveManager: endOpeningSceneフラグを{value}に設定しました");
    }

    public bool GetEndOpeningSceneFlag()
    {
        if (currentSaveData == null)
        {
            Debug.LogWarning("GameSaveManager: セーブデータが存在しません。falseを返します");
            return false;
        }

        bool flagValue = currentSaveData.endOpeningScene;
        Debug.Log($"GameSaveManager: endOpeningSceneフラグの値は{flagValue}です");
        return flagValue;
    }

    /// <summary>
    /// AfterChangeToHerMemoryフラグを設定
    /// </summary>
    public void SetAfterChangeToHerMemoryFlag(bool value)
    {
        if (currentSaveData == null)
        {
            InitializeSaveData();
        }

        // フラグが変更された場合のみ処理
        if (currentSaveData.afterChangeToHerMemory != value)
        {
            // メモリ上のフラグを更新
            hasAfterChangeFlag = value;
            currentSaveData.afterChangeToHerMemory = value;

            if (debugMode)
            {
                Debug.Log($"GameSaveManager: AfterChangeToHerMemoryフラグを {value} に設定しました");
            }

            // 値に関係なく変更があった場合はセーブ
            SaveAfterChangeFlag();
        }
    }

    /// <summary>
    /// AfterChangeフラグのみを含むセーブ
    /// </summary>
    private void SaveAfterChangeFlag()
    {
        try
        {
            // 現在のゲーム状態を収集（軽量版）
            if (currentSaveData != null)
            {
                currentSaveData.saveTimestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

                // JSONファイルに保存
                string jsonData = JsonUtility.ToJson(currentSaveData, true);
                File.WriteAllText(SaveFilePath, jsonData);

                if (debugMode)
                {
                    Debug.Log($"GameSaveManager: AfterChangeToHerMemoryフラグ({currentSaveData.afterChangeToHerMemory})を保存しました");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"GameSaveManager: AfterChangeフラグ保存中にエラー: {ex.Message}");
        }
    }

    /// <summary>
    /// フラグを強制設定してセーブ
    /// </summary>
    private void ForceSetFlagAndSave()
    {
        try
        {
            // 現在のゲーム状態を収集
            CollectGameState();

            // フラグを確実に設定
            currentSaveData.afterChangeToHerMemory = true;
            hasAfterChangeFlag = true;

            // タイムスタンプ更新
            currentSaveData.saveTimestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

            // JSONファイルに直接保存
            string jsonData = JsonUtility.ToJson(currentSaveData, true);
            File.WriteAllText(SaveFilePath, jsonData);

            if (debugMode)
            {
                Debug.Log($"GameSaveManager: AfterChangeToHerMemoryフラグ付きでデータを強制保存しました");
                Debug.Log($"保存されたフラグ値: {currentSaveData.afterChangeToHerMemory}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"GameSaveManager: フラグ付きセーブ中にエラー: {ex.Message}");
        }
    }


    /// <summary>
    /// AfterChangeToHerMemoryフラグを取得
    /// </summary>
    public bool GetAfterChangeToHerMemoryFlag()
    {
        // メモリ上のフラグを優先
        if (hasAfterChangeFlag)
        {
            return true;
        }

        // セーブデータから取得
        if (currentSaveData != null && currentSaveData.afterChangeToHerMemory)
        {
            return true;
        }

        // すべてfalseの場合 [ContextMenu("Debug: Reset All Flags")]
        return false;
    }

    /// <summary>
    /// AfterChangeToLastフラグを設定（新規追加）
    /// </summary>
    public void SetAfterChangeToLastFlag(bool value)
    {
        if (currentSaveData != null)
        {
            currentSaveData.afterChangeToLast = value;
            if (debugMode)
            {
                Debug.Log($"GameSaveManager: AfterChangeToLastフラグを{value}に設定しました");
            }
        }
    }

    /// <summary>
    /// AfterChangeToLastフラグを取得（新規追加）
    /// </summary>
    public bool GetAfterChangeToLastFlag()
    {
        if (currentSaveData != null)
        {
            return currentSaveData.afterChangeToLast;
        }
        return false;
    }

    /// <summary>
    /// allDialoguesCompletedフラグを取得（新規追加）
    /// </summary>
    public bool GetAllDialoguesCompletedFlag()
    {
        // MonologueDisplayManagerから静的フラグを確認
        // または、currentSaveDataから取得する実装を追加
        if (currentSaveData != null && currentSaveData.afterChangeToLast)
        {
            return true;
        }
        return false;
    }

    /// <summary>
    /// 全てのセーブデータを削除
    /// </summary>
    public void DeleteAllSaveData()
    {
        try
        {
            // メインのセーブファイルを削除
            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
                Debug.Log("セーブデータを削除しました: " + SaveFilePath);
            }

            // 個別のJSONファイルも削除
            string txtFilePath = Path.Combine(Application.persistentDataPath, "txt_progress.json");
            string pngFilePath = Path.Combine(Application.persistentDataPath, "png_progress.json");
            string pdfFilePath = Path.Combine(Application.persistentDataPath, "pdf_progress.json");

            if (File.Exists(txtFilePath)) File.Delete(txtFilePath);
            if (File.Exists(pngFilePath)) File.Delete(pngFilePath);
            if (File.Exists(pdfFilePath)) File.Delete(pdfFilePath);

            // メモリ上のデータもクリア
            currentSaveData = new GameSaveData();
            InitializeSaveData();

            // その他の初期化処理
            ResetAllFlags();

            if (debugMode)
            {
                Debug.Log("すべてのセーブデータを削除し、初期化しました");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"セーブデータ削除中にエラー: {ex.Message}");
        }
    }

    /// <summary>
    /// 音量設定を更新（メモリ上のみ）
    /// </summary>
    public void UpdateAudioSettings(float bgmVolume, float seVolume, float masterVolume = -1f)
    {
        if (currentSaveData.audioSettings == null)
            currentSaveData.audioSettings = new AudioSettings();

        // マスター音量が指定されていない場合は現在の値を使用
        if (masterVolume < 0f)
        {
            masterVolume = AudioListener.volume;
        }

        currentSaveData.audioSettings.masterVolume = masterVolume;
        currentSaveData.audioSettings.bgmVolume = bgmVolume;
        currentSaveData.audioSettings.seVolume = seVolume;

        if (debugMode)
        {
            Debug.Log($"音量設定を更新: Master={masterVolume}, BGM={bgmVolume}, SE={seVolume}");
        }
    }

    /// <summary>
    /// 現在のセーブデータを取得
    /// </summary>
    public GameSaveData GetCurrentSaveData()
    {
        return currentSaveData;
    }

    /// <summary>
    /// デバッグ用：AfterChangeToHerMemoryフラグの現在の状態をログ出力
    /// </summary>
    [ContextMenu("Debug: Show AfterChangeToHerMemory Flag")]
    public void DebugShowAfterChangeFlag()
    {
        Debug.Log($"AfterChangeToHerMemoryフラグ: {GetAfterChangeToHerMemoryFlag()}");
    }

    /// <summary>
    /// テスト用：すべてのフラグを初期化
    /// </summary>
    [ContextMenu("Debug: Reset All Flags")]
    public void ResetAllFlags()
    {
        if (currentSaveData != null)
        {
            // ファイル進捗データのクリア
            if (currentSaveData.fileProgress != null)
            {
                currentSaveData.fileProgress.txt.Clear();
                currentSaveData.fileProgress.png.Clear();
                currentSaveData.fileProgress.pdf.Clear();
            }

            // フォルダー状態のリセット
            if (currentSaveData.folderState != null)
            {
                currentSaveData.folderState.activeFolder = defaultActiveFolder;
                currentSaveData.folderState.displayedFolders = initialFolders;
                currentSaveData.folderState.activatedFolders = new string[0];
            }

            // フラグのリセット
            currentSaveData.afterChangeToHerMemory = false;
            currentSaveData.afterChangeToHisFuture = false;
            currentSaveData.portraitDeleted = false;
            currentSaveData.afterChangeToLast = false;
        }

        // メモリ上のフラグをリセット
        hasAfterChangeFlag = false;

        Debug.Log("GameSaveManager: すべてのフラグを初期化しました");
    }

    /// <summary>
    /// テスト用：現在のフラグ状態を表示
    /// </summary>
    [ContextMenu("Debug: Show Flag Status")]
    public void ShowFlagStatus()
    {
        Debug.Log($"=== フラグ状態 ===");
        Debug.Log($"hasAfterChangeFlag: {hasAfterChangeFlag}");
        Debug.Log($"セーブデータ afterChangeToHerMemory: {currentSaveData?.afterChangeToHerMemory}");
        Debug.Log($"================");
    }
}