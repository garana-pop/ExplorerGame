using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class FileOpen : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject infoPanel; // 表示するパネル（インスペクターで設定）
    [SerializeField] private Canvas draggingCanvas; // 最前面用Canvas（インスペクターで設定）
    [SerializeField] private GameObject overlay; // 操作をブロックするオーバーレイ（インスペクターで設定）

    // パズル完了判定用の参照
    private PdfDocumentManager pdfDocManager;
    private ImageRevealer imageRevealer;
    private TxtPuzzleManager txtPuzzleManager;

    private RectTransform panelRectTransform; // パネルのRectTransform
    private Transform originalParent; // パネルの元の親を記録

    private void Awake()
    {
        // DraggingCanvasの設定確認と取得
        if (draggingCanvas == null)
        {
            draggingCanvas = GameObject.Find("DraggingCanvas")?.GetComponent<Canvas>();
            if (draggingCanvas == null)
            {
                Debug.LogError("DraggingCanvasが見つかりません。インスペクターで設定してください");
            }
        }

        // オーバーレイの設定確認
        if (overlay == null)
        {
            Debug.LogWarning("Overlayが設定されていません。操作ブロックが機能しません");
        }
        else
        {
            overlay.SetActive(false); // 初期状態で非アクティブ
        }

        // パネルの初期設定
        if (infoPanel != null)
        {
            panelRectTransform = infoPanel.GetComponent<RectTransform>();
            if (panelRectTransform == null)
            {
                Debug.LogError("InfoPanelにRectTransformがありません");
            }
            originalParent = infoPanel.transform.parent; // 元の親を記録
            SetActiveRecursive(infoPanel, false); // 子を含めて非アクティブに
        }

        // パズル進行管理クラスへの参照を取得
        pdfDocManager = GetComponentInChildren<PdfDocumentManager>(true);
        imageRevealer = GetComponentInChildren<ImageRevealer>(true);
        txtPuzzleManager = GetComponentInChildren<TxtPuzzleManager>(true);
    }

    private void Start()
    {
        // TxtPuzzleManagerの参照を取得
        txtPuzzleManager = GetComponentInChildren<TxtPuzzleManager>(true);
    }

    // TxtPuzzleManagerへのアクセサメソッドを追加
    public TxtPuzzleManager GetTxtPuzzleManager()
    {
        return txtPuzzleManager;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 2 && infoPanel != null && !eventData.dragging)
        {
            // パネルを開く前にTxtPuzzleManagerへの参照を保持
            txtPuzzleManager = infoPanel.GetComponentInChildren<TxtPuzzleManager>(true);

            SetActiveRecursive(infoPanel, true);
            infoPanel.transform.SetParent(draggingCanvas.transform, false);
            CenterPanelOnScreen();

            // TxtPuzzleManager存在確認と再接続処理を強化
            if (txtPuzzleManager != null)
            {
                // パネル表示時に強制的に再チェック（TXT専用パネルの場合）
                if (infoPanel.name.Contains("TXT") || infoPanel.name.EndsWith(".txt", System.StringComparison.OrdinalIgnoreCase))
                {
                    // 少し遅延させて確実に適用（TXTパネル固有処理）
                    StartCoroutine(DelayedPuzzleCheck(txtPuzzleManager));
                }
            }

            if (overlay != null)
            {
                overlay.SetActive(true);
            }
        }
    }
    // 新規追加: TXTパズル状態を遅延チェックするためのコルーチン
    private IEnumerator DelayedPuzzleCheck(TxtPuzzleManager puzzleManager)
    {
        yield return new WaitForSeconds(0.5f);

        // パズルマネージャーが完了状態ならForceCorrectStateを呼び出す
        if (puzzleManager.IsPuzzleCompleted())
        {
            puzzleManager.Invoke("ForceCorrectStateForAllAreas", 0.2f);

            // さらに少し遅れて検証
            puzzleManager.Invoke("VerifyCorrectStateForAllAreas", 1.2f);
        }
    }

    // 子オブジェクトを含めてアクティブ状態を変更するヘルパーメソッド
    private void SetActiveRecursive(GameObject obj, bool state)
    {
        obj.SetActive(state);
        foreach (Transform child in obj.transform)
        {
            SetActiveRecursive(child.gameObject, state);
        }
    }

    // パネルを画面中央に配置するメソッド
    private void CenterPanelOnScreen()
    {
        if (panelRectTransform == null || draggingCanvas == null) return;

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        panelRectTransform.position = screenCenter;
    }

    // パネルの元の親を取得するメソッド（FileClose用）
    public Transform GetOriginalParent()
    {
        return originalParent;
    }

    // パズルが完了しているか確認するメソッド
    private bool IsPuzzleCompleted()
    {
        if (pdfDocManager != null && pdfDocManager.IsDocumentCompleted())
            return true;

        if (imageRevealer != null && imageRevealer.IsImageRevealed())
            return true;

        if (txtPuzzleManager != null && txtPuzzleManager.IsPuzzleCompleted())
            return true;

        return false;
    }
    public void ClosePanel()
    {
        if (infoPanel != null)
        {
            // PDFマネージャーを明示的に取得
            PdfDocumentManager pdfManager = infoPanel.GetComponentInChildren<PdfDocumentManager>(true);
            bool pdfCompleted = false;
            GameObject nextFolder = null;

            // PDFが完了しているか確認し、次のフォルダーを取得
            if (pdfManager != null)
            {
                pdfCompleted = pdfManager.IsDocumentCompleted();

                // 次のフォルダーの参照を直接取得（リフレクションを避ける）
                if (pdfCompleted)
                {
                    // PDFマネージャーに次のフォルダーを直接アクティブ化するよう要求
                    pdfManager.EnsureNextFolderActive();
                }
            }

            // TXTマネージャーの確認（既存コード）
            TxtPuzzleManager txtManager = infoPanel.GetComponentInChildren<TxtPuzzleManager>(true);
            bool txtCompleted = txtManager != null && txtManager.IsPuzzleCompleted();

            // 通常のパネルを閉じる処理
            infoPanel.transform.SetParent(originalParent, false);
            SetActiveRecursive(infoPanel, false);

            if (overlay != null)
            {
                overlay.SetActive(false);
            }

            // PDF完了で次フォルダーの参照がない場合、直接PDFマネージャーに問い合わせ
            if (pdfCompleted && nextFolder == null && pdfManager != null)
            {
                // 修正：PDFマネージャーに次のフォルダを再アクティベートするよう指示
                StartCoroutine(DelayedPdfManagerActivation(pdfManager));
            }

            // TXT完了の場合（既存のロジック）
            if (txtCompleted && nextFolder == null && txtManager != null)
            {
                nextFolder = txtManager.GetNextFolder();
                if (nextFolder != null)
                {
                    StartCoroutine(ReactivateNextFolder(nextFolder));
                }
            }

            // ゲームの状態を保存して完了を確実にする
            if ((pdfCompleted || txtCompleted) && GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.SaveGame();
            }
        }
    }
    private IEnumerator DelayedPdfManagerActivation(PdfDocumentManager pdfManager)
    {
        // 他の処理が完了するまで少し待機
        yield return new WaitForSeconds(0.1f);

        // PDFマネージャーに次のフォルダーを確実に有効化するよう指示
        if (pdfManager != null)
        {
            pdfManager.EnsureNextFolderActive();
        }
    }
    private IEnumerator ReactivateNextFolder(GameObject nextFolder)
    {
        // 処理完了まで待機
        yield return new WaitForSeconds(0.1f);

        if (nextFolder != null)
        {
            // 先にアクティブ状態を確認
            bool wasActive = nextFolder.activeSelf;

            // 確実にアクティブにする
            nextFolder.SetActive(true);
            //Debug.Log($"次のフォルダーを再アクティブ化: {nextFolder.name}, 以前のアクティブ状態: {wasActive}");

            // FolderButtonScriptとFolderActivationGuardの両方を設定
            FolderButtonScript folderScript = nextFolder.GetComponent<FolderButtonScript>();
            if (folderScript == null)
                folderScript = nextFolder.GetComponentInParent<FolderButtonScript>();

            if (folderScript != null)
            {
                folderScript.SetActivatedState(true);
                folderScript.SetVisible(true);

                // ファイルパネルも確実に表示
                if (folderScript.filePanel != null && !folderScript.filePanel.activeSelf)
                {
                    folderScript.filePanel.SetActive(true);
                }

                //Debug.Log($"フォルダーボタンスクリプトを更新: {folderScript.GetFolderName()}");
            }

            FolderActivationGuard guard = nextFolder.GetComponent<FolderActivationGuard>();
            if (guard != null)
            {
                guard.SetActivated(true);
            }
        }
    }

    // パズル完了時に開放されるべきフォルダーを強制的にアクティブにする
    private void EnsureFoldersActive()
    {
        // PDFドキュメントマネージャーの確認
        if (pdfDocManager != null && pdfDocManager.IsDocumentCompleted())
        {
            ActivateNextFolder(pdfDocManager.gameObject);
        }

        // 画像リビーラーの確認
        if (imageRevealer != null && imageRevealer.IsImageRevealed())
        {
            ActivateNextFolder(imageRevealer.gameObject);
        }

        // テキストパズルマネージャーの確認
        if (txtPuzzleManager != null && txtPuzzleManager.IsPuzzleCompleted())
        {
            ActivateNextFolder(txtPuzzleManager.gameObject);
        }
    }

    // 対象コンポーネントからNextFolderOrFileを取得し、アクティブにする
    private void ActivateNextFolder(GameObject obj)
    {
        var fields = obj.GetType().GetFields(System.Reflection.BindingFlags.Instance |
                                              System.Reflection.BindingFlags.Public |
                                              System.Reflection.BindingFlags.NonPublic);

        foreach (var field in fields)
        {
            if (field.Name.Contains("nextFolder") || field.Name.Contains("NextFolder"))
            {
                var nextFolder = field.GetValue(obj.GetComponent(obj.GetType())) as GameObject;
                if (nextFolder != null)
                {
                    nextFolder.SetActive(true);
                    Debug.Log($"フォルダー {nextFolder.name} を強制的にアクティブにしました");
                }
            }
        }
    }
}