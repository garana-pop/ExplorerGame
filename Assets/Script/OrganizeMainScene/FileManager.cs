using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// OrganizeMainSceneでのファイル管理を行うクラス
/// </summary>
public class FileManager : MonoBehaviour
{
    #region Inspector設定用フィールド

    [Header("ファイルアイテム設定")]
    [SerializeField] private List<GameObject> defaultFileItems = new List<GameObject>(); // インスペクターで設定するデフォルトファイル
    [SerializeField] private Transform fileContentPanel; // ファイル表示エリアのTransform

    [Header("削除アニメーション設定")]
    [SerializeField] private bool useDeleteAnimation = true; // 削除アニメーション使用フラグ
    [SerializeField] private float fadeOutDuration = 0.5f; // フェードアウト時間

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false; // デバッグモード

    #endregion

    #region プライベートフィールド

    private List<GameObject> collectedFileItems = new List<GameObject>(); // 収集したファイルアイテム
    private List<string> deletedFiles = new List<string>(); // 削除済みファイル名リスト
    private int totalFileCount = 0; // 総ファイル数
    private int activeFileCount = 0; // アクティブなファイル数

    #endregion

    #region Unityイベント

    /// <summary>
    /// 初期化処理
    /// </summary>
    private void Awake()
    {
        // デフォルトファイルがインスペクターで設定されていれば使用
        if (defaultFileItems != null && defaultFileItems.Count > 0)
        {
            collectedFileItems.AddRange(defaultFileItems);
            totalFileCount = defaultFileItems.Count;
            activeFileCount = totalFileCount;

            if (debugMode)
            {
                DebugLogger.Log($"{nameof(FileManager)}: インスペクターから{defaultFileItems.Count}個のファイルアイテムを設定しました");
            }
        }
    }

    /// <summary>
    /// 開始時処理
    /// </summary>
    private void Start()
    {
        // TitleSceneからの遷移時にファイルを収集
        if (collectedFileItems.Count == 0)
        {
            CollectFileItems();
        }

        // セーブデータから削除済みファイルを復元
        RestoreDeletedFilesFromSave();
    }

    #endregion

    #region ファイル収集

    /// <summary>
    /// ファイルアイテムを収集（外部から呼び出し可能）
    /// </summary>
    public void CollectFileItems()
    {
        collectedFileItems.Clear();
        totalFileCount = 0;
        activeFileCount = 0;

        // インスペクターで設定されたデフォルトファイルを優先使用
        if (defaultFileItems != null && defaultFileItems.Count > 0)
        {
            foreach (GameObject fileItem in defaultFileItems)
            {
                if (fileItem != null)
                {
                    collectedFileItems.Add(fileItem);
                    totalFileCount++;
                    if (fileItem.activeSelf)
                    {
                        activeFileCount++;
                    }
                }
            }

            if (debugMode)
            {
                DebugLogger.Log($"{nameof(FileManager)}: インスペクター設定により{totalFileCount}個のファイルアイテムを収集しました");
            }
            return;
        }

        if (debugMode)
        {
            DebugLogger.Log($"{nameof(FileManager)}: {totalFileCount}個のファイルアイテムを収集しました");
        }
    }

    /// <summary>
    /// Transformから再帰的にファイルアイテムを収集
    /// </summary>
    private void CollectFromTransform(Transform parent)
    {
        foreach (Transform child in parent)
        {
            // ファイルアイテムかどうかを判定
            if (IsFileItem(child.gameObject))
            {
                collectedFileItems.Add(child.gameObject);
                totalFileCount++;
                if (child.gameObject.activeSelf)
                {
                    activeFileCount++;
                }
            }

            // 子要素も再帰的に検索
            if (child.childCount > 0)
            {
                CollectFromTransform(child);
            }
        }
    }


    /// <summary>
    /// オブジェクトがファイルアイテムかどうかを判定
    /// </summary>
    private bool IsFileItem(GameObject obj)
    {
        // ファイルアイテムの判定条件
        return obj.name.Contains("ファイル-") ||
               obj.name.Contains(".txt") ||
               obj.name.Contains(".png") ||
               obj.name.Contains(".pdf");
    }

    #endregion

    #region 削除済みファイル管理

    /// <summary>
    /// 削除済みファイルリストを設定
    /// </summary>
    public void SetDeletedFiles(List<string> deletedFileList)
    {
        if (deletedFileList == null)
        {
            return;
        }

        deletedFiles = new List<string>(deletedFileList);
        ApplyDeletedFiles();

        if (debugMode)
        {
            DebugLogger.Log($"{nameof(FileManager)}: {deletedFiles.Count}個の削除済みファイルを設定しました");
        }
    }

