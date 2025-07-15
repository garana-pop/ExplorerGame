using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using OpeningScene;

public class DialogueEntryController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textComponent;
    [SerializeField] private TextMeshProUGUI speakerNameComponent;
    [SerializeField] private Image backgroundImage;
    [SerializeField] public float typingSpeed = 0.05f;

    [Header("吹き出しの色設定")]
    [SerializeField] private Color normalDialogueColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    [SerializeField] private Color narrationDialogueColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color narrationTextColor = new Color(0.9f, 0.9f, 0.9f);

    // レイアウト調整用のコンポーネント参照
    [SerializeField] private ContentSizeFitter contentSizeFitter;
    [SerializeField] private LayoutElement layoutElement;

    private string fullText;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        InitializeLayoutComponents();
        InitializeTextComponent();
    }

    private void InitializeLayoutComponents()
    {
        // ContentSizeFitterの確認と初期化
        if (contentSizeFitter == null)
        {
            contentSizeFitter = GetComponent<ContentSizeFitter>();
            if (contentSizeFitter == null)
            {
                contentSizeFitter = gameObject.AddComponent<ContentSizeFitter>();
                contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        // LayoutElementの確認と初期化
        if (layoutElement == null)
        {
            layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
                layoutElement.flexibleWidth = 1;
                layoutElement.minHeight = 50;
            }
        }
    }

    private void InitializeTextComponent()
    {
        if (textComponent != null)
        {
            textComponent.textWrappingMode = TextWrappingModes.Normal;
            textComponent.overflowMode = TextOverflowModes.Overflow;

            // テキストが表示領域に合わせて調整されるようにする
            RectTransform textRect = textComponent.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.anchorMin = new Vector2(0, 0);
                textRect.anchorMax = new Vector2(1, 1);
                textRect.sizeDelta = Vector2.zero;
                textRect.anchoredPosition = Vector2.zero;
            }
        }
    }

    public void Initialize(string speaker, string dialogue, DialogueType type = DialogueType.Normal)
    {
        fullText = dialogue;

        // 話者名は内部で保持するが表示はしない
        if (speakerNameComponent != null)
        {
            // 話者名を保存するが、常に非表示にする
            speakerNameComponent.text = string.IsNullOrEmpty(speaker) ? "" : speaker;
            speakerNameComponent.gameObject.SetActive(false);
        }

        // ダイアログタイプに応じたスタイルの適用
        ApplyStyleForType(type);

        // レイアウトを更新
        if (contentSizeFitter != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }

    private void ApplyStyleForType(DialogueType type)
    {
        if (type == DialogueType.Narration)
        {
            // ナレーションスタイル
            if (backgroundImage != null)
                backgroundImage.color = narrationDialogueColor;

            if (textComponent != null)
            {
                textComponent.color = narrationTextColor;
                textComponent.fontStyle = FontStyles.Italic;
            }
        }
        else
        {
            // 通常会話スタイル
            if (backgroundImage != null)
                backgroundImage.color = normalDialogueColor;

            if (textComponent != null)
            {
                textComponent.color = normalTextColor;
                textComponent.fontStyle = FontStyles.Normal;
            }
        }
    }

    public void StartTyping()
    {
        if (textComponent == null)
            return;

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText());
    }

    public void CompleteTyping()
    {
        if (textComponent == null)
            return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        textComponent.text = fullText;
        isTyping = false;

        // タイピング完了後にレイアウトを更新
        UpdateLayout();
    }

    private IEnumerator TypeText()
    {
        isTyping = true;
        textComponent.text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            textComponent.text += fullText[i];

            // スペースや改行でない場合のみ待機
            if (fullText[i] != ' ' && fullText[i] != '\n' && fullText[i] != '　')
            {
                yield return new WaitForSeconds(typingSpeed);
            }

            // 定期的にレイアウトを更新
            if (i % 10 == 0)
            {
                UpdateLayout();
            }
        }

        // 最終的なレイアウト更新
        UpdateLayout();

        isTyping = false;
        typingCoroutine = null;
    }

    private void UpdateLayout()
    {
        if (contentSizeFitter != null)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }

    public bool IsTyping()
    {
        return isTyping;
    }

    private void OnDestroy()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
    }
}