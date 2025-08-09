using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// OrganizeMainScene内のファイルの削除状態（非表示/完全削除）を管理するクラス
/// 既存のDraggableFileやFileHighlighterなどのコンポーネントと連携して動作
/// </summary>
public class FileManager : MonoBehaviour
{
    #region シングルトン実装

    // シングルトンインスタンス
    private static FileManager instance;

    /// <summary>
    /// FileManagerのシングルトンインスタンス
    /// </summary>
    public static FileManager Instance
    {
        get
        {
            if (instance == null)
            {
                // Unity 6の新機能を使用 - 非アクティブオブジェクトも含めて検索
                instance = FindFirstObjectByType<FileManager>(FindObjectsInactive.Include);

                if (instance == null && Application.isPlaying)
                {
                    Debug.LogWarning("FileManager: インスタンスが見つかりません。新規作成します。");
                    GameObject go = new GameObject("FileManager");
                    instance = go.AddComponent<FileManager>();
                }
            }
            return instance;
        }
    }

    #endregion

    #region インスペクター設定

    [Header("ファイル管理設定")]
    [Tooltip("ファイル削除時のフェードアウト時間")]
    [SerializeField] private float fadeOutDuration = 0.5f;

    [Tooltip("ファイル削除時のアニメーション使用フラグ")]
    [SerializeField] private bool useDeleteAnimation = true;

    [Header("デバッグ設定")]
    [Tooltip("デバッグログを表示するか")]
    [SerializeField] private bool debugMode = false;

    #endregion

    #region プライベート変数

    // 管理対象のファイルアイテムリスト
    private Dictionary<string, GameObject> fileItems;

    // 削除済みファイルのリスト
    private List<string> deletedFiles;

    // 表示中のファイル数
    private int activeFileCount;

