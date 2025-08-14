using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Unity.Services.CloudSave.Models;

/// <summary>
/// OrganizeMainSceneの全体制御を行うメインコントローラークラス
/// ファイル整理機能のシーン全体の動作ロジックを管理します
/// </summary>
public class OrganizeMainSceneController : MonoBehaviour
{
    #region シングルトン実装

    // シングルトンインスタンス
    private static OrganizeMainSceneController instance;

    /// <summary>
    /// OrganizeMainSceneControllerのシングルトンインスタンス
    /// </summary>
    public static OrganizeMainSceneController Instance
    {
        get
        {
            if (instance == null)
            {
                // Unity 6の新機能を使用 - 非アクティブオブジェクトも含めて検索
                instance = FindFirstObjectByType<OrganizeMainSceneController>(FindObjectsInactive.Include);

                if (instance == null && Application.isPlaying)
                {
                    Debug.LogWarning("OrganizeMainSceneController: インスタンスが見つかりません。新規作成します。");
                    GameObject go = new GameObject("OrganizeMainSceneController");
                    instance = go.AddComponent<OrganizeMainSceneController>();
                }
            }
            return instance;
        }
    }

    #endregion

    #region インスペクター設定

    [Header("UI参照")]
    [Tooltip("ファイル表示領域")]
    [SerializeField] private List<RectTransform> fileScrollView;

    [Tooltip("ファイル一覧のコンテンツパネル")]
    [SerializeField] private List<RectTransform> fileContentPanel;

    [Tooltip("ゴミ箱オブジェクト")]
    [SerializeField] private GameObject trashBinObject;

    [Tooltip("ゴミ箱オブジェクト-Button")]
    [SerializeField] private Button trashBinButton;

    [Tooltip("メッセージパネル")]
    [SerializeField] private GameObject messagePanel;

    [Tooltip("メッセージテキスト")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Tooltip("確認ダイアログパネル")]
    [SerializeField] private GameObject confirmationPanel;

    [Tooltip("確認ダイアログテキスト")]
    [SerializeField] private TextMeshProUGUI confirmationText;

    [Tooltip("確認ダイアログ - はいボタン")]
    [SerializeField] private Button confirmYesButton;

    [Tooltip("確認ダイアログ - いいえボタン")]
    [SerializeField] private Button confirmNoButton;

    [Tooltip("共通設定パネル")]
    [SerializeField] private GameObject commonSettingsPanel;

    [Header("マネージャー参照")]
    [Tooltip("ファイル管理マネージャー")]
    [SerializeField] private FileManager fileManager;

    [Tooltip("ゴミ箱でのファイル削除管理マネージャー")]
    [SerializeField] private TrashBoxDeletionManagement trashBoxDeletionManagement;

    // SerializeFieldを削除し、プライベート変数として定義
    private GameSaveManager saveManager;  // [SerializeField]を削除

    // SoundEffectManagerもSerializeFieldを削除
    private SoundEffectManager soundManager;  // [SerializeField]を削除

    [Header("シーン設定")]
    [Tooltip("戻る際の遷移先シーン名")]
    [SerializeField] private string returnSceneName = "TitleScene";

    [Tooltip("フェード速度")]
    [SerializeField] private float fadeSpeed = 1.0f;

    [Header("確認ダイアログ設定")]
    [Tooltip("全ファイル削除確認メッセージ")]
    [SerializeField] private string deleteAllMessage = "すべてのファイルを完全に削除しますか？";

    [Tooltip("ダイアログアニメーション時間")]
    [SerializeField] private float dialogAnimationDuration = 0.3f;

    [Header("デバッグ設定")]
    [Tooltip("デバッグログを表示するか")]
    [SerializeField] private bool debugMode = false;

    #endregion

    #region プライベート変数

    // シーンの初期化状態
    private bool isInitialized = false;

    // 現在表示中のファイルリスト
    private List<GameObject> currentFileItems;

    // 削除済みファイルのリスト
    private List<string> deletedFiles;

    // 全ファイル削除完了フラグ
    private bool allFilesDeleted = false;

    // シーン遷移中フラグ
    private bool isTransitioning = false;

    // 確認ダイアログ表示中フラグ
    private bool isDialogOpen = false;

    // 総ファイル数
    private int OrganizeMainScene_totalFileCount = 0;

    // 現在の削除ファイル数
    private int deletedFileCount = 0;

    #endregion

    #region Unityライフサイクル

