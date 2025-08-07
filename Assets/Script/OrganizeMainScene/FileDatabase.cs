using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// OrganizeMainSceneで使用するファイルデータベース
/// MainSceneで登場した全ファイルの定義を保持する
/// </summary>
[CreateAssetMenu(fileName = "FileDatabase", menuName = "ExplorerGame/FileDatabase", order = 1)]
public class FileDatabase : ScriptableObject
{
    #region フィールド

    [Header("ファイルリスト設定")]
    [Tooltip("管理対象の全ファイルデータ")]
    [SerializeField] private List<FileData> allFiles = new List<FileData>();

    [Header("アイコン設定")]
    [Tooltip("TXTファイル用デフォルトアイコン")]
    [SerializeField] private Sprite defaultTxtIcon = null;

    [Tooltip("PNGファイル用デフォルトアイコン")]
    [SerializeField] private Sprite defaultPngIcon = null;

    [Tooltip("PDFファイル用デフォルトアイコン")]
    [SerializeField] private Sprite defaultPdfIcon = null;

    #endregion

    #region プロパティ

    /// <summary>
    /// 全ファイルリスト（読み取り専用）
    /// </summary>
    public IReadOnlyList<FileData> AllFiles => allFiles;

    /// <summary>
    /// ファイル総数
    /// </summary>
    public int FileCount => allFiles?.Count ?? 0;

    #endregion

    #region 初期化メソッド

    /// <summary>
    /// データベースの初期化（エディタ用）
    /// MainSceneのファイル構成に基づいて初期データを設定
    /// </summary>
    [ContextMenu("Initialize Database")]
    private void InitializeDatabase()
    {
        allFiles.Clear();

        // 思い出フォルダーのファイル
        AddFile("初めて見かけた日", FileData.FileType.TXT, "思い出");
        AddFile("カフェの彼女", FileData.FileType.PNG, "思い出");
        AddFile("彼女のこと", FileData.FileType.TXT, "思い出");
        AddFile("偶然の再会", FileData.FileType.TXT, "思い出");
        AddFile("通勤路", FileData.FileType.PNG, "思い出");

        // 恋人フォルダーのファイル
        AddFile("彼女との文通", FileData.FileType.TXT, "恋人");
        AddFile("初めての喧嘩？", FileData.FileType.TXT, "恋人");
        AddFile("窓越しの彼女", FileData.FileType.PNG, "恋人");
        AddFile("プレゼント", FileData.FileType.TXT, "恋人");
        AddFile("特別な場所", FileData.FileType.PNG, "恋人");

        // 友人フォルダーのファイル
        AddFile("親友からの忠告", FileData.FileType.TXT, "友人");
        AddFile("最後の警告", FileData.FileType.TXT, "友人");
        AddFile("喧嘩", FileData.FileType.PNG, "友人");
        AddFile("別れ話", FileData.FileType.TXT, "友人");
        AddFile("警察署前", FileData.FileType.PNG, "友人");

        // 記録フォルダーのファイル
        AddFile("警察記録", FileData.FileType.PDF, "記録");
        AddFile("診断書", FileData.FileType.PDF, "記録");
        AddFile("事故報告書", FileData.FileType.PDF, "記録");

        // 願いフォルダーのファイル
        AddFile("被害者の手記", FileData.FileType.PDF, "願い");
        AddFile("父親からの手紙", FileData.FileType.PDF, "願い");


        Debug.Log($"FileDatabase: {allFiles.Count}個のファイルを初期化しました");
    }

    /// <summary>
    /// ファイルを追加するヘルパーメソッド
    /// </summary>
    private void AddFile(string fileName, FileData.FileType fileType, string folderName)
    {
        var fileData = new FileData(fileName, fileType, folderName);

        // アイコンの自動設定
        switch (fileType)
        {
            case FileData.FileType.TXT:
                if (defaultTxtIcon != null)
                {
                    // リフレクションやSerializedObjectを使用してiconSpriteを設定
                    // （ScriptableObjectのため、直接設定はエディタでのみ可能）
                }
                break;
            case FileData.FileType.PNG:
                // defaultPngIconを設定
                break;
            case FileData.FileType.PDF:
                // defaultPdfIconを設定
                break;
        }

        allFiles.Add(fileData);
    }

