using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// OrganizeMainScene�Ŏg�p����t�@�C���f�[�^�x�[�X
/// MainScene�œo�ꂵ���S�t�@�C���̒�`��ێ�����
/// </summary>
[CreateAssetMenu(fileName = "FileDatabase", menuName = "ExplorerGame/FileDatabase", order = 1)]
public class FileDatabase : ScriptableObject
{
    #region �t�B�[���h

    [Header("�t�@�C�����X�g�ݒ�")]
    [Tooltip("�Ǘ��Ώۂ̑S�t�@�C���f�[�^")]
    [SerializeField] private List<FileData> allFiles = new List<FileData>();

    [Header("�A�C�R���ݒ�")]
    [Tooltip("TXT�t�@�C���p�f�t�H���g�A�C�R��")]
    [SerializeField] private Sprite defaultTxtIcon = null;

    [Tooltip("PNG�t�@�C���p�f�t�H���g�A�C�R��")]
    [SerializeField] private Sprite defaultPngIcon = null;

    [Tooltip("PDF�t�@�C���p�f�t�H���g�A�C�R��")]
    [SerializeField] private Sprite defaultPdfIcon = null;

    #endregion

    #region �v���p�e�B

    /// <summary>
    /// �S�t�@�C�����X�g�i�ǂݎ���p�j
    /// </summary>
    public IReadOnlyList<FileData> AllFiles => allFiles;

    /// <summary>
    /// �t�@�C������
    /// </summary>
    public int FileCount => allFiles?.Count ?? 0;

    #endregion

    #region ���������\�b�h

    /// <summary>
    /// �f�[�^�x�[�X�̏������i�G�f�B�^�p�j
    /// MainScene�̃t�@�C���\���Ɋ�Â��ď����f�[�^��ݒ�
    /// </summary>
    [ContextMenu("Initialize Database")]
    private void InitializeDatabase()
    {
        allFiles.Clear();

        // �v���o�t�H���_�[�̃t�@�C��
        AddFile("���߂Č���������", FileData.FileType.TXT, "�v���o");
        AddFile("�J�t�F�̔ޏ�", FileData.FileType.PNG, "�v���o");
        AddFile("�ޏ��̂���", FileData.FileType.TXT, "�v���o");
        AddFile("���R�̍ĉ�", FileData.FileType.TXT, "�v���o");
        AddFile("�ʋΘH", FileData.FileType.PNG, "�v���o");

        // ���l�t�H���_�[�̃t�@�C��
        AddFile("�ޏ��Ƃ̕���", FileData.FileType.TXT, "���l");
        AddFile("���߂Ă̌��܁H", FileData.FileType.TXT, "���l");
        AddFile("���z���̔ޏ�", FileData.FileType.PNG, "���l");
        AddFile("�v���[���g", FileData.FileType.TXT, "���l");
        AddFile("���ʂȏꏊ", FileData.FileType.PNG, "���l");

        // �F�l�t�H���_�[�̃t�@�C��
        AddFile("�e�F����̒���", FileData.FileType.TXT, "�F�l");
        AddFile("�Ō�̌x��", FileData.FileType.TXT, "�F�l");
        AddFile("����", FileData.FileType.PNG, "�F�l");
        AddFile("�ʂ�b", FileData.FileType.TXT, "�F�l");
        AddFile("�x�@���O", FileData.FileType.PNG, "�F�l");

        // �L�^�t�H���_�[�̃t�@�C��
        AddFile("�x�@�L�^", FileData.FileType.PDF, "�L�^");
        AddFile("�f�f��", FileData.FileType.PDF, "�L�^");
        AddFile("���̕񍐏�", FileData.FileType.PDF, "�L�^");

        // �肢�t�H���_�[�̃t�@�C��
        AddFile("��Q�҂̎�L", FileData.FileType.PDF, "�肢");
        AddFile("���e����̎莆", FileData.FileType.PDF, "�肢");


        Debug.Log($"FileDatabase: {allFiles.Count}�̃t�@�C�������������܂���");
    }

    /// <summary>
    /// �t�@�C����ǉ�����w���p�[���\�b�h
    /// </summary>
    private void AddFile(string fileName, FileData.FileType fileType, string folderName)
    {
        var fileData = new FileData(fileName, fileType, folderName);

        // �A�C�R���̎����ݒ�
        switch (fileType)
        {
            case FileData.FileType.TXT:
                if (defaultTxtIcon != null)
                {
                    // ���t���N�V������SerializedObject���g�p����iconSprite��ݒ�
                    // �iScriptableObject�̂��߁A���ڐݒ�̓G�f�B�^�ł̂݉\�j
                }
                break;
            case FileData.FileType.PNG:
                // defaultPngIcon��ݒ�
                break;
            case FileData.FileType.PDF:
                // defaultPdfIcon��ݒ�
                break;
        }

        allFiles.Add(fileData);
    }

