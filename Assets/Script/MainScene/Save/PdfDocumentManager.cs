using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PDF�h�L�������g���̉B���L�[���[�h���Ǘ�����N���X
/// </summary>
public class PdfDocumentManager : MonoBehaviour
{
    [Header("�L�[���[�h�ݒ�")]
    [Tooltip("����PDF�Ɋ܂܂��B���L�[���[�h")]
    [SerializeField] private List<HiddenKeyword> hiddenKeywords = new List<HiddenKeyword>();

    [Header("���̃t�H���_�[/�t�@�C��")]
    [Tooltip("���ׂẴL�[���[�h���\�����ꂽ�Ƃ��ɃA�N�e�B�u�ɂ���I�u�W�F�N�g")]
    [SerializeField] private GameObject nextFolderOrFile;

    [Header("�ۑ��ݒ�")]
    [Tooltip("����PDF�t�@�C���̎��ʖ��i�K�{�j")]
    [SerializeField] private string fileName = "document.pdf";

    [Header("���ʉ�")]
    [Tooltip("���ׂĕ\�����ꂽ�Ƃ��̌��ʉ�")]
    [SerializeField] private AudioClip completionSound;

    [Header("UI�R���g���[��")]
    [Tooltip("����{�^��")]
    [SerializeField] private Button closeButton;

    [Header("�A�C�R���A�g")]
    [Tooltip("������ԕύX���ɍX�V����PdfFileIconChange�R���|�[�l���g���X�g")]
    [SerializeField] private List<PdfFileIconChange> linkedIconChangers = new List<PdfFileIconChange>();

    [Tooltip("�ݒ肳��Ă��Ȃ��ꍇ�ɓ��I�������s�����ǂ���")]
    [SerializeField] private bool searchForAdditionalIcons = false;

    [Header("�Z�[�u�ݒ�")]
    [Tooltip("������Ԑݒ莞�Ɏ����Z�[�u���邩")]
    [SerializeField] private bool autoSaveOnCompletion = true;

    // �������
    private bool isDocumentCompleted = false;
    private int revealedKeywordsCount = 0;

    private Dictionary<string, bool> completionStateCache = new Dictionary<string, bool>();
    private static Dictionary<string, bool> globalCompletionStateCache = new Dictionary<string, bool>();

    private void Awake()
    {
        // HiddenKeyword���ݒ肳��Ă��Ȃ��ꍇ�͎����擾
        if (hiddenKeywords == null || hiddenKeywords.Count == 0)
        {
            hiddenKeywords = new List<HiddenKeyword>(GetComponentsInChildren<HiddenKeyword>(true));
        }
    }

    private void Start()
    {
        // �O���[�o���L���b�V������̏�ԕ������ŏ��Ɏ��݂�
        if (globalCompletionStateCache.TryGetValue(fileName, out bool cachedState) && cachedState)
        {
            isDocumentCompleted = true;
        }

        // �Z�[�u�f�[�^�������Ԃ𕜌�
        RestoreStateFromSave();

        // ��Ԃ��O���[�o���L���b�V���ɕۑ�
        UpdateGlobalCompletionState();
    }

    // �V���\�b�h - �O���[�o����Ԃ̍X�V
    private void UpdateGlobalCompletionState()
    {
        // ������Ԃ��O���[�o���L���b�V���ɕۑ�
        if (isDocumentCompleted)
        {
            globalCompletionStateCache[fileName] = true;
            //Debug.Log($"PDF '{fileName}': ������Ԃ��O���[�o���L���b�V���ɕۑ����܂���");
        }
    }

    private void OnEnable()
    {
        // �\�����ꂽ�Ƃ��ɏ�Ԃ��Ċm�F
        if (isDocumentCompleted)
        {
            ForceRevealAllKeywords();
            EnsureNextFolderActive();
        }
    }

    /// <summary>
    /// �L�[���[�h���\�����ꂽ�Ƃ��̏���
    /// </summary>
    public void OnKeywordRevealed(HiddenKeyword keyword)
    {
        // ���Ɋ������Ă���ꍇ�͉������Ȃ�
        if (isDocumentCompleted) return;

        // �\���ς݃L�[���[�h�����X�V
        UpdateRevealedKeywordCount();

        // ���ׂẴL�[���[�h���\�����ꂽ���`�F�b�N
        CheckCompletion();
    }

    /// <summary>
    /// �\���ς݃L�[���[�h�����X�V
    /// </summary>
    private void UpdateRevealedKeywordCount()
    {
        revealedKeywordsCount = 0;
        foreach (var keyword in hiddenKeywords)
        {
            if (keyword != null && keyword.IsRevealed())
            {
                revealedKeywordsCount++;
            }
        }
    }

    /// <summary>
    /// ���ׂẴL�[���[�h���\�����ꂽ���`�F�b�N���A�����������s��
    /// </summary>
    private void CheckCompletion()
    {
        // ���ׂĂ��\������Ă��邩�m�F
        bool allRevealed = (revealedKeywordsCount >= hiddenKeywords.Count) && (hiddenKeywords.Count > 0);

        if (allRevealed && !isDocumentCompleted)
        {
            CompleteDocument();
        }
    }

    /// <summary>
    /// �h�L�������g�������̏���
    /// </summary>
    private void CompleteDocument()
    {
        isDocumentCompleted = true;

        // �������ʉ����Đ�
        PlayCompletionSound();

        // ���̃t�H���_�[/�t�@�C�����A�N�e�B�u��
        EnsureNextFolderActive();

        // �C���X�y�N�^�[�Őݒ肳�ꂽ�A�C�R�����X�V
        UpdateLinkedIcons();

        // �Q�[����Ԃ�ۑ�
        SaveGameState();

        Debug.Log($"PDF '{fileName}' �̂��ׂẴL�[���[�h���\������܂����B������Ԃ�ۑ����܂��B");
    }

    /// <summary>
    /// �ݒ肳�ꂽPdfFileIconChange�R���|�[�l���g���X�V
    /// </summary>
    private void UpdateLinkedIcons()
    {
        // �C���X�y�N�^�[�Őݒ肳�ꂽ�A�C�R�����X�V
        foreach (var iconChanger in linkedIconChangers)
        {
            if (iconChanger != null)
            {
                iconChanger.CheckCompletionState();
            }
        }
    }

    /// <summary>
    /// �������ʉ��̍Đ�
    /// </summary>
    private void PlayCompletionSound()
    {
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlayCompletionSound();
        }
        else if (completionSound != null)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.PlayOneShot(completionSound);
        }
    }

    /// <summary>
    /// ���̃t�H���_�[/�t�@�C�����A�N�e�B�u�ɂ���
    /// </summary>
    public void EnsureNextFolderActive()
    {
        // PdfDocumentLinkManager����}�b�s���O��D��I�Ɏ擾
        GameObject targetObject = PdfDocumentLinkManager.Instance.GetNextObjectForPdf(fileName);

        // �}�b�s���O���Ȃ��ꍇ�̂݃C���X�y�N�^�[�Őݒ肳�ꂽnextFolderOrFile���g�p
        if (targetObject == null)
        {
            targetObject = nextFolderOrFile;
            if (targetObject == null)
            {
                Debug.Log($"PDF '{fileName}': ���̃t�H���_�[/�t�@�C�����ݒ肳��Ă��܂���");
                return;
            }
        }

        // �ΏۃI�u�W�F�N�g���A�N�e�B�u��
        targetObject.SetActive(true);

        // FolderButtonScript�̐ݒ�
        FolderButtonScript folderScript = targetObject.GetComponent<FolderButtonScript>();
        if (folderScript == null)
        {
            folderScript = targetObject.GetComponentInParent<FolderButtonScript>();
        }

        if (folderScript != null)
        {
            folderScript.SetActivatedState(true);
            folderScript.SetVisible(true);

            // �t�@�C���p�l�����A�N�e�B�u��
            if (folderScript.filePanel != null)
            {
                folderScript.filePanel.SetActive(true);
            }
        }

        // FolderActivationGuard�̐ݒ�
        FolderActivationGuard guard = targetObject.GetComponent<FolderActivationGuard>();
        if (guard != null)
        {
            guard.SetActivated(true);
        }

    }

    /// <summary>
    /// ���ׂẴL�[���[�h�������I�ɕ\����Ԃɂ���
    /// </summary>
    private void ForceRevealAllKeywords()
    {
        if (hiddenKeywords == null || hiddenKeywords.Count == 0)
        {
            // �����I��HiddenKeywords��T��
            hiddenKeywords = new List<HiddenKeyword>(GetComponentsInChildren<HiddenKeyword>(true));
            Debug.Log($"PDF '{fileName}': {hiddenKeywords.Count}��HiddenKeyword���������o���܂���");
        }

        // �L�[���[�h��������Ȃ��ꍇ�͂���ɍL������
        if (hiddenKeywords.Count == 0)
        {
            // LineText�I�u�W�F�N�g�����ׂĒT��
            Transform[] textObjects = GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.Contains("Line") && t.name.Contains("Text"))
                .ToArray();

            foreach (Transform textObject in textObjects)
            {
                // �q�I�u�W�F�N�g�Ƃ���HiddenKeyword�����邩�m�F
                HiddenKeyword[] keywords = textObject.GetComponentsInChildren<HiddenKeyword>(true);
                if (keywords.Length > 0)
                {
                    hiddenKeywords.AddRange(keywords);
                }
            }

            Debug.Log($"PDF '{fileName}': LineText����{hiddenKeywords.Count}��HiddenKeyword�����o���܂���");
        }

        foreach (var keyword in hiddenKeywords)
        {
            if (keyword != null)
            {
                // �\����Ԃ������I�ɐݒ�i���łɕ\������Ă��Ă��ēx�K�p�j
                keyword.ForceReveal();
                // Debug.Log($"PDF '{fileName}': �L�[���[�h '{keyword.GetHiddenWord()}' �������\�����܂���");
            }
        }

        // �B���L�[���[�h�̃J�E���g���X�V
        UpdateRevealedKeywordCount();
    }

    /// <summary>
    /// �Z�[�u�f�[�^�����Ԃ𕜌�
    /// </summary>
    private void RestoreStateFromSave()
    {
        if (GameSaveManager.Instance == null) return;

        Dictionary<string, PdfFileData> pdfData = GameSaveManager.Instance.GetAllPdfProgress();
        if (pdfData != null && pdfData.TryGetValue(fileName, out PdfFileData fileData))
        {
            // ������Ԃ𕜌��i�C���F��x����������P�v�I�Ɋ�����ԂƂ���j
            isDocumentCompleted = fileData.isCompleted;

            // �L�[���[�h��\����Ԃ�
            if (isDocumentCompleted)
            {
                // ���ׂĕ\������O�Ɋm����HiddenKeyword�����W
                if (hiddenKeywords == null || hiddenKeywords.Count == 0)
                {
                    hiddenKeywords = new List<HiddenKeyword>(GetComponentsInChildren<HiddenKeyword>(true));
                }

                // ���ׂẴL�[���[�h�������\��
                ForceRevealAllKeywords();

                // ���̃t�H���_�[���m���ɃA�N�e�B�u��
                EnsureNextFolderActive();

                //Debug.Log($"PDF '{fileName}': ������Ԃ��畜�����A���ׂẴL�[���[�h��\�����܂���");
            }
            // �ʂ̃L�[���[�h�\����Ԃ𕜌�
            else if (fileData.revealedKeywords != null && fileData.revealedKeywords.Length > 0)
            {
                HashSet<string> revealedWordsSet = new HashSet<string>(fileData.revealedKeywords);

                // �L�[���[�h���X�g���X�V
                if (hiddenKeywords == null || hiddenKeywords.Count == 0)
                {
                    hiddenKeywords = new List<HiddenKeyword>(GetComponentsInChildren<HiddenKeyword>(true));
                }

                foreach (var keyword in hiddenKeywords)
                {
                    if (keyword != null && revealedWordsSet.Contains(keyword.GetHiddenWord()))
                    {
                        keyword.ForceReveal();
                        Debug.Log($"PDF '{fileName}': �L�[���[�h '{keyword.GetHiddenWord()}' �̏�Ԃ𕜌����܂���");
                    }
                }

                // �\���ς݃L�[���[�h�����X�V
                UpdateRevealedKeywordCount();

                // �ă`�F�b�N�i���[�h��Ɋ��������𖞂����ꍇ������j
                CheckCompletion();
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
        }
    }

    /// <summary>
    /// ����PDF�̐i����Ԃ��擾
    /// </summary>
    public PdfFileData GetPdfProgress()
    {
        // �\�����ꂽ�L�[���[�h�����W
        List<string> revealed = new List<string>();

        foreach (var keyword in hiddenKeywords)
        {
            if (keyword != null && keyword.IsRevealed())
            {
                revealed.Add(keyword.GetHiddenWord());
            }
        }

        return new PdfFileData
        {
            fileName = fileName,
            revealedKeywords = revealed.ToArray(),
            totalKeywords = hiddenKeywords.Count,
            isCompleted = isDocumentCompleted
        };
    }

    /// <summary>
    /// PDF�t�@�C�������擾
    /// </summary>
    public string GetPdfFileName()
    {
        return fileName;
    }

    /// <summary>
    /// PDF������Ԃ��擾
    /// </summary>
    public bool IsDocumentCompleted()
    {
        return isDocumentCompleted;
    }

    /// <summary>
    /// ������Ԃ������ݒ�i�f�o�b�O�⃍�[�h�p�j
    /// </summary>
    /// <summary>
    /// ������Ԃ������ݒ�i�f�o�b�O�⃍�[�h�p�j
    /// </summary>
    public void SetCompletionState(bool completed, bool? autoSave = null)
    {
        // autoSave�̒l������i�����w�� > �C���X�y�N�^�[�ݒ� > �f�t�H���gtrue�j
        bool shouldAutoSave = autoSave ?? autoSaveOnCompletion;

        // ���Ɋ�����Ԃ̏ꍇ�͕ύX�s�v
        if (isDocumentCompleted && !completed)
        {
            Debug.Log($"PDF '{fileName}': ���Ɋ������Ă��邽�߁A�񊮗���Ԃɂ͐ݒ肵�܂���");
            return;
        }

        // false��true�̕ύX�̂݋���
        if (completed && !isDocumentCompleted)
        {
            isDocumentCompleted = true;
            globalCompletionStateCache[fileName] = true;

            ForceRevealAllKeywords();
            EnsureNextFolderActive();
            UpdateLinkedIcons();

            // �����Z�[�u���L���ȏꍇ�̂݃Z�[�u
            if (shouldAutoSave)
            {
                SaveGameState();
            }
        }
    }
    //public void SetCompletionState(bool completed)
    //{
    //    // ���Ɋ�����Ԃ̏ꍇ�͕ύX�s��
    //    if (isDocumentCompleted && !completed)
    //    {
    //        Debug.Log($"PDF '{fileName}': ���Ɋ������Ă��邽�߁A�񊮗���Ԃɂ͐ݒ肵�܂���");
    //        return;
    //    }

    //    // false����true�ւ̕ύX�̂݋���
    //    if (completed && !isDocumentCompleted)
    //    {
    //        isDocumentCompleted = true;

    //        // �O���[�o���L���b�V���ɂ��ۑ�
    //        globalCompletionStateCache[fileName] = true;

    //        ForceRevealAllKeywords();
    //        EnsureNextFolderActive();

    //        // �A�C�R���X�V��ǉ�
    //        UpdateLinkedIcons();

    //        SaveGameState();
    //    }
    //    else if (completed && isDocumentCompleted)
    //    {
    //        // ���Ɋ�����Ԃ̏ꍇ�͉������Ȃ��i���������O�͏o���j
    //        //Debug.Log($"PDF '{fileName}': ���łɊ�����Ԃł�");
    //    }
    //}

}