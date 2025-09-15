using ExplorerGame.Localization;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// テキストを一文字ずつタイプライターのように表示するスクリプト
/// </summary>
public class TextTyper : MonoBehaviour
{
    [Header("テキスト設定")]
    [Tooltip("表示するTextMeshProコンポーネント")]
    [SerializeField] private TMP_Text textComponent;

    [Tooltip("表示する完全なテキスト(デフォルト日本語)")]
    [SerializeField] private string fullText = "気持ち悪い。消えてよ・・・";

    [Tooltip("日本語テキスト")]
    [SerializeField] private string JapaneseText = "気持ち悪い。消えてよ・・・";

    [Tooltip("英語テキスト")]
    [SerializeField] private string EnglishText = "I hate this. Disappear...";

    [Header("タイピング設定")]
    [Tooltip("1文字表示するまでの時間（秒）")]
    [SerializeField] private float typingSpeed = 0.1f;

    [Tooltip("各文字表示後の追加待機時間（秒）")]
    [SerializeField] private float characterDelay = 0.0f;

    [Tooltip("句読点での追加待機時間（秒）")]
    [SerializeField] private float punctuationDelay = 0.25f;

    [Header("効果音設定")]
    [Tooltip("タイピング音を再生するか")]
    [SerializeField] private bool playTypeSound = true;

    [Header("自動開始設定")]
    [Tooltip("シーン読み込み時に自動的に開始するか")]
    [SerializeField] private bool autoStart = true;

    [Tooltip("自動開始までの遅延時間（秒）")]
    [SerializeField] private float autoStartDelay = 1.0f;

    // 内部変数
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private string currentText = "";
    private SoundEffectManager soundManager;
    private LocalizationManager localizationManager;

    // タイピング完了イベントデリゲート
    public delegate void TypingCompletedHandler();
    public event TypingCompletedHandler OnTypingCompleted;

    private void Awake()
    {
        // TextMeshProコンポーネントの取得
        if (textComponent == null)
        {
            textComponent = GetComponent<TMP_Text>();
            if (textComponent == null)
            {
                Debug.LogError("TextTyper: TextMeshProコンポーネントが見つかりません。コンポーネントをアタッチするか、インスペクターで設定してください。");
                enabled = false;
                return;
            }
        }

        // SoundEffectManagerの参照を取得
        soundManager = SoundEffectManager.Instance;

        // 初期テキストをクリア
        textComponent.text = "";

        // LocalizationManagerの参照を取得
        localizationManager = LocalizationManager.Instance;
    }

    private void Start()
    {
        // ローカライズ設定の適用（追加）
        ApplyLocalization();

        // 自動開始が有効なら、指定された遅延後に開始
        if (autoStart)
        {
            Invoke("StartTyping", autoStartDelay);
        }
    }

    /// <summary>
    /// 現在の言語設定に基づいてテキストを切り替える
    /// </summary>
    private void ApplyLocalization()
    {
        // LocalizationManagerが存在しない場合は何もしない
        if (localizationManager == null)
        {
            Debug.LogWarning("TextTyper: LocalizationManagerが見つかりません。デフォルトテキストを使用します。");
            return;
        }

        // 現在の言語コードを取得
        string currentLanguageCode = localizationManager.GetCurrentLanguageCode();

        // 英語の場合、englishTextを使用
        if (currentLanguageCode == "en")
        {
            // 英語テキストが設定されている場合のみ適用
            if (!string.IsNullOrEmpty(EnglishText))
            {
                fullText = EnglishText;
                Debug.Log($"TextTyper: 英語モードのため、テキストを切り替えました");
            }
            else
            {
                Debug.LogWarning("TextTyper: 英語テキストが設定されていません。デフォルトテキストを使用します。");
            }
        }
        // 日本語の場合は何もしない（デフォルトのfullTextを使用）
    }

    /// <summary>
    /// タイピングを開始するパブリックメソッド
    /// </summary>
    public void StartTyping()
    {
        // すでにタイピング中なら何もしない
        if (isTyping) return;

        // 既存のコルーチンを停止
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // テキストコンポーネントをクリア
        textComponent.text = "";
        currentText = "";

        // タイピングコルーチンを開始
        typingCoroutine = StartCoroutine(TypeText());
    }