    /// <summary>
    /// Awakeメソッド - 最初に実行される初期化処理
    /// </summary>
    private void Awake()
    {
        // シングルトンパターンの実装
        if (instance == null)
        {
            instance = this;

            if (debugMode)
            {
                Debug.Log($"{nameof(OrganizeMainSceneController)}: インスタンスを設定しました");
            }
        }
        else if (instance != this)
        {
            // 既存のインスタンスがある場合は自身を破棄
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(OrganizeMainSceneController)}: 既存のインスタンスが存在します。このオブジェクトを破棄します。");
            }
            Destroy(gameObject);
            return;
        }

        // 初期化
        InitializeLists();
        SetupButtonEvents();
    }

    /// <summary>
    /// Startメソッド - シーン開始時の処理
    /// </summary>
    private void Start()
    {
        // 初期化処理
        StartCoroutine(InitializeScene());

        // ボタンがクリックされたら OnTrashBinClicked を呼ぶ
        if (trashBinButton != null)
        {
            trashBinButton.onClick.AddListener(OnTrashBinClicked);
        }
    }

    /// <summary>
    /// OnDestroyメソッド - オブジェクト破棄時の処理
    /// </summary>
    private void OnDestroy()
    {
        // セーブデータを保存
        if (!isTransitioning) // シーン遷移中でない場合のみ
        {
            SaveData();
        }

        // シングルトンインスタンスのクリア
        if (instance == this)
        {
            instance = null;
        }

        // ボタンイベントのクリーンアップ
        CleanupButtonEvents();
    }

    #endregion

    #region 初期化処理

    /// <summary>
    /// リストの初期化
    /// </summary>
    private void InitializeLists()
    {
        currentFileItems = new List<GameObject>();
        deletedFiles = new List<string>();
    }

    /// <summary>
    /// ボタンイベントのセットアップ
    /// </summary>
    private void SetupButtonEvents()
    {
        // 確認ダイアログのボタンイベント設定
        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.RemoveAllListeners();
            confirmYesButton.onClick.AddListener(OnConfirmYesClicked);
        }

        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.RemoveAllListeners();
            confirmNoButton.onClick.AddListener(OnConfirmNoClicked);
        }
    }

    /// <summary>
    /// ボタンイベントのクリーンアップ
    /// </summary>
    private void CleanupButtonEvents()
    {
        if (confirmYesButton != null)
        {
            confirmYesButton.onClick.RemoveAllListeners();
        }

        if (confirmNoButton != null)
        {
            confirmNoButton.onClick.RemoveAllListeners();
        }

        if (trashBinButton != null)
        {
            trashBinButton.onClick.RemoveAllListeners();
        }
    }

    /// <summary>
    /// シーン全体の初期化処理
    /// </summary>
    /// <returns>コルーチン</returns>
    private IEnumerator InitializeScene()
    {
        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: シーン初期化開始");
        }

        // マネージャーの取得
        yield return InitializeManagers();

        // UIの初期化
        InitializeUI();

        // ファイル数の初期化
        InitializeFileCount();

        // セーブデータの読み込み
        LoadSaveData();

        // 初期化完了
        isInitialized = true;

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: シーン初期化完了");
        }
    }

    /// <summary>
    /// マネージャーの初期化と取得
    /// </summary>
    /// <returns>コルーチン</returns>
    private IEnumerator InitializeManagers()
    {
        // GameSaveManagerの取得（複数フレーム待機して確実に取得）
        int retryCount = 0;
        const int maxRetries = 10;

        while (saveManager == null && retryCount < maxRetries)
        {
            saveManager = GameSaveManager.Instance;

            if (saveManager == null)
            {
                retryCount++;
                yield return null; // 1フレーム待機

                if (debugMode && retryCount == 1)
                {
                    Debug.Log($"{nameof(OrganizeMainSceneController)}: GameSaveManagerを待機中...");
                }
            }
        }

        if (saveManager == null)
        {
            // それでも見つからない場合は新規作成を試みる
            GameObject gameSaveManagerObj = GameObject.Find("GameSaveManager");
            if (gameSaveManagerObj == null)
            {
                gameSaveManagerObj = new GameObject("GameSaveManager");
                gameSaveManagerObj.AddComponent<GameSaveManager>();
                DontDestroyOnLoad(gameSaveManagerObj);
            }
            saveManager = GameSaveManager.Instance;

            if (debugMode)
            {
                Debug.LogWarning($"{nameof(OrganizeMainSceneController)}: GameSaveManagerを新規作成しました");
            }
        }
        else if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: GameSaveManagerを取得しました（試行回数: {retryCount + 1}）");
        }

        // SoundEffectManagerの取得
        if (soundManager == null)
        {
            soundManager = SoundEffectManager.Instance;
            if (soundManager == null && debugMode)
            {
                Debug.LogWarning($"{nameof(OrganizeMainSceneController)}: SoundEffectManagerが見つかりません");
            }
        }

        // FileManagerの取得
        if (fileManager == null)
        {
            fileManager = GetComponent<FileManager>();
            if (fileManager == null)
            {
                fileManager = FindFirstObjectByType<FileManager>();
            }
        }

        // TrashBoxDeletionManagementの取得
        if (trashBoxDeletionManagement == null)
        {
            trashBoxDeletionManagement = GetComponent<TrashBoxDeletionManagement>();
            if (trashBoxDeletionManagement == null)
            {
                trashBoxDeletionManagement = FindFirstObjectByType<TrashBoxDeletionManagement>();
            }
        }

        yield return null;
    }

    /// <summary>
    /// UIの初期化
    /// </summary>
    private void InitializeUI()
    {
        // メッセージパネルを非表示
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }

        // 確認ダイアログを非表示
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }

        // 共通設定パネルを非表示
        if (commonSettingsPanel != null)
        {
            commonSettingsPanel.SetActive(false);
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: UI初期化完了");
        }
    }

    /// <summary>
    /// ファイル数の初期化
    /// </summary>
    private void InitializeFileCount()
    {
        OrganizeMainScene_totalFileCount = 0;
        currentFileItems.Clear();

        // ファイル数をカウント
        foreach (RectTransform panel in fileContentPanel)
        {
            if (panel == null) continue;

            // 直下の子オブジェクトをすべて数える（非アクティブも含む）
            for (int i = 0; i < panel.childCount; i++)
            {
                GameObject fileItem = panel.GetChild(i).gameObject;
                currentFileItems.Add(fileItem);
                OrganizeMainScene_totalFileCount++;
            }
        }

        // FileManagerにファイルアイテムを収集させる
        if (fileManager != null)
        {
            fileManager.CollectFileItems();
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: 総ファイル数: {OrganizeMainScene_totalFileCount}");
        }
    }

    #endregion

    #region セーブデータ処理

    /// <summary>
    /// セーブデータの読み込み
    /// </summary>
    private void LoadSaveData()
    {
        // saveManagerがnullの場合は再取得を試みる
        if (saveManager == null)
        {
            saveManager = GameSaveManager.Instance;

            if (saveManager == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"{nameof(OrganizeMainSceneController)}: GameSaveManagerが設定されていません");
                }
                return;
            }
        }

        // セーブデータから削除済みファイル情報を読み込み
        var organizeData = saveManager.GetOrganizeSceneData();
        if (organizeData != null && organizeData.deletedFiles != null)
        {
            deletedFiles = new List<string>(organizeData.deletedFiles);
            deletedFileCount = deletedFiles.Count;

            // FileManagerに削除済みファイルリストを設定
            if (fileManager != null)
            {
                fileManager.SetDeletedFiles(deletedFiles);
            }

            // 削除済みファイルを非表示にする
            foreach (string fileName in deletedFiles)
            {
                GameObject fileItem = currentFileItems.Find(item => item != null && item.name == fileName);
                if (fileItem != null)
                {
                    fileItem.SetActive(false);
                }
            }

            // 全ファイル削除フラグをチェック
            allFilesDeleted = organizeData.allFilesCompletelyDeleted;

            if (debugMode)
            {
                Debug.Log($"{nameof(OrganizeMainSceneController)}: 削除済みファイル数: {deletedFileCount}");
                Debug.Log($"{nameof(OrganizeMainSceneController)}: 全ファイル削除フラグ: {allFilesDeleted}");
            }
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: セーブデータ読み込み完了");
        }
    }


    /// <summary>
    /// アプリケーション終了時の処理
    /// </summary>
    private void OnApplicationQuit()
    {
        // セーブデータを強制保存
        SaveData();

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: アプリケーション終了時にセーブデータを保存しました");
        }
    }

    /// <summary>
    /// セーブデータの保存
    /// </summary>
    public void SaveData()
    {
        // saveManagerがnullの場合は再取得を試みる
        if (saveManager == null)
        {
            saveManager = GameSaveManager.Instance;

            if (saveManager == null)
            {
                if (debugMode)
                {
                    Debug.LogWarning($"{nameof(OrganizeMainSceneController)}: GameSaveManagerが設定されていません");
                }
                return;
            }
        }

        // 現在の削除済みファイルリストを保存
        var organizeData = saveManager.GetOrganizeSceneData();
        organizeData.deletedFiles = new List<string>(deletedFiles);
        organizeData.allFilesCompletelyDeleted = allFilesDeleted;

        // セーブデータをファイルに保存
        saveManager.SaveOrganizeSceneData(organizeData);

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: セーブデータ保存完了");
        }
    }


    #endregion

    #region ファイル管理

    /// <summary>
    /// リストから破棄された（nullになっている）GameObjectの参照を削除します。
    /// </summary>
    public void CleanUpFileList()
    {
        // nullになっているGameObjectの参照をリストからすべて削除する
        // item == null は、UnityのObject型に対して破棄されたオブジェクトをチェックする際に有効です。
        // Unityのオブジェクトは破棄されると内部的にnullとして扱われるため。
        int removedCount = currentFileItems.RemoveAll(item => item == null);

        if (removedCount > 0)
        {
            Debug.Log($"CleanUpFileList: {removedCount} 個の破棄されたオブジェクトの参照をリストから削除しました。");
        }
    }

    /// <summary>
    /// ファイルの削除（非表示化）処理
    /// </summary>
    /// <param name="fileName">削除するファイル名</param>
    public void DeleteFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }

        // 削除済みリストに追加
        if (!deletedFiles.Contains(fileName))
        {
            deletedFiles.Add(fileName);
            deletedFileCount++;
        }

        var fileObject = currentFileItems.Find(item => item != null && item.name == fileName);

        // 対応するファイルアイテムを非表示にする
        //GameObject fileItem = currentFileItems.Find(item => item.name == fileName);
        GameObject fileItem = currentFileItems.Find(item => item != null && item.name == fileName);
        if (fileItem != null)
        {
            fileItem.SetActive(false);
        }

        // FileManagerでファイル削除処理（FileManager自体が削除処理を行う）
        if (fileManager != null)
        {
            fileManager.DeleteFile(fileName);
        }

        // GameSaveManagerに削除を記録
        if (saveManager != null)
        {
            saveManager.MarkFileAsDeleted(fileName);
        }

        // 全ファイル削除チェック
        CheckAllFilesDeleted();

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: ファイル '{fileName}' を削除しました（{deletedFileCount}/{OrganizeMainScene_totalFileCount}）");
        }

    }

    /// <summary>
    /// 全ファイル削除のチェック
    /// </summary>
    private void CheckAllFilesDeleted()
    {
        if (deletedFileCount >= OrganizeMainScene_totalFileCount && !allFilesDeleted)
        {
            allFilesDeleted = true;
            AllFilesDeletedHandler(true);
        }
    }

    #endregion

    #region メッセージ表示

    /// <summary>
    /// メッセージパネルの表示
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    /// <param name="duration">表示時間（秒）</param>
    public void ShowMessage(string message, float duration = 3.0f)
    {
        if (messagePanel == null || messageText == null)
        {
            return;
        }

        StartCoroutine(ShowMessageCoroutine(message, duration));
    }

    /// <summary>
    /// メッセージ表示コルーチン
    /// </summary>
    private IEnumerator ShowMessageCoroutine(string message, float duration)
    {
        messageText.text = message;
        messagePanel.SetActive(true);

        yield return new WaitForSeconds(duration);

        messagePanel.SetActive(false);
    }

    /// <summary>
    /// ゴミ箱クリック時の処理
    /// </summary>
    private void OnTrashBinClicked()
    {
        if (allFilesDeleted == true)
        {
            Debug.Log("allFilesDeletedフラグ：" + allFilesDeleted);
            ShowAllFilesDeleteConfirmation();
        }

        ShowMessage("削除したいファイルをドラッグ&ドロップしてください。", 3.0f);

        if (soundManager != null)
        {
            soundManager.PlayClickSound();
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: ゴミ箱がクリックされました");
        }
    }

    #endregion

    #region 確認ダイアログ

    /// <summary>
    /// 全ファイル削除確認ダイアログの表示
    /// </summary>
    private void ShowAllFilesDeleteConfirmation()
    {
        if (confirmationPanel == null || confirmationText == null)
        {
            return;
        }

        isDialogOpen = true;
        confirmationText.text = deleteAllMessage;
        confirmationPanel.SetActive(true);

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: 全ファイル削除確認ダイアログを表示");
        }
    }

    /// <summary>
    /// 確認ダイアログ「はい」ボタンクリック時
    /// </summary>
    private void OnConfirmYesClicked()
    {
        isDialogOpen = false;
        confirmationPanel.SetActive(false);

        // 全ファイル完全削除処理
        CompleteAllFilesDeletion();

        if (soundManager != null)
        {
            soundManager.PlayClickSound();
        }
    }

    /// <summary>
    /// 確認ダイアログ「いいえ」ボタンクリック時
    /// </summary>
    private void OnConfirmNoClicked()
    {
        isDialogOpen = false;
        confirmationPanel.SetActive(false);

        if (soundManager != null)
        {
            soundManager.PlayClickSound();
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: 全ファイル削除をキャンセル");
        }
    }

    /// <summary>
    /// 全ファイル完全削除処理
    /// </summary>
    private void CompleteAllFilesDeletion()
    {
        // セーブデータを更新
        if (saveManager != null)
        {
            saveManager.SetAllFilesCompletelyDeleted(true);
        }

        // BGM変更処理
        if (soundManager != null)
        {
            // TODO: 新しいBGMへの変更処理
            // soundManager.ChangeToSpecialBGM();
        }

        // Steam実績解除処理
        // TODO: Steam実績「前へ」の解除
        // SteamAchievementManager.UnlockAchievement("FORWARD");

        ShowMessage("すべてのファイルが削除されました", 5.0f);

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: 全ファイル完全削除完了");
        }
    }

    /// <summary>
    /// 全ファイル削除イベント登録
    /// </summary>
    private void OnEnable()
    {
        TrashBoxDeletionManagement.AllFilesDeleted += AllFilesDeletedHandler;
    }

    /// <summary>
    /// 全ファイル削除イベント登録解除
    /// </summary>
    private void OnDisable()
    {
        TrashBoxDeletionManagement.AllFilesDeleted -= AllFilesDeletedHandler;
    }

    /// <summary>
    /// 全ファイル削除確認ダイアログ表示：全ファイル削除イベントが発火
    /// </summary>
    /// <param name="isAllFilesDeleted"></param>
    private void AllFilesDeletedHandler(bool isAllFilesDeleted)
    {
        // 全ファイル削除完了フラグを立てる
        allFilesDeleted = true;

        // 全ファイル削除確認ダイアログを表示
        ShowAllFilesDeleteConfirmation();
    }

    #endregion

    #region シーン遷移

    /// <summary>
    /// シーン遷移処理
    /// </summary>
    /// <param name="sceneName">遷移先シーン名</param>
    /// <returns>コルーチン</returns>
    private IEnumerator TransitionToScene(string sceneName)
    {
        isTransitioning = true;

        // フェードアウト処理
        // TODO: フェード演出の実装

        yield return new WaitForSeconds(fadeSpeed);

        // シーン遷移
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// タイトルシーンへ戻る
    /// </summary>
    public void ReturnToTitle()
    {
        if (!isTransitioning)
        {
            StartCoroutine(TransitionToScene(returnSceneName));
        }
    }

    #endregion

    #region 公開メソッド

    /// <summary>
    /// 設定パネルの表示/非表示切り替え
    /// </summary>
    public void ToggleSettingsPanel()
    {
        if (commonSettingsPanel != null)
        {
            bool isActive = commonSettingsPanel.activeSelf;
            commonSettingsPanel.SetActive(!isActive);

            if (soundManager != null)
            {
                soundManager.PlayClickSound();
            }
        }
    }

    /// <summary>
    /// 初期化状態の取得
    /// </summary>
    /// <returns>初期化済みの場合true</returns>
    public bool IsInitialized()
    {
        return isInitialized;
    }

    /// <summary>
    /// 削除ファイル数の取得
    /// </summary>
    /// <returns>削除ファイル数</returns>
    public int GetDeletedFileCount()
    {
        return deletedFileCount;
    }

    /// <summary>
    /// 総ファイル数の取得
    /// </summary>
    /// <returns>総ファイル数</returns>
    public int GetTotalFileCount()
    {
        return OrganizeMainScene_totalFileCount;
    }

    #endregion
}