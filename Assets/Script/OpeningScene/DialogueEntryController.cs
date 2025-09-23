using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using OpeningScene;

public class DialogueEntryController : MonoBehaviour
{
    [Header("サイズ設定")]
    [SerializeField] private float minWidth = 200f;   // 最小横幅
    [SerializeField] private float maxWidth = 800f;   // 最大横幅
    [SerializeField] private float minHeight = 80f;   // 最小高さ
    [SerializeField] private float padding = 40f;     // パディング

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

    // namePanelフィールド
    [SerializeField] private GameObject namePanel;

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
            }
        }

        // 横幅も縦幅も内容に合わせて自動調整
        contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // LayoutElementの確認と初期化
        if (layoutElement == null)
        {
            layoutElement = GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = gameObject.AddComponent<LayoutElement>();
            }
        }

        // レイアウト設定
        layoutElement.minWidth = minWidth;      // 最小横幅
        layoutElement.minHeight = minHeight;    // 最小高さ
        layoutElement.preferredWidth = -1;      // 自動計算
        layoutElement.preferredHeight = -1;     // 自動計算
        layoutElement.flexibleWidth = 0;        // 伸縮なし
        layoutElement.flexibleHeight = 0;       // 伸縮なし
    }

    private void InitializeTextComponent()
    {
        if (textComponent != null)
        {
            // 折り返しなしで一行表示（短いテキスト用）
            // または最大幅で折り返し（長いテキスト用）
            textComponent.textWrappingMode = TextWrappingModes.Normal;
            textComponent.overflowMode = TextOverflowModes.Overflow;

            // 最大幅の制限を設定
            RectTransform textRect = textComponent.GetComponent<RectTransform>();
            if (textRect != null)
            {
                textRect.anchorMin = new Vector2(0, 0);
                textRect.anchorMax = new Vector2(1, 1);
                textRect.offsetMin = new Vector2(padding / 2, padding / 2);
                textRect.offsetMax = new Vector2(-padding / 2, -padding / 2);
            }

            // 最大幅の制約を追加
            LayoutElement textLayout = textComponent.GetComponent<LayoutElement>();
            if (textLayout == null)
            {
                textLayout = textComponent.gameObject.AddComponent<LayoutElement>();
            }
            textLayout.preferredWidth = maxWidth - padding;
        }
    }

    public void Initialize(string speaker, string dialogue, DialogueType type = DialogueType.Normal)
    {
        fullText = dialogue;

        // 話者名は内部で保持するが表示はしない
        if (speakerNameComponent != null)
        {
            // 話者名パネル全体を最初から非表示にする（テキスト設定前に非表示化）
            if (namePanel != null)
            {
                namePanel.SetActive(false);
            }
            else
            {
                // namePanelがない場合は、speakerNameComponentの親オブジェクトを非表示にする
                Transform parentTransform = speakerNameComponent.transform.parent;
                if (parentTransform != null)
                {
                    parentTransform.gameObject.SetActive(false);
                }
                else
                {
                    // 親がない場合は、コンポーネント自体を非表示にする
                    speakerNameComponent.gameObject.SetActive(false);
                }
            }

            // 話者名をセット（非表示状態でセット）
            speakerNameComponent.text = string.IsNullOrEmpty(speaker) ? "" : speaker;
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