    /// <summary>
    /// セーブデータから削除済みファイルを復元
    /// </summary>
    private void RestoreDeletedFilesFromSave()
    {
        GameSaveManager saveManager = GameSaveManager.Instance;
        if (saveManager != null)
        {
            var organizeData = saveManager.GetOrganizeSceneData();
            if (organizeData != null && organizeData.deletedFiles != null)
            {
                SetDeletedFiles(organizeData.deletedFiles);
            }
        }
    }

    /// <summary>
    /// 削除済みファイルを画面に反映
    /// </summary>
    private void ApplyDeletedFiles()
    {
        foreach (string fileName in deletedFiles)
        {
            GameObject fileObject = FindFileObject(fileName);
            if (fileObject != null)
            {
                fileObject.SetActive(false);
                activeFileCount--;
            }
        }
    }

    /// <summary>
    /// ファイル名からGameObjectを検索
    /// </summary>
    private GameObject FindFileObject(string fileName)
    {
        // 収集済みリストから検索
        foreach (GameObject obj in collectedFileItems)
        {
            if (obj != null && obj.name == fileName)
            {
                return obj;
            }
        }

        // 見つからない場合はシーン全体から検索
        GameObject foundObject = GameObject.Find(fileName);
        if (foundObject != null)
        {
            collectedFileItems.Add(foundObject);
            return foundObject;
        }

        return null;
    }

    #endregion

    #region ファイル削除処理

    /// <summary>
    /// ファイルを削除（非表示化）
    /// </summary>
    public bool DeleteFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        // 既に削除済みの場合
        if (deletedFiles.Contains(fileName))
        {
            if (debugMode)
            {
                DebugLogger.Log($"{nameof(FileManager)}: ファイル '{fileName}' は既に削除済みです");
            }
            return false;
        }

        GameObject fileObject = FindFileObject(fileName);
        if (fileObject == null)
        {
            if (debugMode)
            {
                DebugLogger.LogWarning($"{nameof(FileManager)}: ファイル '{fileName}' が見つかりません");
            }
            return false;
        }

        // 削除処理実行
        if (useDeleteAnimation)
        {
            StartCoroutine(DeleteFileWithAnimation(fileObject, fileName));
        }
        else
        {
            DeleteFileImmediate(fileObject, fileName);
        }

        return true;
    }

    /// <summary>
    /// ファイルを即座に削除
    /// </summary>
    private void DeleteFileImmediate(GameObject fileObject, string fileName)
    {
        if (fileObject != null)
        {
            fileObject.SetActive(false);
        }

        // ファイル名が重複しないようチェック
        if (!deletedFiles.Contains(fileName))
        {
            deletedFiles.Add(fileName);
            activeFileCount--;
        }

        if (debugMode)
        {
            DebugLogger.Log($"{nameof(FileManager)}: ファイル '{fileName}' を削除しました");
        }
    }

    /// <summary>
    /// アニメーション付きでファイルを削除
    /// </summary>
    private IEnumerator DeleteFileWithAnimation(GameObject fileObject, string fileName)
    {
        // nullチェックを追加
        if (fileObject == null)
        {
            // オブジェクトが既に破棄されている場合、削除状態のみ更新
            if (!deletedFiles.Contains(fileName))
            {
                deletedFiles.Add(fileName);
                activeFileCount--;
            }

            if (debugMode)
            {
                DebugLogger.Log($"{nameof(FileManager)}: ファイル '{fileName}' は既に破棄されていましたが、削除状態を更新しました");
            }
            yield break;
        }

        CanvasGroup canvasGroup = fileObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = fileObject.AddComponent<CanvasGroup>();
        }

        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;

        // アニメーション中にオブジェクトが破棄されないよう、毎フレームnullチェック
        while (elapsedTime < fadeOutDuration)
        {
            // オブジェクトまたはCanvasGroupが破棄されていたら終了
            if (fileObject == null || canvasGroup == null)
            {
                break;
            }

            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeOutDuration;

            // 安全にalphaを設定
            try
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);
            }
            catch (System.Exception)
            {
                // CanvasGroupが破棄された場合、ログを出さずに終了
                break;
            }

            yield return null;
        }

        // 最終的な削除処理
        DeleteFileImmediate(fileObject, fileName);
    }

    #endregion

    #region 公開プロパティ

    /// <summary>
    /// 収集したファイルアイテムのリスト
    /// </summary>
    public List<GameObject> CollectedFileItems => collectedFileItems;

    /// <summary>
    /// 削除済みファイル名のリスト
    /// </summary>
    public List<string> DeletedFiles => deletedFiles;

    /// <summary>
    /// 総ファイル数
    /// </summary>
    public int TotalFileCount => totalFileCount;

    /// <summary>
    /// アクティブなファイル数
    /// </summary>
    public int ActiveFileCount => activeFileCount;

    /// <summary>
    /// すべてのファイルが削除されたか
    /// </summary>
    public bool AllFilesDeleted => activeFileCount <= 0;

    #endregion
}