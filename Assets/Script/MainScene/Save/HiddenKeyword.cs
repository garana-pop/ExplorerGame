using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 隠されたキーワードをクリックして表示するためのコンポーネント
/// </summary>
public class HiddenKeyword : MonoBehaviour, IPointerClickHandler
{
    [Header("キーワード設定")]
    [Tooltip("隠されている単語")]
    [SerializeField] private string hiddenWord = "";

    [Tooltip("表示後の色")]
    [SerializeField] private Color revealedColor = new Color(1f, 0.2f, 0.2f, 1f);

    [Header("視覚設定")]
    [Tooltip("隠し文字のシンボル")]
    [SerializeField] private string censorSymbol = "█";

    [Tooltip("隠し文字の数（デフォルト5文字分）")]
    [SerializeField] private int censorSymbolCount = 5;

    [Header("参照設定")]
    [Tooltip("PDFドキュメントマネージャーへの直接参照（オプション）")]
    [SerializeField] private PdfDocumentManager documentManagerReference;

    [Tooltip("TextMeshProUGUIコンポーネントへの直接参照")]
    [SerializeField] private TextMeshProUGUI textComponentReference;

    [Header("デバッグ設定")]
    [Tooltip("デバッグログを表示するかどうか")]
    [SerializeField] private bool debugMode = false;

    // コンポーネント参照
    private TextMeshProUGUI textComponent;
    private Image backgroundImage;

    // 状態
    private bool isRevealed = false;

    // 親のPdfDocumentManager参照
    private PdfDocumentManager documentManager;

    private void Awake()
    {
        // コンポーネントの取得
        InitializeComponents();
    }

    private void OnTransformParentChanged()
    {
        // 親が変更されたときにPdfDocumentManager参照を更新
        UpdateDocumentManagerReference();
    }

    private void Start()
    {
        // 初期状態を適用
        ApplyVisualState();
    }

    private void OnEnable()
    {
        // アクティブになった時に参照更新を確実に行う
        InitializeComponents();

        // 状態に合わせて表示を更新
        ApplyVisualState();

        if (debugMode)
        {
            Debug.Log($"HiddenKeyword '{hiddenWord}' OnEnable: isRevealed={isRevealed}");
        }
    }

    /// <summary>
    /// 必要なコンポーネント参照を初期化する
    /// </summary>
    private void InitializeComponents()
    {
        // TextMeshProUGUIコンポーネントの取得
        UpdateTextComponent();

        // 背景イメージの取得
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        // documentManagerの設定
        UpdateDocumentManagerReference();
    }

