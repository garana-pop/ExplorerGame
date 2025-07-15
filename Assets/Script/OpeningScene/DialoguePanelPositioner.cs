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

    [Header("�ʒu�ݒ�")]
    [SerializeField] private DialogueAlignment defaultAlignment = DialogueAlignment.Center;
    [SerializeField] private List<SpeakerSettings> speakerSettings = new List<SpeakerSettings>();
    [SerializeField] private float leftOffset = 50f;      // �����̈ʒu�I�t�Z�b�g
    [SerializeField] private float rightOffset = -50f;    // �E���̈ʒu�I�t�Z�b�g (���̒l�͉E�������)

    [Header("�\���ݒ�")]
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private int fontSize = 24;
    [SerializeField] private float minWidth = 600f;
    [SerializeField] private float minHeight = 100f;

    [Header("�f�o�b�O�I�v�V����")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool forceVisibility = true;

    // �����Q��
    private ScrollRect scrollRect;
    private int entryCount = 0;

    private void Awake()
    {
        // �����ݒ�
        InitializeDefaultSpeakerSettings();

        // ScrollRect�̎Q�Ƃ��擾
        scrollRect = GetComponentInParent<ScrollRect>();

        if (debugMode)
        {
            Debug.Log("DialoguePanelPositioner: ����������");
        }
    }

    // �b�҂̃f�t�H���g�ݒ��������
    private void InitializeDefaultSpeakerSettings()
    {
        // �����̐ݒ肪�Ȃ��ꍇ�͍쐬
        if (speakerSettings.Count == 0)
        {
            speakerSettings.Add(new SpeakerSettings { speakerName = "�j��", alignment = DialogueAlignment.Right });
            speakerSettings.Add(new SpeakerSettings { speakerName = "���e", alignment = DialogueAlignment.Left });
            speakerSettings.Add(new SpeakerSettings { speakerName = "�j��", alignment = DialogueAlignment.Left });
            speakerSettings.Add(new SpeakerSettings { speakerName = "�H�H�H", alignment = DialogueAlignment.Left });
        }
    }

    // �V�����Z���t���ǉ����ꂽ�Ƃ��ɌĂяo�����
    public void OnDialogueEntryAdded(GameObject dialogueEntryObject, DialogueEntry entry)
    {
        if (dialogueEntryObject == null || entry == null) return;

        entryCount++;

        if (debugMode)
        {
            Debug.Log($"�Z���t�z�u #{entryCount}: �u{entry.speaker}�v - {entry.dialogue.Substring(0, Mathf.Min(20, entry.dialogue.Length))}...");
        }

        // �b�҂ɉ������z�u��K�p
        DialogueAlignment alignment = GetSpeakerAlignment(entry.speaker);

        // �\���������I�ɒ���
        if (forceVisibility)
        {
            EnforceVisibility(dialogueEntryObject, entry);
        }

        // �ʒu��K�p
        ApplyAlignment(dialogueEntryObject, alignment);

        // �Z���t�ǉ���ɃX�N���[��
        ScrollToBottom();
    }

    // �����I�ɕ\���𒲐�
    private void EnforceVisibility(GameObject dialogueObject, DialogueEntry entry)
    {
        // �e�L�X�g�R���|�[�l���g��T��
        TextMeshProUGUI[] texts = dialogueObject.GetComponentsInChildren<TextMeshProUGUI>(true);

        if (texts.Length == 0)
        {
            Debug.LogError("�e�L�X�g�R���|�[�l���g��������܂���");
            return;
        }

        // �T�C�Y�ƕ\���ݒ������
        RectTransform objectRect = dialogueObject.GetComponent<RectTransform>();
        if (objectRect != null)
        {
            objectRect.sizeDelta = new Vector2(minWidth, minHeight);
        }

        // �e�e�L�X�g�R���|�[�l���g�𒲐�
        foreach (var text in texts)
        {
            // �e�L�X�g����Ȃ���e������
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

            // �e�L�X�g�̐F�ƃT�C�Y��ݒ�
            text.color = textColor;
            text.fontSize = fontSize;

            // �\�����m����
            text.enabled = true;

            // �e�L�X�g��RectTransform�𒲐�
            RectTransform textRect = text.rectTransform;
            if (textRect != null)
            {
                textRect.sizeDelta = new Vector2(minWidth * 0.9f, minHeight * 0.8f);
            }

            if (debugMode)
            {
                Debug.Log($"�e�L�X�g�����ݒ�: {text.name}, �e�L�X�g=\"{text.text.Substring(0, Mathf.Min(20, text.text.Length))}...\", �F={text.color}, �T�C�Y={text.fontSize}");
            }
        }

        // �e�I�u�W�F�N�g����A�N�e�B�u�Ȃ�A�N�e�B�u�ɂ���
        Transform parent = dialogueObject.transform.parent;
        while (parent != null)
        {
            if (!parent.gameObject.activeSelf)
            {
                parent.gameObject.SetActive(true);
                Debug.Log($"��A�N�e�B�u�Ȑe�I�u�W�F�N�g���A�N�e�B�u��: {parent.name}");
            }
            parent = parent.parent;
        }

        // CanvasGroup������Γ����x���m�F
        CanvasGroup canvasGroup = dialogueObject.GetComponent<CanvasGroup>();
        if (canvasGroup != null && canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha = 1f;
            Debug.Log("CanvasGroup�̓����x��1�ɐݒ�");
        }
    }

    // �b�҂̔z�u�ݒ���擾
    private DialogueAlignment GetSpeakerAlignment(string speaker)
    {
        if (string.IsNullOrEmpty(speaker))
        {
            return DialogueAlignment.Center; // �i���[�V�����Ȃ�
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

    // �z�u��K�p
    private void ApplyAlignment(GameObject dialogueObject, DialogueAlignment alignment)
    {
        if (dialogueObject == null) return;

        // RectTransform�擾
        RectTransform rectTransform = dialogueObject.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("RectTransform������܂���");
            return;
        }

        // ���������̈ʒu����
        switch (alignment)
        {
            case DialogueAlignment.Left:
                // ����
                rectTransform.anchorMin = new Vector2(0, 0.5f);
                rectTransform.anchorMax = new Vector2(0, 0.5f);
                rectTransform.pivot = new Vector2(0, 0.5f);
                rectTransform.anchoredPosition = new Vector2(leftOffset, rectTransform.anchoredPosition.y);
                break;

            case DialogueAlignment.Center:
                // ����
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = new Vector2(0, rectTransform.anchoredPosition.y);
                break;

            case DialogueAlignment.Right:
                // �E��
                rectTransform.anchorMin = new Vector2(1, 0.5f);
                rectTransform.anchorMax = new Vector2(1, 0.5f);
                rectTransform.pivot = new Vector2(1, 0.5f);
                rectTransform.anchoredPosition = new Vector2(rightOffset, rectTransform.anchoredPosition.y);
                break;
        }

        // �e�L�X�g�A���C�����g������
        AdjustTextAlignment(dialogueObject, alignment);

        if (debugMode)
        {
            Debug.Log($"�ʒu�ݒ�: {alignment}, X�ʒu: {rectTransform.anchoredPosition.x}, �T�C�Y: {rectTransform.sizeDelta}");
        }
    }

    // �e�L�X�g�A���C�����g�𒲐�
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

    // �ŉ����ɃX�N���[��
    public void ScrollToBottom()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponentInParent<ScrollRect>();
            if (scrollRect == null) return;
        }

        // ���C�A�E�g�X�V
        Canvas.ForceUpdateCanvases();

        // �X�N���[���ʒu�ݒ�
        scrollRect.verticalNormalizedPosition = 0;

        // �O�̂��ߒx���X�N���[�������s
        StartCoroutine(DelayedScroll());
    }

    // �x���X�N���[��
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