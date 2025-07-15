using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// ����G�t�@�C���̍폜�@�\���Ǘ�����N���X
/// �폜�m�F�_�C�A���O�̕\���A�폜�����AGameSaveManager�Ƃ̘A�g�A�V�[���J�ڂ�S��
/// </summary>
public class PortraitDeletionManager : MonoBehaviour
{
    [Header("UI�Q��")]
    [Tooltip("�폜�m�F�p�l���iDeleteSelectionPanel�j")]
    [SerializeField] private GameObject deletionConfirmationPanel;

    [Tooltip("�폜�m�F���b�Z�[�W�e�L�X�g")]
    [SerializeField] private TextMeshProUGUI confirmationMessageText;

    [Tooltip("�f�t�H���g�̊m�F���b�Z�[�W")]
    [SerializeField] private string defaultConfirmationMessage = "����G���폜���܂����H\n�����̑���͎������܂���";

    [Header("�{�^���Q��")]
    [Tooltip("�폜�����s����u�͂��v�{�^��")]
    [SerializeField] private Button confirmButton;

    [Tooltip("�폜���L�����Z������u�������v�{�^��")]
    [SerializeField] private Button cancelButton;

    [Tooltip("�폜�m�F�p�l����\������g���K�[�{�^���i����G�摜�Ȃǁj")]
    [SerializeField] private Button deleteButton;

    [Header("�폜�Ώ�")]
    [Tooltip("�폜�Ώۂ̎���G�I�u�W�F�N�g")]
    [SerializeField] private GameObject portraitObject;

    [Tooltip("�폜�Ώۂ̃t�@�C����")]
    [SerializeField] private string portraitFileName = "����G.png";

    [Header("�G�t�F�N�g�ݒ�")]
    [Tooltip("�폜���̃t�F�[�h���o����")]
    [SerializeField] private float fadeDuration = 1.0f;

    [Tooltip("�폜�G�t�F�N�g�p��CanvasGroup�i�I�v�V�����j")]
    [SerializeField] private CanvasGroup portraitCanvasGroup;

    [Header("�T�E���h�ݒ�")]
    [Tooltip("�폜���s���̌��ʉ�")]
    [SerializeField] private AudioClip deletionSound;

    [Tooltip("�L�����Z�����̌��ʉ�")]
    [SerializeField] private AudioClip cancelSound;

    [Header("�V�[���J�ڐݒ�")]
    [Tooltip("�폜��ɑJ�ڂ���V�[����")]
    [SerializeField] private string nextSceneName = "TitleScene";

    [Tooltip("�폜��A�V�[���J�ڂ܂ł̑ҋ@����")]
    [SerializeField] private float sceneTransitionDelay = 2.0f;

    [Header("�I�[�o�[���C�ݒ�")]
    [Tooltip("�������ɕ\������I�[�o�[���C�i�I�v�V�����j")]
    [SerializeField] private GameObject processingOverlay;

    [Header("�f�o�b�O�ݒ�")]
    [SerializeField] private bool debugMode = false;

    // �������
    private bool isProcessingDeletion = false;
    private AudioSource audioSource;
    private GameSaveManager saveManager;


    private void Awake()
    {
        // AudioSource�̎擾�܂��͍쐬
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // GameSaveManager�̎Q�Ǝ擾
        saveManager = GameSaveManager.Instance;
        if (saveManager == null && debugMode)
        {
            Debug.LogWarning("PortraitDeletionManager: GameSaveManager��������܂���");
        }
    }

    private void Start()
    {
        // �{�^���C�x���g�̐ݒ�
        SetupButtonListeners();

        // ������Ԃ̐ݒ�
        if (deletionConfirmationPanel != null)
        {
            deletionConfirmationPanel.SetActive(false);
        }

        if (processingOverlay != null)
        {
            processingOverlay.SetActive(false);
        }

        // �m�F���b�Z�[�W�̐ݒ�
        if (confirmationMessageText != null && string.IsNullOrEmpty(confirmationMessageText.text))
        {
            confirmationMessageText.text = defaultConfirmationMessage;
        }
    }

    /// <summary>
    /// �{�^�����X�i�[�̐ݒ�
    /// </summary>
    private void SetupButtonListeners()
    {
        // �폜�{�^���i����G���N���b�N�Ȃǁj
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(ShowDeletionConfirmation);
        }

