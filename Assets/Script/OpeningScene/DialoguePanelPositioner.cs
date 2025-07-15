using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpeningScene;

public class DialoguePanelPositioner : MonoBehaviour
{
    [System.Serializable]
    public class SpeakerSettings
    {
        public string speakerName;
        public DialogueAlignment alignment = DialogueAlignment.Left;
    }

    public enum DialogueAlignment
    {
        Left,
        Center,
        Right
    }

    [Header("位置設定")]
    [SerializeField] private DialogueAlignment defaultAlignment = DialogueAlignment.Center;
    [SerializeField] private List<SpeakerSettings> speakerSettings = new List<SpeakerSettings>();
    [SerializeField] private float leftOffset = 50f;      // 左側の位置オフセット
    [SerializeField] private float rightOffset = -50f;    // 右側の位置オフセット (負の値は右から内側)

    [Header("表示設定")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private int fontSize = 24;
    [SerializeField] private float minWidth = 600f;
    [SerializeField] private float minHeight = 100f;

    [Header("デバッグオプション")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool forceVisibility = true;

    // 内部参照
    private ScrollRect scrollRect;
    private int entryCount = 0;

    private void Awake()
    {
        // 初期設定
        InitializeDefaultSpeakerSettings();

        // ScrollRectの参照を取得
        scrollRect = GetComponentInParent<ScrollRect>();

        if (debugMode)
        {
            Debug.Log("DialoguePanelPositioner: 初期化完了");
        }
    }

    // 話者のデフォルト設定を初期化
    private void InitializeDefaultSpeakerSettings()
    {
        // 既存の設定がない場合は作成
        if (speakerSettings.Count == 0)
        {
            speakerSettings.Add(new SpeakerSettings { speakerName = "男性", alignment = DialogueAlignment.Right });
            speakerSettings.Add(new SpeakerSettings { speakerName = "父親", alignment = DialogueAlignment.Left });
            speakerSettings.Add(new SpeakerSettings { speakerName = "男性", alignment = DialogueAlignment.Left });
            speakerSettings.Add(new SpeakerSettings { speakerName = "？？？", alignment = DialogueAlignment.Left });
        }
    }

    // 新しいセリフが追加されたときに呼び出される
    public void OnDialogueEntryAdded(GameObject dialogueEntryObject, DialogueEntry entry)
    {
        if (dialogueEntryObject == null || entry == null) return;

        entryCount++;

        if (debugMode)
        {
            Debug.Log($"セリフ配置 #{entryCount}: 「{entry.speaker}」 - {entry.dialogue.Substring(0, Mathf.Min(20, entry.dialogue.Length))}...");
        }

        // 話者に応じた配置を適用
        DialogueAlignment alignment = GetSpeakerAlignment(entry.speaker);

        // 表示を強制的に調整
        if (forceVisibility)
        {
            EnforceVisibility(dialogueEntryObject, entry);
        }

        // 位置を適用
        ApplyAlignment(dialogueEntryObject, alignment);

        // セリフ追加後にスクロール
        ScrollToBottom();
    }

    // 強制的に表示を調整
    private void EnforceVisibility(GameObject dialogueObject, DialogueEntry entry)
    {
        // テキストコンポーネントを探す
        TextMeshProUGUI[] texts = dialogueObject.GetComponentsInChildren<TextMeshProUGUI>(true);

        if (texts.Length == 0)
        {
            Debug.LogError("テキストコンポーネントが見つかりません");
            return;
        }

        // サイズと表示設定を強制
        RectTransform objectRect = dialogueObject.GetComponent<RectTransform>();
        if (objectRect != null)
        {
            objectRect.sizeDelta = new Vector2(minWidth, minHeight);
        }

        // 各テキストコンポーネントを調整
        foreach (var text in texts)
        {
            // テキストが空なら内容を入れる
            if (string.IsNullOrEmpty(text.text))
            {
                if (text.name.Contains("Speaker") || text.name.Contains("Name"))
                {
                    text.text = entry.speaker;
                }
                else if (text.name.Contains("Dialogue") || text.name.Contains("Text"))
                {
                    text.text = entry.dialogue;
                }
            }

            // テキストの色とサイズを設定
            text.color = textColor;
            text.fontSize = fontSize;

            // 表示を確実に
            text.enabled = true;

            // テキストのRectTransformを調整
            RectTransform textRect = text.rectTransform;
            if (textRect != null)
            {
                textRect.sizeDelta = new Vector2(minWidth * 0.9f, minHeight * 0.8f);
            }

            if (debugMode)
            {
                Debug.Log($"テキスト強制設定: {text.name}, テキスト=\"{text.text.Substring(0, Mathf.Min(20, text.text.Length))}...\", 色={text.color}, サイズ={text.fontSize}");
            }
        }

        // 親オブジェクトが非アクティブならアクティブにする
        Transform parent = dialogueObject.transform.parent;
        while (parent != null)
        {
            if (!parent.gameObject.activeSelf)
            {
                parent.gameObject.SetActive(true);
                Debug.Log($"非アクティブな親オブジェクトをアクティブ化: {parent.name}");
            }
            parent = parent.parent;
        }

        // CanvasGroupがあれば透明度を確認
        CanvasGroup canvasGroup = dialogueObject.GetComponent<CanvasGroup>();
        if (canvasGroup != null && canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha = 1f;
            Debug.Log("CanvasGroupの透明度を1に設定");
        }
    }

    // 話者の配置設定を取得
    private DialogueAlignment GetSpeakerAlignment(string speaker)
    {
        if (string.IsNullOrEmpty(speaker))
        {
            return DialogueAlignment.Center; // ナレーションなど
        }

        foreach (var setting in speakerSettings)
        {
            if (setting.speakerName == speaker)
            {
                return setting.alignment;
            }
        }

        return defaultAlignment;
    }

    // 配置を適用
    private void ApplyAlignment(GameObject dialogueObject, DialogueAlignment alignment)
    {
        if (dialogueObject == null) return;

        // RectTransform取得
        RectTransform rectTransform = dialogueObject.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("RectTransformがありません");
            return;
        }

        // 水平方向の位置調整
        switch (alignment)
        {
            case DialogueAlignment.Left:
                // 左寄せ
                rectTransform.anchorMin = new Vector2(0, 0.5f);
                rectTransform.anchorMax = new Vector2(0, 0.5f);
                rectTransform.pivot = new Vector2(0, 0.5f);
                rectTransform.anchoredPosition = new Vector2(leftOffset, rectTransform.anchoredPosition.y);
                break;

            case DialogueAlignment.Center:
                // 中央
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = new Vector2(0, rectTransform.anchoredPosition.y);
                break;

            case DialogueAlignment.Right:
                // 右寄せ
                rectTransform.anchorMin = new Vector2(1, 0.5f);
                rectTransform.anchorMax = new Vector2(1, 0.5f);
                rectTransform.pivot = new Vector2(1, 0.5f);
                rectTransform.anchoredPosition = new Vector2(rightOffset, rectTransform.anchoredPosition.y);
                break;
        }

        // テキストアライメントも調整
        AdjustTextAlignment(dialogueObject, alignment);

        if (debugMode)
        {
            Debug.Log($"位置設定: {alignment}, X位置: {rectTransform.anchoredPosition.x}, サイズ: {rectTransform.sizeDelta}");
        }
    }

    // テキストアライメントを調整
    private void AdjustTextAlignment(GameObject dialogueObject, DialogueAlignment alignment)
    {
        TextMeshProUGUI[] texts = dialogueObject.GetComponentsInChildren<TextMeshProUGUI>();

        TextAlignmentOptions textAlign;
        switch (alignment)
        {
            case DialogueAlignment.Left:
                textAlign = TextAlignmentOptions.Left;
                break;
            case DialogueAlignment.Right:
                textAlign = TextAlignmentOptions.Right;
                break;
            default:
                textAlign = TextAlignmentOptions.Center;
                break;
        }

        foreach (var text in texts)
        {
            text.alignment = textAlign;
        }
    }

    // 最下部にスクロール
    public void ScrollToBottom()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponentInParent<ScrollRect>();
            if (scrollRect == null) return;
        }

        // レイアウト更新
        Canvas.ForceUpdateCanvases();

        // スクロール位置設定
        scrollRect.verticalNormalizedPosition = 0;

        // 念のため遅延スクロールも実行
        StartCoroutine(DelayedScroll());
    }

    // 遅延スクロール
    private System.Collections.IEnumerator DelayedScroll()
    {
        yield return null;
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }
}