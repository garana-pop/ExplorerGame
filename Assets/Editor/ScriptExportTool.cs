using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/Script内のC#ファイルをtxtデータに出力するUnityエディターツール
/// </summary>
public class ScriptExportTool : EditorWindow
{
    #region Private Fields

    [SerializeField] private bool debugMode = true; // デバッグモード
    [SerializeField] private string outputDirectory = "C:/Users/wakam/Desktop/アイデア/ファイル整理ゲーム/Claudeに提供するもの/スクリプト"; // 出力ディレクトリ
    [SerializeField] private bool includeSubfolders = true; // サブフォルダーを含むかどうか
    [SerializeField] private bool addFileExtensionToName = true; // ファイル名に拡張子を追加するか
    [SerializeField] private bool createSubfolderStructure = true; // フォルダー構造を再現するか
    [SerializeField] private Vector2 scrollPosition; // スクロール位置

    private const string SCRIPT_FOLDER_PATH = "Assets/Script"; // スクリプトフォルダーのパス
    private const string OUTPUT_EXTENSION = ".txt"; // 出力ファイルの拡張子
    private const int MAX_FILE_DISPLAY_COUNT = 50; // 最大表示ファイル数

    private List<string> foundScriptPaths = new List<string>(); // 発見されたスクリプトパス
    private int totalFoundFiles = 0; // 発見されたファイル総数
    private bool isScanning = false; // スキャン中フラグ

    #endregion

    #region Unity Menu

    /// <summary>
    /// メニューからツールウィンドウを開く
    /// </summary>
    [MenuItem("Tools/Script Export Tool")]
    public static void ShowWindow()
    {
        ScriptExportTool window = GetWindow<ScriptExportTool>("Script Export Tool");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }

    #endregion

    #region Unity Callbacks

    /// <summary>
    /// エディターウィンドウの有効化時に呼ばれる
    /// </summary>
    private void OnEnable()
    {
        // デフォルトの出力ディレクトリを設定
        if (string.IsNullOrEmpty(outputDirectory))
        {
            outputDirectory = Path.Combine(Application.dataPath, "ExportedScripts");
        }

        // 初回スキャン実行
        ScanScriptFiles();
    }

    /// <summary>
    /// GUIを描画
    /// </summary>
    private void OnGUI()
    {
        DrawHeader();
        DrawSettings();
        DrawFileList();
        DrawExportSection();
    }

    #endregion

    #region GUI Drawing Methods

    /// <summary>
    /// ヘッダーを描画
    /// </summary>
    private void DrawHeader()
    {
        EditorGUILayout.Space(10);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };

        EditorGUILayout.LabelField("Script Export Tool", titleStyle);
        EditorGUILayout.LabelField("Assets/Script内のC#ファイルをtxtに出力", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.Space(10);
        DrawSeparator();
    }

