#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// テストプレイ用のフラグとセーブデータを完全初期化するエディタースクリプト
/// </summary>
public class TestFlagResetter : EditorWindow
{
    [MenuItem("Tools/Test Flag Resetter")]
    public static void ShowWindow()
    {
        GetWindow<TestFlagResetter>("テストフラグリセッター");
    }

    private void OnGUI()
    {
        GUILayout.Label("テスト用フラグ・データ初期化", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // 現在の状態表示
        GUILayout.Label("現在の状態:", EditorStyles.boldLabel);
        GUILayout.Label($"PlayerPrefs AfterChangeToHerMemory: {PlayerPrefs.GetInt("AfterChangeToHerMemory", 0)}");
        GUILayout.Label($"PlayerPrefs TitleTextChanged: {PlayerPrefs.GetInt("TitleTextChanged", 0)}");
        GUILayout.Label($"PlayerPrefs LastSceneName: {PlayerPrefs.GetString("LastSceneName", "なし")}");

        string saveFilePath = Path.Combine(Application.persistentDataPath, "game_save.json");
        GUILayout.Label($"セーブファイル存在: {File.Exists(saveFilePath)}");

        GUILayout.Space(20);

        // 個別リセットボタン
        GUILayout.Label("個別リセット:", EditorStyles.boldLabel);

        if (GUILayout.Button("PlayerPrefs フラグのみリセット"))
        {
            ResetPlayerPrefsFlags();
        }

        if (GUILayout.Button("セーブデータのみ削除"))
        {
            DeleteSaveData();
        }

        if (GUILayout.Button("ランタイムフラグのみリセット"))
        {
            ResetRuntimeFlags();
        }

        GUILayout.Space(10);

        // 完全リセットボタン
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("完全リセット（すべて初期化）", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("確認",
                "すべてのフラグとセーブデータを削除します。\n本当によろしいですか？",
                "はい", "キャンセル"))
            {
                CompleteReset();
            }
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(20);

        // 情報表示
        GUILayout.Label("使用方法:", EditorStyles.boldLabel);
        GUILayout.Label("1. テスト前に「完全リセット」を実行");
        GUILayout.Label("2. または個別リセットで必要な部分のみ初期化");
        GUILayout.Label("3. プレイモード開始でクリーンな状態でテスト可能");
    }

    private void ResetPlayerPrefsFlags()
    {
        PlayerPrefs.DeleteKey("AfterChangeToHerMemory");
        PlayerPrefs.DeleteKey("TitleTextChanged");
        PlayerPrefs.DeleteKey("LastSceneName");
        PlayerPrefs.Save();

        Debug.Log("TestFlagResetter: PlayerPrefs フラグを初期化しました");
    }

    private void DeleteSaveData()
    {
        try
        {
            string saveFilePath = Path.Combine(Application.persistentDataPath, "game_save.json");
            string txtFilePath = Path.Combine(Application.persistentDataPath, "txt_progress.json");
            string pngFilePath = Path.Combine(Application.persistentDataPath, "png_progress.json");
            string pdfFilePath = Path.Combine(Application.persistentDataPath, "pdf_progress.json");

            if (File.Exists(saveFilePath)) File.Delete(saveFilePath);
            if (File.Exists(txtFilePath)) File.Delete(txtFilePath);
            if (File.Exists(pngFilePath)) File.Delete(pngFilePath);
            if (File.Exists(pdfFilePath)) File.Delete(pdfFilePath);

            Debug.Log("TestFlagResetter: セーブデータを削除しました");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TestFlagResetter: セーブデータ削除エラー: {e.Message}");
        }
    }

    private void ResetRuntimeFlags()
    {
        // ランタイム中のGameSaveManagerのフラグをリセット
        GameSaveManager saveManager = Object.FindFirstObjectByType<GameSaveManager>();
        if (saveManager != null)
        {
            // GameSaveManagerに存在するパブリックメソッドを使用
            if (Application.isPlaying)
            {
                // プレイ中の場合、直接フラグをリセット
                saveManager.SetAfterChangeToHerMemoryFlag(false);
            }
        }

        // TitleTextChangerのフラグもリセット
        TitleTextChanger titleChanger = Object.FindFirstObjectByType<TitleTextChanger>();
        if (titleChanger != null)
        {
            // TitleTextChangerに存在するパブリックメソッドを使用
            titleChanger.ResetText();
        }

        Debug.Log("TestFlagResetter: ランタイムフラグを初期化しました");
    }

    private void CompleteReset()
    {
        // すべてを初期化
        ResetPlayerPrefsFlags();
        DeleteSaveData();

        if (Application.isPlaying)
        {
            ResetRuntimeFlags();
        }

        Debug.Log("TestFlagResetter: 完全初期化が完了しました");

        // ウィンドウを更新
        Repaint();
    }
}
#endif