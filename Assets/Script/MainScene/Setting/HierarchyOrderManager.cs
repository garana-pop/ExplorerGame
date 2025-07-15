using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// DraggingCanvas内の子オブジェクトの階層順序を管理するクラス
/// 特に設定画面表示時に、FilePanel→Overlay→SettingsPanelの順序を保証する
/// </summary>
public class HierarchyOrderManager : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] private Canvas draggingCanvas;
    [SerializeField] private GameObject overlay;
    [SerializeField] private GameObject settingsPanel;

    [Header("デバッグ")]
    [SerializeField] private bool debugMode = false;

    // MainSceneSettingsManagerへの参照
    private MainSceneSettingsManager settingsManager;

    // 有効なファイルパネルリスト（キャッシュ）
    private List<GameObject> filePanels = new List<GameObject>();

    private void Awake()
    {
        // 参照の初期化
        InitializeReferences();
    }

    private void Start()
    {
        // MainSceneSettingsManagerとの連携（Start内で行う）
        ConnectToSettingsManager();
    }

    private void OnEnable()
    {
        // オブジェクトが有効になった時にも参照を確認
        if (draggingCanvas == null || overlay == null || settingsPanel == null)
        {
            InitializeReferences();
        }
    }

    /// <summary>
    /// 必要な参照を初期化する
    /// </summary>
    private void InitializeReferences()
    {
        // DraggingCanvasが設定されていない場合は自動検索
        if (draggingCanvas == null)
        {
            draggingCanvas = GameObject.Find("DraggingCanvas")?.GetComponent<Canvas>();
            if (draggingCanvas == null)
            {
                Debug.LogError("HierarchyOrderManager: DraggingCanvasが見つかりません。");
                enabled = false;
                return;
            }
        }

        // Overlayが設定されていない場合は自動検索
        if (overlay == null)
        {
            overlay = draggingCanvas.transform.Find("Overlay")?.gameObject;
            if (overlay == null)
            {
                Debug.LogWarning("HierarchyOrderManager: Overlayが見つかりません。DraggingCanvas/Overlayを作成してください。");
            }
        }

        // SettingsPanelが設定されていない場合は自動検索
        if (settingsPanel == null)
        {
            // DraggingCanvas内の「SettingsPanel」という名前のオブジェクトを検索
            settingsPanel = FindObjectWithNameInCanvas("SettingsPanel");
            if (settingsPanel == null)
            {
                Debug.LogWarning("HierarchyOrderManager: SettingsPanelが見つかりません。手動で設定してください。");
            }
        }

        // ファイルパネルのリストを最新化
        UpdateFilePanelsList();

        // 初期状態ではOverlayを非アクティブにする
        SetOverlayActive(false);

        if (debugMode)
        {
            LogCurrentHierarchy("初期化後のヒエラルキー");
        }
    }

    /// <summary>
    /// MainSceneSettingsManagerを検索して連携する
    /// </summary>
    private void ConnectToSettingsManager()
    {
        settingsManager = FindFirstObjectByType<MainSceneSettingsManager>();

        if (settingsManager != null)
        {
            // SettingsManagerのメソッドを探して実行時に接続するための処理
            // 反射を使用してプライベートメソッドにアクセス（非推奨だがやむを得ない場合）
            var settingsType = settingsManager.GetType();

            // ShowSettingsとHideSettingsを監視する
            // Update内で状態変化を検出するように変更
            if (debugMode)
            {
                Debug.Log("HierarchyOrderManager: MainSceneSettingsManagerと連携しました");
            }
        }
        else
        {
            Debug.LogWarning("HierarchyOrderManager: MainSceneSettingsManagerが見つかりません");
        }
    }

    private void Update()
    {
        // settingsManagerが存在し、settingsPanelも取得できている場合
        if (settingsManager != null && settingsPanel != null)
        {
            // SettingsPanelのアクティブ状態に基づいてOverlayを制御
            bool settingsActive = settingsPanel.activeInHierarchy;

            // SettingsPanelがアクティブで、Overlayが非アクティブの場合
            if (settingsActive && overlay != null && !overlay.activeInHierarchy)
            {
                // 設定画面が表示された→階層順序を調整してOverlayをアクティブに
                AdjustHierarchyOrderForSettings();
                SetOverlayActive(true);

                if (debugMode)
                {
                    Debug.Log("HierarchyOrderManager: 設定画面が表示されました。Overlayをアクティブにします。");
                }
            }
            // SettingsPanelが非アクティブで、Overlayがアクティブの場合
            else if (!settingsActive && overlay != null && overlay.activeInHierarchy)
            {
                // 設定画面が閉じられた→Overlayを非アクティブに
                SetOverlayActive(false);

                if (debugMode)
                {
                    Debug.Log("HierarchyOrderManager: 設定画面が閉じられました。Overlayを非アクティブにします。");
                }
            }
        }
    }

    /// <summary>
    /// Overlayの表示/非表示を設定
    /// </summary>
    /// <param name="active">表示する場合はtrue、非表示の場合はfalse</param>
    public void SetOverlayActive(bool active)
    {
        if (overlay != null && overlay.activeSelf != active)
        {
            overlay.SetActive(active);

            if (debugMode)
            {
                Debug.Log($"HierarchyOrderManager: Overlayを{(active ? "表示" : "非表示")}にしました");
            }
        }
    }

    /// <summary>
    /// 設定パネル表示前に呼ばれる階層順序調整メソッド
    /// </summary>
    public void AdjustHierarchyOrderForSettings()
    {
        if (draggingCanvas == null)
        {
            Debug.LogError("HierarchyOrderManager: DraggingCanvasが設定されていません");
            return;
        }

        // ファイルパネルのリストを最新化
        UpdateFilePanelsList();

        if (debugMode)
        {
            LogCurrentHierarchy("調整前のヒエラルキー");
        }

        // FilePanelを最も下に配置
        foreach (GameObject filePanel in filePanels)
        {
            if (filePanel != null && filePanel.activeInHierarchy)
            {
                filePanel.transform.SetAsFirstSibling();
                if (debugMode)
                {
                    Debug.Log($"HierarchyOrderManager: {filePanel.name}を最下部に移動しました");
                }
            }
        }

        // Overlayを中間に配置（存在する場合）
        if (overlay != null)
        {
            // FilePanelの上、SettingsPanelの下に配置
            int targetIndex = 0;
            // アクティブなFilePanelの数をカウント
            foreach (GameObject panel in filePanels)
            {
                if (panel != null && panel.activeInHierarchy)
                {
                    targetIndex++;
                }
            }

            overlay.transform.SetSiblingIndex(targetIndex);
            if (debugMode)
            {
                Debug.Log($"HierarchyOrderManager: Overlayを位置 {targetIndex} に移動しました");
            }
        }

        // SettingsPanelを最も上に配置（存在する場合）
        if (settingsPanel != null)
        {
            settingsPanel.transform.SetAsLastSibling();
            if (debugMode)
            {
                Debug.Log("HierarchyOrderManager: SettingsPanelを最上部に移動しました");
            }
        }

        if (debugMode)
        {
            LogCurrentHierarchy("調整後のヒエラルキー");
        }
    }

    /// <summary>
    /// DraggingCanvas内の現在のFilePanelを探索して更新
    /// </summary>
    private void UpdateFilePanelsList()
    {
        if (draggingCanvas == null) return;

        filePanels.Clear();

        for (int i = 0; i < draggingCanvas.transform.childCount; i++)
        {
            Transform child = draggingCanvas.transform.GetChild(i);
            // FilePanelを含む名前のオブジェクトを探す
            if (child.name.Contains("FilePanel"))
            {
                filePanels.Add(child.gameObject);
                if (debugMode)
                {
                    Debug.Log($"HierarchyOrderManager: ファイルパネルを検出: {child.name}");
                }
            }
        }
    }

    /// <summary>
    /// DraggingCanvas内で特定の名前を含むオブジェクトを検索
    /// </summary>
    private GameObject FindObjectWithNameInCanvas(string nameContains)
    {
        if (draggingCanvas == null) return null;

        for (int i = 0; i < draggingCanvas.transform.childCount; i++)
        {
            Transform child = draggingCanvas.transform.GetChild(i);
            if (child.name.Contains(nameContains))
            {
                return child.gameObject;
            }
        }

        return null;
    }

    /// <summary>
    /// 現在のヒエラルキー状態をログに出力（デバッグ用）
    /// </summary>
    private void LogCurrentHierarchy(string message)
    {
        if (!debugMode || draggingCanvas == null) return;

        Debug.Log($"--- {message} ---");

        for (int i = 0; i < draggingCanvas.transform.childCount; i++)
        {
            Transform child = draggingCanvas.transform.GetChild(i);
            Debug.Log($"位置 {i}: {child.name} (アクティブ: {child.gameObject.activeInHierarchy})");
        }

        Debug.Log("------------------------");
    }

    /// <summary>
    /// 手動でヒエラルキー調整を呼び出すためのパブリックメソッド
    /// </summary>
    public void ManualAdjustHierarchy()
    {
        AdjustHierarchyOrderForSettings();
    }

    /// <summary>
    /// 設定画面が閉じられたときに呼び出されるメソッド
    /// </summary>
    public void OnSettingsClosed()
    {
        SetOverlayActive(false);

        if (debugMode)
        {
            Debug.Log("HierarchyOrderManager: 設定画面クローズ時の処理を実行しました");
        }
    }

    private void OnDestroy()
    {
        // リソースのクリーンアップ（必要に応じて）
    }
}