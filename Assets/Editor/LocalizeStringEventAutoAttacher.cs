using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.Localization.Settings;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Localization;
using UnityEngine.Events;

/// <summary>
/// Localize String Eventコンポーネントを自動アタッチするEditor拡張
/// Build SettingsのシーンリストにあるすべてのシーンのTextMeshProコンポーネントに設定を適用
/// Update Stringイベントも自動設定
/// </summary>
public class LocalizeStringEventAutoAttacher : EditorWindow
{
    // 定数定義
    private const string TABLE_COLLECTION_NAME = "SceneStringTable";
    private const string JAPANESE_LOCALE_CODE = "ja";
    private const float WINDOW_MIN_WIDTH = 600f;
    private const float WINDOW_MIN_HEIGHT = 400f;

    // UI表示用変数
    private Vector2 scrollPosition;
    private List<string> processingLogs = new List<string>();
    private bool isProcessing;
    private StringTableCollection tableCollection;
    private StringTable japaneseTable;

    // デバッグモード
    [SerializeField] private bool debugMode = false;

    // Update String設定オプション
    [SerializeField] private bool configureUpdateString = true;
    [SerializeField] private bool overwriteExistingComponents = true;

    /// <summary>
    /// メニューからウィンドウを開く
    /// </summary>
    [MenuItem("Tools/Localization/Localize String Event Auto Attacher")]
    public static void ShowWindow()
    {
        LocalizeStringEventAutoAttacher window = GetWindow<LocalizeStringEventAutoAttacher>();
        window.titleContent = new GUIContent("Localize String Event Auto Attacher");
        window.minSize = new Vector2(WINDOW_MIN_WIDTH, WINDOW_MIN_HEIGHT);
        window.Show();
    }

    private void OnEnable()
    {
        LoadLocalizationTables();
    }