    #endregion

    #region 検索メソッド

    /// <summary>
    /// ファイル名でファイルデータを検索
    /// </summary>
    /// <param name="fileName">検索するファイル名</param>
    /// <returns>見つかったファイルデータ、見つからない場合はnull</returns>
    public FileData GetFileByName(string fileName)
    {
        return allFiles?.FirstOrDefault(f => f.FileName == fileName);
    }

    /// <summary>
    /// フォルダー名でファイルを取得
    /// </summary>
    /// <param name="folderName">フォルダー名</param>
    /// <returns>該当フォルダーのファイルリスト</returns>
    public List<FileData> GetFilesByFolder(string folderName)
    {
        return allFiles?.Where(f => f.FolderName == folderName).ToList() ?? new List<FileData>();
    }

    /// <summary>
    /// ファイルタイプでファイルを取得
    /// </summary>
    /// <param name="fileType">ファイルタイプ</param>
    /// <returns>該当タイプのファイルリスト</returns>
    public List<FileData> GetFilesByType(FileData.FileType fileType)
    {
        return allFiles?.Where(f => f.Type == fileType).ToList() ?? new List<FileData>();
    }

    /// <summary>
    /// 表示可能なファイルのみを取得
    /// </summary>
    /// <returns>表示可能なファイルリスト</returns>
    public List<FileData> GetVisibleFiles()
    {
        return allFiles?.Where(f => f.IsVisible).OrderBy(f => f.DisplayOrder).ToList() ?? new List<FileData>();
    }

    /// <summary>
    /// 削除済みファイルを取得
    /// </summary>
    /// <returns>削除済みファイルリスト</returns>
    public List<FileData> GetDeletedFiles()
    {
        return allFiles?.Where(f => f.IsDeleted && !f.IsPermanentlyDeleted).ToList() ?? new List<FileData>();
    }

    #endregion

    #region 状態管理メソッド

    /// <summary>
    /// 全ファイルの削除状態をリセット
    /// </summary>
    public void ResetAllFileStates()
    {
        if (allFiles == null) return;

        foreach (var file in allFiles)
        {
            file.Restore();
        }

        Debug.Log("FileDatabase: 全ファイルの削除状態をリセットしました");
    }

    /// <summary>
    /// 削除状態を復元
    /// </summary>
    /// <param name="deletedFileNames">削除済みファイル名のリスト</param>
    public void RestoreDeletedState(List<string> deletedFileNames)
    {
        if (allFiles == null || deletedFileNames == null) return;

        foreach (var fileName in deletedFileNames)
        {
            var file = GetFileByName(fileName);
            if (file != null)
            {
                file.Delete();
            }
        }
    }

    /// <summary>
    /// 全ファイルが削除されているかチェック
    /// </summary>
    /// <returns>全ファイル削除済みの場合true</returns>
    public bool AreAllFilesDeleted()
    {
        if (allFiles == null || allFiles.Count == 0) return false;

        return allFiles.All(f => f.IsDeleted || f.IsPermanentlyDeleted);
    }

    #endregion

    #region デバッグメソッド

    /// <summary>
    /// データベースの内容をデバッグログに出力
    /// </summary>
    [ContextMenu("Debug Print Database")]
    private void DebugPrintDatabase()
    {
        if (allFiles == null || allFiles.Count == 0)
        {
            Debug.Log("FileDatabase: ファイルが登録されていません");
            return;
        }

        Debug.Log($"FileDatabase: {allFiles.Count}個のファイル");

        var folders = allFiles.GroupBy(f => f.FolderName);
        foreach (var folder in folders)
        {
            Debug.Log($"  フォルダー: {folder.Key}");
            foreach (var file in folder)
            {
                Debug.Log($"    - {file.GetFileNameWithExtension()} (削除: {file.IsDeleted})");
            }
        }
    }

    #endregion
}