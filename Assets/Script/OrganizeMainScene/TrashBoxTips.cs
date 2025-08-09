using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// �S�~���̃q���g���b�Z�[�W�\�����Ǘ�����N���X
/// �N���b�N���̃��b�Z�[�W�\���@�\�𐧌䂵�܂�
/// </summary>
public class TrashBoxTips : MonoBehaviour
{
    #region �C���X�y�N�^�[�ݒ�

    [Header("���b�Z�[�W�ݒ�")]
    [Tooltip("�S�~���N���b�N���̃��b�Z�[�W")]
    [SerializeField] private string clickMessage = "�폜�������t�@�C�����h���b�O&�h���b�v���Ă��������B";

    [Tooltip("���b�Z�[�W�\�����ԁi�b�j")]
    [SerializeField] private float messageDisplayTime = 3.0f;

    [Tooltip("�t�F�[�h�C�����ԁi�b�j")]
    [SerializeField] private float fadeInTime = 0.5f;

    [Tooltip("�t�F�[�h�A�E�g���ԁi�b�j")]
    [SerializeField] private float fadeOutTime = 0.5f;

    [Header("UI�Q��")]
    [Tooltip("���b�Z�[�W�\���p�p�l��")]
    [SerializeField] private GameObject messagePanel;

    [Tooltip("���b�Z�[�W�e�L�X�g�R���|�[�l���g")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("�\���ݒ�")]
    [Tooltip("���b�Z�[�W�\�����̔w�i�F")]
    [SerializeField] private Color messagePanelColor = new Color(0f, 0f, 0f, 0.7f);

    [Tooltip("���b�Z�[�W�e�L�X�g�̐F")]
    [SerializeField] private Color messageTextColor = Color.white;

    [Tooltip("���b�Z�[�W�e�L�X�g�̃t�H���g�T�C�Y")]
    [SerializeField] private float messageFontSize = 18f;

    [Header("�A�j���[�V�����ݒ�")]
    [Tooltip("���b�Z�[�W�\�����̃X�P�[���A�j���[�V����")]
    [SerializeField] private bool useScaleAnimation = true;

    [Tooltip("�X�P�[���A�j���[�V�����̊J�n�l")]
    [SerializeField] private float scaleAnimationStart = 0.8f;

    [Tooltip("�X�P�[���A�j���[�V�����̏I���l")]
    [SerializeField] private float scaleAnimationEnd = 1.0f;

    [Header("�f�o�b�O�ݒ�")]
    [Tooltip("�f�o�b�O���O��\�����邩")]
    [SerializeField] private bool debugMode = false;

    #endregion

    #region �v���C�x�[�g�ϐ�

    // �V�[���R���g���[���[�Q��
    private OrganizeMainSceneController sceneController;

    // ���b�Z�[�W�\����ԊǗ�
    private Coroutine messageCoroutine;
    private bool isMessageDisplaying = false;

    // UI�R���|�[�l���g�Q��
    private CanvasGroup messagePanelCanvasGroup;
    private UnityEngine.UI.Image messagePanelImage;

    // �A�j���[�V�����p
    private Vector3 originalScale;

    // �萔
    private const float MIN_DISPLAY_TIME = 0.1f;
    private const float MAX_DISPLAY_TIME = 10.0f;
    private const float MIN_FADE_TIME = 0.0f;
    private const float MAX_FADE_TIME = 2.0f;

    #endregion

    #region Unity ���C�t�T�C�N��

