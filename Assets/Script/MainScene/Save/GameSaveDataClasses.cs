using System;
using System.Collections.Generic;
using static WindowPosition;

/// <summary>
/// ゲームのセーブデータ構造クラス群
/// </summary>
[Serializable]
public class GameSaveData
{
    /// <summary>ゲームのバージョン</summary>
    public string gameVersion;

    /// <summary>セーブした日時（ISO 8601形式）</summary>
    public string saveTimestamp;

    /// <summary>フォルダの状態</summary>
    public FolderState folderState;

    /// <summary>ファイル進捗データ</summary>
    public FileProgressData fileProgress;

    /// <summary>オーディオ設定</summary>
    public AudioSettings audioSettings;

    /// <summary>OpeningScene→MainSceneに移行完了判定フラグ</summary>
    public bool endOpeningScene = false;

    /// <summary>タイトルが"「彼女」の記憶"に変更フラグ</summary>
    public bool afterChangeToHerMemory = false;

    /// <summary>似顔絵削除後のフラグ</summary>
    public bool afterChangeToHisFuture = false;  //

    /// <summary>肖像画が削除されたかどうかのフラグ</summary>
    public bool portraitDeleted = false;

    /// <summary>MonologueScene完了後のフラグ</summary>
    public bool afterChangeToLast = false;

    /// <summary>MonologueSceneから遷移したかのフラグ</summary>
    public bool fromMonologueScene = false;

    /// <summary>初回ファイルヒント表示済みフラグ</summary>
    public bool firstFileTipShown = false;

    /// <summary>選択された解像度のインデックス（0-3）</summary>
    public int resolutionIndex = 2; // デフォルトは1280x720（インデックス2）

    /// <summary>ウィンドウ位置情報（オプション）</summary>
    public WindowPosition windowPosition;

    /// <summary>OrganizeMainSceneのデータ</summary>
    public OrganizeSceneData organizeSceneData;

    /// <summary>
    /// 現在の言語設定コード
    /// "ja" = 日本語
    /// "en" = 英語
    /// デフォルトは英語
    /// </summary>
    public string languageCode = "en";


    /// <summary>
    /// デフォルト値で初期化する
    /// </summary>
    public GameSaveData()
    {
        gameVersion = "1.0";
        saveTimestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        folderState = new FolderState();
        fileProgress = new FileProgressData();
        audioSettings = new AudioSettings();
        windowPosition = new WindowPosition();
        organizeSceneData = new OrganizeSceneData();
        afterChangeToHerMemory = false;
        afterChangeToHisFuture = false;
        portraitDeleted = false;
        afterChangeToLast = false;
        fromMonologueScene = false;
        languageCode = "en";
    }
}

[Serializable]
public class FolderState
{
    /// <summary>現在アクティブなフォルダー</summary>
    public string activeFolder = "";

    /// <summary>表示されているフォルダー一覧</summary>
    public string[] displayedFolders = Array.Empty<string>();

    /// <summary>一度アクティブになったフォルダー一覧</summary>
    public string[] activatedFolders = Array.Empty<string>();
}

[Serializable]
public class AudioSettings
{
    /// <summary>マスター音量（0-1の範囲）</summary>
    public float masterVolume = 0.8f;

    /// <summary>BGM音量（0-1の範囲）</summary>
    public float bgmVolume = 0.5f;

    /// <summary>効果音音量（0-1の範囲）</summary>
    public float seVolume = 0.5f;
}

[Serializable]
public class FileProgressData
{
    /// <summary>TXTファイルの進捗（ファイル名 -> 進捗データ）</summary>
    public Dictionary<string, TxtFileData> txt = new Dictionary<string, TxtFileData>();

    /// <summary>PNGファイルの進捗（ファイル名 -> 進捗データ）</summary>
    public Dictionary<string, PngFileData> png = new Dictionary<string, PngFileData>();

    /// <summary>PDFファイルの進捗（ファイル名 -> 進捗データ）</summary>
    public Dictionary<string, PdfFileData> pdf = new Dictionary<string, PdfFileData>();
}

[Serializable]
public class TxtFileData
{
    /// <summary>TXTファイル名</summary>
    public string fileName = "";

    /// <summary>パズルが完成しているかどうか</summary>
    public bool isCompleted = false;

