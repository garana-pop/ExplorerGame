using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PDF�t�@�C�����Ǝ��ɃA�N�e�B�u�ɂ���I�u�W�F�N�g�̊֘A�t�����Ǘ�����N���X
/// �C���X�y�N�^�[�Őݒ肷�邩�A�����PDF�t�@�C�����Ɋ�Â��Ď����I�Ɋ֘A�t�����s���܂�
/// </summary>
public class PdfDocumentLinkManager : MonoBehaviour
{
    [System.Serializable]
    public class PdfNextObjectMapping
    {
        [Tooltip("PDF�t�@�C�����i��F�x�@�L�^.pdf�j")]
        public string pdfFileName;

        [Tooltip("����PDF�����������Ƃ��ɃA�N�e�B�u�ɂ���I�u�W�F�N�g")]
        public GameObject nextObject;
    }

    [Header("PDF�t�@�C���̃����N�ݒ�")]
    [Tooltip("�ePDF�t�@�C���Ǝ��ɃA�N�e�B�u�ɂ���I�u�W�F�N�g�̃}�b�s���O")]
    [SerializeField] private List<PdfNextObjectMapping> customMappings = new List<PdfNextObjectMapping>();

    [Header("�f�o�b�O�ݒ�")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool useDefaultMappings = true;

    // �V���O���g���C���X�^���X
    private static PdfDocumentLinkManager _instance;
    public static PdfDocumentLinkManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<PdfDocumentLinkManager>(FindObjectsInactive.Include);

                if (_instance == null && Application.isPlaying)
                {
                    GameObject go = new GameObject("PdfDocumentLinkManager");
                    _instance = go.AddComponent<PdfDocumentLinkManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    // �����}�b�s���O�L���b�V��
    private Dictionary<string, GameObject> mappingCache = new Dictionary<string, GameObject>();
    private bool isInitialized = false;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
            return;
        }

        _instance = this;

        if (Application.isPlaying)
        {
            DontDestroyOnLoad(gameObject);
        }


        // �}�b�s���O�̏�����
        InitializeMappings();
    }

    // �}�b�s���O�̏�����
    private void InitializeMappings()
    {
        if (isInitialized) return;

        mappingCache.Clear();

        // �J�X�^���}�b�s���O�̓K�p
        foreach (var mapping in customMappings)
        {
            if (!string.IsNullOrEmpty(mapping.pdfFileName) && mapping.nextObject != null)
            {
                mappingCache[mapping.pdfFileName] = mapping.nextObject;
                if (debugMode)
                    Debug.Log($"�J�X�^���}�b�s���O��ǉ�: {mapping.pdfFileName} -> {mapping.nextObject.name}");
            }
        }

        // �f�t�H���g�}�b�s���O�̓K�p�i�v���Ɋ�Â��j
        if (useDefaultMappings)
        {
            ApplyDefaultMappings();
        }

        isInitialized = true;
    }

    // �v���Ɋ�Â��f�t�H���g�}�b�s���O�̓K�p
    private void ApplyDefaultMappings()
    {
        // ���������͊e�I�u�W�F�N�g�������邾���ŁA���ۂ̃}�b�s���O�̓I�u�W�F�N�g�����������ꍇ�̂ݍs��
        GameObject diagnosisPdf = FindObjectByFileName("�L�^�t�@�C��-�f�f��.pdf");
        GameObject accidentPdf = FindObjectByFileName("�L�^�t�@�C��-���̕񍐏�.pdf");
        GameObject wishFolder = FindObjectByName("FolderButton_5 (�肢)");
        GameObject fatherLetterPdf = FindObjectByFileName("�肢�t�@�C��-���e����̎莆.pdf");
        GameObject daughterWishFile = FindObjectByFileName("�肢�t�@�C��-������̂��肢");

        // ���������I�u�W�F�N�g���}�b�s���O�ɒǉ�
        if (diagnosisPdf != null)
            mappingCache["�x�@�L�^.pdf"] = diagnosisPdf;

        if (accidentPdf != null)
            mappingCache["�f�f��.pdf"] = accidentPdf;

        if (wishFolder != null)
            mappingCache["���̕񍐏�.pdf"] = wishFolder;

        if (fatherLetterPdf != null)
            mappingCache["��Q�҂̎�L.pdf"] = fatherLetterPdf;

        if (daughterWishFile != null)
            mappingCache["���e����̎莆.pdf"] = daughterWishFile;

        if (debugMode)
        {
            foreach (var kvp in mappingCache)
            {
                Debug.Log($"�}�b�s���O: {kvp.Key} -> {kvp.Value.name}");
            }
        }
    }

    // �t�@�C������GameObject�������i�œK���̂���FindFirstObjectByType���g�p�j
    private GameObject FindObjectByFileName(string fileName)
    {
        // �t�@�C�������܂�FileOpen�R���|�[�l���g������
        foreach (var fileOpen in FindObjectsByType<FileOpen>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (fileOpen.gameObject.name == fileName || fileOpen.gameObject.name.Contains(fileName))
                return fileOpen.gameObject;
        }
        return null;
    }

    // �I�u�W�F�N�g����GameObject������
    private GameObject FindObjectByName(string objectName)
    {
        var objects = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var obj in objects)
        {
            if (obj.name == objectName || obj.name.Contains(objectName))
                return obj;
        }
        return null;
    }

    // PDF�t�@�C�����Ɋ�Â��Ď��ɃA�N�e�B�u�ɂ���I�u�W�F�N�g���擾
    public GameObject GetNextObjectForPdf(string pdfFileName)
    {
        if (string.IsNullOrEmpty(pdfFileName)) return null;

        // �܂�����������Ă��Ȃ��ꍇ�͏�����
        if (!isInitialized)
            InitializeMappings();

        // �}�b�s���O�L���b�V�����猟��
        if (mappingCache.TryGetValue(pdfFileName, out GameObject nextObject))
        {
            return nextObject;
        }

        return null;
    }

    // �}�b�s���O�𓮓I�ɒǉ�
    public void AddMapping(string pdfFileName, GameObject nextObject)
    {
        if (string.IsNullOrEmpty(pdfFileName) || nextObject == null) return;

        mappingCache[pdfFileName] = nextObject;

        if (debugMode)
            Debug.Log($"�}�b�s���O�𓮓I�ɒǉ�: {pdfFileName} -> {nextObject.name}");
    }
}