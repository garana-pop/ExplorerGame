using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// �S�~���ł̃t�@�C���폜�Ǘ����s���N���X
/// �t�@�C���h���b�v���̍폜�����Ə�ԊǗ��𐧌䂵�܂�
/// </summary>
public class TrashBoxDeletionManagement : MonoBehaviour, IDropHandler
{
    #region �C���X�y�N�^�[�ݒ�

    [Header("�폜�A�j���[�V�����ݒ�")]
    [Tooltip("�t�@�C���폜���̃t�F�[�h�A�E�g���ԁi�b�j")]
    [SerializeField] private float fileDeleteFadeTime = 0.5f;

    [Tooltip("�t�@�C���폜���̃X�P�[���A�j���[�V����")]
    [SerializeField] private bool useScaleAnimation = true;

    [Tooltip("�폜�A�j���[�V�����I�����̃X�P�[��")]
    [SerializeField] private float deleteAnimationEndScale = 0.3f;

    [Header("�폜�m�F�ݒ�")]
    [Tooltip("�S�t�@�C���폜���̊m�F���b�Z�[�W")]
    [SerializeField] private string allFilesDeleteMessage = "���ׂẴt�@�C�������S�ɍ폜���܂����H";

    [Header("�t�@�C���Ǘ��ݒ�")]
    [Tooltip("�폜���ꂽ�t�@�C���𕜌��\�ɂ��邩")]
    [SerializeField] private bool enableFileRestore = false;

    [Tooltip("�폜�t�@�C���̍ő�ێ���")]
    [SerializeField] private int maxDeletedFilesCount = 50;

    [Header("�f�o�b�O�ݒ�")]
    [Tooltip("�f�o�b�O���O��\�����邩")]
    [SerializeField] private bool debugMode = false;

    #endregion

    #region �v���C�x�[�g�ϐ�

    // �폜�Ǘ�
    private List<string> deletedFileNames; // �폜���ꂽ�t�@�C�����̃��X�g
    private List<GameObject> deletedFileObjects; // �폜���ꂽ�t�@�C���I�u�W�F�N�g�̃��X�g�i�����p�j
    private Dictionary<string, FileDeleteInfo> fileDeleteHistory; // �t�@�C���폜����

    // ���ݕ\�����̃t�@�C����
    private int currentVisibleFileCount = 0;

    // �A�j���[�V�����Ǘ�
    private List<Coroutine> activeDeleteAnimations;

    // ���̃R���|�[�l���g�Q��
    private TrashBoxSoundSetting soundSetting;
    private TrashBoxTips tips;
    private OrganizeMainSceneController sceneController;
    private FileManager fileManager;

    // �萔
    private const float MIN_FADE_TIME = 0.1f;
    private const float MAX_FADE_TIME = 3.0f;
    private const float MIN_SCALE = 0.0f;
    private const float MAX_SCALE = 2.0f;

    #endregion

    #region �����N���X

    /// <summary>
    /// �t�@�C���폜�����i�[����N���X
    /// </summary>
    [System.Serializable]
    private class FileDeleteInfo
    {
        public string fileName; // �t�@�C����
        public System.DateTime deleteTime; // �폜����
        public Vector3 originalPosition; // ���̈ʒu
        public Transform originalParent; // ���̐e
        public bool isImportantFile; // �d�v�t�@�C�����ǂ���

        public FileDeleteInfo(string name, Vector3 position, Transform parent, bool important)
        {
            fileName = name;
            deleteTime = System.DateTime.Now;
            originalPosition = position;
            originalParent = parent;
            isImportantFile = important;
        }
    }

    #endregion

    #region Unity ���C�t�T�C�N��

    /// <summary>
    /// Awake���\�b�h - ����������
    /// </summary>
    private void Awake()
    {
        InitializeLists();
        ValidateSettings();
    }