    // 初期化済みフラグ
    private bool isInitialized = false;

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
                Debug.LogWarning($"{nameof(FileManager)}: 既存のインスタンスが存在します。このオブジェクトを破棄します。");
            }
            Destroy(gameObject);
            return;
        }

        // 初期化
        InitializeManager();
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
    /// マネージャーの初期化
    /// </summary>
    private void InitializeManager()
    {
        fileItems = new Dictionary<string, GameObject>();
        deletedFiles = new List<string>();
        activeFileCount = 0;
        isInitialized = true;
    }

    /// <summary>
    /// シーン内のファイルアイテムを収集して管理下に置く
    /// </summary>
    public void CollectFileItems()
    {
        if (!isInitialized)
        {
            InitializeManager();
        }

        // 既存のリストをクリア
        fileItems.Clear();
        activeFileCount = 0;

        // FilePanelタグを持つオブジェクトを検索
        GameObject[] filePanels = GameObject.FindGameObjectsWithTag("FilePanel");

        // タグがない場合は名前で検索
        if (filePanels.Length == 0)
        {
            // すべてのGameObjectから "FilePanel" を含む名前のオブジェクトを検索
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            List<GameObject> foundPanels = new List<GameObject>();

            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("FilePanel") || obj.name.Contains(".png") ||
                    obj.name.Contains(".txt") || obj.name.Contains(".pdf"))
                {
                    // DraggableFileコンポーネントを持っているか確認
                    DraggableFile draggable = obj.GetComponent<DraggableFile>();
                    if (draggable != null)
                    {
                        RegisterFileItem(obj);
                    }
                }
            }
        }
        else
        {
            // タグで見つかった場合
            foreach (GameObject panel in filePanels)
            {
                RegisterFileItem(panel);
            }
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(FileManager)}: {fileItems.Count}個のファイルアイテムを収集しました");
        }
    }

    /// <summary>
    /// ファイルアイテムを管理リストに登録
    /// </summary>
    /// <param name="fileObject">登録するファイルオブジェクト</param>
    private void RegisterFileItem(GameObject fileObject)
    {
        if (fileObject == null) return;

        string fileName = fileObject.name;

        // 重複チェック
        if (!fileItems.ContainsKey(fileName))
        {
            fileItems.Add(fileName, fileObject);

            // アクティブなファイルをカウント
            if (fileObject.activeSelf)
            {
                activeFileCount++;
            }

            if (debugMode)
            {
                Debug.Log($"{nameof(FileManager)}: ファイル '{fileName}' を登録しました");
            }
        }
    }

    #endregion

    #region ファイル削除処理

    /// <summary>
    /// ファイルを削除（非表示化）する
    /// </summary>
    /// <param name="fileName">削除するファイル名</param>
    /// <returns>削除成功時はtrue</returns>
    public bool DeleteFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        // ファイルが管理リストに存在するか確認
        if (!fileItems.ContainsKey(fileName))
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(FileManager)}: ファイル '{fileName}' が見つかりません");
            }
            return false;
        }

        GameObject fileObject = fileItems[fileName];

        // 既に削除済みの場合
        if (deletedFiles.Contains(fileName))
        {
            if (debugMode)
            {
                Debug.Log($"{nameof(FileManager)}: ファイル '{fileName}' は既に削除済みです");
            }
            return false;
        }

        // 削除処理実行
        if (useDeleteAnimation)
        {
            // アニメーション付き削除
            StartCoroutine(DeleteFileWithAnimation(fileObject, fileName));
        }
        else
        {
            // 即座に削除
            DeleteFileImmediate(fileObject, fileName);
        }

        return true;
    }

    /// <summary>
    /// ファイルを即座に削除（非表示化）
    /// </summary>
    /// <param name="fileObject">削除するオブジェクト</param>
    /// <param name="fileName">ファイル名</param>
    private void DeleteFileImmediate(GameObject fileObject, string fileName)
    {
        // オブジェクトを非表示にする
        fileObject.SetActive(false);

        // 削除済みリストに追加
        deletedFiles.Add(fileName);

        // アクティブファイル数を減らす
        activeFileCount--;

        // OrganizeMainSceneControllerに通知
        if (OrganizeMainSceneController.Instance != null)
        {
            OrganizeMainSceneController.Instance.DeleteFile(fileName);
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(FileManager)}: ファイル '{fileName}' を削除しました");
        }
    }

    /// <summary>
    /// アニメーション付きでファイルを削除
    /// </summary>
    /// <param name="fileObject">削除するオブジェクト</param>
    /// <param name="fileName">ファイル名</param>
    /// <returns>コルーチン</returns>
    private IEnumerator DeleteFileWithAnimation(GameObject fileObject, string fileName)
    {
        // CanvasGroupコンポーネントを取得または追加
        CanvasGroup canvasGroup = fileObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = fileObject.AddComponent<CanvasGroup>();
        }

        // フェードアウトアニメーション
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsedTime < fadeOutDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeOutDuration;

            // アルファ値を徐々に減らす
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);

            // スケールも小さくする（オプション）
            fileObject.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, progress * 0.5f);

            yield return null;
        }

        // 最終的に削除
        DeleteFileImmediate(fileObject, fileName);
    }

    #endregion

    #region ファイル復元処理

    /// <summary>
    /// 削除したファイルを復元する
    /// </summary>
    /// <param name="fileName">復元するファイル名</param>
    /// <returns>復元成功時はtrue</returns>
    public bool RestoreFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        // ファイルが削除済みリストに存在するか確認
        if (!deletedFiles.Contains(fileName))
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(FileManager)}: ファイル '{fileName}' は削除されていません");
            }
            return false;
        }

        // ファイルオブジェクトを取得
        if (!fileItems.ContainsKey(fileName))
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(FileManager)}: ファイル '{fileName}' が管理リストに存在しません");
            }
            return false;
        }

        GameObject fileObject = fileItems[fileName];

        // オブジェクトを表示
        fileObject.SetActive(true);

        // スケールとアルファ値を元に戻す
        fileObject.transform.localScale = Vector3.one;
        CanvasGroup canvasGroup = fileObject.GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        // 削除済みリストから削除
        deletedFiles.Remove(fileName);

        // アクティブファイル数を増やす
        activeFileCount++;

        if (debugMode)
        {
            Debug.Log($"{nameof(FileManager)}: ファイル '{fileName}' を復元しました");
        }

        return true;
    }

    #endregion

    #region 状態管理

    /// <summary>
    /// 削除済みファイルリストを取得
    /// </summary>
    /// <returns>削除済みファイル名のリスト</returns>
    public List<string> GetDeletedFiles()
    {
        return new List<string>(deletedFiles);
    }

    /// <summary>
    /// 削除済みファイルリストを設定（セーブデータ読み込み用）
    /// </summary>
    /// <param name="files">削除済みファイル名のリスト</param>
    public void SetDeletedFiles(List<string> files)
    {
        if (files == null) return;

        deletedFiles = new List<string>(files);

        // 削除済みファイルを非表示にする
        foreach (string fileName in deletedFiles)
        {
            if (fileItems.ContainsKey(fileName))
            {
                GameObject fileObject = fileItems[fileName];
                if (fileObject != null)
                {
                    fileObject.SetActive(false);
                    activeFileCount--;
                }
            }
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(FileManager)}: {deletedFiles.Count}個の削除済みファイルを設定しました");
        }
    }

    /// <summary>
    /// すべてのファイルが削除されたかチェック
    /// </summary>
    /// <returns>すべて削除済みの場合はtrue</returns>
    public bool AreAllFilesDeleted()
    {
        return activeFileCount <= 0;
    }

    /// <summary>
    /// アクティブなファイル数を取得
    /// </summary>
    /// <returns>表示中のファイル数</returns>
    public int GetActiveFileCount()
    {
        return activeFileCount;
    }

    /// <summary>
    /// 管理中のファイル総数を取得
    /// </summary>
    /// <returns>管理中のファイル総数</returns>
    public int GetTotalFileCount()
    {
        return fileItems.Count;
    }

    #endregion

    #region ユーティリティ

    /// <summary>
    /// すべてのファイルをクリア（初期化）
    /// </summary>
    public void ClearAllFiles()
    {
        fileItems.Clear();
        deletedFiles.Clear();
        activeFileCount = 0;

        if (debugMode)
        {
            Debug.Log($"{nameof(FileManager)}: すべてのファイル情報をクリアしました");
        }
    }

    /// <summary>
    /// ファイルが存在するかチェック
    /// </summary>
    /// <param name="fileName">チェックするファイル名</param>
    /// <returns>存在する場合はtrue</returns>
    public bool FileExists(string fileName)
    {
        return fileItems.ContainsKey(fileName);
    }

    /// <summary>
    /// ファイルが削除済みかチェック
    /// </summary>
    /// <param name="fileName">チェックするファイル名</param>
    /// <returns>削除済みの場合はtrue</returns>
    public bool IsFileDeleted(string fileName)
    {
        return deletedFiles.Contains(fileName);
    }

    #endregion
}