    /// <summary>解いたマッチの数</summary>
    public int solvedMatches = 0;

    /// <summary>合計マッチ数</summary>
    public int totalMatches = 0;
}

[Serializable]
public class PngFileData
{
    /// <summary>PNGファイル名</summary>
    public string fileName = "";

    /// <summary>現在のモザイクレベル</summary>
    public int currentLevel = 0;

    /// <summary>最大モザイクレベル</summary>
    public int maxLevel = 0;

    /// <summary>画像が完全に表示されているかどうか</summary>
    public bool isRevealed = false;
}

[Serializable]
public class PdfFileData
{
    /// <summary>PDFファイル名</summary>
    public string fileName = "";

    /// <summary>発見されたキーワード一覧</summary>
    public string[] revealedKeywords = Array.Empty<string>();

    /// <summary>合計キーワード数</summary>
    public int totalKeywords = 0;

    /// <summary>すべてのキーワードが見つかったかどうか</summary>
    public bool isCompleted = false;
}

/// <summary>
/// ウィンドウ位置情報
/// </summary>
[Serializable]
public class WindowPosition
{
    /// <summary>ウィンドウのX座標</summary>
    public int x = -1; // -1は未設定を示す

    /// <summary>ウィンドウのY座標</summary>
    public int y = -1; // -1は未設定を示す

    /// <summary>位置が有効かどうか</summary>
    public bool isValid = false;

    /// <summary>
    /// デフォルトコンストラクタ
    /// </summary>
    public WindowPosition()
    {
        x = -1;
        y = -1;
        isValid = false;
    }

    /// <summary>
    /// 位置を指定するコンストラクタ
    /// </summary>
    public WindowPosition(int x, int y)
    {
        this.x = x;
        this.y = y;
        this.isValid = true;
    }

    /// <summary>
    /// OrganizeMainSceneのセーブデータ構造
    /// ファイル整理シーン専用のデータを管理
    /// </summary>
    [Serializable]
    public class OrganizeSceneData
    {
        /// <summary>削除済みファイル名のリスト</summary>
        public List<string> deletedFiles;

        /// <summary>全ファイル完全削除フラグ</summary>
        public bool allFilesCompletelyDeleted;

        /// <summary>BGM変更済みフラグ</summary>
        public bool bgmChanged;

        /// <summary>Steam実績解除済みフラグ</summary>
        public bool steamAchievementUnlocked;

        /// <summary>最終更新日時</summary>
        public string lastModified;

        /// <summary>
        /// デフォルトコンストラクタ
        /// </summary>
        public OrganizeSceneData()
        {
            deletedFiles = new List<string>();
            allFilesCompletelyDeleted = false;
            bgmChanged = false;
            steamAchievementUnlocked = false;
            lastModified = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        }

        /// <summary>
        /// ファイルを削除済みリストに追加
        /// </summary>
        /// <param name="fileName">削除するファイル名</param>
        public void AddDeletedFile(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName) && !deletedFiles.Contains(fileName))
            {
                deletedFiles.Add(fileName);
                lastModified = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
            }
        }

        /// <summary>
        /// ファイルを削除済みリストから削除（復元）
        /// </summary>
        /// <param name="fileName">復元するファイル名</param>
        /// <returns>削除成功時はtrue</returns>
        public bool RemoveDeletedFile(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName) && deletedFiles.Contains(fileName))
            {
                deletedFiles.Remove(fileName);
                lastModified = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
                return true;
            }
            return false;
        }

        /// <summary>
        /// ファイルが削除済みかチェック
        /// </summary>
        /// <param name="fileName">チェックするファイル名</param>
        /// <returns>削除済みの場合true</returns>
        public bool IsFileDeleted(string fileName)
        {
            return !string.IsNullOrEmpty(fileName) && deletedFiles.Contains(fileName);
        }

        /// <summary>
        /// 削除済みファイル数を取得
        /// </summary>
        /// <returns>削除済みファイル数</returns>
        public int GetDeletedFileCount()
        {
            return deletedFiles?.Count ?? 0;
        }

        /// <summary>
        /// データをリセット
        /// </summary>
        public void Reset()
        {
            deletedFiles.Clear();
            allFilesCompletelyDeleted = false;
            bgmChanged = false;
            steamAchievementUnlocked = false;
            lastModified = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        }
    }
}