        // �m�F�{�^��
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmDeletion);
        }

        // �L�����Z���{�^��
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(OnCancelDeletion);
        }
    }

    /// <summary>
    /// �폜�m�F�_�C�A���O��\��
    /// </summary>
    public void ShowDeletionConfirmation()
    {
        if (isProcessingDeletion)
        {
            if (debugMode)
                Debug.Log("PortraitDeletionManager: �폜�������̂��߁A�m�F�_�C�A���O��\���ł��܂���");
            return;
        }

        if (deletionConfirmationPanel != null)
        {
            deletionConfirmationPanel.SetActive(true);

            // �T�E���h�Đ�
            PlaySound(null); // �f�t�H���g�̃N���b�N��

            if (debugMode)
                Debug.Log("PortraitDeletionManager: �폜�m�F�_�C�A���O��\�����܂���");
        }
        else
        {
            Debug.LogError("PortraitDeletionManager: �폜�m�F�p�l�����ݒ肳��Ă��܂���");
        }
    }

    /// <summary>
    /// �폜�m�F�_�C�A���O���\��
    /// </summary>
    private void HideDeletionConfirmation()
    {
        if (deletionConfirmationPanel != null)
        {
            deletionConfirmationPanel.SetActive(false);
        }
    }

    /// <summary>
    /// �폜�m�F���̏���
    /// </summary>
    private void OnConfirmDeletion()
    {
        if (isProcessingDeletion) return;

        isProcessingDeletion = true;

        // �m�F�_�C�A���O���\��
        HideDeletionConfirmation();

        // �폜�����Đ�
        PlaySound(deletionSound);

        // �폜�������J�n
        StartCoroutine(ProcessDeletion());

        if (debugMode)
            Debug.Log("PortraitDeletionManager: �폜�������J�n���܂�");
    }

    /// <summary>
    /// �폜�L�����Z�����̏���
    /// </summary>
    private void OnCancelDeletion()
    {
        // �L�����Z�������Đ�
        PlaySound(cancelSound);

        // �m�F�_�C�A���O���\��
        HideDeletionConfirmation();

        if (debugMode)
            Debug.Log("PortraitDeletionManager: �폜���L�����Z�����܂���");
    }

    /// <summary>
    /// �폜�����̃R���[�`��
    /// </summary>
    private IEnumerator ProcessDeletion()
    {
        // GameSaveManager�ō폜�t���O��ݒ�
        if (saveManager != null)
        {
            // ����G�폜�t���O��ݒ�
            PlayerPrefs.SetInt("PortraitDeleted", 1);
            PlayerPrefs.Save();

            // AfterChangeToHisFuture�t���O��ݒ�
            saveManager.SetAfterChangeToHisFutureFlag(true);

            // ���݂̏�Ԃ�ۑ�
            saveManager.SaveGame();

            if (debugMode)
                Debug.Log("PortraitDeletionManager: afterChangeToHisFuture�t���O��ݒ肵�A�Z�[�u�f�[�^���X�V���܂���");
        }

        // �����ҋ@
        yield return new WaitForSeconds(0.2f);

        // �V�[���J�ڑO�̑ҋ@
        yield return new WaitForSeconds(sceneTransitionDelay);

        // �C���ӏ�: �R���[�`���𐳂����Ăяo��
        if (debugMode)
            Debug.Log($"PortraitDeletionManager: {nextSceneName}�֑J�ڂ��J�n���܂�");

        // TransitionToNextScene()���R���[�`���̏ꍇ
        yield return StartCoroutine(TransitionToNextScene());
    }

    /// <summary>
    /// �V�[���J�ڑO�Ƀt���O��ݒ�
    /// </summary>
    private void SetSceneTransitionFlags()
    {
        // GameSaveManager�ɍ폜�t���O��ݒ�
        GameSaveManager saveManager = GameSaveManager.Instance;
        if (saveManager != null)
        {
            saveManager.SetPortraitDeleted(true);
            saveManager.SaveGame();
        }

        // TitleTextChangerForHim�̃t���O��ݒ�
        TitleTextChangerForHim.SetTransitionFlag();

        // RememberButtonTextChangerForHer�̃t���O��ݒ�i�ǉ��j
        RememberButtonTextChangerForHer.SetTransitionFlag();
    }

    /// <summary>
    /// ���̃V�[���֑J��
    /// </summary>
    private IEnumerator TransitionToNextScene()
    {
        // �f�o�b�O���O�ǉ�
        if (debugMode)
            Debug.Log("PortraitDeletionManager: TransitionToNextScene�J�n");

        // �x�����Ԃ�҂�
        yield return new WaitForSeconds(sceneTransitionDelay);

        // �����ɒǉ��F�V�[���J�ڑO�Ƀt���O��ݒ�
        SetSceneTransitionFlags();

        // �f�o�b�O���O�ǉ�
        if (debugMode)
            Debug.Log($"PortraitDeletionManager: SceneManager.LoadScene({nextSceneName})�����s");

        // �V�[���J��
        SceneManager.LoadScene(nextSceneName);
    }

    /// <summary>
    /// �T�E���h���Đ�
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
        else if (SoundEffectManager.Instance != null)
        {
            // �f�t�H���g�̃N���b�N�����Đ�
            SoundEffectManager.Instance.PlayClickSound();
        }
    }

    /// <summary>
    /// �폜���������ǂ������擾
    /// </summary>
    public bool IsProcessingDeletion()
    {
        return isProcessingDeletion;
    }

    /// <summary>
    /// �폜�m�F���b�Z�[�W�𓮓I�ɐݒ�
    /// </summary>
    public void SetConfirmationMessage(string message)
    {
        if (confirmationMessageText != null)
        {
            confirmationMessageText.text = message;
        }
    }

    private void OnDestroy()
    {
        // �{�^�����X�i�[�̃N���[���A�b�v
        if (deleteButton != null)
            deleteButton.onClick.RemoveAllListeners();

        if (confirmButton != null)
            confirmButton.onClick.RemoveAllListeners();

        if (cancelButton != null)
            cancelButton.onClick.RemoveAllListeners();
    }
}