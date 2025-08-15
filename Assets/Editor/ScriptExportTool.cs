using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Assets/Script内のC#ファイルをtxtデータに出力するUnityエディターツール
/// GitHub連携機能を追加
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
    [SerializeField] private Vector2 gitScrollPosition; // Git結果用スクロール位置

    private const string SCRIPT_FOLDER_PATH = "Assets/Script"; // スクリプトフォルダーのパス
    private const string OUTPUT_EXTENSION = ".txt"; // 出力ファイルの拡張子
    private const int MAX_FILE_DISPLAY_COUNT = 50; // 最大表示ファイル数

    private List<string> foundScriptPaths = new List<string>(); // 発見されたスクリプトパス
    private List<string> latestGitFiles = new List<string>(); // 最新のGitでpushされたファイル
    private int totalFoundFiles = 0; // 発見されたファイル総数
    private bool isScanning = false; // スキャン中フラグ
    private bool isGitScanning = false; // Git取得中フラグ
    private string lastGitCommitHash = ""; // 最新コミットのハッシュ

    // タブ管理
    private enum TabType
    {
        AllFiles,
        GitLatest
    }
    private TabType currentTab = TabType.AllFiles;

    #endregion

    #region Unity Menu

    /// <summary>
    /// メニューからツールウィンドウを開く
    /// </summary>
    [MenuItem("Tools/Script Export Tool")]
    public static void ShowWindow()
    {
        ScriptExportTool window = GetWindow<ScriptExportTool>("Script Export Tool");

        // 最小サイズ設定
        window.minSize = new Vector2(600, 700);

        // 初期サイズと位置を設定
        window.position = new Rect(
            (Screen.currentResolution.width - 600) / 2,   // 画面中央X
            (Screen.currentResolution.height - 500) / 2,  // 画面中央Y
            600,  // 初期幅
            700   // 初期高さ
        );

        // 最大サイズ設定（オプション）
        window.maxSize = new Vector2(1200, 800);

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
        DrawTabs();

        switch (currentTab)
        {
            case TabType.AllFiles:
                DrawFileList();
                DrawExportSection();
                break;
            case TabType.GitLatest:
                DrawGitSection();
                break;
        }
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
    /// タブを描画
    /// </summary>
    private void DrawTabs()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Toggle(currentTab == TabType.AllFiles, "全ファイル", "Button"))
        {
            currentTab = TabType.AllFiles;
        }

        if (GUILayout.Toggle(currentTab == TabType.GitLatest, "GitHub最新Push", "Button"))
        {
            currentTab = TabType.GitLatest;
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        DrawSeparator();
    }

    /// <summary>
    /// GitHub最新セクションを描画
    /// </summary>
    private void DrawGitSection()
    {
        EditorGUILayout.LabelField("GitHub最新Pushファイル", EditorStyles.boldLabel);

        EditorGUILayout.Space(5);

        // Git取得ボタン
        if (GUILayout.Button("最新のPushファイルを取得", GUILayout.Height(30)))
        {
            GetLatestGitPushedFiles();
        }

        if (isGitScanning)
        {
            EditorGUILayout.LabelField("Git情報を取得中...", EditorStyles.centeredGreyMiniLabel);
            return;
        }

        // コミット情報表示
        if (!string.IsNullOrEmpty(lastGitCommitHash))
        {
            EditorGUILayout.LabelField($"最新コミット: {lastGitCommitHash}", EditorStyles.miniLabel);
        }

        // ファイルリスト表示
        if (latestGitFiles.Count > 0)
        {
            EditorGUILayout.LabelField($"見つかったC#ファイル: {latestGitFiles.Count}個");

            gitScrollPosition = EditorGUILayout.BeginScrollView(gitScrollPosition, GUILayout.Height(200));

            foreach (string file in latestGitFiles)
            {
                EditorGUILayout.LabelField(file, EditorStyles.miniLabel);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            // エクスポートボタン
            GUI.enabled = latestGitFiles.Count > 0 && !string.IsNullOrEmpty(outputDirectory);

            if (GUILayout.Button("最新Pushファイルを出力", GUILayout.Height(30)))
            {
                ExportGitLatestFiles();
            }

            GUI.enabled = true;
        }
        else if (!isGitScanning && lastGitCommitHash != "")
        {
            EditorGUILayout.LabelField("最新のPushにC#ファイルは含まれていません", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.Space(5);

        if (GUILayout.Button("出力フォルダーを開く", GUILayout.Height(25)))
        {
            OpenOutputDirectory();
        }
    }

    /// <summary>
    /// 最新のGit pushファイルを取得
    /// </summary>
    private void GetLatestGitPushedFiles()
    {
        isGitScanning = true;
        latestGitFiles.Clear();
        lastGitCommitHash = "";

        try
        {
            // Gitコマンドを実行して最新のpushファイルを取得
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "diff --name-only HEAD~1 HEAD",
                WorkingDirectory = Application.dataPath.Replace("/Assets", ""),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(error))
                {
                    Debug.LogError($"Git エラー: {error}");
                    EditorUtility.DisplayDialog("エラー", "Gitコマンドの実行に失敗しました。\nGitがインストールされていることを確認してください。", "OK");
                }
                else
                {
                    // コミットハッシュを取得
                    GetLatestCommitHash();

                    // ファイルリストを処理
                    string[] files = output.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string file in files)
                    {
                        // Assets/Script内のC#ファイルのみフィルタリング
                        if (file.StartsWith("Assets/Script") && file.EndsWith(".cs"))
                        {
                            latestGitFiles.Add(file);
                        }
                    }

                    if (debugMode)
                    {
                        Debug.Log($"最新Push: {latestGitFiles.Count}個のC#ファイルを検出");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Git取得エラー: {e.Message}");
            EditorUtility.DisplayDialog("エラー", "Gitコマンドの実行に失敗しました。", "OK");
        }
        finally
        {
            isGitScanning = false;
        }
    }

    /// <summary>
    /// 最新のコミットハッシュを取得
    /// </summary>
    private void GetLatestCommitHash()
    {
        try
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "rev-parse --short HEAD",
                WorkingDirectory = Application.dataPath.Replace("/Assets", ""),
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(startInfo))
            {
                lastGitCommitHash = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"コミットハッシュ取得エラー: {e.Message}");
        }
    }

    /// <summary>
    /// Git最新ファイルをエクスポート
    /// </summary>
    private void ExportGitLatestFiles()
    {
        if (latestGitFiles.Count == 0)
        {
            EditorUtility.DisplayDialog("エラー", "出力するファイルがありません", "OK");
            return;
        }

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // GitHub最新用のサブフォルダを作成
        string gitOutputDir = Path.Combine(outputDirectory, $"GitLatest_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(gitOutputDir);

        int successCount = 0;
        int errorCount = 0;

        EditorUtility.DisplayProgressBar("Git最新ファイル出力中", "処理中...", 0f);

        try
        {
            for (int i = 0; i < latestGitFiles.Count; i++)
            {
                float progress = (float)i / latestGitFiles.Count;
                string fileName = Path.GetFileName(latestGitFiles[i]);

                EditorUtility.DisplayProgressBar("Git最新ファイル出力中", $"処理中: {fileName}", progress);

                string fullPath = Path.Combine(Application.dataPath.Replace("/Assets", ""), latestGitFiles[i]);

                if (File.Exists(fullPath))
                {
                    if (ExportSingleScriptToDirectory(fullPath, gitOutputDir))
                    {
                        successCount++;
                    }
                    else
                    {
                        errorCount++;
                    }
                }
                else
                {
                    Debug.LogWarning($"ファイルが見つかりません: {fullPath}");
                    errorCount++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        string message = $"Git最新ファイル出力完了\n成功: {successCount}個\nエラー: {errorCount}個\n\n出力先:\n{gitOutputDir}";
        EditorUtility.DisplayDialog("出力結果", message, "OK");

        if (debugMode)
        {
            Debug.Log($"Git最新ファイル出力: 成功{successCount}個、エラー{errorCount}個");
        }
    }

    /// <summary>
    /// 単一ファイルを指定ディレクトリに出力
    /// </summary>
    private bool ExportSingleScriptToDirectory(string scriptPath, string targetDirectory)
    {
        try
        {
            string scriptContent = File.ReadAllText(scriptPath);
            string fileName = Path.GetFileNameWithoutExtension(scriptPath);

            if (addFileExtensionToName)
            {
                fileName += ".cs";
            }

            string outputFileName = fileName + OUTPUT_EXTENSION;
            string outputPath = Path.Combine(targetDirectory, outputFileName);

            File.WriteAllText(outputPath, scriptContent);

            if (debugMode)
            {
                Debug.Log($"出力完了: {outputPath}");
            }

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"ファイル出力エラー: {scriptPath}: {e.Message}");
            return false;
        }
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