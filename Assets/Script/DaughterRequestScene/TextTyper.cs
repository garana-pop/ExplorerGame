using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// �e�L�X�g���ꕶ�����^�C�v���C�^�[�̂悤�ɕ\������X�N���v�g
/// </summary>
public class TextTyper : MonoBehaviour
{
    [Header("�e�L�X�g�ݒ�")]
    [Tooltip("�\������TextMeshPro�R���|�[�l���g")]
    [SerializeField] private TMP_Text textComponent;

    [Tooltip("�\�����銮�S�ȃe�L�X�g")]
    [SerializeField] private string fullText = "�C���������B�����Ă�E�E�E";

    [Header("�^�C�s���O�ݒ�")]
    [Tooltip("1�����\������܂ł̎��ԁi�b�j")]
    [SerializeField] private float typingSpeed = 0.1f;

    [Tooltip("�e�����\����̒ǉ��ҋ@���ԁi�b�j")]
    [SerializeField] private float characterDelay = 0.0f;

    [Tooltip("��Ǔ_�ł̒ǉ��ҋ@���ԁi�b�j")]
    [SerializeField] private float punctuationDelay = 0.25f;

    [Header("���ʉ��ݒ�")]
    [Tooltip("�^�C�s���O�����Đ����邩")]
    [SerializeField] private bool playTypeSound = true;

    [Header("�����J�n�ݒ�")]
    [Tooltip("�V�[���ǂݍ��ݎ��Ɏ����I�ɊJ�n���邩")]
    [SerializeField] private bool autoStart = true;

    [Tooltip("�����J�n�܂ł̒x�����ԁi�b�j")]
    [SerializeField] private float autoStartDelay = 1.0f;

    // �����ϐ�
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private string currentText = "";
    private SoundEffectManager soundManager;

    // �^�C�s���O�����C�x���g�f���Q�[�g
    public delegate void TypingCompletedHandler();
    public event TypingCompletedHandler OnTypingCompleted;

    private void Awake()
    {
        // TextMeshPro�R���|�[�l���g�̎擾
        if (textComponent == null)
        {
            textComponent = GetComponent<TMP_Text>();
            if (textComponent == null)
            {
                Debug.LogError("TextTyper: TextMeshPro�R���|�[�l���g��������܂���B�R���|�[�l���g���A�^�b�`���邩�A�C���X�y�N�^�[�Őݒ肵�Ă��������B");
                enabled = false;
                return;
            }
        }

        // SoundEffectManager�̎Q�Ƃ��擾
        soundManager = SoundEffectManager.Instance;

        // �����e�L�X�g���N���A
        textComponent.text = "";
    }

    private void Start()
    {
        // �����J�n���L���Ȃ�A�w�肳�ꂽ�x����ɊJ�n
        if (autoStart)
        {
            Invoke("StartTyping", autoStartDelay);
        }
    }

    /// <summary>
    /// �^�C�s���O���J�n����p�u���b�N���\�b�h
    /// </summary>
    public void StartTyping()
    {
        // ���łɃ^�C�s���O���Ȃ牽�����Ȃ�
        if (isTyping) return;

        // �����̃R���[�`�����~
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // �e�L�X�g�R���|�[�l���g���N���A
        textComponent.text = "";
        currentText = "";

        // �^�C�s���O�R���[�`�����J�n
        typingCoroutine = StartCoroutine(TypeText());
    }

    /// <summary>
    /// �^�C�s���O���~����p�u���b�N���\�b�h
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
    /// �^�C�s���O���X�L�b�v���đS�e�L�X�g��\������p�u���b�N���\�b�h
    /// </summary>
    public void SkipToEnd()
    {
        StopTyping();
        textComponent.text = fullText;
        currentText = fullText;
        isTyping = false;
        IsCompleted = true; // �����t���O��ݒ�

        // �^�C�s���O�����C�x���g�𔭉�
        OnTypingCompleted?.Invoke();
    }

    /// <summary>
    /// �^�C�s���O�R���[�`��
    /// </summary>
    private IEnumerator TypeText()
    {
        isTyping = true;
        IsCompleted = false; // �J�n���Ƀt���O�����Z�b�g


        // �ҋ@���Ԃ̏����ݒ�
        float waitTime = typingSpeed;

        // ������1���\��
        for (int i = 0; i < fullText.Length; i++)
        {
            // ���̕�����ǉ�
            currentText += fullText[i];
            textComponent.text = currentText;

            // �����ɉ��������ʉ��Đ��Ƒҋ@���Ԑݒ�
            char currentChar = fullText[i];
            if (IsPunctuation(currentChar))
            {
                // ��Ǔ_���ʉ��̍Đ�
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
                // �ʏ�̕������ʉ�
                if (playTypeSound)
                {
                    if (soundManager != null)
                    {
                        soundManager.PlayTypeSound();
                    }
                }
                waitTime = typingSpeed;
            }

            // ���̕����\���܂őҋ@
            yield return new WaitForSeconds(waitTime + characterDelay);
        }

        isTyping = false;
        IsCompleted = true; // �����t���O��ݒ�

        // �^�C�s���O�����C�x���g�𔭉�
        OnTypingCompleted?.Invoke();
    }

    /// <summary>
    /// �^�C�s���O��Ԃ����Z�b�g����
    /// </summary>
    public void ResetTyping()
    {
        StopTyping();
        textComponent.text = "";
        currentText = "";
        IsCompleted = false;
    }

    /// <summary>
    /// ��Ǔ_���ǂ������`�F�b�N
    /// </summary>
    private bool IsPunctuation(char character)
    {
        return character == '�B' || character == '�A' || character == '.' ||
               character == ',' || character == '?' || character == '!' ||
               character == '�H' || character == '�I' || character == '�c' ||
               character == '�E';
    }

    /// <summary>
    /// �\���e�L�X�g��ݒ肷��p�u���b�N���\�b�h
    /// </summary>
    public void SetText(string text)
    {
        fullText = text;
        IsCompleted = false; // �V�����e�L�X�g�ݒ莞�Ƀ��Z�b�g

        // ���łɃ^�C�s���O���Ȃ�ĊJ�n
        if (isTyping)
        {
            StopTyping();
            StartTyping();
        }
    }

    /// <summary>
    /// ������Ԃ��O�����狭���ݒ肷��i�f�o�b�O�p�j
    /// </summary>
    public void SetCompleted(bool completed)
    {
        IsCompleted = completed;
        if (completed && !isTyping)
        {
            // ������Ԃɐݒ肵�A�C�x���g�𔭉�
            OnTypingCompleted?.Invoke();
        }
    }

    /// <summary>
    /// �^�C�s���O�����ǂ������擾
    /// </summary>
    public bool IsTyping()
    {
        return isTyping;
    }

    /// <summary>
    /// �^�C�s���O���������Ă��邩�ǂ������擾
    /// </summary>
    public bool IsCompleted { get; private set; } = false;

    /// <summary>
    /// ���ݕ\������Ă���e�L�X�g���擾
    /// </summary>
    public string CurrentText => currentText;

    /// <summary>
    /// �\���\��̑S�����擾
    /// </summary>
    public string FullText => fullText;
}