    /// <summary>
    /// Awake���\�b�h - ����������
    /// </summary>
    private void Awake()
    {
        ValidateSettings();
        InitializeUI();

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxTips)}: ����������");
        }
    }

    /// <summary>
    /// Start���\�b�h - �V�[���J�n��̏���
    /// </summary>
    private void Start()
    {
        // �V�[���R���g���[���[�̎Q�Ǝ擾
        sceneController = OrganizeMainSceneController.Instance;
        if (sceneController == null && debugMode)
        {
            Debug.LogWarning($"{nameof(TrashBoxTips)}: OrganizeMainSceneController��������܂���");
        }

        // ���b�Z�[�W�p�l����������ԂŔ�\���ɐݒ�
        SetMessagePanelVisible(false);
    }

    #endregion

    #region ����������

    /// <summary>
    /// �ݒ�l�̌���
    /// </summary>
    private void ValidateSettings()
    {
        // �\�����Ԃ̌���
        messageDisplayTime = Mathf.Clamp(messageDisplayTime, MIN_DISPLAY_TIME, MAX_DISPLAY_TIME);
        fadeInTime = Mathf.Clamp(fadeInTime, MIN_FADE_TIME, MAX_FADE_TIME);
        fadeOutTime = Mathf.Clamp(fadeOutTime, MIN_FADE_TIME, MAX_FADE_TIME);

        // �X�P�[���l�̌���
        scaleAnimationStart = Mathf.Max(0.1f, scaleAnimationStart);
        scaleAnimationEnd = Mathf.Max(0.1f, scaleAnimationEnd);

        // �t�H���g�T�C�Y�̌���
        messageFontSize = Mathf.Clamp(messageFontSize, 8f, 72f);

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxTips)}: �ݒ�l���؊���");
        }
    }

    /// <summary>
    /// UI�R���|�[�l���g�̏�����
    /// </summary>
    private void InitializeUI()
    {
        // ���b�Z�[�W�p�l���̃R���|�[�l���g�擾
        if (messagePanel != null)
        {
            // CanvasGroup�̎擾�܂��͒ǉ�
            messagePanelCanvasGroup = messagePanel.GetComponent<CanvasGroup>();
            if (messagePanelCanvasGroup == null)
            {
                messagePanelCanvasGroup = messagePanel.AddComponent<CanvasGroup>();
            }

            // Image�R���|�[�l���g�̎擾
            messagePanelImage = messagePanel.GetComponent<UnityEngine.UI.Image>();
            if (messagePanelImage != null)
            {
                messagePanelImage.color = messagePanelColor;
            }

            // ���̃X�P�[����ۑ�
            originalScale = messagePanel.transform.localScale;
        }

        // ���b�Z�[�W�e�L�X�g�̐ݒ�
        if (messageText != null)
        {
            messageText.text = "";
            messageText.color = messageTextColor;
            messageText.fontSize = messageFontSize;
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxTips)}: UI����������");
        }
    }

    #endregion

    #region ���b�Z�[�W�\������

    /// <summary>
    /// �N���b�N���̃��b�Z�[�W��\��
    /// </summary>
    public void ShowClickMessage()
    {
        Debug.LogWarning($"{nameof(TrashBoxTips)}: Tips���b�Z�[�W�\��");
        ShowMessage(clickMessage);
    }

    /// <summary>
    /// �J�X�^�����b�Z�[�W��\��
    /// </summary>
    /// <param name="message">�\�����郁�b�Z�[�W</param>
    public void ShowMessage(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(TrashBoxTips)}: ��̃��b�Z�[�W���w�肳��܂���");
            }
            return;
        }

        // �����̃��b�Z�[�W�\�����~
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }

        // ���b�Z�[�W�\���R���[�`�����J�n
        messageCoroutine = StartCoroutine(DisplayMessageCoroutine(message));
    }

    /// <summary>
    /// ���b�Z�[�W���\���ɂ���
    /// </summary>
    public void HideMessage()
    {
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
            messageCoroutine = null;
        }

        StartCoroutine(HideMessageCoroutine());
    }

    /// <summary>
    /// ���b�Z�[�W�\���R���[�`��
    /// </summary>
    /// <param name="message">�\�����郁�b�Z�[�W</param>
    /// <returns>�R���[�`��</returns>
    private IEnumerator DisplayMessageCoroutine(string message)
    {
        isMessageDisplaying = true;

        // ���b�Z�[�W�e�L�X�g��ݒ�
        if (messageText != null)
        {
            messageText.text = message;
        }

        // ���b�Z�[�W�p�l����\��
        SetMessagePanelVisible(true);

        // �t�F�[�h�C��&�X�P�[���A�j���[�V����
        yield return StartCoroutine(FadeInAnimation());

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxTips)}: ���b�Z�[�W�\�� - {message}");
        }

        // �w�莞�ԑҋ@
        yield return new WaitForSeconds(messageDisplayTime);

        // �t�F�[�h�A�E�g&�X�P�[���A�j���[�V����
        yield return StartCoroutine(FadeOutAnimation());

        // ���b�Z�[�W�p�l�����\��
        SetMessagePanelVisible(false);

        isMessageDisplaying = false;
        messageCoroutine = null;

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxTips)}: ���b�Z�[�W��\��");
        }
    }

    /// <summary>
    /// ���b�Z�[�W��\���R���[�`��
    /// </summary>
    /// <returns>�R���[�`��</returns>
    private IEnumerator HideMessageCoroutine()
    {
        if (!isMessageDisplaying) yield break;

        // �t�F�[�h�A�E�g&�X�P�[���A�j���[�V����
        yield return StartCoroutine(FadeOutAnimation());

        // ���b�Z�[�W�p�l�����\��
        SetMessagePanelVisible(false);

        isMessageDisplaying = false;

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxTips)}: ���b�Z�[�W��������\���ɂ��܂���");
        }
    }

    #endregion

    #region �A�j���[�V��������

    /// <summary>
    /// �t�F�[�h�C���A�j���[�V����
    /// </summary>
    /// <returns>�R���[�`��</returns>
    private IEnumerator FadeInAnimation()
    {
        if (messagePanelCanvasGroup == null) yield break;

        float elapsedTime = 0f;
        float startAlpha = 0f;
        float targetAlpha = 1f;

        Vector3 startScale = useScaleAnimation ? originalScale * scaleAnimationStart : originalScale;
        Vector3 targetScale = originalScale * scaleAnimationEnd;

        while (elapsedTime < fadeInTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeInTime;

            // �A���t�@�l�̕��
            messagePanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);

            // �X�P�[���A�j���[�V����
            if (useScaleAnimation && messagePanel != null)
            {
                messagePanel.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            }

            yield return null;
        }

        // �ŏI�l��ݒ�
        messagePanelCanvasGroup.alpha = targetAlpha;
        if (useScaleAnimation && messagePanel != null)
        {
            messagePanel.transform.localScale = targetScale;
        }
    }

    /// <summary>
    /// �t�F�[�h�A�E�g�A�j���[�V����
    /// </summary>
    /// <returns>�R���[�`��</returns>
    private IEnumerator FadeOutAnimation()
    {
        if (messagePanelCanvasGroup == null) yield break;

        float elapsedTime = 0f;
        float startAlpha = messagePanelCanvasGroup.alpha;
        float targetAlpha = 0f;

        Vector3 startScale = messagePanel != null ? messagePanel.transform.localScale : originalScale;
        Vector3 targetScale = useScaleAnimation ? originalScale * scaleAnimationStart : originalScale;

        while (elapsedTime < fadeOutTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fadeOutTime;

            // �A���t�@�l�̕��
            messagePanelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);

            // �X�P�[���A�j���[�V����
            if (useScaleAnimation && messagePanel != null)
            {
                messagePanel.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            }

            yield return null;
        }

        // �ŏI�l��ݒ�
        messagePanelCanvasGroup.alpha = targetAlpha;
        if (useScaleAnimation && messagePanel != null)
        {
            messagePanel.transform.localScale = targetScale;
        }
    }

    #endregion

    #region UI����

    /// <summary>
    /// ���b�Z�[�W�p�l���̕\��/��\����ݒ�
    /// </summary>
    /// <param name="visible">�\�����邩�ǂ���</param>
    private void SetMessagePanelVisible(bool visible)
    {
        if (messagePanel != null)
        {
            messagePanel.SetActive(visible);
        }
    }

    #endregion

    #region �p�u���b�N���\�b�h

    /// <summary>
    /// ���b�Z�[�W���\�������ǂ������擾
    /// </summary>
    /// <returns>�\�����̏ꍇ��true</returns>
    public bool IsMessageDisplaying()
    {
        return isMessageDisplaying;
    }

    /// <summary>
    /// �N���b�N���b�Z�[�W��ݒ�
    /// </summary>
    /// <param name="newMessage">�V�������b�Z�[�W</param>
    public void SetClickMessage(string newMessage)
    {
        if (!string.IsNullOrEmpty(newMessage))
        {
            clickMessage = newMessage;

            if (debugMode)
            {
                Debug.Log($"{nameof(TrashBoxTips)}: �N���b�N���b�Z�[�W���X�V - {newMessage}");
            }
        }
    }

    /// <summary>
    /// ���b�Z�[�W�\�����Ԃ�ݒ�
    /// </summary>
    /// <param name="newDisplayTime">�V�����\������</param>
    public void SetMessageDisplayTime(float newDisplayTime)
    {
        messageDisplayTime = Mathf.Clamp(newDisplayTime, MIN_DISPLAY_TIME, MAX_DISPLAY_TIME);

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxTips)}: ���b�Z�[�W�\�����Ԃ��X�V - {messageDisplayTime}�b");
        }
    }

    /// <summary>
    /// �t�F�[�h���Ԃ�ݒ�
    /// </summary>
    /// <param name="newFadeInTime">�V�����t�F�[�h�C������</param>
    /// <param name="newFadeOutTime">�V�����t�F�[�h�A�E�g����</param>
    public void SetFadeTimes(float newFadeInTime, float newFadeOutTime)
    {
        fadeInTime = Mathf.Clamp(newFadeInTime, MIN_FADE_TIME, MAX_FADE_TIME);
        fadeOutTime = Mathf.Clamp(newFadeOutTime, MIN_FADE_TIME, MAX_FADE_TIME);

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxTips)}: �t�F�[�h���Ԃ��X�V - In:{fadeInTime}�b, Out:{fadeOutTime}�b");
        }
    }

    /// <summary>
    /// ���b�Z�[�WUI�̎Q�Ƃ�ݒ�
    /// </summary>
    /// <param name="newMessagePanel">�V�������b�Z�[�W�p�l��</param>
    /// <param name="newMessageText">�V�������b�Z�[�W�e�L�X�g</param>
    public void SetMessageUI(GameObject newMessagePanel, TextMeshProUGUI newMessageText)
    {
        messagePanel = newMessagePanel;
        messageText = newMessageText;

        // UI���ď�����
        InitializeUI();

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxTips)}: ���b�Z�[�WUI�Q�Ƃ��X�V���܂���");
        }
    }

    #endregion
}