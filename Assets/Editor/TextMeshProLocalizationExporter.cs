using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Unity プロジェクト内の TextMeshPro テキストを抽出し、
/// Localization Tables 用の CSV ファイルを生成するエディターツール
/// </summary>
public class TextMeshProLocalizationExporter : EditorWindow
{
    #region 定数定義

    // CSV ヘッダー定義
    private const string CSV_HEADER = "Key,id,Japanese(ja),English(en)";
    private const string DEFAULT_ID = "0";
    private const string KEY_SEPARATOR = "-";

    // ウィンドウサイズ
    private const float MIN_WINDOW_WIDTH = 600f;
    private const float MIN_WINDOW_HEIGHT = 400f;

    // パッケージパスを除外
    private const string PACKAGES_PATH = "Packages/";

    #endregion

    #region インスペクター設定

    [Header("出力設定")]
    [Tooltip("CSV ファイルの出力先フォルダー")]
    [SerializeField] private string outputPath = "Assets/Localization/CSVExport";

    [Tooltip("出力ファイル名")]
    [SerializeField] private string fileName = "TextMeshProTexts.csv";

    [Header("検索設定")]
    [Tooltip("プレハブも検索対象に含める")]
    [SerializeField] private bool includePrefabs = true;

    [Tooltip("非アクティブなオブジェクトも検索対象に含める")]
    [SerializeField] private bool includeInactive = true;

    [Tooltip("重複するテキストを除外する")]
    [SerializeField] private bool excludeDuplicates = true;

    [Tooltip("ビルド設定に含まれるシーンのみ対象にする")]
    [SerializeField] private bool onlyBuildScenes = true;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;

    #endregion

    #region プライベート変数

    private List<LocalizationEntry> extractedEntries = new List<LocalizationEntry>();
    private Vector2 scrollPosition;
    private bool isExtracting = false;
    private string extractionStatus = "";
    private float extractionProgress = 0f;

    #endregion

    #region データ構造

    /// <summary>
    /// ローカライゼーションエントリーデータ
    /// </summary>
    private class LocalizationEntry
    {
        public string key;
        public string id;
        public string japaneseText;
        public string englishText;
        public string sceneName;
        public string objectPath;

        public LocalizationEntry(string key, string text, string scene, string path)
        {
            this.key = key;
            this.id = DEFAULT_ID;
            this.japaneseText = text;
            this.englishText = ""; // 英語は後で翻訳予定
            this.sceneName = scene;
            this.objectPath = path;
        }
    }

    #endregion

    #region Unity エディターメニュー

    /// <summary>
    /// エディターメニューからツールを開く
    /// </summary>
    [MenuItem("Tools/Localization/TextMeshPro Text Exporter")]
    public static void ShowWindow()
    {
        TextMeshProLocalizationExporter window = GetWindow<TextMeshProLocalizationExporter>("TMP Localization Exporter");
        window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
    }

    #endregion

    #region GUI 描画

