using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// OrganizeMainScene�̑S�̐�����s�����C���R���g���[���[�N���X
/// �t�@�C�������@�\�̃V�[���S�̂̓��샍�W�b�N���Ǘ����܂�
/// </summary>
public class OrganizeMainSceneController : MonoBehaviour
{
    #region �V���O���g������

    // �V���O���g���C���X�^���X
    private static OrganizeMainSceneController instance;

    /// <summary>
    /// OrganizeMainSceneController�̃V���O���g���C���X�^���X
    /// </summary>
    public static OrganizeMainSceneController Instance
    {
        get
        {
            if (instance == null)
            {
                // Unity 6�̐V�@�\���g�p - ��A�N�e�B�u�I�u�W�F�N�g���܂߂Č���
                instance = FindFirstObjectByType<OrganizeMainSceneController>(FindObjectsInactive.Include);

                if (instance == null && Application.isPlaying)
                {
                    Debug.LogWarning("OrganizeMainSceneController: �C���X�^���X��������܂���B�V�K�쐬���܂��B");
                    GameObject go = new GameObject("OrganizeMainSceneController");
                    instance = go.AddComponent<OrganizeMainSceneController>();
                }
            }
            return instance;
        }
    }

    #endregion

    #region �C���X�y�N�^�[�ݒ�

    [Header("UI�Q��")]
    [Tooltip("�t�@�C���\���̈�")]
    [SerializeField] private RectTransform fileScrollView;

    [Tooltip("�t�@�C���ꗗ�̃R���e���c�p�l��")]
    [SerializeField] private RectTransform fileContentPanel;

    [Tooltip("�S�~���I�u�W�F�N�g")]
    [SerializeField] private GameObject trashBinObject;

    [Tooltip("���b�Z�[�W�p�l��")]
    [SerializeField] private GameObject messagePanel;

    [Tooltip("���b�Z�[�W�e�L�X�g")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Tooltip("�m�F�_�C�A���O�p�l��")]
    [SerializeField] private GameObject confirmationPanel;

    [Tooltip("�m�F�_�C�A���O�e�L�X�g")]
    [SerializeField] private TextMeshProUGUI confirmationText;

    [Tooltip("���ʐݒ�p�l��")]
    [SerializeField] private GameObject commonSettingsPanel;

    [Header("�}�l�[�W���[�Q��")]
    [Tooltip("�t�@�C���Ǘ��}�l�[�W���[")]
    [SerializeField] private FileManager fileManager;

    [Tooltip("�Z�[�u�f�[�^�Ǘ��}�l�[�W���[")]
    [SerializeField] private GameSaveManager saveManager;

    [Tooltip("�T�E���h�G�t�F�N�g�Ǘ��}�l�[�W���[")]
    [SerializeField] private SoundEffectManager soundManager;

    [Header("�V�[���ݒ�")]
    [Tooltip("�߂�ۂ̑J�ڐ�V�[����")]
    [SerializeField] private string returnSceneName = "TitleScene";

    [Tooltip("�t�F�[�h���x")]
    [SerializeField] private float fadeSpeed = 1.0f;

    [Header("�f�o�b�O�ݒ�")]
    [Tooltip("�f�o�b�O���O��\�����邩")]
    [SerializeField] private bool debugMode = false;

    #endregion

    #region �v���C�x�[�g�ϐ�

    // �V�[���̏��������
    private bool isInitialized = false;

    // ���ݕ\�����̃t�@�C�����X�g
    private List<GameObject> currentFileItems;

    // �폜�ς݃t�@�C���̃��X�g
    private List<string> deletedFiles;

    // �S�t�@�C���폜�����t���O
    private bool allFilesDeleted = false;

    // �V�[���J�ڒ��t���O
    private bool isTransitioning = false;

    #endregion

    #region Unity���C�t�T�C�N��

