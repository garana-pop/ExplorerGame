using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PDF�t�@�C���̃A�C�R����PdfDocumentManager�̊�����Ԃɉ����ĕύX����R���|�[�l���g
/// </summary>
public class PdfFileIconChange : MonoBehaviour
{
    [Header("�A�C�R���ݒ�")]
    [Tooltip("�ύX�O�̃A�C�R���X�v���C�g")]
    [SerializeField] private Sprite defaultSprite;

    [Tooltip("�ύX��̃A�C�R���X�v���C�g")]
    [SerializeField] private Sprite completedSprite;

    [Header("�Q�Ɛݒ�")]
    [Tooltip("�Q�Ƃ���PdfDocumentManager�R���|�[�l���g�i���ݒ�̏ꍇ�͎��������j")]
    [SerializeField] private PdfDocumentManager pdfDocumentManager;

    [Tooltip("�ύX�Ώۂ�Image�R���|�[�l���g�i���ݒ�̏ꍇ�͎��g��Image���g�p�j")]
    [SerializeField] private Image iconImage;

    [Header("�f�o�b�O�ݒ�")]
    [Tooltip("�f�o�b�O���O��\�����邩�ǂ���")]
    [SerializeField] private bool debugMode = false;

    // ������Ԃ������t���O
    private bool isCompleted = false;

    private void Awake()
    {
        // Image�R���|�[�l���g���ݒ肳��Ă��Ȃ���Ύ��g����擾
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
            if (iconImage == null)
            {
                Debug.LogError("PdfFileIconChange: Image�R���|�[�l���g��������܂���B���̃X�N���v�g��Image�R���|�[�l���g���A�^�b�`���ꂽ�I�u�W�F�N�g�ɒǉ����Ă��������B");
                enabled = false;
                return;
            }
        }

        // �f�t�H���g�X�v���C�g��ݒ�
        if (defaultSprite != null && iconImage.sprite == null)
        {
            iconImage.sprite = defaultSprite;
        }
    }

    private void Start()
    {
        // PdfDocumentManager���ݒ肳��Ă��Ȃ���Ύ�������
        if (pdfDocumentManager == null)
        {
            FindPdfDocumentManager();
        }

        // ������Ԃ̊m�F
        CheckCompletionState();
    }

    private void OnEnable()
    {
        // �I�u�W�F�N�g���L���ɂȂ邽�тɊ�����Ԃ��m�F
        CheckCompletionState();
    }

    /// <summary>
    /// �Q�Ƃ���PdfDocumentManager������
    /// </summary>
    private void FindPdfDocumentManager()
    {
        // �e�K�w�����ǂ���PdfDocumentManager������
        Transform current = transform.parent;
        while (current != null)
        {
            PdfDocumentManager manager = current.GetComponent<PdfDocumentManager>();
            if (manager != null)
            {
                pdfDocumentManager = manager;
                if (debugMode)
                {
                    Debug.Log($"PdfFileIconChange: �e�K�w����PdfDocumentManager���������o���܂���: {current.name}");
                }
                return;
            }
            current = current.parent;
        }

        // ����PDF�t�@�C���p�l����������
        Transform filePanel = transform;
        while (filePanel != null && !filePanel.name.Contains("FilePanel"))
        {
            filePanel = filePanel.parent;
        }

        if (filePanel != null)
        {
            PdfDocumentManager manager = filePanel.GetComponentInChildren<PdfDocumentManager>(true);
            if (manager != null)
            {
                pdfDocumentManager = manager;
                if (debugMode)
                {
                    Debug.Log($"PdfFileIconChange: �t�@�C���p�l��������PdfDocumentManager���������o���܂���: {filePanel.name}");
                }
                return;
            }
        }

        // ����ł�������Ȃ��ꍇ�͌x��
        Debug.LogWarning("PdfFileIconChange: �Q�Ƃ���PdfDocumentManager��������܂���ł����B�C���X�y�N�^�[�Ŏ蓮�ݒ肵�Ă��������B");
    }

    /// <summary>
    /// ������Ԃ��m�F���ēK�p����p�u���b�N���\�b�h
    /// </summary>
    public void CheckCompletionState()
    {
        if (pdfDocumentManager == null || iconImage == null) return;

        bool currentState = pdfDocumentManager.IsDocumentCompleted();

        // ��Ԃ��ω������ꍇ�̂ݏ���
        if (currentState != isCompleted)
        {
            isCompleted = currentState;

            if (isCompleted)
            {
                // ������Ԃ̃X�v���C�g�ɕύX
                if (completedSprite != null)
                {
                    iconImage.sprite = completedSprite;
                    if (debugMode)
                    {
                        Debug.Log($"PdfFileIconChange: �A�C�R����������ԂɕύX���܂��� - {gameObject.name}");
                    }
                }
            }
            else
            {
                // �f�t�H���g��Ԃ̃X�v���C�g�ɕύX
                if (defaultSprite != null)
                {
                    iconImage.sprite = defaultSprite;
                    if (debugMode)
                    {
                        Debug.Log($"PdfFileIconChange: �A�C�R�����f�t�H���g��ԂɕύX���܂��� - {gameObject.name}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// �Q�Ƃ���PdfDocumentManager���蓮�Őݒ�
    /// </summary>
    public void SetPdfDocumentManager(PdfDocumentManager manager)
    {
        pdfDocumentManager = manager;

        // �ݒ��ɂ����ɏ�Ԃ��`�F�b�N
        CheckCompletionState();
    }

    /// <summary>
    /// �蓮�ŃA�C�R����������Ԃɐݒ�
    /// </summary>
    public void SetCompleted(bool completed)
    {
        if (iconImage == null) return;

        isCompleted = completed;

        if (completed && completedSprite != null)
        {
            iconImage.sprite = completedSprite;
        }
        else if (defaultSprite != null)
        {
            iconImage.sprite = defaultSprite;
        }
    }
}