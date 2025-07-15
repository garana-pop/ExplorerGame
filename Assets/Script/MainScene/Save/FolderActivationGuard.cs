using UnityEngine;

/// <summary>
/// �t�H���_�[����x�A�N�e�B�u�ɂȂ�����A�ȍ~��A�N�e�B�u�ɂȂ�Ȃ��悤�ɂ���K�[�h�X�N���v�g
/// FolderButton�I�u�W�F�N�g�ɒ��ڃA�^�b�`���Ďg�p���܂�
/// </summary>
public class FolderActivationGuard : MonoBehaviour
{
    [Tooltip("�ŏ�����L����ԂƂ��Ĉ������ǂ���")]
    [SerializeField] private bool activatedByDefault = false;

    [Tooltip("�t�H���_�[���i�����擾�j")]
    [SerializeField] private string folderName = "";

    [Tooltip("�f�o�b�O���O��\�����邩�ǂ���")]
    [SerializeField] private bool debugMode = false;

    // �t�H���_�[����x�ł��A�N�e�B�u�ɂ��ꂽ�����L�^
    private bool hasBeenActivated = false;

    // �ăA�N�e�B�u���̂��߂Ɏg�p����ϐ�
    private bool needsReactivation = false;

    private void Awake()
    {
        // �����l��K�p
        hasBeenActivated = activatedByDefault;

        // �t�H���_�[�����擾
        if (string.IsNullOrEmpty(folderName))
        {
            FolderButtonScript folderScript = GetComponent<FolderButtonScript>();
            if (folderScript != null)
            {
                folderName = folderScript.GetFolderName();
            }
            else
            {
                folderName = gameObject.name;
            }
        }

        // �u�肢�v�t�H���_�[�̏ꍇ�A�����I�ɃA�N�e�B�u��Ԃ��`�F�b�N
        if (gameObject.name.Contains("�肢"))
        {
            FolderButtonScript folderScript = GetComponent<FolderButtonScript>();
            if (folderScript != null)
            {
                folderScript.SetActivatedState(true);
                if (debugMode)
                    Debug.Log($"[FolderActivationGuard] �肢�t�H���_�[�������I�ɃA�N�e�B�u����Ԃɐݒ肵�܂���");
            }
        }
    }

    private void OnEnable()
    {
        // �A�N�e�B�u�ɂȂ������_�Ńt���O�𗧂Ă�
        if (!hasBeenActivated)
        {
            hasBeenActivated = true;

            if (debugMode)
            {
                Debug.Log($"[FolderActivationGuard] {folderName} �����߂ăA�N�e�B�u�ɂȂ�܂���");
            }
        }

        // �ăA�N�e�B�u���t���O�����Z�b�g
        needsReactivation = false;
    }

    private void OnDisable()
    {
        // ��x�A�N�e�B�u�ɂȂ����t�H���_�͔�A�N�e�B�u�ɂ��Ȃ�
        if (hasBeenActivated)
        {
            if (debugMode)
            {
                Debug.Log($"[FolderActivationGuard] {folderName} �̔�A�N�e�B�u����h�~���܂�");
            }

            // ���̃t���[���ōăA�N�e�B�u�����邽�߂̃t���O���Z�b�g
            needsReactivation = true;

            // �d�v: �����ɍăA�N�e�B�u��
            Invoke("ReactivateFolder", 0.01f);
        }
    }

    private void Update()
    {
        // �ăA�N�e�B�u���t���O�������Ă���Ύ��s
        if (needsReactivation && !gameObject.activeSelf)
        {
            ReactivateFolder();
        }
    }

    // �t�H���_�[���ăA�N�e�B�u��
    private void ReactivateFolder()
    {
        if (!gameObject.activeSelf && hasBeenActivated)
        {
            gameObject.SetActive(true);
            needsReactivation = false;

            if (debugMode)
            {
                Debug.Log($"[FolderActivationGuard] {folderName} ���ăA�N�e�B�u�����܂���");
            }
        }
    }

    /// <summary>
    /// �t�H���_�[����x�ł��A�N�e�B�u�ɂȂ��������擾
    /// </summary>
    public bool IsActivated()
    {
        return hasBeenActivated;
    }

    /// <summary>
    /// �t�H���_�[�̃A�N�e�B�u����Ԃ������I�ɐݒ�
    /// </summary>
    public void SetActivated(bool activated)
    {
        hasBeenActivated = activated;

        // �A�N�e�B�u�����ꂽ�ꍇ�A�m���ɃA�N�e�B�u��Ԃɂ���
        if (activated && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);

            // FolderButtonScript���A�����čX�V
            FolderButtonScript folderScript = GetComponent<FolderButtonScript>();
            if (folderScript != null)
            {
                folderScript.SetActivatedState(true);
            }
        }
    }
}