    /// <summary>
    /// Awake���\�b�h - �ŏ��Ɏ��s����鏉��������
    /// </summary>
    private void Awake()
    {
        // �V���O���g���p�^�[���̎���
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            // �����̃C���X�^���X������ꍇ�͎��g��j��
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(OrganizeMainSceneController)}: �����̃C���X�^���X�����݂��܂��B���̃I�u�W�F�N�g��j�����܂��B");
            }
            Destroy(gameObject);
            return;
        }

        // ������
        InitializeLists();
    }

    /// <summary>
    /// Start���\�b�h - �V�[���J�n���̏���
    /// </summary>
    private void Start()
    {
        // ����������
        StartCoroutine(InitializeScene());
    }

    /// <summary>
    /// OnDestroy���\�b�h - �I�u�W�F�N�g�j�����̏���
    /// </summary>
    private void OnDestroy()
    {
        // �V���O���g���C���X�^���X�̃N���A
        if (instance == this)
        {
            instance = null;
        }
    }

    #endregion

    #region ����������

    /// <summary>
    /// ���X�g�̏�����
    /// </summary>
    private void InitializeLists()
    {
        currentFileItems = new List<GameObject>();
        deletedFiles = new List<string>();
    }

    /// <summary>
    /// �V�[���S�̂̏���������
    /// </summary>
    /// <returns>�R���[�`��</returns>
    private IEnumerator InitializeScene()
    {

        // �}�l�[�W���[�̎擾
        yield return InitializeManagers();

        // UI�̏�����
        InitializeUI();

        // �Z�[�u�f�[�^�̓ǂݍ���
        LoadSaveData();

        // ����������
        isInitialized = true;

    }

    /// <summary>
    /// �}�l�[�W���[�̏������Ǝ擾
    /// </summary>
    /// <returns>�R���[�`��</returns>
    private IEnumerator InitializeManagers()
    {
        // GameSaveManager�̎擾
        if (saveManager == null)
        {
            saveManager = GameSaveManager.Instance;
            if (saveManager == null && debugMode)
            {
                Debug.LogWarning($"{nameof(OrganizeMainSceneController)}: GameSaveManager��������܂���");
            }
        }

        // SoundEffectManager�̎擾
        if (soundManager == null)
        {
            soundManager = SoundEffectManager.Instance;
            if (soundManager == null && debugMode)
            {
                Debug.LogWarning($"{nameof(OrganizeMainSceneController)}: SoundEffectManager��������܂���");
            }
        }

        // FileManager�̎擾�i�������̏ꍇ�̓X�L�b�v�j
        if (fileManager == null)
        {
            fileManager = GetComponent<FileManager>();
            if (fileManager == null)
            {
                //fileManager = FindFirstObjectByType<FileManager>();
            }
        }

        yield return null;
    }

    /// <summary>
    /// UI�̏�����
    /// </summary>
    private void InitializeUI()
    {
        // ���b�Z�[�W�p�l�����\��
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }

        // �m�F�_�C�A���O���\��
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }

        // ���ʐݒ�p�l�����\��
        if (commonSettingsPanel != null)
        {
            commonSettingsPanel.SetActive(false);
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: UI����������");
        }
    }

    #endregion

    #region �Z�[�u�f�[�^����

    /// <summary>
    /// �Z�[�u�f�[�^�̓ǂݍ���
    /// </summary>
    private void LoadSaveData()
    {
        if (saveManager == null)
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(OrganizeMainSceneController)}: GameSaveManager���ݒ肳��Ă��܂���");
            }
            return;
        }

        // TODO: �Z�[�u�f�[�^����폜�ς݃t�@�C������ǂݍ���
        // ���̏�����GameSaveData�̊g����Ɏ���

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: �Z�[�u�f�[�^�ǂݍ��݊���");
        }
    }

    /// <summary>
    /// �Z�[�u�f�[�^�̕ۑ�
    /// </summary>
    public void SaveData()
    {
        if (saveManager == null)
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(OrganizeMainSceneController)}: GameSaveManager���ݒ肳��Ă��܂���");
            }
            return;
        }

        // TODO: �폜�ς݃t�@�C�������Z�[�u�f�[�^�ɕۑ�
        // ���̏�����GameSaveData�̊g����Ɏ���

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: �Z�[�u�f�[�^�ۑ�����");
        }
    }

    #endregion

    #region �t�@�C���Ǘ�

    /// <summary>
    /// �t�@�C���̍폜�i��\�����j����
    /// </summary>
    /// <param name="fileName">�폜����t�@�C����</param>
    public void DeleteFile(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return;
        }

        // �폜�ς݃��X�g�ɒǉ�
        if (!deletedFiles.Contains(fileName))
        {
            deletedFiles.Add(fileName);
        }

        // TODO: �Ή�����t�@�C���A�C�e�����\���ɂ���

        // �S�t�@�C���폜�`�F�b�N
        CheckAllFilesDeleted();

        // �Z�[�u�f�[�^���X�V
        SaveData();

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: �t�@�C�� '{fileName}' ���폜���܂���");
        }
    }

    /// <summary>
    /// �S�t�@�C���폜�m�F
    /// </summary>
    private void CheckAllFilesDeleted()
    {
        // TODO: �S�t�@�C�����폜���ꂽ���`�F�b�N
        // ���̏����̓t�@�C���ꗗ�@�\������ɏڍ׎���

        if (allFilesDeleted && !isTransitioning)
        {
            ShowAllFilesDeleteConfirmation();
        }
    }

    #endregion

    #region ���b�Z�[�W�\��

    /// <summary>
    /// ���b�Z�[�W�p�l���̕\��
    /// </summary>
    /// <param name="message">�\�����郁�b�Z�[�W</param>
    /// <param name="duration">�\�����ԁi�b�j</param>
    public void ShowMessage(string message, float duration = 3.0f)
    {
        if (messagePanel == null || messageText == null)
        {
            return;
        }

        messageText.text = message;
        messagePanel.SetActive(true);

        // ������\���^�C�}�[
        StartCoroutine(HideMessageAfterDelay(duration));

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: ���b�Z�[�W�\��: {message}");
        }
    }

    /// <summary>
    /// ���b�Z�[�W���w�莞�Ԍ�ɔ�\���ɂ���
    /// </summary>
    /// <param name="delay">�x�����ԁi�b�j</param>
    /// <returns>�R���[�`��</returns>
    private IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }
    }

    /// <summary>
    /// �S�t�@�C���폜�m�F�_�C�A���O�̕\��
    /// </summary>
    private void ShowAllFilesDeleteConfirmation()
    {
        if (confirmationPanel == null || confirmationText == null)
        {
            return;
        }

        confirmationText.text = "���ׂẴt�@�C�������S�ɍ폜���܂����H";
        confirmationPanel.SetActive(true);

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: �S�t�@�C���폜�m�F�_�C�A���O��\��");
        }
    }

    #endregion

    #region �{�^���C�x���g

    /// <summary>
    /// �m�F�_�C�A���O�́u�͂��v�{�^���������̏���
    /// </summary>
    public void OnConfirmYesClicked()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }

        // �S�t�@�C�����S�폜����
        CompleteAllFilesDelete();

        // �{�^���N���b�N��
        if (soundManager != null)
        {
            soundManager.PlayClickSound();
        }
    }

    /// <summary>
    /// �m�F�_�C�A���O�́u�������v�{�^���������̏���
    /// </summary>
    public void OnConfirmNoClicked()
    {
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false);
        }

        // �{�^���N���b�N��
        if (soundManager != null)
        {
            soundManager.PlayClickSound();
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: �S�t�@�C���폜���L�����Z�����܂���");
        }
    }

    /// <summary>
    /// �߂�{�^���������̏���
    /// </summary>
    public void OnBackButtonClicked()
    {
        if (isTransitioning)
        {
            return;
        }

        // �{�^���N���b�N��
        if (soundManager != null)
        {
            soundManager.PlayClickSound();
        }

        // TitleScene�֑J��
        StartCoroutine(TransitionToScene(returnSceneName));
    }

    /// <summary>
    /// �ۑ����ďI���{�^���������̏���
    /// </summary>
    public void OnSaveAndQuitClicked()
    {
        // �f�[�^��ۑ�
        SaveData();

        // �{�^���N���b�N��
        if (soundManager != null)
        {
            soundManager.PlayClickSound();
        }

        // �A�v���P�[�V�����I��
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    #endregion

    #region ���S�폜����

    /// <summary>
    /// �S�t�@�C�����S�폜����
    /// </summary>
    private void CompleteAllFilesDelete()
    {
        // �Z�[�u�f�[�^�Ɋ��S�폜�t���O���L�^
        allFilesDeleted = true;
        SaveData();

        // BGM�ύX�����iSoundEffectManager�ɐVBGM�؂�ւ����\�b�h���������ꂽ��ɑΉ��j
        // TODO: soundManager.ChangeToBGMForComplete();

        // Steam���щ����iSteam API������ɑΉ��j
        // TODO: UnlockSteamAchievement("�O��");

        if (debugMode)
        {
            Debug.Log($"{nameof(OrganizeMainSceneController)}: �S�t�@�C�����S�폜����");
        }
    }

    #endregion

    #region �V�[���J��

    /// <summary>
    /// �V�[���J�ڏ���
    /// </summary>
    /// <param name="sceneName">�J�ڐ�V�[����</param>
    /// <returns>�R���[�`��</returns>
    private IEnumerator TransitionToScene(string sceneName)
    {
        isTransitioning = true;

        // �t�F�[�h�A�E�g�����i����������ꍇ�j
        // TODO: �t�F�[�h�����̎���

        yield return new WaitForSeconds(fadeSpeed);

        // �V�[���J��
        SceneManager.LoadScene(sceneName);
    }

    #endregion

    #region �p�u���b�N���\�b�h

    /// <summary>
    /// ������������Ԃ��擾
    /// </summary>
    /// <returns>���������������Ă���ꍇ��true</returns>
    public bool IsInitialized()
    {
        return isInitialized;
    }

    /// <summary>
    /// �폜�ς݃t�@�C�����X�g���擾
    /// </summary>
    /// <returns>�폜�ς݃t�@�C�����̃��X�g</returns>
    public List<string> GetDeletedFiles()
    {
        return new List<string>(deletedFiles);
    }

    /// <summary>
    /// �S�~���N���b�N���̏���
    /// </summary>
    public void OnTrashBinClicked()
    {
        ShowMessage("�폜�������t�@�C�����h���b�O&�h���b�v���Ă��������B", 3.0f);

        // �N���b�N��
        if (soundManager != null)
        {
            soundManager.PlayClickSound();
        }
    }

    #endregion
}