    #endregion

    #region �������\�b�h

    /// <summary>
    /// �t�@�C�����Ńt�@�C���f�[�^������
    /// </summary>
    /// <param name="fileName">��������t�@�C����</param>
    /// <returns>���������t�@�C���f�[�^�A������Ȃ��ꍇ��null</returns>
    public FileData GetFileByName(string fileName)
    {
        return allFiles?.FirstOrDefault(f => f.FileName == fileName);
    }

    /// <summary>
    /// �t�H���_�[���Ńt�@�C�����擾
    /// </summary>
    /// <param name="folderName">�t�H���_�[��</param>
    /// <returns>�Y���t�H���_�[�̃t�@�C�����X�g</returns>
    public List<FileData> GetFilesByFolder(string folderName)
    {
        return allFiles?.Where(f => f.FolderName == folderName).ToList() ?? new List<FileData>();
    }

    /// <summary>
    /// �t�@�C���^�C�v�Ńt�@�C�����擾
    /// </summary>
    /// <param name="fileType">�t�@�C���^�C�v</param>
    /// <returns>�Y���^�C�v�̃t�@�C�����X�g</returns>
    public List<FileData> GetFilesByType(FileData.FileType fileType)
    {
        return allFiles?.Where(f => f.Type == fileType).ToList() ?? new List<FileData>();
    }

    /// <summary>
    /// �\���\�ȃt�@�C���݂̂��擾
    /// </summary>
    /// <returns>�\���\�ȃt�@�C�����X�g</returns>
    public List<FileData> GetVisibleFiles()
    {
        return allFiles?.Where(f => f.IsVisible).OrderBy(f => f.DisplayOrder).ToList() ?? new List<FileData>();
    }

    /// <summary>
    /// �폜�ς݃t�@�C�����擾
    /// </summary>
    /// <returns>�폜�ς݃t�@�C�����X�g</returns>
    public List<FileData> GetDeletedFiles()
    {
        return allFiles?.Where(f => f.IsDeleted && !f.IsPermanentlyDeleted).ToList() ?? new List<FileData>();
    }

    #endregion

    #region ��ԊǗ����\�b�h

    /// <summary>
    /// �S�t�@�C���̍폜��Ԃ����Z�b�g
    /// </summary>
    public void ResetAllFileStates()
    {
        if (allFiles == null) return;

        foreach (var file in allFiles)
        {
            file.Restore();
        }

        Debug.Log("FileDatabase: �S�t�@�C���̍폜��Ԃ����Z�b�g���܂���");
    }

    /// <summary>
    /// �폜��Ԃ𕜌�
    /// </summary>
    /// <param name="deletedFileNames">�폜�ς݃t�@�C�����̃��X�g</param>
    public void RestoreDeletedState(List<string> deletedFileNames)
    {
        if (allFiles == null || deletedFileNames == null) return;

        foreach (var fileName in deletedFileNames)
        {
            var file = GetFileByName(fileName);
            if (file != null)
            {
                file.Delete();
            }
        }
    }

    /// <summary>
    /// �S�t�@�C�����폜����Ă��邩�`�F�b�N
    /// </summary>
    /// <returns>�S�t�@�C���폜�ς݂̏ꍇtrue</returns>
    public bool AreAllFilesDeleted()
    {
        if (allFiles == null || allFiles.Count == 0) return false;

        return allFiles.All(f => f.IsDeleted || f.IsPermanentlyDeleted);
    }

    #endregion

    #region �f�o�b�O���\�b�h

    /// <summary>
    /// �f�[�^�x�[�X�̓��e���f�o�b�O���O�ɏo��
    /// </summary>
    [ContextMenu("Debug Print Database")]
    private void DebugPrintDatabase()
    {
        if (allFiles == null || allFiles.Count == 0)
        {
            Debug.Log("FileDatabase: �t�@�C�����o�^����Ă��܂���");
            return;
        }

        Debug.Log($"FileDatabase: {allFiles.Count}�̃t�@�C��");

        var folders = allFiles.GroupBy(f => f.FolderName);
        foreach (var folder in folders)
        {
            Debug.Log($"  �t�H���_�[: {folder.Key}");
            foreach (var file in folder)
            {
                Debug.Log($"    - {file.GetFileNameWithExtension()} (�폜: {file.IsDeleted})");
            }
        }
    }

    #endregion
}