    /// <summary>
    /// Start���\�b�h - �V�[���J�n��̏���
    /// </summary>
    private void Start()
    {
        InitializeComponents();
        CountVisibleFiles();
    }

    #endregion

    #region ����������

    /// <summary>
    /// ���X�g�̏�����
    /// </summary>
    private void InitializeLists()
    {
        deletedFileNames = new List<string>();
        deletedFileObjects = new List<GameObject>();
        fileDeleteHistory = new Dictionary<string, FileDeleteInfo>();
        activeDeleteAnimations = new List<Coroutine>();
    }

    /// <summary>
    /// �ݒ�l�̌���
    /// </summary>
    private void ValidateSettings()
    {
        fileDeleteFadeTime = Mathf.Clamp(fileDeleteFadeTime, MIN_FADE_TIME, MAX_FADE_TIME);
        deleteAnimationEndScale = Mathf.Clamp(deleteAnimationEndScale, MIN_SCALE, MAX_SCALE);
        maxDeletedFilesCount = Mathf.Max(1, maxDeletedFilesCount);
    }

    /// <summary>
    /// ���̃R���|�[�l���g�̏�����
    /// </summary>
    private void InitializeComponents()
    {
        // �����Q�[���I�u�W�F�N�g��̃R���|�[�l���g���擾
        soundSetting = GetComponent<TrashBoxSoundSetting>();
        tips = GetComponent<TrashBoxTips>();

        // �V�[���R���g���[���[���擾
        sceneController = OrganizeMainSceneController.Instance;
        if (sceneController == null && debugMode)
        {
            Debug.LogWarning($"{nameof(TrashBoxDeletionManagement)}: OrganizeMainSceneController��������܂���");
        }

        // FileManager���擾
        fileManager = FindFirstObjectByType<FileManager>();
        if (fileManager == null && debugMode)
        {
            Debug.LogWarning($"{nameof(TrashBoxDeletionManagement)}: FileManager��������܂���");
        }
    }

