using System;
using System.Collections.Generic;

/// <summary>
/// �Q�[���̃Z�[�u�f�[�^�\���N���X�Q
/// </summary>
[Serializable]
public class GameSaveData
{
    /// <summary>�Q�[���̃o�[�W����</summary>
    public string gameVersion;

    /// <summary>�Z�[�u���������iISO 8601�`���j</summary>
    public string saveTimestamp;

    /// <summary>�t�H���_�̏��</summary>
    public FolderState folderState;

    /// <summary>�t�@�C���i���f�[�^</summary>
    public FileProgressData fileProgress;

    /// <summary>�I�[�f�B�I�ݒ�</summary>
    public AudioSettings audioSettings;

    /// <summary>OpeningScene��MainScene�Ɉڍs��������t���O</summary>
    public bool endOpeningScene = false;

    /// <summary>�^�C�g����"�u�ޏ��v�̋L��"�ɕύX�t���O</summary>
    public bool afterChangeToHerMemory = false;

    /// <summary>����G�폜��̃t���O</summary>
    public bool afterChangeToHisFuture = false;  //

    /// <summary>�ё��悪�폜���ꂽ���ǂ����̃t���O</summary>
    public bool portraitDeleted = false;

    /// <summary>MonologueScene������̃t���O</summary>
    public bool afterChangeToLast = false;

    /// <summary>MonologueScene����J�ڂ������̃t���O</summary>
    public bool fromMonologueScene = false;

    /// <summary>
    /// �f�t�H���g�l�ŏ���������
    /// </summary>
    public GameSaveData()
    {
        gameVersion = "1.0";
        saveTimestamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        folderState = new FolderState();
        fileProgress = new FileProgressData();
        audioSettings = new AudioSettings();
        afterChangeToHerMemory = false;
        afterChangeToHisFuture = false;
        portraitDeleted = false;
        afterChangeToLast = false;
        fromMonologueScene = false;
    }
}

[Serializable]
public class FolderState
{
    /// <summary>���݃A�N�e�B�u�ȃt�H���_�[</summary>
    public string activeFolder = "";

    /// <summary>�\������Ă���t�H���_�[�ꗗ</summary>
    public string[] displayedFolders = Array.Empty<string>();

    /// <summary>��x�A�N�e�B�u�ɂȂ����t�H���_�[�ꗗ</summary>
    public string[] activatedFolders = Array.Empty<string>();
}

[Serializable]
public class AudioSettings
{
    /// <summary>�}�X�^�[���ʁi0-1�͈̔́j</summary>
    public float masterVolume = 0.8f;

    /// <summary>BGM���ʁi0-1�͈̔́j</summary>
    public float bgmVolume = 0.5f;

    /// <summary>���ʉ����ʁi0-1�͈̔́j</summary>
    public float seVolume = 0.5f;
}

[Serializable]
public class FileProgressData
{
    /// <summary>TXT�t�@�C���̐i���i�t�@�C���� -> �i���f�[�^�j</summary>
    public Dictionary<string, TxtFileData> txt = new Dictionary<string, TxtFileData>();

    /// <summary>PNG�t�@�C���̐i���i�t�@�C���� -> �i���f�[�^�j</summary>
    public Dictionary<string, PngFileData> png = new Dictionary<string, PngFileData>();

    /// <summary>PDF�t�@�C���̐i���i�t�@�C���� -> �i���f�[�^�j</summary>
    public Dictionary<string, PdfFileData> pdf = new Dictionary<string, PdfFileData>();

    //public static implicit operator FileProgressData(FileProgressData v)
    //{
    //    throw new NotImplementedException();
    //}
}

[Serializable]
public class TxtFileData
{
    /// <summary>TXT�t�@�C����</summary>
    public string fileName = "";

    /// <summary>�p�Y�����������Ă��邩�ǂ���</summary>
    public bool isCompleted = false;

    /// <summary>�������}�b�`�̐�</summary>
    public int solvedMatches = 0;

    /// <summary>���v�}�b�`��</summary>
    public int totalMatches = 0;
}

[Serializable]
public class PngFileData
{
    /// <summary>PNG�t�@�C����</summary>
    public string fileName = "";

    /// <summary>���݂̃��U�C�N���x��</summary>
    public int currentLevel = 0;

    /// <summary>�ő僂�U�C�N���x��</summary>
    public int maxLevel = 0;

    /// <summary>�摜�����S�ɕ\������Ă��邩�ǂ���</summary>
    public bool isRevealed = false;
}

[Serializable]
public class PdfFileData
{
    /// <summary>PDF�t�@�C����</summary>
    public string fileName = "";

    /// <summary>�������ꂽ�L�[���[�h�ꗗ</summary>
    public string[] revealedKeywords = Array.Empty<string>();

    /// <summary>���v�L�[���[�h��</summary>
    public int totalKeywords = 0;

    /// <summary>���ׂẴL�[���[�h�������������ǂ���</summary>
    public bool isCompleted = false;
}