    /// <summary>
    /// 設定セクションを描画
    /// </summary>
    private void DrawSettings()
    {
        EditorGUILayout.LabelField("設定", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();

        // 出力ディレクトリ設定
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("出力ディレクトリ:", GUILayout.Width(100));
        outputDirectory = EditorGUILayout.TextField(outputDirectory);
        if (GUILayout.Button("選択", GUILayout.Width(50)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("出力ディレクトリを選択", outputDirectory, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                outputDirectory = selectedPath;
            }
        }
        EditorGUILayout.EndHorizontal();

        // オプション設定
        includeSubfolders = EditorGUILayout.Toggle("サブフォルダーを含む", includeSubfolders);
        addFileExtensionToName = EditorGUILayout.Toggle("ファイル名に拡張子を追加", addFileExtensionToName);
        createSubfolderStructure = EditorGUILayout.Toggle("フォルダー構造を再現", createSubfolderStructure);
        debugMode = EditorGUILayout.Toggle("デバッグモード", debugMode);

        if (EditorGUI.EndChangeCheck())
        {
            ScanScriptFiles();
        }

        EditorGUILayout.Space(5);
        DrawSeparator();
    }

    /// <summary>
    /// ファイルリストを描画
    /// </summary>
    private void DrawFileList()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("発見されたファイル", EditorStyles.boldLabel);

        if (GUILayout.Button("再スキャン", GUILayout.Width(80)))
        {
            ScanScriptFiles();
        }
        EditorGUILayout.EndHorizontal();

        if (isScanning)
        {
            EditorGUILayout.LabelField("スキャン中...", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        if (totalFoundFiles > 0)
        {
            EditorGUILayout.LabelField($"合計: {totalFoundFiles}個のC#ファイルが見つかりました");

            // ファイルリスト表示
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));

            int displayCount = Mathf.Min(foundScriptPaths.Count, MAX_FILE_DISPLAY_COUNT);
            for (int i = 0; i < displayCount; i++)
            {
                string relativePath = foundScriptPaths[i].Replace(Application.dataPath, "Assets");
                EditorGUILayout.LabelField($"{i + 1}. {relativePath}", EditorStyles.miniLabel);
            }

            if (totalFoundFiles > MAX_FILE_DISPLAY_COUNT)
            {
                EditorGUILayout.LabelField($"... 他{totalFoundFiles - MAX_FILE_DISPLAY_COUNT}個", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.LabelField("C#ファイルが見つかりませんでした", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.Space(5);
        DrawSeparator();
    }

    /// <summary>
    /// エクスポートセクションを描画
    /// </summary>
    private void DrawExportSection()
    {
        EditorGUILayout.LabelField("エクスポート", EditorStyles.boldLabel);

        GUI.enabled = totalFoundFiles > 0 && !string.IsNullOrEmpty(outputDirectory);

        if (GUILayout.Button("すべてのスクリプトを出力", GUILayout.Height(30)))
        {
            ExportAllScripts();
        }

        GUI.enabled = true;

        EditorGUILayout.Space(5);

        if (GUILayout.Button("出力フォルダーを開く", GUILayout.Height(25)))
        {
            OpenOutputDirectory();
        }
    }

    /// <summary>
    /// セパレーターを描画
    /// </summary>
    private void DrawSeparator()
    {
        EditorGUILayout.Space(5);
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        EditorGUILayout.Space(5);
    }

    #endregion

    #region Core Methods

    /// <summary>
    /// スクリプトファイルをスキャン
    /// </summary>
    private void ScanScriptFiles()
    {
        isScanning = true;
        foundScriptPaths.Clear();
        totalFoundFiles = 0;

        try
        {
            if (!Directory.Exists(SCRIPT_FOLDER_PATH))
            {
                if (debugMode)
                {
                    Debug.LogWarning($"{nameof(ScriptExportTool)}: スクリプトフォルダーが見つかりません: {SCRIPT_FOLDER_PATH}");
                }
                return;
            }

            string fullScriptPath = Path.Combine(Application.dataPath, "Script");
            SearchOption searchOption = includeSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            string[] scriptFiles = Directory.GetFiles(fullScriptPath, "*.cs", searchOption);

            foundScriptPaths.AddRange(scriptFiles);
            totalFoundFiles = scriptFiles.Length;

            if (debugMode)
            {
                Debug.Log($"{nameof(ScriptExportTool)}: {totalFoundFiles}個のC#ファイルを発見しました");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{nameof(ScriptExportTool)}: スキャン中にエラーが発生しました: {e.Message}");
        }
        finally
        {
            isScanning = false;
        }
    }

    /// <summary>
    /// すべてのスクリプトを出力
    /// </summary>
    private void ExportAllScripts()
    {
        if (foundScriptPaths.Count == 0)
        {
            EditorUtility.DisplayDialog("エラー", "出力するファイルがありません", "OK");
            return;
        }

        if (!Directory.Exists(outputDirectory))
        {
            try
            {
                Directory.CreateDirectory(outputDirectory);
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("エラー", $"出力ディレクトリの作成に失敗しました:\n{e.Message}", "OK");
                return;
            }
        }

        int successCount = 0;
        int errorCount = 0;

        EditorUtility.DisplayProgressBar("スクリプト出力中", "ファイルを処理しています...", 0f);

        try
        {
            for (int i = 0; i < foundScriptPaths.Count; i++)
            {
                string scriptPath = foundScriptPaths[i];
                float progress = (float)i / foundScriptPaths.Count;

                string fileName = Path.GetFileNameWithoutExtension(scriptPath);
                EditorUtility.DisplayProgressBar("スクリプト出力中", $"処理中: {fileName}", progress);

                if (ExportSingleScript(scriptPath))
                {
                    successCount++;
                }
                else
                {
                    errorCount++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // 結果ダイアログ表示
        string message = $"出力完了\n成功: {successCount}個\nエラー: {errorCount}個";
        EditorUtility.DisplayDialog("出力結果", message, "OK");

        if (debugMode)
        {
            Debug.Log($"{nameof(ScriptExportTool)}: {message.Replace('\n', ' ')}");
        }
    }

    /// <summary>
    /// 単一のスクリプトファイルを出力
    /// </summary>
    private bool ExportSingleScript(string scriptPath)
    {
        try
        {
            string scriptContent = File.ReadAllText(scriptPath);
            string fileName = Path.GetFileNameWithoutExtension(scriptPath);

            // ファイル名に拡張子を追加するかどうか
            if (addFileExtensionToName)
            {
                fileName += ".cs";
            }

            string outputFileName = fileName + OUTPUT_EXTENSION;
            string outputPath;

            if (createSubfolderStructure)
            {
                // フォルダー構造を再現
                string fullScriptDir = Path.Combine(Application.dataPath, "Script");
                string relativePath = Path.GetRelativePath(fullScriptDir, Path.GetDirectoryName(scriptPath));
                string outputSubDir = Path.Combine(outputDirectory, relativePath);

                if (!Directory.Exists(outputSubDir))
                {
                    Directory.CreateDirectory(outputSubDir);
                }

                outputPath = Path.Combine(outputSubDir, outputFileName);
            }
            else
            {
                outputPath = Path.Combine(outputDirectory, outputFileName);
            }

            File.WriteAllText(outputPath, scriptContent);

            if (debugMode)
            {
                Debug.Log($"{nameof(ScriptExportTool)}: 出力完了 - {outputPath}");
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{nameof(ScriptExportTool)}: ファイル出力エラー - {scriptPath}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 出力ディレクトリを開く
    /// </summary>
    private void OpenOutputDirectory()
    {
        if (Directory.Exists(outputDirectory))
        {
            EditorUtility.RevealInFinder(outputDirectory);
        }
        else
        {
            EditorUtility.DisplayDialog("エラー", "出力ディレクトリが存在しません", "OK");
        }
    }

    #endregion
}