    private void OnGUI()
    {
        GUILayout.Label("TextMeshPro Localization CSV Exporter", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 出力設定
        EditorGUILayout.LabelField("出力設定", EditorStyles.boldLabel);
        outputPath = EditorGUILayout.TextField("出力フォルダー", outputPath);
        fileName = EditorGUILayout.TextField("ファイル名", fileName);
        EditorGUILayout.Space();

        // 検索設定
        EditorGUILayout.LabelField("検索設定", EditorStyles.boldLabel);
        onlyBuildScenes = EditorGUILayout.Toggle("ビルド設定のシーンのみ", onlyBuildScenes);
        includePrefabs = EditorGUILayout.Toggle("プレハブを含める", includePrefabs);
        includeInactive = EditorGUILayout.Toggle("非アクティブを含める", includeInactive);
        excludeDuplicates = EditorGUILayout.Toggle("重複を除外", excludeDuplicates);
        EditorGUILayout.Space();

        // デバッグモード
        debugMode = EditorGUILayout.Toggle("デバッグモード", debugMode);
        EditorGUILayout.Space();

        // ボタン
        EditorGUI.BeginDisabledGroup(isExtracting);
        if (GUILayout.Button("テキスト抽出", GUILayout.Height(30)))
        {
            ExtractAllTexts();
        }
        EditorGUI.EndDisabledGroup();

        // 抽出中の進捗表示
        if (isExtracting)
        {
            EditorGUILayout.Space();
            EditorGUI.ProgressBar(
                EditorGUILayout.GetControlRect(GUILayout.Height(20)),
                extractionProgress,
                extractionStatus
            );
        }

        EditorGUI.BeginDisabledGroup(extractedEntries.Count == 0);
        if (GUILayout.Button("CSV エクスポート", GUILayout.Height(30)))
        {
            ExportToCSV();
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();

        // 結果表示
        if (extractedEntries.Count > 0)
        {
            EditorGUILayout.LabelField($"抽出されたテキスト: {extractedEntries.Count} 件", EditorStyles.boldLabel);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            foreach (var entry in extractedEntries.Take(50)) // 最初の50件のみ表示
            {
                EditorGUILayout.BeginHorizontal("box");
                EditorGUILayout.LabelField(entry.key, GUILayout.Width(250));
                EditorGUILayout.LabelField(entry.japaneseText);
                EditorGUILayout.EndHorizontal();
            }

            if (extractedEntries.Count > 50)
            {
                EditorGUILayout.LabelField($"... 他 {extractedEntries.Count - 50} 件");
            }
            EditorGUILayout.EndScrollView();
        }
    }

    #endregion

    #region テキスト抽出処理

    /// <summary>
    /// プロジェクト内のすべての TextMeshPro テキストを抽出
    /// </summary>
    private void ExtractAllTexts()
    {
        isExtracting = true;
        extractedEntries.Clear();
        extractionProgress = 0f;
        extractionStatus = "準備中...";

        try
        {
            // 現在開いているシーンを保存
            Scene[] originalScenes = new Scene[SceneManager.sceneCount];
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                originalScenes[i] = SceneManager.GetSceneAt(i);
            }
            Scene originalActiveScene = SceneManager.GetActiveScene();

            // 現在のシーンを保存
            if (EditorSceneManager.GetActiveScene().isDirty)
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            }

            // 対象となるシーンパスを取得
            string[] scenePaths = GetTargetScenePaths();

            // 各シーンから抽出
            for (int i = 0; i < scenePaths.Length; i++)
            {
                string scenePath = scenePaths[i];
                extractionProgress = (float)i / scenePaths.Length;
                extractionStatus = $"シーン処理中 ({i + 1}/{scenePaths.Length}): {Path.GetFileNameWithoutExtension(scenePath)}";

                try
                {
                    // シーンを開く
                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    ExtractFromScene(scene);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"シーン '{scenePath}' の処理中にエラーが発生しました: {e.Message}");
                }

                // GUI更新
                if (i % 5 == 0)
                {
                    EditorUtility.DisplayProgressBar(
                        "TextMeshPro テキスト抽出",
                        extractionStatus,
                        extractionProgress
                    );
                }
            }

            // プレハブから抽出
            if (includePrefabs)
            {
                extractionStatus = "プレハブ処理中...";
                EditorUtility.DisplayProgressBar("TextMeshPro テキスト抽出", extractionStatus, 0.9f);
                ExtractFromPrefabs();
            }

            // 重複除外処理
            if (excludeDuplicates)
            {
                extractionStatus = "重複除外処理中...";
                EditorUtility.DisplayProgressBar("TextMeshPro テキスト抽出", extractionStatus, 0.95f);
                RemoveDuplicates();
            }

            // 元のシーンを復元
            if (originalScenes.Length > 0 && originalScenes[0].IsValid())
            {
                EditorSceneManager.OpenScene(originalScenes[0].path, OpenSceneMode.Single);
                for (int i = 1; i < originalScenes.Length; i++)
                {
                    if (originalScenes[i].IsValid())
                    {
                        EditorSceneManager.OpenScene(originalScenes[i].path, OpenSceneMode.Additive);
                    }
                }
                SceneManager.SetActiveScene(originalActiveScene);
            }

            if (debugMode)
            {
                Debug.Log($"TextMeshProLocalizationExporter: {extractedEntries.Count} 件のテキストを抽出しました");
            }

            EditorUtility.DisplayDialog(
                "抽出完了",
                $"{extractedEntries.Count} 件のテキストを抽出しました",
                "OK"
            );
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TextMeshProLocalizationExporter: エラーが発生しました - {e}");
            EditorUtility.DisplayDialog("エラー", $"抽出中にエラーが発生しました:\n{e.Message}", "OK");
        }
        finally
        {
            isExtracting = false;
            extractionStatus = "";
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>
    /// 対象となるシーンパスを取得
    /// </summary>
    private string[] GetTargetScenePaths()
    {
        List<string> scenePaths = new List<string>();

        if (onlyBuildScenes)
        {
            // ビルド設定に含まれるシーンのみ
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                if (buildScene.enabled && !string.IsNullOrEmpty(buildScene.path))
                {
                    // パッケージ内のシーンを除外
                    if (!buildScene.path.StartsWith(PACKAGES_PATH))
                    {
                        scenePaths.Add(buildScene.path);
                    }
                }
            }
        }
        else
        {
            // プロジェクト内のすべてのシーン（パッケージを除外）
            string[] allScenePaths = AssetDatabase.FindAssets("t:Scene")
                .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                .Where(path => !path.StartsWith(PACKAGES_PATH))
                .ToArray();

            scenePaths.AddRange(allScenePaths);
        }

        return scenePaths.ToArray();
    }

