using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PDFファイルのアイコンをPdfDocumentManagerの完了状態に応じて変更するコンポーネント
/// </summary>
public class PdfFileIconChange : MonoBehaviour
{
    [Header("アイコン設定")]
    [Tooltip("変更前のアイコンスプライト")]
    [SerializeField] private Sprite defaultSprite;

    [Tooltip("変更後のアイコンスプライト")]
    [SerializeField] private Sprite completedSprite;

    [Header("参照設定")]
    [Tooltip("参照するPdfDocumentManagerコンポーネント（未設定の場合は自動検索）")]
    [SerializeField] private PdfDocumentManager pdfDocumentManager;

    [Tooltip("変更対象のImageコンポーネント（未設定の場合は自身のImageを使用）")]
    [SerializeField] private Image iconImage;

    [Header("デバッグ設定")]
    [Tooltip("デバッグログを表示するかどうか")]
    [SerializeField] private bool debugMode = false;

    // 完了状態を示すフラグ
    private bool isCompleted = false;

    private void Awake()
    {
        // Imageコンポーネントが設定されていなければ自身から取得
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
            if (iconImage == null)
            {
                Debug.LogError("PdfFileIconChange: Imageコンポーネントが見つかりません。このスクリプトはImageコンポーネントがアタッチされたオブジェクトに追加してください。");
                enabled = false;
                return;
            }
        }

        // デフォルトスプライトを設定
        if (defaultSprite != null && iconImage.sprite == null)
        {
            iconImage.sprite = defaultSprite;
        }
    }

    private void Start()
    {
        // PdfDocumentManagerが設定されていなければ自動検索
        if (pdfDocumentManager == null)
        {
            FindPdfDocumentManager();
        }

        // 初期状態の確認
        CheckCompletionState();
    }

    private void OnEnable()
    {
        // オブジェクトが有効になるたびに完了状態を確認
        CheckCompletionState();
    }

    /// <summary>
    /// 参照するPdfDocumentManagerを検索
    /// </summary>
    private void FindPdfDocumentManager()
    {
        // 親階層をたどってPdfDocumentManagerを検索
        Transform current = transform.parent;
        while (current != null)
        {
            PdfDocumentManager manager = current.GetComponent<PdfDocumentManager>();
            if (manager != null)
            {
                pdfDocumentManager = manager;
                if (debugMode)
                {
                    Debug.Log($"PdfFileIconChange: 親階層からPdfDocumentManagerを自動検出しました: {current.name}");
                }
                return;
            }
            current = current.parent;
        }

        // 同じPDFファイルパネル内を検索
        Transform filePanel = transform;
        while (filePanel != null && !filePanel.name.Contains("FilePanel"))
        {
            filePanel = filePanel.parent;
        }

        if (filePanel != null)
        {
            PdfDocumentManager manager = filePanel.GetComponentInChildren<PdfDocumentManager>(true);
            if (manager != null)
            {
                pdfDocumentManager = manager;
                if (debugMode)
                {
                    Debug.Log($"PdfFileIconChange: ファイルパネル内からPdfDocumentManagerを自動検出しました: {filePanel.name}");
                }
                return;
            }
        }

        // それでも見つからない場合は警告
        Debug.LogWarning("PdfFileIconChange: 参照するPdfDocumentManagerが見つかりませんでした。インスペクターで手動設定してください。");
    }

    /// <summary>
    /// 完了状態を確認して適用するパブリックメソッド
    /// </summary>
    public void CheckCompletionState()
    {
        if (pdfDocumentManager == null || iconImage == null) return;

        bool currentState = pdfDocumentManager.IsDocumentCompleted();

        // 状態が変化した場合のみ処理
        if (currentState != isCompleted)
        {
            isCompleted = currentState;

            if (isCompleted)
            {
                // 完了状態のスプライトに変更
                if (completedSprite != null)
                {
                    iconImage.sprite = completedSprite;
                    if (debugMode)
                    {
                        Debug.Log($"PdfFileIconChange: アイコンを完了状態に変更しました - {gameObject.name}");
                    }
                }
            }
            else
            {
                // デフォルト状態のスプライトに変更
                if (defaultSprite != null)
                {
                    iconImage.sprite = defaultSprite;
                    if (debugMode)
                    {
                        Debug.Log($"PdfFileIconChange: アイコンをデフォルト状態に変更しました - {gameObject.name}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 参照するPdfDocumentManagerを手動で設定
    /// </summary>
    public void SetPdfDocumentManager(PdfDocumentManager manager)
    {
        pdfDocumentManager = manager;

        // 設定後にすぐに状態をチェック
        CheckCompletionState();
    }

    /// <summary>
    /// 手動でアイコンを完了状態に設定
    /// </summary>
    public void SetCompleted(bool completed)
    {
        if (iconImage == null) return;

        isCompleted = completed;

        if (completed && completedSprite != null)
        {
            iconImage.sprite = completedSprite;
        }
        else if (defaultSprite != null)
        {
            iconImage.sprite = defaultSprite;
        }
    }
}