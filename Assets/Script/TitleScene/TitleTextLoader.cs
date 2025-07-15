using UnityEngine;
using TMPro;

/// <summary>
/// �Q�[�����[�h����afterChangeToHerMemory�t���O�Ɋ�Â��ă^�C�g���e�L�X�g��ݒ肷��N���X
/// TitleContainer�I�u�W�F�N�g�ɔz�u���Ďg�p
/// </summary>
public class TitleTextLoader : MonoBehaviour
{
    [Header("�e�L�X�g�ݒ�")]
    [Tooltip("�ύX�Ώۂ�TextMeshPro�R���|�[�l���g")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("�ʏ펞�̃^�C�g���e�L�X�g")]
    [SerializeField] private string normalTitleText = "�u�ށv�̋L��";

    [Tooltip("afterChangeToHerMemory=true���̃^�C�g���e�L�X�g")]
    [SerializeField] private string changedTitleText = "�u�ޏ��v�̋L��";

    [Header("TitleTextChanger�Q��")]
    [Tooltip("TitleTextChanger�ւ̒��ڎQ�Ɓi�I�v�V�����j")]
    [SerializeField] private TitleTextChanger titleTextChanger;

    [Header("�f�o�b�O�ݒ�")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceChangedTitle = false; // �e�X�g�p�̋����ύX

    private void Awake()
    {
        // TextMeshPro�R���|�[�l���g�̎����擾
        if (titleText == null)
        {
            titleText = GetComponent<TMP_Text>();
            if (titleText == null)
            {
                titleText = GetComponentInChildren<TMP_Text>();
            }
        }

        if (titleText == null)
        {
            Debug.LogError("TitleTextLoader: TextMeshPro�R���|�[�l���g��������܂���B�C���X�y�N�^�[�Őݒ肵�Ă��������B");
            enabled = false;
            return;
        }

        // TitleTextChanger�̎�������
        if (titleTextChanger == null)
        {
            titleTextChanger = FindFirstObjectByType<TitleTextChanger>();
        }

        // TitleTextChanger����ݒ�l���擾
        if (titleTextChanger != null)
        {
            // �ʏ�e�L�X�g�Ƃ��Č��̃e�L�X�g���擾
            if (string.IsNullOrEmpty(normalTitleText))
            {
                normalTitleText = titleTextChanger.OriginalTitleText;
            }

            // �ύX��e�L�X�g���擾
            if (string.IsNullOrEmpty(changedTitleText))
            {
                changedTitleText = titleTextChanger.NewTitleText;
            }
            // ���[�h���͌��ʉ��𖳌���
            titleTextChanger.SetSoundEnabled(false);
        }
    }

    private void Start()
    {
        // �����x�������Ċm����GameSaveManager������������Ă�����s
        Invoke("LoadAndApplyTitle", 0.1f);
    }

    /// <summary>
    /// afterChangeToHerMemory�t���O���擾�i���ʏ����j
    /// </summary>
    private bool GetAfterChangeFlag()
    {
        // GameSaveManager����t���O���擾
        if (GameSaveManager.Instance != null)
        {
            return GameSaveManager.Instance.GetAfterChangeToHerMemoryFlag();
        }

        if (debugMode)
            Debug.Log("TitleTextLoader: GameSaveManager�����݂��Ȃ����߁Afalse ��Ԃ��܂�");
        return false;
    }

    /// <summary>
    /// �Z�[�u�f�[�^����t���O��ǂݍ��݁A�^�C�g���e�L�X�g��ݒ�
    /// afterChangeToHerMemory=false�̏ꍇ�͉������Ȃ�
    /// </summary>
    private void LoadAndApplyTitle()
    {
        // afterChangeToLast�t���O��true�̏ꍇ�͏������X�L�b�v
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.GetAfterChangeToLastFlag())
        {
            if (debugMode) Debug.Log("TitleTextLoader: afterChangeToLast��true�̂��ߏ������X�L�b�v���܂�");
            return;
        }

        try
        {
            bool afterChangeFlag = GetAfterChangeFlag();

            if (debugMode)
            {
                Debug.Log($"TitleTextLoader: afterChangeToHerMemory�t���O = {afterChangeFlag}");
                Debug.Log($"TitleTextLoader: ���݂̃^�C�g���e�L�X�g = '{titleText?.text}'");
            }

            // �t���O�Ɋ�Â��ăe�L�X�g��ݒ�
            ApplyTitleText(afterChangeFlag);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"TitleTextLoader: �^�C�g���e�L�X�g�ݒ蒆�ɃG���[: {ex.Message}");
        }
    }


    /// <summary>
    /// �t���O�Ɋ�Â��ă^�C�g���e�L�X�g��K�p
    /// </summary>
    /// <param name="useChangedTitle">�ύX��e�L�X�g���g�p���邩�ǂ���</param>
    private void ApplyTitleText(bool useChangedTitle)
    {
        if (titleText == null)
        {
            Debug.LogError("TitleTextLoader: titleText��null�ł�");
            return;
        }

        string targetText = useChangedTitle ? changedTitleText : normalTitleText;

        // �e�L�X�g����łȂ����Ƃ��m�F
        if (string.IsNullOrEmpty(targetText))
        {
            Debug.LogWarning($"TitleTextLoader: �ݒ肷��e�L�X�g����ł� (useChangedTitle: {useChangedTitle})");
            targetText = useChangedTitle ? "�u�ޏ��v�̋L��" : "�u�ށv�̋L��"; // �t�H�[���o�b�N
        }

        titleText.text = targetText;

        if (debugMode)
        {
            Debug.Log($"TitleTextLoader: �^�C�g���e�L�X�g��ݒ肵�܂���: '{targetText}' (�ύX��: {useChangedTitle})");
        }
    }

    /// <summary>
    /// �O������蓮�Ń^�C�g���e�L�X�g���X�V
    /// afterChangeToHerMemory=false�̏ꍇ�͉������Ȃ�
    /// </summary>
    public void RefreshTitleText()
    {
        LoadAndApplyTitle();
    }

    /// <summary>
    /// �ʏ�^�C�g���������ݒ�i�f�o�b�O�p�j
    /// </summary>
    [ContextMenu("Debug: Set Normal Title")]
    public void SetNormalTitle()
    {
        ApplyTitleText(false);
    }

    /// <summary>
    /// �ύX��^�C�g���������ݒ�i�f�o�b�O�p�j
    /// </summary>
    [ContextMenu("Debug: Set Changed Title")]
    public void SetChangedTitle()
    {
        ApplyTitleText(true);
    }

    /// <summary>
    /// ���݂̃t���O��Ԃ��m�F�i�f�o�b�O�p�j
    /// </summary>
    [ContextMenu("Debug: Check Flag Status")]
    public void CheckFlagStatus()
    {
        bool gameSaveFlag = GameSaveManager.Instance?.GetAfterChangeToHerMemoryFlag() ?? false;
        bool titleChangerFlag = titleTextChanger?.GetAfterChangeToHerMemoryFlag() ?? false;

        Debug.Log($"=== TitleTextLoader �t���O��� ===");
        Debug.Log($"GameSaveManager: {gameSaveFlag}");
        Debug.Log($"TitleTextChanger: {titleChangerFlag}");
        Debug.Log($"���݂̃^�C�g��: '{titleText?.text}'");
        Debug.Log($"==============================");
    }

    /// <summary>
    /// TitleTextChanger����ݒ���Ď擾
    /// </summary>
    public void RefreshFromTitleTextChanger()
    {
        if (titleTextChanger == null)
        {
            titleTextChanger = FindFirstObjectByType<TitleTextChanger>();
        }

        if (titleTextChanger != null)
        {
            normalTitleText = titleTextChanger.OriginalTitleText;
            changedTitleText = titleTextChanger.NewTitleText;

            if (debugMode)
            {
                Debug.Log($"TitleTextLoader: TitleTextChanger����ݒ���X�V���܂���");
                Debug.Log($"�ʏ�e�L�X�g: '{normalTitleText}'");
                Debug.Log($"�ύX��e�L�X�g: '{changedTitleText}'");
            }
        }
    }

    // �v���p�e�B�ŃA�N�Z�X�\�ɂ���
    public string NormalTitleText => normalTitleText;
    public string ChangedTitleText => changedTitleText;
    public bool IsShowingChangedTitle => titleText?.text == changedTitleText;
}