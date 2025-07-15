using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// PNG�t�@�C���̕\���𐧌䂷��X�N���v�g
/// TXT�p�Y���������Ɍ��̉摜��\�����܂�
/// </summary>
public class ImageRevealer : MonoBehaviour
{
    [Header("��{�ݒ�")]
    [Tooltip("���U�C�N�摜�̃R���e�i")]
    [SerializeField] private GameObject mosaicContainer;

    [Tooltip("�p�Y���������ɕ\�����錳�̉摜")]
    [SerializeField] private GameObject originalImage;

    [Tooltip("���ɉ�������t�H���_�[/�t�@�C��")]
    [SerializeField] private GameObject nextFolderOrFile;

    [Header("TXT�p�Y���A�g�ݒ�")]
    [Tooltip("�֘A����TXT�p�Y���}�l�[�W���[�i�C���X�y�N�^�[�Őݒ�j")]
    [SerializeField] private TxtPuzzleManager linkedTxtPuzzleManager;

    [Header("�Z�[�u�p�ݒ�")]
    [Tooltip("����PNG�t�@�C���̎��ʖ��i�K���ݒ肵�Ă��������j")]
    [SerializeField] private string fileName = "image.png";

    [Header("�i���\��")]
    [SerializeField] private TextMeshProUGUI progressText;

    [Header("�f�o�b�O")]
    [SerializeField] private bool debugMode = false;

    // �摜�\�����
    private bool isRevealed = false;

    // ���U�C�N���i���I�ɔ�A�N�e�B�u�ɂȂ������̃t���O
    private bool mosaicPermanentlyDisabled = false;

    // �p�Y�������`�F�b�N�ς݃t���O
    private bool hasCheckedPuzzleCompletion = false;

    private AudioSource audioSource;

    // �m���Ɉ�x�����p�Y���������`�F�b�N���邽�߂̏����J�E���^�[
    private int initializationAttempts = 0;
    private const int MAX_INITIALIZATION_ATTEMPTS = 3;

    private void Awake()
    {
        // �I�[�f�B�I�\�[�X�̎擾
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        // ������Ԃ��m���ɐݒ�
        InitializeState();

        // ����̃p�Y�������`�F�b�N���J�n
        if (!hasCheckedPuzzleCompletion)
        {
            // �x���`�F�b�N�̊J�n
            StartCoroutine(DelayedCompletionCheck());
        }
    }

    private void OnEnable()
    {
        // ��ʕ\�����ɏ�Ԃ��X�V
        UpdateVisualState();

        // ���������������g���C����d�g��
        if (initializationAttempts < MAX_INITIALIZATION_ATTEMPTS)
        {
            Invoke("DelayedStateCheck", 0.5f);
            initializationAttempts++;
        }
    }

    // �e�I�u�W�F�N�g�ύX���̏�����ǉ�
    private void OnTransformParentChanged()
    {
        if (debugMode)
            Debug.Log($"ImageRevealer '{fileName}': �e�I�u�W�F�N�g���ύX����܂���");

        // �e���ς����������Ԃ��ĕ]��
        UpdateVisualState();

        // DraggingCanvas�ֈړ������ꍇ�̓��ʏ���
        if (transform.parent != null && transform.parent.name.Contains("DraggingCanvas"))
        {
            if (debugMode)
                Debug.Log($"ImageRevealer '{fileName}': DraggingCanvas�Ɉړ����܂���");

            // �p�Y�������`�F�b�N�������I�Ɏ��s
            ForceCheckTxtPuzzleCompletion();
        }
    }

    // �x�����ăp�Y���������`�F�b�N����R���[�`��
    private System.Collections.IEnumerator DelayedCompletionCheck()
    {
        // �����t���[���ҋ@���Ċm����TXT�p�Y���̏�Ԃ��擾
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForSeconds(0.2f);
            CheckTxtPuzzleCompletion();
        }