    /// <summary>
    /// タイピングを停止するパブリックメソッド
    /// </summary>
    public void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            isTyping = false;
        }
    }

    /// <summary>
    /// タイピングをスキップして全テキストを表示するパブリックメソッド
    /// </summary>
    public void SkipToEnd()
    {
        StopTyping();
        textComponent.text = fullText;
        currentText = fullText;
        isTyping = false;
        IsCompleted = true; // 完了フラグを設定

        // タイピング完了イベントを発火
        OnTypingCompleted?.Invoke();
    }

    /// <summary>
    /// タイピングコルーチン
    /// </summary>
    private IEnumerator TypeText()
    {
        isTyping = true;
        IsCompleted = false; // 開始時にフラグをリセット


        // 待機時間の初期設定
        float waitTime = typingSpeed;

        // 文字を1つずつ表示
        for (int i = 0; i < fullText.Length; i++)
        {
            // 次の文字を追加
            currentText += fullText[i];
            textComponent.text = currentText;

            // 文字に応じた効果音再生と待機時間設定
            char currentChar = fullText[i];
            if (IsPunctuation(currentChar))
            {
                // 句読点効果音の再生
                if (playTypeSound)
                {
                    if (soundManager != null)
                    {
                        soundManager.PlayPunctuationTypeSound();
                    }
                }
                waitTime = punctuationDelay;
            }
            else
            {
                // 通常の文字効果音
                if (playTypeSound)
                {
                    if (soundManager != null)
                    {
                        soundManager.PlayTypeSound();
                    }
                }
                waitTime = typingSpeed;
            }

            // 次の文字表示まで待機
            yield return new WaitForSeconds(waitTime + characterDelay);
        }

        isTyping = false;
        IsCompleted = true; // 完了フラグを設定

        // タイピング完了イベントを発火
        OnTypingCompleted?.Invoke();
    }

    /// <summary>
    /// タイピング状態をリセットする
    /// </summary>
    public void ResetTyping()
    {
        StopTyping();
        textComponent.text = "";
        currentText = "";
        IsCompleted = false;
    }

    /// <summary>
    /// 句読点かどうかをチェック
    /// </summary>
    private bool IsPunctuation(char character)
    {
        return character == '。' || character == '、' || character == '.' ||
               character == ',' || character == '?' || character == '!' ||
               character == '？' || character == '！' || character == '…' ||
               character == '・';
    }

    /// <summary>
    /// 表示テキストを設定するパブリックメソッド
    /// </summary>
    public void SetText(string text)
    {
        // 言語設定を確認して適切なテキストを設定
        if (localizationManager != null && localizationManager.GetCurrentLanguageCode() == "en")
        {
            // 英語モードで、EnglishTextが設定されている場合は無視
            if (!string.IsNullOrEmpty(EnglishText))
            {
                Debug.Log("TextTyper: 英語モードでは、インスペクターで設定されたEnglishTextを使用します");
                return;
            }
        }

        fullText = text;
        IsCompleted = false; // 新しいテキスト設定時にリセット

        // すでにタイピング中なら再開始
        if (isTyping)
        {
            StopTyping();
            StartTyping();
        }
    }

    /// <summary>
    /// 完了状態を外部から強制設定する（デバッグ用）
    /// </summary>
    public void SetCompleted(bool completed)
    {
        IsCompleted = completed;
        if (completed && !isTyping)
        {
            // 完了状態に設定し、イベントを発火
            OnTypingCompleted?.Invoke();
        }
    }

    /// <summary>
    /// タイピング中かどうかを取得
    /// </summary>
    public bool IsTyping()
    {
        return isTyping;
    }

    /// <summary>
    /// タイピングが完了しているかどうかを取得
    /// </summary>
    public bool IsCompleted { get; private set; } = false;

    /// <summary>
    /// 現在表示されているテキストを取得
    /// </summary>
    public string CurrentText => currentText;

    /// <summary>
    /// 表示予定の全文を取得
    /// </summary>
    public string FullText => fullText;
}