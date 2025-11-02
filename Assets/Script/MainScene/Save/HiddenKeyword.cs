using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ExplorerGame.Localization; // LocalizationManager用

/// <summary>
/// 隠されたキーワードをクリックして表示するためのコンポーネント
/// </summary>
public class HiddenKeyword : MonoBehaviour, IPointerClickHandler
{
    [Header("キーワード設定")]
    [Tooltip("隠されている単語（日本語）")]
    [SerializeField] private string hiddenWord = "";

    [Tooltip("隠されている単語（英語）")]
    [SerializeField] private string hiddenWord_English = "";

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

    // 現在表示する単語（言語設定により決定）
    private string currentDisplayWord = "";

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
        // 現在の言語設定に基づいて表示単語を決定
        UpdateCurrentDisplayWord();

        // 初期状態を適用
        ApplyVisualState();

        // 言語変更イベントに登録
        RegisterLanguageChangeEvent();
    }

    private void OnEnable()
    {
        // アクティブになった時に参照更新を確実に行う
        InitializeComponents();

        // 現在の言語設定に基づいて表示単語を更新
        UpdateCurrentDisplayWord();

        // 状態に合わせて表示を更新
        ApplyVisualState();

        if (debugMode)
        {
            DebugLogger.Log($"HiddenKeyword '{currentDisplayWord}' OnEnable: isRevealed={isRevealed}");
        }
    }

    private void OnDestroy()
    {
        // 言語変更イベントの登録解除
        UnregisterLanguageChangeEvent();
    }

    /// <summary>
    /// 言語変更イベントに登録
    /// </summary>
    private void RegisterLanguageChangeEvent()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }
    }

    /// <summary>
    /// 言語変更イベントの登録解除
    /// </summary>
    private void UnregisterLanguageChangeEvent()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    /// <summary>
    /// 言語が変更された時のコールバック
    /// </summary>
    private void OnLanguageChanged(UnityEngine.Localization.Locale newLocale)
    {
        UpdateCurrentDisplayWord();

        // 既に表示状態の場合は、新しい言語で表示を更新
        if (isRevealed)
        {
            ApplyVisualState();
        }

        if (debugMode)
        {
            DebugLogger.Log($"HiddenKeyword: 言語が {newLocale.Identifier.Code} に変更されました。現在の表示単語: {currentDisplayWord}");
        }
    }

    /// <summary>
    /// 現在の言語設定に基づいて表示単語を更新
    /// </summary>
    private void UpdateCurrentDisplayWord()
    {
        if (LocalizationManager.Instance == null)
        {
            // LocalizationManagerが存在しない場合は日本語をデフォルトとする
            currentDisplayWord = hiddenWord;
            if (debugMode)
            {
                DebugLogger.LogWarning($"{nameof(HiddenKeyword)}: LocalizationManagerが見つかりません。日本語をデフォルトとして使用します。");
            }
            return;
        }

        // 現在の言語コードを取得
        string currentLanguageCode = LocalizationManager.Instance.GetCurrentLanguageCode();

        // 言語コードに応じて表示単語を設定
        if (currentLanguageCode == "en")
        {
            // 英語が設定されていない場合は日本語を使用
            currentDisplayWord = string.IsNullOrEmpty(hiddenWord_English) ? hiddenWord : hiddenWord_English;

            if (debugMode)
            {
                DebugLogger.Log($"{nameof(HiddenKeyword)}: 英語モード - 表示単語: {currentDisplayWord}");
            }
        }
        else
        {
            // 日本語またはその他の言語の場合
            currentDisplayWord = hiddenWord;

            if (debugMode)
            {
                DebugLogger.Log($"{nameof(HiddenKeyword)}: 日本語モード - 表示単語: {currentDisplayWord}");
            }
        }
    }

    /// <summary>
    /// 必要なコンポーネント参照を初期化する
    /// </summary>
    private void InitializeComponents()
    {
        // TextMeshProUGUIコンポーネントの取得
        CheckTextComponent();

        // 背景イメージの取得
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        // documentManagerの設定
        UpdateDocumentManagerReference();
    }

    /// <summary>
    /// TextMeshProUGUIコンポーネントが設定されているか確認
    /// </summary>
    private void CheckTextComponent()
    {
        // インスペクターで設定されている場合
        if (textComponentReference != null)
        {
            textComponent = textComponentReference;

            if (debugMode)
            {
                DebugLogger.Log($"HiddenKeyword '{currentDisplayWord}': インスペクターで設定されたTextMeshProUGUIコンポーネントを使用します");
            }

            return;
        }

        // インスペクターで設定されていない場合
        if (textComponentReference == null)
        {
            DebugLogger.LogWarning($"HiddenKeyword '{currentDisplayWord}': インスペクターでTextMeshProUGUIコンポーネントが設定されていません");
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
            DebugLogger.LogWarning($"HiddenKeyword '{currentDisplayWord}': PdfDocumentManagerを見つけられませんでした");
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
            CheckTextComponent();
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
                DebugLogger.LogWarning($"隠しキーワード '{currentDisplayWord}' のPdfDocumentManagerが見つかりません");
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
            CheckTextComponent();
        }

        // 強制的に表示状態に設定
        isRevealed = true;

        // 表示状態を確実に適用
        ApplyVisualState();

        if (debugMode)
        {
            DebugLogger.Log($"HiddenKeyword '{currentDisplayWord}' を強制的に表示状態にしました");
        }
    }

    /// <summary>
    /// 視覚的な状態を適用
    /// </summary>
    private void ApplyVisualState()
    {
        CheckTextComponent();

        if (isRevealed)
        {
            // 表示状態 - 実際の単語を表示（現在の言語設定に応じた単語）
            textComponent.text = currentDisplayWord;
            textComponent.color = revealedColor;

            if (debugMode)
            {
                DebugLogger.Log($"HiddenKeyword '{currentDisplayWord}': 表示状態を適用しました");
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
            int count = (currentDisplayWord.Length > 0) ?
                Mathf.Max(3, currentDisplayWord.Length) : censorSymbolCount;

            string censorText = string.Empty;
            for (int i = 0; i < count; i++)
            {
                censorText += censorSymbol;
            }
            textComponent.text = censorText;

            if (debugMode)
            {
                DebugLogger.Log($"HiddenKeyword '{currentDisplayWord}': 隠し状態を適用しました");
            }
        }
    }

    /// <summary>
    /// 隠されたキーワードを取得（言語設定に応じた値を返す）
    /// </summary>
    public string GetHiddenWord()
    {
        return currentDisplayWord;
    }

    /// <summary>
    /// 隠されたキーワードを取得（日本語版）
    /// </summary>
    public string GetHiddenWordJapanese()
    {
        return hiddenWord;
    }

    /// <summary>
    /// 隠されたキーワードを取得（英語版）
    /// </summary>
    public string GetHiddenWordEnglish()
    {
        return hiddenWord_English;
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
            UpdateCurrentDisplayWord();

            // すでに表示状態なら表示を更新
            if (isRevealed)
            {
                ApplyVisualState();
            }
        }
    }

    /// <summary>
    /// 英語版キーワードの文字列を直接設定（編集ツール用）
    /// </summary>
    public void SetHiddenWordEnglish(string word)
    {
        hiddenWord_English = word;
        UpdateCurrentDisplayWord();

        // すでに表示状態なら表示を更新
        if (isRevealed)
        {
            ApplyVisualState();
        }
    }
}