    /// <summary>
    /// Localization Tableを読み込む
    /// </summary>
    private void LoadLocalizationTables()
    {
        processingLogs.Clear();

        // StringTableCollectionを取得
        string[] guids = AssetDatabase.FindAssets($"t:StringTableCollection {TABLE_COLLECTION_NAME}");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            tableCollection = AssetDatabase.LoadAssetAtPath<StringTableCollection>(path);

            if (tableCollection != null)
            {
                AddLog($"✓ {TABLE_COLLECTION_NAME}の読み込みに成功しました");

                // 日本語テーブルを取得
                Locale japaneseLocale = null;
                var locales = LocalizationSettings.AvailableLocales.Locales;

                foreach (var locale in locales)
                {
                    if (locale.Identifier.Code == JAPANESE_LOCALE_CODE)
                    {
                        japaneseLocale = locale;
                        break;
                    }
                }

                if (japaneseLocale != null)
                {
                    japaneseTable = tableCollection.GetTable(japaneseLocale.Identifier) as StringTable;

                    if (japaneseTable != null)
                    {
                        AddLog($"  テーブルエントリ数: {japaneseTable.Count}");
                    }
                    else
                    {
                        AddLog($"✗ 日本語テーブルが見つかりません");
                    }
                }
                else
                {
                    AddLog($"✗ 日本語ロケールが見つかりません");
                }
            }
        }
        else
        {
            AddLog($"✗ {TABLE_COLLECTION_NAME}が見つかりません");
            AddLog("  StringTableCollectionアセットを作成してください");
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        // タイトル
        EditorGUILayout.LabelField("Localize String Event 自動アタッチツール", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // 説明
        EditorGUILayout.HelpBox(
            "このツールは、Build SettingsのScene Listに含まれるすべてのシーンで、\n" +
            "TextMeshProコンポーネントを持つオブジェクトにLocalize String Eventコンポーネントを\n" +
            "自動的にアタッチし、Update Stringイベントも設定します。",
            MessageType.Info
        );

        EditorGUILayout.Space(10);

        // テーブル状態表示
        if (tableCollection != null && japaneseTable != null)
        {
            EditorGUILayout.LabelField($"Table Collection: {TABLE_COLLECTION_NAME}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Japanese Table Entries: {japaneseTable.Count}", EditorStyles.miniLabel);
        }
        else
        {
            EditorGUILayout.HelpBox("Localization Tableが読み込まれていません", MessageType.Warning);
            if (GUILayout.Button("テーブルを再読み込み"))
            {
                LoadLocalizationTables();
            }
        }

        EditorGUILayout.Space(10);

        // オプション設定
        EditorGUILayout.LabelField("設定オプション", EditorStyles.boldLabel);
        configureUpdateString = EditorGUILayout.Toggle("Update Stringを設定", configureUpdateString);
        overwriteExistingComponents = EditorGUILayout.Toggle("既存コンポーネントを上書き", overwriteExistingComponents);

        EditorGUILayout.Space(5);

        // デバッグモード
        debugMode = EditorGUILayout.Toggle("デバッグモード", debugMode);

        EditorGUILayout.Space(10);

        // 実行ボタン
        GUI.enabled = !isProcessing && tableCollection != null && japaneseTable != null;
        if (GUILayout.Button("実行", GUILayout.Height(30)))
        {
            ProcessAllScenes();
        }
        GUI.enabled = true;

        // 処理中インジケータ
        if (isProcessing)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("処理中...", EditorStyles.boldLabel);
        }

        EditorGUILayout.Space(10);

        // ログ表示エリア
        EditorGUILayout.LabelField("処理ログ:", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

        foreach (string log in processingLogs)
        {
            // ログの種類によって色を変える
            GUIStyle logStyle = new GUIStyle(EditorStyles.label);
            logStyle.wordWrap = true;

            if (log.StartsWith("✓"))
                logStyle.normal.textColor = new Color(0.2f, 0.7f, 0.2f);
            else if (log.StartsWith("✗"))
                logStyle.normal.textColor = Color.red;
            else if (log.StartsWith("!"))
                logStyle.normal.textColor = new Color(1f, 0.5f, 0f);

            EditorGUILayout.LabelField(log, logStyle);
        }

        EditorGUILayout.EndScrollView();

        // クリアボタン
        if (processingLogs.Count > 0 && !isProcessing)
        {
            if (GUILayout.Button("ログをクリア"))
            {
                processingLogs.Clear();
            }
        }
    }

    /// <summary>
    /// すべてのシーンを処理
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
                AddLog("✗ Build Settingsにシーンがありません");
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

            // 各シーンを処理
            foreach (string scenePath in scenePaths)
            {
                AddLog($"\n▶ シーン処理中: {System.IO.Path.GetFileNameWithoutExtension(scenePath)}");

                // シーンを開く
                Scene scene = EditorSceneManager.OpenScene(scenePath);

                if (!scene.IsValid())
                {
                    AddLog($"  ✗ シーンを開けませんでした");
                    continue;
                }

                int processedCount = ProcessSceneObjects(scene);
                totalProcessed += processedCount;

                // シーンを保存
                if (processedCount > 0)
                {
                    EditorSceneManager.SaveScene(scene);
                    AddLog($"  ✓ シーンを保存しました");
                }
            }

            // 元のシーンに戻る
            if (!string.IsNullOrEmpty(currentScenePath))
            {
                EditorSceneManager.OpenScene(currentScenePath);
            }

            AddLog($"\n===== 処理完了 =====");
            AddLog($"合計 {totalProcessed} 個のオブジェクトを処理しました");

            // アセットをリフレッシュ
            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            AddLog($"✗ エラーが発生しました: {e.Message}");
            Debug.LogError(e);
        }
        finally
        {
            isProcessing = false;
        }
    }

    /// <summary>
    /// シーン内のオブジェクトを処理
    /// </summary>
    /// <param name="scene">処理対象のシーン</param>
    /// <returns>処理したオブジェクト数</returns>
    private int ProcessSceneObjects(Scene scene)
    {
        int processedCount = 0;

        // シーン内のすべてのルートオブジェクトを取得
        GameObject[] rootObjects = scene.GetRootGameObjects();

        foreach (GameObject root in rootObjects)
        {
            // TextMeshProコンポーネントを持つすべてのオブジェクトを検索
            TMP_Text[] tmpComponents = root.GetComponentsInChildren<TMP_Text>(true);

            foreach (TMP_Text tmp in tmpComponents)
            {
                if (ProcessTextMeshProObject(tmp.gameObject, tmp))
                {
                    processedCount++;
                }
            }
        }

        AddLog($"  処理済みオブジェクト数: {processedCount}");
        return processedCount;
    }

    /// <summary>
    /// TextMeshProオブジェクトを処理
    /// </summary>
    /// <param name="targetObject">処理対象のGameObject</param>
    /// <param name="tmpComponent">TextMeshProコンポーネント</param>
    /// <returns>処理成功の場合true</returns>
    private bool ProcessTextMeshProObject(GameObject targetObject, TMP_Text tmpComponent)
    {
        string currentText = tmpComponent.text;

        // 空のテキストはスキップ
        if (string.IsNullOrEmpty(currentText))
        {
            if (debugMode)
                AddLog($"    - {targetObject.name}: テキストが空のためスキップ");
            return false;
        }

        // 既存のLocalizeStringEventコンポーネントの確認
        LocalizeStringEvent existingLocalizer = targetObject.GetComponent<LocalizeStringEvent>();

        // 既存コンポーネントがあり、上書きしない設定の場合はスキップ
        if (existingLocalizer != null && !overwriteExistingComponents)
        {
            if (debugMode)
                AddLog($"    - {targetObject.name}: すでにLocalizeStringEventが存在（スキップ）");
            return false;
        }

        // 日本語テーブルから一致するKeyを検索
        string matchingKey = FindMatchingKey(currentText);

        if (string.IsNullOrEmpty(matchingKey))
        {
            if (debugMode)
                AddLog($"    ! {targetObject.name}: 一致するKeyが見つかりません (Text: \"{TruncateText(currentText, 30)}\")");
            return false;
        }

        // LocalizeStringEventコンポーネントを設定
        LocalizeStringEvent localizer = existingLocalizer;
        bool isNewComponent = false;

        if (localizer == null)
        {
            localizer = targetObject.AddComponent<LocalizeStringEvent>();
            isNewComponent = true;
        }

        // String Referenceを設定
        localizer.StringReference.TableReference = TABLE_COLLECTION_NAME;
        localizer.StringReference.TableEntryReference = matchingKey;

        // Update Stringイベントを設定
        if (configureUpdateString)
        {
            //ConfigureUpdateStringEvent(localizer, tmpComponent);

            // 代替実装
            ConfigureUpdateStringEventAlternative(localizer, tmpComponent);
        }

        string actionText = isNewComponent ? "追加" : "更新";
        AddLog($"    ✓ {targetObject.name}: Key \"{matchingKey}\" を{actionText}");

        // 変更を記録
        EditorUtility.SetDirty(targetObject);

        return true;
    }

    /// <summary>
    /// Update Stringイベントを設定
    /// </summary>
    /// <param name="localizer">LocalizeStringEventコンポーネント</param>
    /// <param name="tmpComponent">TextMeshProコンポーネント</param>
    private void ConfigureUpdateStringEvent(LocalizeStringEvent localizer, TMP_Text tmpComponent)
    {
        // OnUpdateStringイベントをクリア
        localizer.OnUpdateString.RemoveAllListeners();

        // SerializedObjectを使用してUnityEventを設定
        SerializedObject serializedLocalizer = new SerializedObject(localizer);
        SerializedProperty updateStringProperty = serializedLocalizer.FindProperty("m_UpdateString");

        if (updateStringProperty != null)
        {
            SerializedProperty callsProperty = updateStringProperty.FindPropertyRelative("m_PersistentCalls.m_Calls");

            // 既存のリスナーをクリア
            callsProperty.ClearArray();

            // 新しいリスナーを追加
            callsProperty.InsertArrayElementAtIndex(0);
            SerializedProperty callProperty = callsProperty.GetArrayElementAtIndex(0);

            // ターゲットオブジェクトを設定
            SerializedProperty targetProperty = callProperty.FindPropertyRelative("m_Target");
            targetProperty.objectReferenceValue = tmpComponent;

            // コンポーネントの型に応じてモードを設定
            SerializedProperty modeProperty = callProperty.FindPropertyRelative("m_Mode");

            // TextMeshProUGUIかTextMeshProかを判定
            System.Type tmpType = tmpComponent.GetType();
            string typeName = tmpType.Name;

            // PropertyNameモード (Mode = 5) を使用
            modeProperty.enumValueIndex = 5; // Object.Property mode

            // メソッド名を設定（TextMeshProUGUI.textプロパティ）
            SerializedProperty methodNameProperty = callProperty.FindPropertyRelative("m_MethodName");
            methodNameProperty.stringValue = "text";

            // 呼び出し状態をEdit and Runtimeに設定
            SerializedProperty callStateProperty = callProperty.FindPropertyRelative("m_CallState");
            callStateProperty.enumValueIndex = 1; // EditorAndRuntime

            // 引数の設定
            SerializedProperty argumentsProperty = callProperty.FindPropertyRelative("m_Arguments");

            // Object引数の型情報を設定
            SerializedProperty objectArgTypeProperty = argumentsProperty.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName");
            objectArgTypeProperty.stringValue = typeof(System.String).AssemblyQualifiedName;

            // 文字列引数をクリア
            SerializedProperty stringArgProperty = argumentsProperty.FindPropertyRelative("m_StringArgument");
            stringArgProperty.stringValue = "";

            // 変更を適用
            serializedLocalizer.ApplyModifiedProperties();
        }

        if (debugMode)
        {
            AddLog($"      Update String設定: {tmpComponent.name}.text (Edit and Runtime)");
        }
    }

    /// <summary>
    /// テキストに一致するKeyを検索
    /// </summary>
    /// <param name="text">検索するテキスト</param>
    /// <returns>一致するKey、見つからない場合はnull</returns>
    private string FindMatchingKey(string text)
    {
        if (japaneseTable == null)
            return null;

        // 改行や空白を正規化
        string normalizedText = NormalizeText(text);

        // SharedDataから全エントリを取得して検索
        var sharedData = japaneseTable.SharedData;
        if (sharedData != null)
        {
            foreach (var sharedEntry in sharedData.Entries)
            {
                // SharedDataのKeyを使用
                string key = sharedEntry.Key;

                // このKeyに対応する値を取得
                var entry = japaneseTable.GetEntry(key);
                if (entry != null)
                {
                    string entryValue = entry.LocalizedValue;
                    if (!string.IsNullOrEmpty(entryValue))
                    {
                        string normalizedEntry = NormalizeText(entryValue);
                        if (normalizedEntry == normalizedText)
                        {
                            return key;
                        }
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// テキストを正規化（比較用）
    /// </summary>
    private string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        // 改行コードを統一、前後の空白を削除
        return text.Replace("\r\n", "\n")
                  .Replace("\r", "\n")
                  .Trim();
    }

    /// <summary>
    /// テキストを指定文字数で切り詰める（ログ表示用）
    /// </summary>
    private string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        text = text.Replace("\n", "\\n").Replace("\r", "\\r");

        if (text.Length <= maxLength)
            return text;

        return text.Substring(0, maxLength) + "...";
    }

    /// <summary>
    /// ログを追加
    /// </summary>
    private void AddLog(string message)
    {
        processingLogs.Add(message);
        Repaint();
    }

    /// <summary>
    /// Update Stringイベントを設定（代替実装）
    /// </summary>
    /// <param name="localizer">LocalizeStringEventコンポーネント</param>
    /// <param name="tmpComponent">TextMeshProコンポーネント</param>
    private void ConfigureUpdateStringEventAlternative(LocalizeStringEvent localizer, TMP_Text tmpComponent)
    {
        // 既存のリスナーをクリア
        localizer.OnUpdateString.RemoveAllListeners();

        // リフレクションを使用してUnityEventの内部データに直接アクセス
        var updateStringEvent = localizer.OnUpdateString;
        var eventType = updateStringEvent.GetType();

        // m_PersistentCallsフィールドを取得
        var persistentCallsField = eventType.GetField("m_PersistentCalls",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (persistentCallsField != null)
        {
            var persistentCalls = persistentCallsField.GetValue(updateStringEvent);
            var callsType = persistentCalls.GetType();

            // m_Callsリストを取得
            var callsField = callsType.GetField("m_Calls",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (callsField != null)
            {
                var callsList = callsField.GetValue(persistentCalls) as System.Collections.IList;

                // 既存のリストをクリア
                callsList.Clear();

                // 新しいPersistentCallを作成
                var persistentCallType = callsList.GetType().GetGenericArguments()[0];
                var newCall = System.Activator.CreateInstance(persistentCallType);

                // ターゲットを設定
                var targetField = persistentCallType.GetField("m_Target",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                targetField.SetValue(newCall, tmpComponent);

                // メソッド名を設定（textプロパティ）
                var methodNameField = persistentCallType.GetField("m_MethodName",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                methodNameField.SetValue(newCall, "text");

                // モードを設定（Property mode）
                var modeField = persistentCallType.GetField("m_Mode",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                modeField.SetValue(newCall, 5); // Property mode

                // 呼び出し状態を設定（EditorAndRuntime）
                var callStateField = persistentCallType.GetField("m_CallState",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                callStateField.SetValue(newCall, 1); // EditorAndRuntime

                // 引数を設定
                var argumentsField = persistentCallType.GetField("m_Arguments",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var arguments = argumentsField.GetValue(newCall);

                // 引数の型情報を設定
                var argTypeField = arguments.GetType().GetField("m_ObjectArgumentAssemblyTypeName",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                argTypeField.SetValue(arguments, typeof(string).AssemblyQualifiedName);

                // リストに追加
                callsList.Add(newCall);
            }
        }

        // 変更を記録
        EditorUtility.SetDirty(localizer);

        if (debugMode)
        {
            AddLog($"      Update String設定: {tmpComponent.name}.text (Edit and Runtime)");
        }
    }
}