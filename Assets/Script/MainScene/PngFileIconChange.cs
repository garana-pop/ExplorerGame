using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FileIconChange.cs�̃A�C�R���ω��ɘA�����Ď��g�̃A�C�R����ύX����R���|�[�l���g
/// </summary>
public class PngFileIconChange : MonoBehaviour
{
    [Header("�A�C�R���ݒ�")]
    [Tooltip("�ύX�O�̃A�C�R���X�v���C�g")]
    [SerializeField] private Sprite defaultSprite;

    [Tooltip("�ύX��̃A�C�R���X�v���C�g")]
    [SerializeField] private Sprite completedSprite;

    [Tooltip("�Ď��Ώۂ�FileIconChange�R���|�[�l���g�i���ݒ�̏ꍇ�͎��������j")]
    [SerializeField] private FileIconChange targetFileIconChange;

    [Tooltip("�ύX�Ώۂ�Image�R���|�[�l���g�i���ݒ�̏ꍇ�͎��g��Image���g�p�j")]
    [SerializeField] private Image iconImage;

    [Header("�f�o�b�O�ݒ�")]
    [Tooltip("�f�o�b�O���O��\�����邩�ǂ���")]
    [SerializeField] private bool debugMode = false;

    // �Ď��Ώۂ̃X�v���C�g
    private Sprite initialSprite;
    private bool hasCompletedState = false;

    private void Awake()
    {
        // Image�R���|�[�l���g���ݒ肳��Ă��Ȃ���Ύ��g����擾
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
            if (iconImage == null)
            {
                Debug.LogError("PngFileIconChange: Image�R���|�[�l���g��������܂���B���̃X�N���v�g��Image�R���|�[�l���g���A�^�b�`���ꂽ�I�u�W�F�N�g�ɒǉ����Ă��������B");
                enabled = false;
                return;
            }
        }

        // �f�t�H���g�X�v���C�g��ݒ�
        if (defaultSprite != null)
        {
            iconImage.sprite = defaultSprite;
        }
    }

    private void Start()
    {
        // �Ď��Ώۂ�FileIconChange���ݒ肳��Ă��Ȃ���Ύ�������
        if (targetFileIconChange == null)
        {
            FindTargetFileIconChange();
        }

        // ������Ԃ̋L�^
        if (targetFileIconChange != null)
        {
            Image targetImage = targetFileIconChange.GetComponent<Image>();
            if (targetImage != null && targetImage.sprite != null)
            {
                initialSprite = targetImage.sprite;
            }
        }

        // ������Ԃ̊m�F
        CheckIconState();
    }

    private void OnEnable()
    {
        // �I�u�W�F�N�g���L���ɂȂ邽�тɏ�Ԃ��`�F�b�N
        if (targetFileIconChange != null)
        {
            // �Ď��Ώۂ�image�̖��O��"txt�t�@�C���A�C�R��_0"���`�F�b�N
            Image targetImage = targetFileIconChange.GetComponent<Image>();
            if (targetImage != null && targetImage.sprite != null)
            {
                // �X�v���C�g����"txt�t�@�C���A�C�R��_0"�������ꍇ
                if (targetImage.sprite.name == "txt�t�@�C���A�C�R��_0")
                {
                    // completedSprite�ɕύX
                    if (completedSprite != null && iconImage != null)
                    {
                        iconImage.sprite = completedSprite;
                        hasCompletedState = true;

                        if (debugMode)
                        {
                            Debug.Log($"PngFileIconChange(OnEnable): txt�t�@�C���A�C�R��_0�����o�������߁A������Ԃɐݒ肵�܂��� - {gameObject.name}");
                        }
                    }
                }
            }
        }
    }

    private void Update()
    {
        // FileIconChange�̏�Ԃ��`�F�b�N
        CheckIconState();
    }

    /// <summary>
    /// �Ď��Ώۂ�FileIconChange������
    /// </summary>
    private void FindTargetFileIconChange()
    {
        // �e�K�w�����ǂ���FileIconChange������
        Transform current = transform.parent;
        while (current != null)
        {
            FileIconChange fileIconChange = current.GetComponent<FileIconChange>();
            if (fileIconChange != null)
            {
                targetFileIconChange = fileIconChange;
                if (debugMode)
                {
                    Debug.Log($"PngFileIconChange: �e�K�w����FileIconChange���������o���܂���: {current.name}");
                }
                return;
            }
            current = current.parent;
        }

        // �e�K�w�Ɍ�����Ȃ��ꍇ�̓V�[�������疼�O�̗ގ����Ō���
        string myName = gameObject.name;
        string baseName = ExtractBaseName(myName);

        if (!string.IsNullOrEmpty(baseName))
        {
            FileIconChange[] allFileIconChanges = FindObjectsByType<FileIconChange>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var fileIconChange in allFileIconChanges)
            {
                string otherName = fileIconChange.gameObject.name;
                if (otherName.Contains(baseName) || baseName.Contains(ExtractBaseName(otherName)))
                {
                    targetFileIconChange = fileIconChange;
                    if (debugMode)
                    {
                        Debug.Log($"PngFileIconChange: ���O�̗ގ�������FileIconChange���������o���܂���: {otherName}");
                    }
                    return;
                }
            }
        }

        // ����ł�������Ȃ��ꍇ�͌x��
        Debug.LogWarning("PngFileIconChange: �Ď��Ώۂ�FileIconChange��������܂���ł����B�C���X�y�N�^�[�Ŏ蓮�ݒ肵�Ă��������B");
    }

    /// <summary>
    /// ���O�̃x�[�X�����𒊏o�i�g���q�␔���������j
    /// </summary>
    private string ExtractBaseName(string fullName)
    {
        // �g���q����菜��
        int dotIndex = fullName.LastIndexOf('.');
        if (dotIndex > 0)
        {
            fullName = fullName.Substring(0, dotIndex);
        }

        return fullName;
    }

    /// <summary>
    /// FileIconChange�̏�Ԃ��m�F���A�K�v�ɉ����ăA�C�R����ύX
    /// </summary>
    private void CheckIconState()
    {
        if (targetFileIconChange == null || iconImage == null) return;

        // FileIconChange��Image�R���|�[�l���g���擾
        Image targetImage = targetFileIconChange.GetComponent<Image>();
        if (targetImage == null || targetImage.sprite == null) return;

        // �����l�����ݒ�Ȃ�ݒ�
        if (initialSprite == null)
        {
            initialSprite = targetImage.sprite;
            return;
        }

        // �X�v���C�g�������l����ύX���ꂽ�����`�F�b�N
        bool isChanged = (initialSprite != targetImage.sprite);

        // ��Ԃ��ω������ꍇ�̂ݏ���
        if (isChanged != hasCompletedState)
        {
            hasCompletedState = isChanged;

            if (isChanged)
            {
                // ������Ԃ̃X�v���C�g�ɕύX
                if (completedSprite != null)
                {
                    iconImage.sprite = completedSprite;
                    if (debugMode)
                    {
                        Debug.Log($"PngFileIconChange: �A�C�R����������ԂɕύX���܂��� - {gameObject.name}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// �Ď��Ώۂ�FileIconChange���蓮�Őݒ�
    /// </summary>
    public void SetTargetFileIconChange(FileIconChange target)
    {
        targetFileIconChange = target;

        // �����X�v���C�g�����Z�b�g
        initialSprite = null;

        // �O�̃X�v���C�g���X�V
        if (target != null)
        {
            Image targetImage = target.GetComponent<Image>();
            if (targetImage != null)
            {
                initialSprite = targetImage.sprite;
            }
        }

        // �ݒ��ɂ����ɏ�Ԃ��`�F�b�N
        CheckIconState();
    }

    /// <summary>
    /// �蓮�ŃA�C�R����������Ԃɐݒ�
    /// </summary>
    public void SetCompleted(bool completed)
    {
        if (iconImage == null) return;

        if (completed && completedSprite != null)
        {
            iconImage.sprite = completedSprite;
            hasCompletedState = true;
        }
        else if (defaultSprite != null)
        {
            iconImage.sprite = defaultSprite;
            hasCompletedState = false;
        }
    }
}