    /// <summary>
    /// ���ݕ\�����̃t�@�C�������J�E���g
    /// </summary>
    private void CountVisibleFiles()
    {
        DraggableFile[] allFiles = FindObjectsByType<DraggableFile>(FindObjectsSortMode.None);
        currentVisibleFileCount = 0;

        foreach (DraggableFile file in allFiles)
        {
            if (file.gameObject.activeInHierarchy)
            {
                currentVisibleFileCount++;
            }
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDeletionManagement)}: ���݂̕\���t�@�C���� - {currentVisibleFileCount}");
        }
    }

    #endregion

    #region �h���b�v����

    /// <summary>
    /// �t�@�C�����h���b�v���ꂽ���̏���
    /// </summary>
    /// <param name="eventData">�|�C���^�C�x���g�f�[�^</param>
    public void OnDrop(PointerEventData eventData)
    {
        // �h���b�v���ꂽ�I�u�W�F�N�g���擾
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject == null)
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(TrashBoxDeletionManagement)}: �h���b�v�I�u�W�F�N�g��null�ł�");
            }
            return;
        }

        // DraggableFile�R���|�[�l���g���m�F
        DraggableFile draggableFile = droppedObject.GetComponent<DraggableFile>();
        if (draggableFile == null)
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(TrashBoxDeletionManagement)}: �h���b�v���ꂽ�I�u�W�F�N�g��DraggableFile�ł͂���܂���");
            }
            return;
        }

        // �h���b�v�ʒu���擾���ăt�@�C���폜���������s
        Vector2 dropPosition = Input.mousePosition;

        // DraggableFile�̍폜�������t���O��ݒ�
        draggableFile.SetDeleting(true);

        StartCoroutine(DeleteFileCoroutine(draggableFile, dropPosition));
    }

    #endregion

    #region �t�@�C���폜����

    /// <summary>
    /// �t�@�C���폜�R���[�`��
    /// </summary>
    /// <param name="draggableFile">�폜�Ώۂ̃t�@�C��</param>
    /// <param name="dropPosition">�h���b�v���ꂽ�ʒu</param>
    /// <returns>�R���[�`��</returns>
    private IEnumerator DeleteFileCoroutine(DraggableFile draggableFile, Vector2 dropPosition)
    {
        if (draggableFile == null) yield break;

        string fileName = draggableFile.name;
        bool isImportantFile = IsImportantFile(draggableFile);

        // GridLayoutGroup�̉e����������邽�߂Ƀt�@�C����ʂ̐e�Ɉړ�
        GameObject fileObject = draggableFile.gameObject;
        Transform originalParent = fileObject.transform.parent;

        // DraggingCanvas�܂��͓K�؂Ȑe��T���Ĉړ�
        Canvas draggingCanvas = GameObject.Find("DraggingCanvas")?.GetComponent<Canvas>();
        if (draggingCanvas == null)
        {
            // DraggingCanvas��������Ȃ��ꍇ�͍ŏ�ʂ�Canvas���g�p
            draggingCanvas = fileObject.GetComponentInParent<Canvas>();
        }

        if (draggingCanvas != null)
        {
            // GridLayoutGroup�̉e�����󂯂Ȃ��悤�ɐe��ύX
            fileObject.transform.SetParent(draggingCanvas.transform, true);

            // �h���b�v�ʒu�Ƀt�@�C�����ړ�
            if (draggingCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // �X�N���[���X�y�[�X�I�[�o�[���C�̏ꍇ
                fileObject.transform.position = dropPosition;
            }
            else if (draggingCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                // �X�N���[���X�y�[�X�J�����̏ꍇ
                Vector3 worldPosition;
                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    draggingCanvas.transform as RectTransform,
                    dropPosition,
                    draggingCanvas.worldCamera,
                    out worldPosition);
                fileObject.transform.position = worldPosition;
            }
        }

        // �폜�����ɒǉ��i���̈ʒu����ۑ��j
        FileDeleteInfo deleteInfo = new FileDeleteInfo(
            fileName,
            originalParent.position, // ���̐e�̈ʒu��ۑ�
            originalParent,
            isImportantFile
        );

        AddToDeleteHistory(fileName, deleteInfo);

        // �폜�A�j���[�V�������s�i�h���b�v�ʒu����J�n�j
        yield return StartCoroutine(PlayDeleteAnimation(draggableFile));

        // �t�@�C���I�u�W�F�N�g���\��/�폜����O�ɍ폜�t���O�����Z�b�g
        draggableFile.SetDeleting(false);

        // �t�@�C���I�u�W�F�N�g���\��/�폜
        ProcessFileRemoval(draggableFile);

        // �\���t�@�C�������X�V
        currentVisibleFileCount--;

        // �S�t�@�C���폜�`�F�b�N
        CheckAllFilesDeleted();

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDeletionManagement)}: �t�@�C���폜���� - {fileName}");
        }
    }

    /// <summary>
    /// �t�@�C�����d�v�t�@�C�����ǂ����𔻒�
    /// </summary>
    /// <param name="draggableFile">�`�F�b�N�Ώۂ̃t�@�C��</param>
    /// <returns>�d�v�t�@�C���̏ꍇ��true</returns>
    private bool IsImportantFile(DraggableFile draggableFile)
    {
        // �t�@�C������g���q����d�v�x�𔻒�
        string fileName = draggableFile.name.ToLower();

        string[] importantKeywords = { "�L�^", "�؋�", "�x�@", "�񍐏�" };
        foreach (string keyword in importantKeywords)
        {
            if (fileName.Contains(keyword))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// �폜�����ɒǉ�
    /// </summary>
    /// <param name="fileName">�t�@�C����</param>
    /// <param name="deleteInfo">�폜���</param>
    private void AddToDeleteHistory(string fileName, FileDeleteInfo deleteInfo)
    {
        // �����̗���������΍X�V�A�Ȃ���Βǉ�
        if (fileDeleteHistory.ContainsKey(fileName))
        {
            fileDeleteHistory[fileName] = deleteInfo;
        }
        else
        {
            fileDeleteHistory.Add(fileName, deleteInfo);
        }

        // �폜�t�@�C�������X�g�ɒǉ�
        if (!deletedFileNames.Contains(fileName))
        {
            deletedFileNames.Add(fileName);
        }

        // �ő�ێ����𒴂����ꍇ�͌Â����̂���폜
        if (deletedFileNames.Count > maxDeletedFilesCount)
        {
            string oldestFile = deletedFileNames[0];
            deletedFileNames.RemoveAt(0);
            fileDeleteHistory.Remove(oldestFile);
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDeletionManagement)}: �폜�����ɒǉ� - {fileName}");
        }
    }

    /// <summary>
    /// �w�莞�Ԍ�ɃG�t�F�N�g�I�u�W�F�N�g��j��
    /// </summary>
    /// <param name="effectObject">�G�t�F�N�g�I�u�W�F�N�g</param>
    /// <param name="delay">�ҋ@����</param>
    /// <returns>�R���[�`��</returns>
    private IEnumerator DestroyEffectAfterTime(GameObject effectObject, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (effectObject != null)
        {
            Destroy(effectObject);
        }
    }

    /// <summary>
    /// �폜�A�j���[�V�������Đ�
    /// </summary>
    /// <param name="draggableFile">�폜�Ώۂ̃t�@�C��</param>
    /// <returns>�R���[�`��</returns>
    private IEnumerator PlayDeleteAnimation(DraggableFile draggableFile)
    {
        if (draggableFile == null) yield break;

        GameObject fileObject = draggableFile.gameObject;
        CanvasGroup canvasGroup = fileObject.GetComponent<CanvasGroup>();

        // CanvasGroup�������ꍇ�͒ǉ�
        if (canvasGroup == null)
        {
            canvasGroup = fileObject.AddComponent<CanvasGroup>();
        }

        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;
        Vector3 startScale = fileObject.transform.localScale;
        Vector3 targetScale = startScale * deleteAnimationEndScale;

        while (elapsedTime < fileDeleteFadeTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / fileDeleteFadeTime;

            // �t�F�[�h�A�E�g
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, progress);

            // �X�P�[���A�j���[�V����
            if (useScaleAnimation)
            {
                fileObject.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            }

            yield return null;
        }

        // �ŏI�l��ݒ�
        canvasGroup.alpha = 0f;
        if (useScaleAnimation)
        {
            fileObject.transform.localScale = targetScale;
        }
    }

    /// <summary>
    /// �t�@�C���I�u�W�F�N�g�̍폜����
    /// </summary>
    /// <param name="draggableFile">�폜�Ώۂ̃t�@�C��</param>
    private void ProcessFileRemoval(DraggableFile draggableFile)
    {
        if (draggableFile == null) return;

        GameObject fileObject = draggableFile.gameObject;

        if (enableFileRestore)
        {
            // �����\�ɂ���ꍇ�͔�\���ɂ��邾��
            fileObject.SetActive(false);
            deletedFileObjects.Add(fileObject);
        }
        else
        {
            // �����s�̏ꍇ�͊��S�ɍ폜
            Destroy(fileObject);
        }

        // FileManager�ɍ폜��ʒm
        if (fileManager != null)
        {
            // TODO: fileManager.OnFileDeleted(draggableFile.name);
        }
    }

    /// <summary>
    /// �S�t�@�C���폜�`�F�b�N
    /// </summary>
    private void CheckAllFilesDeleted()
    {
        if (currentVisibleFileCount <= 0)
        {
            if (debugMode)
            {
                Debug.Log($"{nameof(TrashBoxDeletionManagement)}: �S�t�@�C�����폜����܂���");
            }

            // �S�t�@�C���폜�m�F�_�C�A���O�\��
            StartCoroutine(ShowAllFilesDeletedConfirmation());
        }
    }

    /// <summary>
    /// �S�t�@�C���폜�m�F�_�C�A���O�\��
    /// </summary>
    /// <returns>�R���[�`��</returns>
    private IEnumerator ShowAllFilesDeletedConfirmation()
    {
        // ���b�Z�[�W�\��
        if (tips != null)
        {
            tips.ShowMessage(allFilesDeleteMessage);
        }

        // TODO: �m�F�_�C�A���O�̎���
        // bool userConfirmed = yield return ShowConfirmationDialog(allFilesDeleteMessage);
        // if (userConfirmed)
        // {
        //     ExecuteCompleteFileDeletion();
        // }

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDeletionManagement)}: �S�t�@�C���폜�m�F�_�C�A���O�\��");
        }

        yield return null;
    }

    #endregion

    #region �t�@�C�������@�\

    /// <summary>
    /// �폜���ꂽ�t�@�C���𕜌�
    /// </summary>
    /// <param name="fileName">��������t�@�C����</param>
    /// <returns>������������true</returns>
    public bool RestoreFile(string fileName)
    {
        if (!enableFileRestore)
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(TrashBoxDeletionManagement)}: �t�@�C�������@�\�������ł�");
            }
            return false;
        }

        if (!fileDeleteHistory.ContainsKey(fileName))
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(TrashBoxDeletionManagement)}: �����Ώۃt�@�C����������܂��� - {fileName}");
            }
            return false;
        }

        // �폜���ꂽ�t�@�C���I�u�W�F�N�g������
        GameObject fileObject = deletedFileObjects.Find(obj => obj != null && obj.name == fileName);
        if (fileObject == null)
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(TrashBoxDeletionManagement)}: �����ΏۃI�u�W�F�N�g��������܂��� - {fileName}");
            }
            return false;
        }

        try
        {
            // �폜�����擾
            FileDeleteInfo deleteInfo = fileDeleteHistory[fileName];

            // �t�@�C���I�u�W�F�N�g�𕜌�
            fileObject.SetActive(true);
            fileObject.transform.position = deleteInfo.originalPosition;
            fileObject.transform.SetParent(deleteInfo.originalParent);

            // CanvasGroup�̐ݒ�����Z�b�g
            CanvasGroup canvasGroup = fileObject.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            // �X�P�[�������Z�b�g
            fileObject.transform.localScale = Vector3.one;

            // ���X�g����폜
            deletedFileObjects.Remove(fileObject);
            deletedFileNames.Remove(fileName);
            fileDeleteHistory.Remove(fileName);

            // �\���t�@�C�������X�V
            currentVisibleFileCount++;

            if (debugMode)
            {
                Debug.Log($"{nameof(TrashBoxDeletionManagement)}: �t�@�C���������� - {fileName}");
            }

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{nameof(TrashBoxDeletionManagement)}: �t�@�C�������G���[ - {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// �S�Ă̍폜�t�@�C���𕜌�
    /// </summary>
    /// <returns>�������ꂽ�t�@�C����</returns>
    public int RestoreAllFiles()
    {
        if (!enableFileRestore)
        {
            if (debugMode)
            {
                Debug.LogWarning($"{nameof(TrashBoxDeletionManagement)}: �t�@�C�������@�\�������ł�");
            }
            return 0;
        }

        int restoredCount = 0;
        List<string> filesToRestore = new List<string>(deletedFileNames);

        foreach (string fileName in filesToRestore)
        {
            if (RestoreFile(fileName))
            {
                restoredCount++;
            }
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDeletionManagement)}: �S�t�@�C���������� - {restoredCount}����");
        }

        return restoredCount;
    }

    #endregion

    #region �p�u���b�N���\�b�h

    /// <summary>
    /// �폜���ꂽ�t�@�C�����̃��X�g���擾
    /// </summary>
    /// <returns>�폜�t�@�C�������X�g</returns>
    public List<string> GetDeletedFileNames()
    {
        return new List<string>(deletedFileNames);
    }

    /// <summary>
    /// �폜�t�@�C�������擾
    /// </summary>
    /// <returns>�폜���ꂽ�t�@�C����</returns>
    public int GetDeletedFileCount()
    {
        return deletedFileNames.Count;
    }

    /// <summary>
    /// ���ݕ\�����̃t�@�C�������擾
    /// </summary>
    /// <returns>�\�����̃t�@�C����</returns>
    public int GetVisibleFileCount()
    {
        return currentVisibleFileCount;
    }

    /// <summary>
    /// ����̃t�@�C�����폜����Ă��邩�`�F�b�N
    /// </summary>
    /// <param name="fileName">�`�F�b�N����t�@�C����</param>
    /// <returns>�폜����Ă���ꍇ��true</returns>
    public bool IsFileDeleted(string fileName)
    {
        return deletedFileNames.Contains(fileName);
    }

    /// <summary>
    /// �폜�������N���A
    /// </summary>
    public void ClearDeleteHistory()
    {
        deletedFileNames.Clear();
        deletedFileObjects.Clear();
        fileDeleteHistory.Clear();

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDeletionManagement)}: �폜�������N���A���܂���");
        }
    }

    /// <summary>
    /// �폜�A�j���[�V�����ݒ���X�V
    /// </summary>
    /// <param name="newFadeTime">�V�����t�F�[�h����</param>
    /// <param name="newEndScale">�V�����I���X�P�[��</param>
    /// <param name="enableScale">�X�P�[���A�j���[�V������L���ɂ��邩</param>
    public void UpdateDeleteAnimationSettings(float newFadeTime, float newEndScale, bool enableScale)
    {
        fileDeleteFadeTime = Mathf.Clamp(newFadeTime, MIN_FADE_TIME, MAX_FADE_TIME);
        deleteAnimationEndScale = Mathf.Clamp(newEndScale, MIN_SCALE, MAX_SCALE);
        useScaleAnimation = enableScale;
    }

    /// <summary>
    /// �t�@�C�������@�\�̗L��/������ݒ�
    /// </summary>
    /// <param name="enable">�����@�\��L���ɂ��邩</param>
    public void SetFileRestoreEnabled(bool enable)
    {
        enableFileRestore = enable;

        if (!enable)
        {
            // �����@�\�𖳌��ɂ���ꍇ�A�����̍폜�t�@�C�������S�폜
            foreach (GameObject fileObject in deletedFileObjects)
            {
                if (fileObject != null)
                {
                    Destroy(fileObject);
                }
            }
            ClearDeleteHistory();
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDeletionManagement)}: �t�@�C�������@�\��{(enable ? "�L��" : "����")}�ɂ��܂���");
        }
    }

    /// <summary>
    /// �\���t�@�C�������蓮�ōX�V
    /// </summary>
    public void RefreshVisibleFileCount()
    {
        CountVisibleFiles();
    }

    /// <summary>
    /// ���S�ȃt�@�C���폜�����s�i�����s�j
    /// </summary>
    public void ExecuteCompleteFileDeletion()
    {
        // �S�Ă̍폜�t�@�C���I�u�W�F�N�g��j��
        foreach (GameObject fileObject in deletedFileObjects)
        {
            if (fileObject != null)
            {
                Destroy(fileObject);
            }
        }

        // �폜�������N���A
        ClearDeleteHistory();

        // �V�[���R���g���[���[�Ɋ�����ʒm
        if (sceneController != null)
        {
            // TODO: sceneController.OnAllFilesCompletelyDeleted();
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDeletionManagement)}: ���S�t�@�C���폜�����s���܂���");
        }
    }

    #endregion
}