using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TXT�p�Y����PNG�摜�\�����Ȃ���X�N���v�g
/// �p�Y���������ɒ��ڃI���W�i���摜��\�����܂�
/// </summary>
public class TxtPuzzleConnector : MonoBehaviour
{
    [Header("��{�ݒ�")]
    [Tooltip("TXT�p�Y���̊Ǘ��X�N���v�g")]
    [SerializeField] private TxtPuzzleManager txtPuzzleManager;

    [Header("�摜�ݒ�")]
    [Tooltip("���U�C�N�摜�̃R���e�i�i��\���ɂ��邽�߁j")]
    [SerializeField] private GameObject mosaicContainer;

    [Tooltip("�������ɕ\�����錳�̉摜")]
    [SerializeField] private GameObject originalImage;

    [Tooltip("�p�Y�������ŉ�������t�H���_�[")]
    [SerializeField] private GameObject nextFolderOrFile;

    [Header("���ʉ�")]
    [Tooltip("�摜�\�����̌��ʉ�")]
    [SerializeField] private AudioClip completionSound;

    [Header("�f�o�b�O�ݒ�")]
    [SerializeField] private bool debugMode = false;

    // �p�Y���������
    [SerializeField] private bool isPuzzleSolved = false;

    // �v���C�x�[�g�ϐ�
    private AudioSource audioSource;

    private void Awake()
    {
        // �I�[�f�B�I�\�[�X�̎擾�E������
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void Start()
    {
        InitializeTxtPuzzleManager();
        InitializeImageState();

        if (debugMode)
        {
            LogDebug("����������");
        }
    }

    private void OnEnable()
    {
        InitializeImageState();
    }

    /// <summary>
    /// TxtPuzzleManager��������
    /// </summary>
    private void InitializeTxtPuzzleManager()
    {
        if (txtPuzzleManager == null)
        {
            txtPuzzleManager = FindFirstObjectByType<TxtPuzzleManager>();
            if (txtPuzzleManager == null)
            {
                LogDebug("TxtPuzzleManager��������܂���");
            }
        }
    }

    /// <summary>
    /// �摜�\����Ԃ̏����ݒ�
    /// </summary>
    private void InitializeImageState()
    {
        // �p�Y�������łɉ�����Ă���ꍇ�̓I���W�i���摜��\��
        if (isPuzzleSolved)
        {
            ShowCompletedImage();
            return;
        }

        // ���U�C�N�R���e�i��\���A�I���W�i���摜���\��
        if (mosaicContainer != null)
        {
            mosaicContainer.SetActive(true);
        }

        if (originalImage != null)
        {
            originalImage.SetActive(false);
        }

        // ���̃t�H���_�[���\��
        if (nextFolderOrFile != null)
        {
            nextFolderOrFile.SetActive(false);
        }
    }

    /// <summary>
    /// TXT�p�Y���̊������ɌĂ΂�郁�\�b�h
    /// </summary>
    public void OnTxtPuzzleSolved()
    {
        LogDebug("TXT�p�Y�������ʒm����M");

        // �����ς݂Ȃ牽�����Ȃ�
        if (isPuzzleSolved)
        {
            LogDebug("���łɊ������Ă��邽�ߏ������X�L�b�v���܂�");
            return;
        }

        // ���U�C�N������TxtPuzzleManager�ɔC���邩�m�F
        if (txtPuzzleManager != null && txtPuzzleManager.GetComponent<TxtPuzzleManager>() != null)
        {
            LogDebug($"TxtPuzzleManager�Ƀ��U�C�N������C���܂�: {txtPuzzleManager.name}");
            // �����ł͉摜�\���݂̂��s���A���U�C�N��\����TxtPuzzleManager�ɔC����
        }
        else
        {
            // TxtPuzzleManager��������Ȃ��ꍇ�͎����ŏ���
            LogDebug("TxtPuzzleManager��������Ȃ����߁A���̃R���|�[�l���g�Ń��U�C�N�������s���܂�");
        }

        // ���ڊ�����ԂɈڍs
        CompleteRevealing();
    }

    /// <summary>
    /// �����摜��\������
    /// </summary>
    private void CompleteRevealing()
    {
        // ���U�C�N�R���e�i���\��
        if (mosaicContainer != null)
        {
            mosaicContainer.SetActive(false);
        }

        // �I���W�i���摜��\��
        if (originalImage != null)
        {
            originalImage.SetActive(true);
            LogDebug("�I���W�i���摜��\�����܂���");
        }

        // ���̃t�H���_�[�����
        if (nextFolderOrFile != null)
        {
            nextFolderOrFile.SetActive(true);

            // FolderButtonScript��FolderActivationGuard�̐ݒ�
            FolderButtonScript folderScript = nextFolderOrFile.GetComponent<FolderButtonScript>();
            if (folderScript != null)
            {
                folderScript.SetActivatedState(true);
                folderScript.SetVisible(true);
                LogDebug($"���̃t�H���_�[ {folderScript.GetFolderName()} ��������܂���");
            }

            FolderActivationGuard guard = nextFolderOrFile.GetComponent<FolderActivationGuard>();
            if (guard != null)
            {
                guard.SetActivated(true);
            }
        }

        // �������ʉ�
        PlaySound(completionSound);

        // �p�Y�������t���O��ݒ�
        isPuzzleSolved = true;

        // ������Ԃ�ۑ�
        SaveCompletionState();
    }

    /// <summary>
    /// ������Ԃ�\������i�ĕ\���p�j
    /// </summary>
    public void ShowCompletedImage()
    {
        // ���U�C�N�R���e�i���\��
        if (mosaicContainer != null)
        {
            mosaicContainer.SetActive(false);
        }

        // �����摜��\��
        if (originalImage != null)
        {
            originalImage.SetActive(true);
        }

        // ���̃t�H���_�[��\��
        if (nextFolderOrFile != null)
        {
            nextFolderOrFile.SetActive(true);
        }
    }

    /// <summary>
    /// ������Ԃ�ۑ�
    /// </summary>
    private void SaveCompletionState()
    {
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.SaveGame();
            LogDebug("�Q�[����Ԃ�ۑ����܂���");
        }
    }

    /// <summary>
    /// ���ʉ��Đ�
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        // SoundEffectManager��D��
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlayCompletionSound();
            return;
        }

        // ����AudioSource�ōĐ�
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    /// <summary>
    /// �f�o�b�O���O�o��
    /// </summary>
    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"[TxtPuzzleConnector] {message}");
        }
    }
}