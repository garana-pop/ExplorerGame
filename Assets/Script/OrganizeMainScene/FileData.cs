using System;
using UnityEngine;

/// <summary>
/// OrganizeMainScene�ŊǗ�����t�@�C���̊�{�f�[�^�\��
/// </summary>
[Serializable]
public class FileData
{
    #region �t�@�C���^�C�v��`

    /// <summary>
    /// �t�@�C���̎�ނ�\���񋓌^
    /// </summary>
    public enum FileType
    {
        TXT,  // �e�L�X�g�t�@�C��
        PNG,  // �摜�t�@�C��
        PDF   // PDF�t�@�C��
    }

    #endregion

    #region �t�B�[���h

    [Header("��{���")]
    [Tooltip("�t�@�C����")]
    [SerializeField] private string fileName = "";

    [Tooltip("�t�@�C���^�C�v")]
    [SerializeField] private FileType fileType = FileType.TXT;

    [Tooltip("�����t�H���_�[��")]
    [SerializeField] private string folderName = "";

    [Header("��ԊǗ�")]
    [Tooltip("�폜�ς݃t���O�i��\����ԁj")]
    [SerializeField] private bool isDeleted = false;

    [Tooltip("���S�폜�ς݃t���O")]
    [SerializeField] private bool isPermanentlyDeleted = false;

    [Header("�\���ݒ�")]
    [Tooltip("�A�C�R���X�v���C�g")]
    [SerializeField] private Sprite iconSprite = null;

    [Tooltip("�\������")]
    [SerializeField] private int displayOrder = 0;

    #endregion

    #region �v���p�e�B

    /// <summary>
    /// �t�@�C����
    /// </summary>
    public string FileName => fileName;

    /// <summary>
    /// �t�@�C���^�C�v
    /// </summary>
    public FileType Type => fileType;

    /// <summary>
    /// �����t�H���_�[��
    /// </summary>
    public string FolderName => folderName;

    /// <summary>
    /// �폜�ς݂��ǂ���
    /// </summary>
    public bool IsDeleted
    {
        get => isDeleted;
        set => isDeleted = value;
    }

    /// <summary>
    /// ���S�폜�ς݂��ǂ���
    /// </summary>
    public bool IsPermanentlyDeleted
    {
        get => isPermanentlyDeleted;
        set => isPermanentlyDeleted = value;
    }

    /// <summary>
    /// �A�C�R���X�v���C�g
    /// </summary>
    public Sprite IconSprite => iconSprite;

    /// <summary>
    /// �\������
    /// </summary>
    public int DisplayOrder => displayOrder;

    /// <summary>
    /// �t�@�C�����\���\���ǂ���
    /// </summary>
    public bool IsVisible => !isDeleted && !isPermanentlyDeleted;

    /// <summary>
    /// �t�@�C���̊��S�Ȏ��ʎq�i�t�H���_�[��/�t�@�C�����j
    /// </summary>
    public string FullPath => string.IsNullOrEmpty(folderName) ? fileName : $"{folderName}/{fileName}";

    #endregion

    #region �R���X�g���N�^

    /// <summary>
    /// �f�t�H���g�R���X�g���N�^
    /// </summary>
    public FileData()
    {
    }

    /// <summary>
    /// ��{�����w�肷��R���X�g���N�^
    /// </summary>
    /// <param name="fileName">�t�@�C����</param>
    /// <param name="fileType">�t�@�C���^�C�v</param>
    /// <param name="folderName">�����t�H���_�[��</param>
    public FileData(string fileName, FileType fileType, string folderName = "")
    {
        this.fileName = fileName;
        this.fileType = fileType;
        this.folderName = folderName;
    }

    #endregion

    #region ���\�b�h

    /// <summary>
    /// �t�@�C�����폜�ς݂ɂ���i��\�����j
    /// </summary>
    public void Delete()
    {
        isDeleted = true;
    }

    /// <summary>
    /// �t�@�C�������S�폜����
    /// </summary>
    public void DeletePermanently()
    {
        isPermanentlyDeleted = true;
    }

    /// <summary>
    /// �폜��Ԃ����Z�b�g����
    /// </summary>
    public void Restore()
    {
        isDeleted = false;
        isPermanentlyDeleted = false;
    }

    /// <summary>
    /// �t�@�C���g���q���擾
    /// </summary>
    /// <returns>�g���q������</returns>
    public string GetExtension()
    {
        switch (fileType)
        {
            case FileType.TXT:
                return ".txt";
            case FileType.PNG:
                return ".png";
            case FileType.PDF:
                return ".pdf";
            default:
                return "";
        }
    }

    /// <summary>
    /// �t�@�C�����i�g���q�t���j���擾
    /// </summary>
    /// <returns>�g���q�t���t�@�C����</returns>
    public string GetFileNameWithExtension()
    {
        return fileName + GetExtension();
    }

    /// <summary>
    /// �f�o�b�O�p�̕�����\��
    /// </summary>
    /// <returns>�f�o�b�O������</returns>
    public override string ToString()
    {
        return $"FileData: {FullPath} ({fileType}), Deleted: {isDeleted}, Permanently: {isPermanentlyDeleted}";
    }

    #endregion
}