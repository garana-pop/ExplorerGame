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

    [Header("�����o���̐F�ݒ�")]
    [SerializeField] private Color normalDialogueColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);
    [SerializeField] private Color narrationDialogueColor = new Color(0.2f, 0.2f, 0.2f, 0.6f);
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color narrationTextColor = new Color(0.9f, 0.9f, 0.9f);

    // ���C�A�E�g�����p�̃R���|�[�l���g�Q��
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
        // ContentSizeFitter�̊m�F�Ə�����
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

        // LayoutElement�̊m�F�Ə�����
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

            // �e�L�X�g���\���̈�ɍ��킹�Ē��������悤�ɂ���
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

        // �b�Җ��͓����ŕێ����邪�\���͂��Ȃ�
        if (speakerNameComponent != null)
        {
            // �b�Җ���ۑ����邪�A��ɔ�\���ɂ���
            speakerNameComponent.text = string.IsNullOrEmpty(speaker) ? "" : speaker;
            speakerNameComponent.gameObject.SetActive(false);
        }

        // �_�C�A���O�^�C�v�ɉ������X�^�C���̓K�p
        ApplyStyleForType(type);

        // ���C�A�E�g���X�V
        if (contentSizeFitter != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }

    private void ApplyStyleForType(DialogueType type)
    {
        if (type == DialogueType.Narration)
        {
            // �i���[�V�����X�^�C��
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
            // �ʏ��b�X�^�C��
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

        // �^�C�s���O������Ƀ��C�A�E�g���X�V
        UpdateLayout();
    }

    private IEnumerator TypeText()
    {
        isTyping = true;
        textComponent.text = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            textComponent.text += fullText[i];

            // �X�y�[�X����s�łȂ��ꍇ�̂ݑҋ@
            if (fullText[i] != ' ' && fullText[i] != '\n' && fullText[i] != '�@')
            {
                yield return new WaitForSeconds(typingSpeed);
            }

            // ����I�Ƀ��C�A�E�g���X�V
            if (i % 10 == 0)
            {
                UpdateLayout();
            }
        }

        // �ŏI�I�ȃ��C�A�E�g�X�V
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