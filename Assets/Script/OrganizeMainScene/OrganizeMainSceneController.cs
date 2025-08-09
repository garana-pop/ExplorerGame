using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private RectTransform fileScrollView;

    [Tooltip("ファイル一覧のコンテンツパネル")]
    [SerializeField] private RectTransform fileContentPanel;

    [Tooltip("ゴミ箱オブジェクト")]
    [SerializeField] private GameObject trashBinObject;

    [Tooltip("メッセージパネル")]
    [SerializeField] private GameObject messagePanel;

    [Tooltip("メッセージテキスト")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Tooltip("確認ダイアログパネル")]
    [SerializeField] private GameObject confirmationPanel;

    [Tooltip("確認ダイアログテキスト")]
    [SerializeField] private TextMeshProUGUI confirmationText;

    [Tooltip("共通設定パネル")]
    [SerializeField] private GameObject commonSettingsPanel;

    [Header("マネージャー参照")]
    [Tooltip("ファイル管理マネージャー")]
    [SerializeField] private FileManager fileManager;

    [Tooltip("セーブデータ管理マネージャー")]
    [SerializeField] private GameSaveManager saveManager;

    [Tooltip("サウンドエフェクト管理マネージャー")]
    [SerializeField] private SoundEffectManager soundManager;

    [Header("シーン設定")]
    [Tooltip("戻る際の遷移先シーン名")]
    [SerializeField] private string returnSceneName = "TitleScene";

    [Tooltip("フェード速度")]
    [SerializeField] private float fadeSpeed = 1.0f;

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
    }

    /// <summary>
    /// Startメソッド - シーン開始時の処理
    /// </summary>
    private void Start()
    {
        // 初期化処理
        StartCoroutine(InitializeScene());
    }

    /// <summary>
    /// OnDestroyメソッド - オブジェクト破棄時の処理
    /// </summary>
    private void OnDestroy()
    {
        // シングルトンインスタンスのクリア
        if (instance == this)
        {
            instance = null;
        }
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
    /// シーン全体の初期化処理
    /// </summary>
    /// <returns>コルーチン</returns>
    private IEnumerator InitializeScene()
    {

        // マネージャーの取得
        yield return InitializeManagers();

        // UIの初期化
        InitializeUI();

        // セーブデータの読み込み
        LoadSaveData();

        // 初期化完了
        isInitialized = true;

    }

    /// <summary>
    /// マネージャーの初期化と取得
    /// </summary>
    /// <returns>コルーチン</returns>
    private IEnumerator InitializeManagers()
    {
        // GameSaveManagerの取得
        if (saveManager == null)
        {
            saveManager = GameSaveManager.Instance;
            if (saveManager == null && debugMode)
            {
                Debug.LogWarning($"{nameof(OrganizeMainSceneController)}: GameSaveManagerが見つかりません");
            }
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

        // FileManagerの取得（未実装の場合はスキップ）
        if (fileManager == null)
        {
            fileManager = GetComponent<FileManager>();
            if (fileManager == null)
            {
                //fileManager = FindFirstObjectByType<FileManager>();
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

    #endregion

    #region セーブデータ処理

    /// <summary>
    /// セーブデータの読み込み
    /// </summary>
    private void LoadSaveData()
    {
        if (saveManager == null)
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(OrganizeMainSceneController)}: GameSaveManagerが設定されていません");
            }
            return;
        }

        // TODO: セーブデータから削除済みファイル情報を読み込む
        // この処理はGameSaveDataの拡張後に実装

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: セーブデータ読み込み完了");
        }
    }

    /// <summary>
    /// セーブデータの保存
    /// </summary>
    public void SaveData()
    {
        if (saveManager == null)
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(OrganizeMainSceneController)}: GameSaveManagerが設定されていません");
            }
            return;
        }

        // TODO: 削除済みファイル情報をセーブデータに保存
        // この処理はGameSaveDataの拡張後に実装

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: セーブデータ保存完了");
        }
    }

    #endregion

    #region ファイル管理

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
        }

        // TODO: 対応するファイルアイテムを非表示にする

        // 全ファイル削除チェック
        CheckAllFilesDeleted();

        // セーブデータを更新
        SaveData();

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: ファイル '{fileName}' を削除しました");
        }
    }

    /// <summary>
    /// 全ファイル削除確認
    /// </summary>
    private void CheckAllFilesDeleted()
    {
        // TODO: 全ファイルが削除されたかチェック
        // この処理はファイル一覧機能実装後に詳細実装

        if (allFilesDeleted && !isTransitioning)
        {
            ShowAllFilesDeleteConfirmation();
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

        messageText.text = message;
        messagePanel.SetActive(true);

        // 自動非表示タイマー
        StartCoroutine(HideMessageAfterDelay(duration));

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: メッセージ表示: {message}");
        }
    }

    /// <summary>
    /// メッセージを指定時間後に非表示にする
    /// </summary>
    /// <param name="delay">遅延時間（秒）</param>
    /// <returns>コルーチン</returns>
    private IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }
    }

    /// <summary>
    /// 全ファイル削除確認ダイアログの表示
    /// </summary>
    private void ShowAllFilesDeleteConfirmation()
    {
        if (confirmationPanel == null || confirmationText == null)
        {
            return;
        }

        confirmationText.text = "すべてのファイルを完全に削除しますか？";
        confirmationPanel.SetActive(true);

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: 全ファイル削除確認ダイアログを表示");
        }
    }

    #endregion

    #region ボタンイベント

    /// <summary>
    /// 確認ダイアログの「はい」ボタン押下時の処理
    /// </summary>
    public void OnConfirmYesClicked()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }

        // 全ファイル完全削除処理
        CompleteAllFilesDelete();

        // ボタンクリック音
        if (soundManager != null)
        {
            soundManager.PlayClickSound();
        }
    }

    /// <summary>
    /// 確認ダイアログの「いいえ」ボタン押下時の処理
    /// </summary>
    public void OnConfirmNoClicked()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }

        // ボタンクリック音
        if (soundManager != null)
        {
            soundManager.PlayClickSound();
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: 全ファイル削除をキャンセルしました");
        }
    }

    /// <summary>
    /// 戻るボタン押下時の処理
    /// </summary>
    public void OnBackButtonClicked()
    {
        if (isTransitioning)
        {
            return;
        }

        // ボタンクリック音
        if (soundManager != null)
        {
            soundManager.PlayClickSound();
        }

        // TitleSceneへ遷移
        StartCoroutine(TransitionToScene(returnSceneName));
    }

    /// <summary>
    /// 保存して終了ボタン押下時の処理
    /// </summary>
    public void OnSaveAndQuitClicked()
    {
        // データを保存
        SaveData();

        // ボタンクリック音
        if (soundManager != null)
        {
            soundManager.PlayClickSound();
        }

        // アプリケーション終了
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region 完全削除処理

    /// <summary>
    /// 全ファイル完全削除処理
    /// </summary>
    private void CompleteAllFilesDelete()
    {
        // セーブデータに完全削除フラグを記録
        allFilesDeleted = true;
        SaveData();

        // BGM変更処理（SoundEffectManagerに新BGM切り替えメソッドが実装された後に対応）
        // TODO: soundManager.ChangeToBGMForComplete();

        // Steam実績解除（Steam API実装後に対応）
        // TODO: UnlockSteamAchievement("前へ");

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: 全ファイル完全削除完了");
        }
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

        // フェードアウト処理（実装がある場合）
        // TODO: フェード処理の実装

        yield return new WaitForSeconds(fadeSpeed);

        // シーン遷移
        SceneManager.LoadScene(sceneName);
    }

    #endregion

    #region パブリックメソッド

    /// <summary>
    /// 初期化完了状態を取得
    /// </summary>
    /// <returns>初期化が完了している場合はtrue</returns>
    public bool IsInitialized()
    {
        return isInitialized;
    }

    /// <summary>
    /// 削除済みファイルリストを取得
    /// </summary>
    /// <returns>削除済みファイル名のリスト</returns>
    public List<string> GetDeletedFiles()
    {
        return new List<string>(deletedFiles);
    }

    /// <summary>
    /// ゴミ箱クリック時の処理
    /// </summary>
    public void OnTrashBinClicked()
    {
        ShowMessage("削除したいファイルをドラッグ&ドロップしてください。", 3.0f);

        // クリック音
        if (soundManager != null)
        {
            soundManager.PlayClickSound();
        }
    }

    #endregion
}