    /// <summary>
    /// TextMeshProUGUIコンポーネントを確実に更新
    /// </summary>
    private void UpdateTextComponent()
    {
        // インスペクターで直接設定されている場合はそれを使用
        if (textComponentReference != null)
        {
            textComponent = textComponentReference;
            return;
        }

        // まず自分自身から検索
        textComponent = GetComponent<TextMeshProUGUI>();

        // 見つからない場合は子オブジェクトから検索
        if (textComponent == null)
        {
            textComponent = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // 見つからない場合は親オブジェクトから検索
        if (textComponent == null)
        {
            textComponent = GetComponentInParent<TextMeshProUGUI>();
        }

        // Line1Text, Line2Textなどの親オブジェクトを探す
        if (textComponent == null)
        {
            Transform current = transform;
            while (current != null && textComponent == null)
            {
                if (current.name.Contains("Line") && current.name.Contains("Text"))
                {
                    textComponent = current.GetComponent<TextMeshProUGUI>();
                    if (textComponent != null)
                    {
                        if (debugMode)
                        {
                            Debug.Log($"HiddenKeyword '{hiddenWord}': 親の {current.name} からテキストコンポーネントを見つけました");
                        }
                        break;
                    }
                }
                current = current.parent;
            }
        }

        if (textComponent == null)
        {
            // テキストオブジェクトが見つからなかった場合、インスペクターで設定できるようにする
            Debug.LogWarning($"HiddenKeyword '{name}': TextMeshProUGUIコンポーネントが見つかりません。インスペクターで直接指定してください。");
        }
        else if (debugMode)
        {
            Debug.Log($"HiddenKeyword '{hiddenWord}': TextMeshProUGUIコンポーネントを取得しました");
        }
    }

    // PdfDocumentManagerへの参照を更新
    private void UpdateDocumentManagerReference()
    {
        // インスペクターで設定された直接参照がある場合はそれを使用
        if (documentManagerReference != null)
        {
            documentManager = documentManagerReference;
            return;
        }

        // 親階層から検索
        PdfDocumentManager parentManager = GetComponentInParent<PdfDocumentManager>();
        if (parentManager != null)
        {
            documentManager = parentManager;
            return;
        }

        // 親階層で見つからない場合、自分のPDFFilePanel内を探す
        Transform current = transform;
        while (current != null)
        {
            if (current.name.Contains("PDFFilePanel"))
            {
                documentManager = current.GetComponentInChildren<PdfDocumentManager>(true);
                if (documentManager != null) break;
            }
            current = current.parent;
        }

        if (documentManager == null && debugMode)
        {
            Debug.LogWarning($"HiddenKeyword '{hiddenWord}': PdfDocumentManagerを見つけられませんでした");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isRevealed)
        {
            // クリック時に参照を確認
            if (documentManager == null)
            {
                UpdateDocumentManagerReference();
            }

            RevealKeyword();
        }
    }

    /// <summary>
    /// キーワードを表示します
    /// </summary>
    public void RevealKeyword()
    {
        if (isRevealed) return;

        // テキストコンポーネント確認
        if (textComponent == null)
        {
            UpdateTextComponent();
        }

        isRevealed = true;
        ApplyVisualState();

        // 効果音を再生
        SoundEffectManager.Instance?.PlayClickSound();

        // 親のPdfDocumentManagerに通知
        if (documentManager != null)
        {
            documentManager.OnKeywordRevealed(this);
        }
        else
        {
            // 最後の手段として再検索
            UpdateDocumentManagerReference();
            if (documentManager != null)
            {
                documentManager.OnKeywordRevealed(this);
            }
            else
            {
                Debug.LogWarning($"隠しキーワード '{hiddenWord}' のPdfDocumentManagerが見つかりません");
            }
        }
    }

    /// <summary>
    /// 外部からプログラム的に表示状態に設定
    /// </summary>
    public void ForceReveal()
    {
        // テキストコンポーネント確認（いつでも確実に取得）
        if (textComponent == null)
        {
            UpdateTextComponent();
        }

        // 強制的に表示状態に設定
        isRevealed = true;

        // 表示状態を確実に適用
        ApplyVisualState();

        if (debugMode)
        {
            Debug.Log($"HiddenKeyword '{hiddenWord}' を強制的に表示状態にしました");
        }
    }

    /// <summary>
    /// 視覚的な状態を適用
    /// </summary>
    private void ApplyVisualState()
    {
        // テキストコンポーネントがなければ取得を試みる
        if (textComponent == null)
        {
            UpdateTextComponent();
            if (textComponent == null)
            {
                // ここでエラーを表示するだけで終了せず、警告して継続
                Debug.LogWarning($"HiddenKeyword '{hiddenWord}': TextMeshProUGUIコンポーネントがないため表示更新できません。インスペクターで設定してください。");
                return; // それでも見つからなければ処理しない
            }
        }

        if (isRevealed)
        {
            // 表示状態 - 実際の単語を表示
            textComponent.text = hiddenWord;
            textComponent.color = revealedColor;

            if (debugMode)
            {
                Debug.Log($"HiddenKeyword '{hiddenWord}': 表示状態を適用しました");
            }

            // 背景の透明度を調整
            if (backgroundImage != null)
            {
                Color newColor = backgroundImage.color;
                newColor.a = 0.1f;
                backgroundImage.color = newColor;
            }
        }
        else
        {
            // 非表示状態（黒塗り）
            int count = (hiddenWord.Length > 0) ?
                Mathf.Max(3, hiddenWord.Length) : censorSymbolCount;

            string censorText = string.Empty;
            for (int i = 0; i < count; i++)
            {
                censorText += censorSymbol;
            }
            textComponent.text = censorText;

            if (debugMode)
            {
                Debug.Log($"HiddenKeyword '{hiddenWord}': 隠し状態を適用しました");
            }
        }
    }

    /// <summary>
    /// 隠されたキーワードを取得
    /// </summary>
    public string GetHiddenWord()
    {
        return hiddenWord;
    }

    /// <summary>
    /// 表示状態かどうかを取得
    /// </summary>
    public bool IsRevealed()
    {
        return isRevealed;
    }

    /// <summary>
    /// キーワードの文字列を直接設定（編集ツール用、通常使用しない）
    /// </summary>
    public void SetHiddenWord(string word)
    {
        if (!string.IsNullOrEmpty(word))
        {
            hiddenWord = word;
            // すでに表示状態なら表示を更新
            if (isRevealed)
            {
                ApplyVisualState();
            }
        }
    }
}