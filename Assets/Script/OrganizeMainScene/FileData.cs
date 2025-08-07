using System;
using UnityEngine;

/// <summary>
/// OrganizeMainSceneで管理するファイルの基本データ構造
/// </summary>
[Serializable]
public class FileData
{
    #region ファイルタイプ定義

    /// <summary>
    /// ファイルの種類を表す列挙型
    /// </summary>
    public enum FileType
    {
        TXT,  // テキストファイル
        PNG,  // 画像ファイル
        PDF   // PDFファイル
    }

    #endregion

    #region フィールド

    [Header("基本情報")]
    [Tooltip("ファイル名")]
    [SerializeField] private string fileName = "";

    [Tooltip("ファイルタイプ")]
    [SerializeField] private FileType fileType = FileType.TXT;

    [Tooltip("所属フォルダー名")]
    [SerializeField] private string folderName = "";

    [Header("状態管理")]
    [Tooltip("削除済みフラグ（非表示状態）")]
    [SerializeField] private bool isDeleted = false;

    [Tooltip("完全削除済みフラグ")]
    [SerializeField] private bool isPermanentlyDeleted = false;

    [Header("表示設定")]
    [Tooltip("アイコンスプライト")]
    [SerializeField] private Sprite iconSprite = null;

    [Tooltip("表示順序")]
    [SerializeField] private int displayOrder = 0;

    #endregion

    #region プロパティ

    /// <summary>
    /// ファイル名
    /// </summary>
    public string FileName => fileName;

    /// <summary>
    /// ファイルタイプ
    /// </summary>
    public FileType Type => fileType;

    /// <summary>
    /// 所属フォルダー名
    /// </summary>
    public string FolderName => folderName;

    /// <summary>
    /// 削除済みかどうか
    /// </summary>
    public bool IsDeleted
    {
        get => isDeleted;
        set => isDeleted = value;
    }

    /// <summary>
    /// 完全削除済みかどうか
    /// </summary>
    public bool IsPermanentlyDeleted
    {
        get => isPermanentlyDeleted;
        set => isPermanentlyDeleted = value;
    }

    /// <summary>
    /// アイコンスプライト
    /// </summary>
    public Sprite IconSprite => iconSprite;

    /// <summary>
    /// 表示順序
    /// </summary>
    public int DisplayOrder => displayOrder;

    /// <summary>
    /// ファイルが表示可能かどうか
    /// </summary>
    public bool IsVisible => !isDeleted && !isPermanentlyDeleted;

    /// <summary>
    /// ファイルの完全な識別子（フォルダー名/ファイル名）
    /// </summary>
    public string FullPath => string.IsNullOrEmpty(folderName) ? fileName : $"{folderName}/{fileName}";

    #endregion

    #region コンストラクタ

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public FileData()
    {
    }

    /// <summary>
    /// 基本情報を指定するコンストラクタ
    /// </summary>
    /// <param name="fileName">ファイル名</param>
    /// <param name="fileType">ファイルタイプ</param>
    /// <param name="folderName">所属フォルダー名</param>
    public FileData(string fileName, FileType fileType, string folderName = "")
    {
        this.fileName = fileName;
        this.fileType = fileType;
        this.folderName = folderName;
    }

    #endregion

    #region メソッド

    /// <summary>
    /// ファイルを削除済みにする（非表示化）
    /// </summary>
    public void Delete()
    {
        isDeleted = true;
    }

    /// <summary>
    /// ファイルを完全削除する
    /// </summary>
    public void DeletePermanently()
    {
        isPermanentlyDeleted = true;
    }

    /// <summary>
    /// 削除状態をリセットする
    /// </summary>
    public void Restore()
    {
        isDeleted = false;
        isPermanentlyDeleted = false;
    }

    /// <summary>
    /// ファイル拡張子を取得
    /// </summary>
    /// <returns>拡張子文字列</returns>
    public string GetExtension()
    {
        switch (fileType)
        {
            case FileType.TXT:
                return ".txt";
            case FileType.PNG:
                return ".png";
            case FileType.PDF:
                return ".pdf";
            default:
                return "";
        }
    }

    /// <summary>
    /// ファイル名（拡張子付き）を取得
    /// </summary>
    /// <returns>拡張子付きファイル名</returns>
    public string GetFileNameWithExtension()
    {
        return fileName + GetExtension();
    }

    /// <summary>
    /// デバッグ用の文字列表現
    /// </summary>
    /// <returns>デバッグ文字列</returns>
    public override string ToString()
    {
        return $"FileData: {FullPath} ({fileType}), Deleted: {isDeleted}, Permanently: {isPermanentlyDeleted}";
    }

    #endregion
}