        hasCheckedPuzzleCompletion = true;
    }

    // �x�����ď�ԃ`�F�b�N���s���iInvoke����Ă΂��j
    private void DelayedStateCheck()
    {
        // �p�Y��������Ԃ��ă`�F�b�N
        CheckTxtPuzzleCompletion();

        // ���o�I�ȏ�Ԃ��X�V
        UpdateVisualState();
    }

    /// <summary>
    /// �摜�\����Ԃ̏�����
    /// </summary>
    public void InitializeState()
    {
        // �Q�[���̊J�n���܂��͍ēǂݍ��ݎ��Ɉ�x�����Ă΂��
        if (isRevealed || mosaicPermanentlyDisabled)
        {
            // �i���I�ȕ\����Ԃ̏ꍇ
            DisableMosaicPermanently();
        }
        else
        {
            // �܂��\������Ă��Ȃ��������
            if (mosaicContainer != null)
                mosaicContainer.SetActive(true);

            if (originalImage != null)
                originalImage.SetActive(false);

            if (nextFolderOrFile != null)
                nextFolderOrFile.SetActive(false);
        }

        // �i���\���̍X�V
        UpdateProgressDisplay();
    }

    /// <summary>
    /// ���o�I�ȏ�Ԃ��X�V�i������ԂɊ�Â��ĕ\���𒲐��j
    /// </summary>
    private void UpdateVisualState()
    {
        if (isRevealed || mosaicPermanentlyDisabled)
        {
            // �\���ςݏ��
            ShowRevealedImage();
        }
        else
        {
            // �������
            if (mosaicContainer != null)
                mosaicContainer.SetActive(true);

            if (originalImage != null)
                originalImage.SetActive(false);
        }

        // �i���\���̍X�V
        UpdateProgressDisplay();
    }

    /// <summary>
    /// ���U�C�N���i���I�ɔ�\���ɂ���
    /// </summary>
    private void DisableMosaicPermanently()
    {
        // ���U�C�N���i���I�ɔ�\����
        mosaicPermanentlyDisabled = true;

        // ���o�v�f�̍X�V
        if (mosaicContainer != null)
            mosaicContainer.SetActive(false);

        if (originalImage != null)
            originalImage.SetActive(true);
    }

    /// <summary>
    /// �����摜��\������
    /// </summary>
    public void RevealImage()
    {
        // ���łɕ\���ς݂Ȃ牽�����Ȃ�
        if (isRevealed) return;

        // ��ԃt���O��ݒ�
        isRevealed = true;
        mosaicPermanentlyDisabled = true;

        // ���U�C�N���\��
        if (mosaicContainer != null)
        {
            mosaicContainer.SetActive(false);

            if (debugMode)
                Debug.Log($"ImageRevealer '{fileName}': ���U�C�N���i���I�ɔ�\���ɂ��܂���");
        }

        // �����摜��\��
        if (originalImage != null)
        {
            originalImage.SetActive(true);

            if (debugMode)
                Debug.Log($"ImageRevealer '{fileName}': �I���W�i���摜��\�����܂���");
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
            }

            FolderActivationGuard guard = nextFolderOrFile.GetComponent<FolderActivationGuard>();
            if (guard != null)
            {
                guard.SetActivated(true);
            }

            if (debugMode)
                Debug.Log($"ImageRevealer '{fileName}': ���̃t�H���_�[��������܂���");
        }

        // ���ʉ��Đ�
        SoundEffectManager.Instance?.PlayCompletionSound();

        // �i���\���̍X�V
        UpdateProgressDisplay();

        // �������ɃQ�[����Ԃ�ۑ�
        SaveGameState();
    }

    /// <summary>
    /// �\���ς݉摜���ĕ\������
    /// </summary>
    private void ShowRevealedImage()
    {
        // ���U�C�N���\��
        if (mosaicContainer != null)
            mosaicContainer.SetActive(false);

        // ���U�C�N���i���I�ɔ�A�N�e�B�u�ɐݒ�
        mosaicPermanentlyDisabled = true;

        // �����摜��\��
        if (originalImage != null)
            originalImage.SetActive(true);

        // ���̃t�H���_�[��\��
        if (nextFolderOrFile != null)
            nextFolderOrFile.SetActive(true);

        // �i���\���̍X�V
        UpdateProgressDisplay();
    }

    /// <summary>
    /// TXT�p�Y���̊�����Ԃ������I�Ƀ`�F�b�N
    /// </summary>
    public void ForceCheckTxtPuzzleCompletion()
    {
        if (debugMode)
            Debug.Log($"ImageRevealer '{fileName}': TXT�p�Y�������������`�F�b�N���܂�");

        // TXT�p�Y�����������Ă���΋����I�ɕ\��
        if (linkedTxtPuzzleManager != null && linkedTxtPuzzleManager.IsPuzzleCompleted())
        {
            if (debugMode)
                Debug.Log($"ImageRevealer '{fileName}': TXT�p�Y�����������Ă��܂� - �摜�\�����������܂�");

            RevealImage();
        }
        else if (debugMode)
        {
            // TXT�p�Y���}�l�[�W���[�̏�Ԃ��ڍ׃��O
            if (linkedTxtPuzzleManager == null)
                Debug.Log($"ImageRevealer '{fileName}': �����N���ꂽTXT�p�Y���}�l�[�W���[������܂���");
            else
                Debug.Log($"ImageRevealer '{fileName}': TXT�p�Y���̊������: {linkedTxtPuzzleManager.IsPuzzleCompleted()}");
        }
    }

    /// <summary>
    /// TXT�p�Y���̊�����Ԃ��`�F�b�N���A�������Ă���Ή摜��\��
    /// </summary>
    public void CheckTxtPuzzleCompletion()
    {
        // ���łɕ\���ς݂Ȃ牽�����Ȃ�
        if (isRevealed || mosaicPermanentlyDisabled) return;

        // TxtPuzzleManager���ݒ肳��Ă��炸�A���̏ꏊ�ɂ��邩������Ȃ��ꍇ�͌���
        if (linkedTxtPuzzleManager == null)
        {
            // �e��k����TxtPuzzleManager������
            linkedTxtPuzzleManager = GetComponentInParent<TxtPuzzleManager>();

            // �e�Ɍ�����Ȃ���΃V�[�����Ō���
            if (linkedTxtPuzzleManager == null)
            {
                linkedTxtPuzzleManager = FindFirstObjectByType<TxtPuzzleManager>();

                if (linkedTxtPuzzleManager != null && debugMode)
                    Debug.Log($"ImageRevealer '{fileName}': �V�[������TxtPuzzleManager�������܂���: {linkedTxtPuzzleManager.name}");
            }
        }

        // TxtPuzzleManager���ݒ肳��Ă���A�p�Y�����������Ă���ꍇ
        if (linkedTxtPuzzleManager != null && linkedTxtPuzzleManager.IsPuzzleCompleted())
        {
            if (debugMode)
                Debug.Log($"ImageRevealer '{fileName}': TXT�p�Y�����������Ă��邽�߉摜��\�����܂�");

            // �摜��\��
            RevealImage();
        }
    }

    /// <summary>
    /// �i���\�����X�V
    /// </summary>
    private void UpdateProgressDisplay()
    {
        if (progressText != null)
        {
            if (isRevealed || mosaicPermanentlyDisabled)
            {
                progressText.text = "�摜��������!";
            }
            else if (linkedTxtPuzzleManager != null)
            {
                progressText.text = "TXT�p�Y�����N���A���ĉ摜��\��";
            }
            else
            {
                progressText.text = "�֘A�p�Y�������������Ă�������";
            }
        }
    }

    /// <summary>
    /// �Q�[����Ԃ�ۑ�
    /// </summary>
    private void SaveGameState()
    {
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.SaveGame();

            if (debugMode)
                Debug.Log($"�摜�\������: {fileName} - �Q�[����Ԃ�ۑ����܂���");
        }
    }

    /// <summary>
    /// ���̉摜��PNG�i�����擾
    /// </summary>
    public PngFileData GetImageProgress()
    {
        return new PngFileData
        {
            fileName = fileName,
            currentLevel = isRevealed ? 1 : 0,
            maxLevel = 1,
            isRevealed = isRevealed || mosaicPermanentlyDisabled // ���U�C�N�i���������t���O�����f
        };
    }

    /// <summary>
    /// �摜�t�@�C�������擾
    /// </summary>
    public string GetImageFileName() => fileName;

    /// <summary>
    /// �摜�̐i����K�p
    /// </summary>
    public void ApplyImageProgress(PngFileData progressData)
    {
        if (progressData == null) return;

        // �Z�[�u�f�[�^�����Ԃ𕜌�
        isRevealed = progressData.isRevealed;

        // isRevealed��true�Ȃ烂�U�C�N�i�����������ݒ�
        if (isRevealed)
            mosaicPermanentlyDisabled = true;

        if (isRevealed || mosaicPermanentlyDisabled)
        {
            ShowRevealedImage();

            if (debugMode)
                Debug.Log($"ImageRevealer '{fileName}': �Z�[�u�f�[�^���畜�� - �摜�\�����");
        }
        else
        {
            InitializeState();

            // TXT�p�Y���̏�Ԃ��m�F�i�Z�[�u�f�[�^�K�p��j
            Invoke("CheckTxtPuzzleCompletion", 0.2f);
        }
    }

    /// <summary>
    /// �摜���\������Ă��邩�擾
    /// </summary>
    public bool IsImageRevealed() => isRevealed || mosaicPermanentlyDisabled;
}