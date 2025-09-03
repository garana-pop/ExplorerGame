using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// SpeakerDropAreaコンポーネントのexpectedSpeaker_JapaneseとexpectedSpeaker_Englishを自動設定するツール
/// </summary>
public class SpeakerDropAreaAutoSetter : EditorWindow
{
    // 定数定義
    private const float WINDOW_MIN_WIDTH = 500f;
    private const float WINDOW_MIN_HEIGHT = 400f;
    private const string TOOL_TITLE = "SpeakerDropArea Auto Setter";

    // 英語話者名の対応表
    private Dictionary<string, string> speakerTranslations = new Dictionary<string, string>()
    {
        // デフォルトの対応表
        { "ぼく", "Me" },
        { "友人", "Friend" },
        { "彼女", "Her" },
        { "大家", "Landlord" },
        { "店員", "Clerk" },
        { "近所の人", "Neighbor" },
        { "父親", "Father" },
        { "私", "Daughter" }
    };

    // UI表示用変数
    private Vector2 scrollPosition;
    private List<string> processingLogs = new List<string>();
    private bool isProcessing = false;
    private bool showTranslationTable = true;
    private string newJapaneseKey = "";
    private string newEnglishValue = "";

    /// <summary>
    /// メニューからウィンドウを開く
    /// </summary>
    [MenuItem("Tools/ExplorerGame/SpeakerDropArea Auto Setter")]
    public static void ShowWindow()
    {
        SpeakerDropAreaAutoSetter window = GetWindow<SpeakerDropAreaAutoSetter>();
        window.titleContent = new GUIContent(TOOL_TITLE);
        window.minSize = new Vector2(WINDOW_MIN_WIDTH, WINDOW_MIN_HEIGHT);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField(TOOL_TITLE, EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 説明文
        EditorGUILayout.HelpBox(
            "このツールは、Build SettingsのScene Listに登録されている全シーンの" +
            "SpeakerDropAreaコンポーネントに対して、expectedSpeaker_Japaneseと" +
            "expectedSpeaker_Englishを自動設定します。",
            MessageType.Info);

        EditorGUILayout.Space();

        // 英語対応表セクション
        showTranslationTable = EditorGUILayout.Foldout(showTranslationTable, "話者名英語対応表");

        if (showTranslationTable)
        {
            EditorGUI.indentLevel++;

            // 既存の対応表を表示
            EditorGUILayout.BeginVertical("box");

            List<string> keysToRemove = new List<string>();

            foreach (var kvp in speakerTranslations)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(kvp.Key, GUILayout.Width(150));
                EditorGUILayout.LabelField("→", GUILayout.Width(30));

                // 値の編集
                string newValue = EditorGUILayout.TextField(kvp.Value, GUILayout.Width(150));
                if (newValue != kvp.Value)
                {
                    speakerTranslations[kvp.Key] = newValue;
                }

                if (GUILayout.Button("削除", GUILayout.Width(50)))
                {
                    keysToRemove.Add(kvp.Key);
                }

                EditorGUILayout.EndHorizontal();
            }

            // 削除処理
            foreach (string key in keysToRemove)
            {
                speakerTranslations.Remove(key);
            }

            EditorGUILayout.EndVertical();

            // 新規追加セクション
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("新規追加:");

            EditorGUILayout.BeginHorizontal();
            newJapaneseKey = EditorGUILayout.TextField("日本語:", newJapaneseKey, GUILayout.Width(200));
            newEnglishValue = EditorGUILayout.TextField("英語:", newEnglishValue, GUILayout.Width(200));

            if (GUILayout.Button("追加", GUILayout.Width(60)))
            {
                if (!string.IsNullOrEmpty(newJapaneseKey) && !string.IsNullOrEmpty(newEnglishValue))
                {
                    if (!speakerTranslations.ContainsKey(newJapaneseKey))
                    {
                        speakerTranslations.Add(newJapaneseKey, newEnglishValue);
                        newJapaneseKey = "";
                        newEnglishValue = "";
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("エラー", "その日本語キーは既に存在します。", "OK");
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();

        // 実行ボタン
        GUI.enabled = !isProcessing;

        if (GUILayout.Button("全シーンの SpeakerDropArea を自動設定", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "確認",
                "Build Settingsに登録されている全シーンのSpeakerDropAreaコンポーネントを更新します。\n" +
                "続行しますか？",
                "はい", "いいえ"))
            {
                ProcessAllScenes();
            }
        }

        GUI.enabled = true;

        EditorGUILayout.Space();

        // ログ表示
        EditorGUILayout.LabelField("処理ログ:", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));

        GUIStyle logStyle = new GUIStyle(EditorStyles.label);
        logStyle.wordWrap = true;

        foreach (string log in processingLogs)
        {
            // エラーメッセージは赤色で表示
            if (log.Contains("エラー") || log.Contains("失敗"))
            {
                logStyle.normal.textColor = Color.red;
            }
            else if (log.Contains("成功") || log.Contains("完了"))
            {
                logStyle.normal.textColor = Color.green;
            }
            else
            {
                logStyle.normal.textColor = Color.white;
            }

            EditorGUILayout.LabelField(log, logStyle);
        }

        EditorGUILayout.EndScrollView();

        // ログクリアボタン
        if (processingLogs.Count > 0 && !isProcessing)
        {
            if (GUILayout.Button("ログをクリア"))
            {
                processingLogs.Clear();
            }
        }
    }

    /// <summary>
    /// 全シーンを処理
    /// </summary>
    private void ProcessAllScenes()
    {
        isProcessing = true;
        processingLogs.Clear();

        try
        {
            AddLog("===== 処理開始 =====");

            // Build SettingsのScene Listを取得
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
            List<string> scenePaths = buildScenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToList();

            if (scenePaths.Count == 0)
            {
                AddLog("エラー: Build Settingsにシーンが登録されていません。");
                return;
            }

            AddLog($"処理対象シーン数: {scenePaths.Count}");

            // 現在のシーンを保存
            Scene currentScene = SceneManager.GetActiveScene();
            string currentScenePath = currentScene.path;

            if (currentScene.isDirty)
            {
                EditorSceneManager.SaveScene(currentScene);
            }

            int totalProcessed = 0;
            int totalComponents = 0;

            // 各シーンを処理
            foreach (string scenePath in scenePaths)
            {
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                AddLog($"\n▶ シーン処理中: {sceneName}");

                // シーンを開く
                Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

                if (!scene.IsValid())
                {
                    AddLog($"  エラー: シーンを開けませんでした。");
                    continue;
                }

                // SpeakerDropAreaコンポーネントを検索
                SpeakerDropArea[] dropAreas = Object.FindObjectsOfType<SpeakerDropArea>(true);

                if (dropAreas.Length == 0)
                {
                    AddLog($"  SpeakerDropAreaコンポーネントが見つかりませんでした。");
                    continue;
                }

                AddLog($"  発見: {dropAreas.Length}個のSpeakerDropAreaコンポーネント");

                int processedInScene = 0;

                foreach (SpeakerDropArea dropArea in dropAreas)
                {
                    if (ProcessSpeakerDropArea(dropArea))
                    {
                        processedInScene++;
                        totalComponents++;
                    }
                }

                if (processedInScene > 0)
                {
                    // シーンを保存
                    EditorSceneManager.SaveScene(scene);
                    AddLog($"  成功: {processedInScene}個のコンポーネントを更新しました。");
                    totalProcessed++;
                }
            }

            // 元のシーンに戻る
            if (!string.IsNullOrEmpty(currentScenePath))
            {
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
            }

            AddLog("\n===== 処理完了 =====");
            AddLog($"処理済みシーン数: {totalProcessed}");
            AddLog($"更新済みコンポーネント数: {totalComponents}");

            EditorUtility.DisplayDialog(
                "完了",
                $"処理が完了しました。\n\n" +
                $"処理済みシーン数: {totalProcessed}\n" +
                $"更新済みコンポーネント数: {totalComponents}",
                "OK");
        }
        catch (System.Exception ex)
        {
            AddLog($"エラー: {ex.Message}");
            Debug.LogError($"SpeakerDropAreaAutoSetter エラー: {ex}");
        }
        finally
        {
            isProcessing = false;
        }
    }

    /// <summary>
    /// 個別のSpeakerDropAreaコンポーネントを処理
    /// </summary>
    private bool ProcessSpeakerDropArea(SpeakerDropArea dropArea)
    {
        if (dropArea == null) return false;

        try
        {
            // SerializedObjectを使用してプロパティを更新
            SerializedObject serializedObject = new SerializedObject(dropArea);

            // expectedSpeakerプロパティを取得
            SerializedProperty expectedSpeakerProp = serializedObject.FindProperty("expectedSpeaker");
            SerializedProperty expectedSpeakerJapaneseProp = serializedObject.FindProperty("expectedSpeaker_Japanese");
            SerializedProperty expectedSpeakerEnglishProp = serializedObject.FindProperty("expectedSpeaker_English");

            if (expectedSpeakerProp == null || expectedSpeakerJapaneseProp == null || expectedSpeakerEnglishProp == null)
            {
                AddLog($"    警告: {dropArea.gameObject.name} - プロパティが見つかりません。");
                return false;
            }

            string currentExpectedSpeaker = expectedSpeakerProp.stringValue;

            // expectedSpeaker_Japaneseを設定（expectedSpeakerと同じ値）
            expectedSpeakerJapaneseProp.stringValue = currentExpectedSpeaker;

            // expectedSpeaker_Englishを設定（対応表から取得）
            if (speakerTranslations.ContainsKey(currentExpectedSpeaker))
            {
                expectedSpeakerEnglishProp.stringValue = speakerTranslations[currentExpectedSpeaker];
                AddLog($"    更新: {dropArea.gameObject.name} - JP: {currentExpectedSpeaker} → EN: {speakerTranslations[currentExpectedSpeaker]}");
            }
            else
            {
                // 対応表にない場合は元の値をそのまま使用
                expectedSpeakerEnglishProp.stringValue = currentExpectedSpeaker;
                AddLog($"    注意: {dropArea.gameObject.name} - '{currentExpectedSpeaker}' の英語対応が未設定のため、同じ値を使用");
            }

            // 変更を適用
            serializedObject.ApplyModifiedProperties();

            // オブジェクトを更新済みとしてマーク
            EditorUtility.SetDirty(dropArea);

            return true;
        }
        catch (System.Exception ex)
        {
            AddLog($"    エラー: {dropArea.gameObject.name} - {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// ログを追加
    /// </summary>
    private void AddLog(string message)
    {
        processingLogs.Add(message);
        Repaint();
    }
}