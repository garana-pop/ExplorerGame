using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// セーブデータのパス確認と削除を行うデバッグツール
/// </summary>
public class SaveDataDebugTool : MonoBehaviour
{
    // セーブファイル名（GameSaveManagerと同じ）
    private const string SAVE_FILE_NAME = "gamesave.json";

    /// <summary>
    /// セーブデータのフルパスを取得
    /// </summary>
    /// <returns>セーブファイルのフルパス</returns>
    public static string GetSaveDataPath()
    {
        return Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);
    }

    /// <summary>
    /// セーブデータが存在するかチェック
    /// </summary>
    /// <returns>セーブファイルが存在する場合はtrue</returns>
    public static bool SaveDataExists()
    {
        return File.Exists(GetSaveDataPath());
    }

    /// <summary>
    /// セーブデータのファイルサイズを取得
    /// </summary>
    /// <returns>ファイルサイズ（バイト）、存在しない場合は-1</returns>
    public static long GetSaveDataSize()
    {
        string path = GetSaveDataPath();
        if (File.Exists(path))
        {
            FileInfo fileInfo = new FileInfo(path);
            return fileInfo.Length;
        }
        return -1;
    }

    /// <summary>
    /// セーブデータを削除
    /// </summary>
    /// <returns>削除成功時はtrue</returns>
    public static bool DeleteSaveData()
    {
        string path = GetSaveDataPath();
        if (File.Exists(path))
        {
            try
            {
                File.Delete(path);
                Debug.Log($"セーブデータを削除しました: {path}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"セーブデータの削除に失敗しました: {e.Message}");
                return false;
            }
        }
        else
        {
            Debug.LogWarning("削除するセーブデータが存在しません");
            return false;
        }
    }

    /// <summary>
    /// セーブデータの内容を表示（最初の500文字まで）
    /// </summary>
    public static void ShowSaveDataContent()
    {
        string path = GetSaveDataPath();
        if (File.Exists(path))
        {
            try
            {
                string content = File.ReadAllText(path);
                int maxLength = Mathf.Min(content.Length, 500);
                string preview = content.Substring(0, maxLength);
                if (content.Length > 500)
                {
                    preview += "\n... (以下省略)";
                }
                Debug.Log($"セーブデータ内容:\n{preview}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"セーブデータの読み込みに失敗しました: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("セーブデータが存在しません");
        }
    }

    /// <summary>
    /// フォルダをエクスプローラーで開く（Windows限定）
    /// </summary>
    public static void OpenSaveDataFolder()
    {
        string folderPath = Application.persistentDataPath;

#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        folderPath = folderPath.Replace("/", "\\");
        System.Diagnostics.Process.Start("explorer.exe", folderPath);
        Debug.Log($"フォルダを開きました: {folderPath}");
#else
        Debug.Log($"セーブデータフォルダ: {folderPath}");
        Debug.LogWarning("フォルダを自動で開く機能はWindows環境でのみ動作します");
#endif
    }

    // インスペクターにボタンを表示するためのContext Menu
    [ContextMenu("1. セーブデータのパスを表示")]
    public void ShowSaveDataPath()
    {
        string path = GetSaveDataPath();
        bool exists = SaveDataExists();
        long size = GetSaveDataSize();

        Debug.Log("========== セーブデータ情報 ==========");
        Debug.Log($"パス: {path}");
        Debug.Log($"存在: {(exists ? "あり" : "なし")}");
        if (size >= 0)
        {
            Debug.Log($"サイズ: {size} バイト ({size / 1024f:F2} KB)");
        }
        Debug.Log("=====================================");

        // クリップボードにコピー（Unity Editor内でのみ動作）
#if UNITY_EDITOR
        GUIUtility.systemCopyBuffer = path;
        Debug.Log("(パスをクリップボードにコピーしました)");
#endif
    }

    [ContextMenu("2. セーブデータの内容を表示")]
    public void ShowContent()
    {
        ShowSaveDataContent();
    }

    [ContextMenu("3. セーブデータフォルダを開く")]
    public void OpenFolder()
    {
        OpenSaveDataFolder();
    }

    [ContextMenu("4. セーブデータを削除")]
    public void DeleteSave()
    {
        if (SaveDataExists())
        {
#if UNITY_EDITOR
            bool confirm = EditorUtility.DisplayDialog(
                "セーブデータ削除の確認",
                "本当にセーブデータを削除しますか？\nこの操作は取り消せません。",
                "削除する",
                "キャンセル"
            );

            if (confirm)
            {
                DeleteSaveData();
            }
            else
            {
                Debug.Log("セーブデータの削除をキャンセルしました");
            }
#else
            DeleteSaveData();
#endif
        }
        else
        {
            Debug.LogWarning("削除するセーブデータが存在しません");
        }
    }
}

#if UNITY_EDITOR
/// <summary>
/// Unity Editor用のカスタムエディター
/// </summary>
[CustomEditor(typeof(SaveDataDebugTool))]
public class SaveDataDebugToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("セーブデータ管理", EditorStyles.boldLabel);

        // パス情報の表示
        string path = SaveDataDebugTool.GetSaveDataPath();
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("保存パス:", EditorStyles.miniBoldLabel);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.SelectableLabel(path, GUILayout.Height(20));
        if (GUILayout.Button("コピー", GUILayout.Width(50)))
        {
            GUIUtility.systemCopyBuffer = path;
            Debug.Log("パスをクリップボードにコピーしました");
        }
        EditorGUILayout.EndHorizontal();

        // ステータス表示
        bool exists = SaveDataDebugTool.SaveDataExists();
        long size = SaveDataDebugTool.GetSaveDataSize();

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("ステータス:", exists ? "存在" : "なし",
            exists ? EditorStyles.boldLabel : EditorStyles.label);

        if (size >= 0)
        {
            EditorGUILayout.LabelField($"サイズ: {size} バイト ({size / 1024f:F2} KB)");
        }
        EditorGUILayout.EndVertical();

        // ボタン
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();

        GUI.enabled = exists;
        if (GUILayout.Button("内容を表示", GUILayout.Height(25)))
        {
            SaveDataDebugTool.ShowSaveDataContent();
        }
        GUI.enabled = true;

        if (GUILayout.Button("フォルダを開く", GUILayout.Height(25)))
        {
            SaveDataDebugTool.OpenSaveDataFolder();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // 削除ボタン（赤色で表示）
        GUI.enabled = exists;
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = Color.red;

        if (GUILayout.Button("セーブデータを削除", GUILayout.Height(30)))
        {
            bool confirm = EditorUtility.DisplayDialog(
                "セーブデータ削除の確認",
                "本当にセーブデータを削除しますか？\nこの操作は取り消せません。",
                "削除する",
                "キャンセル"
            );

            if (confirm)
            {
                SaveDataDebugTool.DeleteSaveData();
                // インスペクターを更新
                EditorUtility.SetDirty(target);
            }
        }

        GUI.backgroundColor = originalColor;
        GUI.enabled = true;

        // 自動更新ボタン
        EditorGUILayout.Space(10);
        if (GUILayout.Button("情報を更新", GUILayout.Height(25)))
        {
            Repaint();
        }
    }
}
#endif