    /// <summary>
    /// 特定のシーンから TextMeshPro テキストを抽出
    /// </summary>
    private void ExtractFromScene(Scene scene)
    {
        if (!scene.IsValid()) return;

        string sceneName = string.IsNullOrEmpty(scene.name) ? "Unknown" : scene.name;

        // Unity 6 の FindObjectsByType を使用
        TextMeshProUGUI[] tmpUGUIComponents = FindObjectsByType<TextMeshProUGUI>(
            includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        // 通常の TextMeshPro コンポーネントも検索
        TextMeshPro[] tmpComponents = FindObjectsByType<TextMeshPro>(
            includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        // UGUI版の処理
        foreach (var tmp in tmpUGUIComponents)
        {
            ProcessTextMeshPro(tmp, sceneName);
        }

        // 通常版の処理
        foreach (var tmp in tmpComponents)
        {
            ProcessTextMeshPro(tmp, sceneName);
        }
    }

    /// <summary>
    /// TextMeshPro コンポーネントの処理
    /// </summary>
    private void ProcessTextMeshPro(TMP_Text tmp, string sceneName)
    {
        if (tmp == null || string.IsNullOrEmpty(tmp.text)) return;

        // 空白のみのテキストをスキップ
        if (string.IsNullOrWhiteSpace(tmp.text)) return;

        // オブジェクトパスを生成
        string objectPath = GetGameObjectPath(tmp.gameObject);

        // キーを生成 (シーン名-オブジェクトパス)
        string key = $"{sceneName}{KEY_SEPARATOR}{objectPath}";

        // エントリーを追加
        LocalizationEntry entry = new LocalizationEntry(key, tmp.text, sceneName, objectPath);
        extractedEntries.Add(entry);

        if (debugMode)
        {
            Debug.Log($"抽出: {key} = {tmp.text}");
        }
    }

    /// <summary>
    /// プレハブから TextMeshPro テキストを抽出
    /// </summary>
    private void ExtractFromPrefabs()
    {
        string[] prefabPaths = AssetDatabase.FindAssets("t:Prefab")
            .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
            .Where(path => !path.StartsWith(PACKAGES_PATH)) // パッケージ内のプレハブを除外
            .ToArray();

        foreach (string prefabPath in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) continue;

            // UGUI版
            TextMeshProUGUI[] tmpUGUIComponents = prefab.GetComponentsInChildren<TextMeshProUGUI>(includeInactive);
            // 通常版
            TextMeshPro[] tmpComponents = prefab.GetComponentsInChildren<TextMeshPro>(includeInactive);

            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

            foreach (var tmp in tmpUGUIComponents)
            {
                ProcessPrefabTextMeshPro(tmp, prefabName);
            }

            foreach (var tmp in tmpComponents)
            {
                ProcessPrefabTextMeshPro(tmp, prefabName);
            }
        }
    }

    /// <summary>
    /// プレハブ内の TextMeshPro コンポーネントの処理
    /// </summary>
    private void ProcessPrefabTextMeshPro(TMP_Text tmp, string prefabName)
    {
        if (tmp == null || string.IsNullOrEmpty(tmp.text)) return;

        // 空白のみのテキストをスキップ
        if (string.IsNullOrWhiteSpace(tmp.text)) return;

        string objectPath = GetGameObjectPath(tmp.gameObject);
        string key = $"Prefab{KEY_SEPARATOR}{prefabName}{KEY_SEPARATOR}{objectPath}";

        LocalizationEntry entry = new LocalizationEntry(key, tmp.text, "Prefab", objectPath);
        extractedEntries.Add(entry);
    }

    /// <summary>
    /// GameObject のパスを取得
    /// </summary>
    private string GetGameObjectPath(GameObject obj)
    {
        if (obj == null) return "";

        string path = obj.name;
        Transform parent = obj.transform.parent;

        while (parent != null)
        {
            path = parent.name + KEY_SEPARATOR + path;
            parent = parent.parent;
        }

        return path;
    }

    /// <summary>
    /// 重複エントリーを除外
    /// </summary>
    private void RemoveDuplicates()
    {
        var uniqueEntries = new Dictionary<string, LocalizationEntry>();

        foreach (var entry in extractedEntries)
        {
            // キーをそのまま使用（同じパスのオブジェクトは1つだけ保持）
            if (!uniqueEntries.ContainsKey(entry.key))
            {
                uniqueEntries.Add(entry.key, entry);
            }
        }

        extractedEntries = uniqueEntries.Values.ToList();
    }

    #endregion

    #region CSV エクスポート処理

    /// <summary>
    /// 抽出したテキストを CSV ファイルにエクスポート
    /// </summary>
    private void ExportToCSV()
    {
        if (extractedEntries.Count == 0)
        {
            EditorUtility.DisplayDialog("エラー", "エクスポートするテキストがありません", "OK");
            return;
        }

        // 出力ディレクトリの作成
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }

        // CSV ファイルパス
        string filePath = Path.Combine(outputPath, fileName);

        try
        {
            using (StreamWriter writer = new StreamWriter(filePath, false, new UTF8Encoding(true))) // BOM付きUTF-8
            {
                // ヘッダー行を書き込み
                writer.WriteLine(CSV_HEADER);

                // データ行を書き込み（キーでソート）
                var sortedEntries = extractedEntries.OrderBy(e => e.key);
                foreach (var entry in sortedEntries)
                {
                    string csvLine = FormatCSVLine(entry);
                    writer.WriteLine(csvLine);
                }
            }

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "成功",
                $"CSV ファイルをエクスポートしました\n{filePath}\n\n{extractedEntries.Count} 件のテキスト",
                "OK"
            );

            // エクスポート後、ファイルを選択状態にする
            Object csvAsset = AssetDatabase.LoadAssetAtPath<Object>(filePath);
            if (csvAsset != null)
            {
                Selection.activeObject = csvAsset;
                EditorGUIUtility.PingObject(csvAsset);
            }

            if (debugMode)
            {
                Debug.Log($"TextMeshProLocalizationExporter: CSV エクスポート完了 - {filePath}");
            }
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("エラー", $"CSV エクスポートに失敗しました\n{e.Message}", "OK");
            Debug.LogError($"TextMeshProLocalizationExporter: エクスポートエラー - {e}");
        }
    }

    /// <summary>
    /// LocalizationEntry を CSV 行にフォーマット
    /// </summary>
    private string FormatCSVLine(LocalizationEntry entry)
    {
        // CSV エスケープ処理
        string key = EscapeCSVField(entry.key);
        string id = entry.id;
        string japanese = EscapeCSVField(entry.japaneseText);
        string english = EscapeCSVField(entry.englishText);

        return $"{key},{id},{japanese},{english}";
    }

    /// <summary>
    /// CSV フィールドのエスケープ処理
    /// </summary>
    private string EscapeCSVField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        // カンマ、改行、ダブルクォートが含まれている場合はダブルクォートで囲む
        if (field.Contains(",") || field.Contains("\n") || field.Contains("\"") || field.Contains("\r"))
        {
            // ダブルクォートを二重にエスケープ
            field = field.Replace("\"", "\"\"");
            return $"\"{field}\"";
        }

        return field;
    }

    #endregion
}