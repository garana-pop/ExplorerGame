using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ExportUserScriptsIncludingInactive : EditorWindow
{
    [MenuItem("Tools/Export All User Scripts (Including Inactive)")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(ExportUserScriptsIncludingInactive));
    }

    void OnGUI()
    {
        if (GUILayout.Button("Export User Scripts"))
        {
            ExportScripts();
        }
    }

    /// <summary>
    /// 現在のシーン内（アクティブ／非アクティブ問わず）の全 MonoBehaviour から、ユーザー作成（Assetsフォルダ内）スクリプトのファイル名を収集し、
    /// テキストファイルへ書き出します。
    /// </summary>
    void ExportScripts()
    {
        // Resources.FindObjectsOfTypeAll を使い、全MonoBehaviour（非アクティブ含む）を取得
        MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();

        // 重複排除用のハッシュセット
        HashSet<string> scriptNames = new HashSet<string>();

        foreach (var behaviour in behaviours)
        {
            // シーンに所属しているオブジェクトのみ対象とする
            if (!behaviour.gameObject.scene.IsValid())
                continue;

            // MonoBehaviour から関連付けられている MonoScript を取得
            MonoScript monoScript = MonoScript.FromMonoBehaviour(behaviour);
            if (monoScript == null)
                continue;

            // 取得した MonoScript からアセットパスを抜き出す
            string scriptPath = AssetDatabase.GetAssetPath(monoScript);

            // ユーザーが作成したスクリプト（Assetsフォルダ内のもの）のみ対象とする
            if (!scriptPath.StartsWith("Assets/"))
                continue;

            string fileName = Path.GetFileName(scriptPath);
            if (!string.IsNullOrEmpty(fileName))
            {
                scriptNames.Add(fileName);
            }
        }

        // 出力先は Assets フォルダ直下に "UserScripts.txt" として保存
        string filePath = Path.Combine(Application.dataPath, "UserScripts.txt");
        File.WriteAllLines(filePath, scriptNames);

        EditorUtility.DisplayDialog("Export Complete", "Exported user script file names to:\n" + filePath, "OK");
    }
}
