using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

/// <summary>
/// ビルド後にsteam_appid.txtを実行ファイルと同じフォルダーにコピーするエディター拡張
/// </summary>
public class SteamBuildPostProcess
{
    [PostProcessBuild(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        // Windows以外のプラットフォームは処理をスキップ
        if (target != BuildTarget.StandaloneWindows && target != BuildTarget.StandaloneWindows64)
        {
            return;
        }

        // プロジェクトルートのsteam_appid.txtのパス
        string sourceFile = Path.Combine(Application.dataPath, "..", "steam_appid.txt");

        // ビルドした実行ファイルのディレクトリ
        string buildDirectory = Path.GetDirectoryName(pathToBuiltProject);

        // コピー先のパス
        string destinationFile = Path.Combine(buildDirectory, "steam_appid.txt");

        try
        {
            // ファイルが存在する場合のみコピー
            if (File.Exists(sourceFile))
            {
                File.Copy(sourceFile, destinationFile, true);
                Debug.Log($"steam_appid.txt をビルドフォルダーにコピーしました: {destinationFile}");
            }
            else
            {
                Debug.LogWarning($"steam_appid.txt が見つかりません: {sourceFile}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"steam_appid.txt のコピー中にエラーが発生しました: {e.Message}");
        }
    }
}