using System;
using System.Collections.Generic;

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
        afterChangeToHerMemory = false;
        afterChangeToHisFuture = false;
        portraitDeleted = false;
        afterChangeToLast = false;
        fromMonologueScene = false;
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
}