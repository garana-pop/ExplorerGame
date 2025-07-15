using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// �p�Y���������icorrectCount == totalCount�j�Ƀt�@�C���A�C�R����ύX����R���|�[�l���g
/// </summary>
public class FileIconChange : MonoBehaviour
{
    [Header("�A�C�R���ݒ�")]
    [Tooltip("�ύX�O�̃A�C�R���X�v���C�g")]
    [SerializeField] private Sprite defaultSprite;

    [Tooltip("�ύX��̃A�C�R���X�v���C�g")]
    [SerializeField] private Sprite completedSprite;

    [Tooltip("�ύX�Ώۂ�Image�R���|�[�l���g�i���ݒ�̏ꍇ�͎��g��Image���g�p�j")]
    [SerializeField] private Image iconImage;

    [Header("�p�Y���Q��")]
    [Tooltip("�J�X�^���F�C���X�y�N�^�[�Œ��ڐݒ肷��h���b�v�G���A")]
    [SerializeField] private List<SpeakerDropArea> dropAreas = new List<SpeakerDropArea>();

    [Tooltip("�]���݊��F�p�Y�����܂Ƃ߂Ċ܂܂��p�l���idropAreas����̏ꍇ�̂ݎg�p�j")]
    [SerializeField] private GameObject puzzlePanel;

    [Header("�f�o�b�O�ݒ�")]
    [Tooltip("�f�o�b�O���O��\�����邩�ǂ���")]
    [SerializeField] private bool debugMode = false;

    private void OnEnable()
    {
        // �I�u�W�F�N�g���L���ɂȂ邽�тɃp�Y���̏�Ԃ��`�F�b�N
        CheckPuzzleState();

        if (debugMode)
        {
            Debug.Log($"FileIconChange: OnEnable�Ńp�Y����Ԃ��`�F�b�N���܂��� - {gameObject.name}");
        }
    }

    private void Start()
    {
        // iconImage���ݒ肳��Ă��Ȃ���΁A���g��Image�R���|�[�l���g���擾
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }

        // �f�t�H���g�X�v���C�g��K�p
        if (iconImage != null && defaultSprite != null)
        {
            iconImage.sprite = defaultSprite;
        }

        // �p�Y���̏�Ԃ��`�F�b�N�i�����R�[�h�j
        CheckPuzzleState();
    }

    /// <summary>
    /// �p�Y���̏�Ԃ��`�F�b�N���A�A�C�R�����X�V
    /// </summary>
    private void CheckPuzzleState()
    {
        // �C���X�y�N�^�[�Œ��ڐݒ肳�ꂽ�h���b�v�G���A������ꍇ�͂�����g�p
        if (dropAreas != null && dropAreas.Count > 0)
        {
            CheckCustomDropAreas();
            return;
        }

        // �]���݊��F�p�Y���p�l�����玩������
        if (puzzlePanel == null) return;

        // �p�l������SpeakerDropArea��S�Ď擾
        SpeakerDropArea[] panelDropAreas = puzzlePanel.GetComponentsInChildren<SpeakerDropArea>(true);
        if (panelDropAreas.Length == 0) return;

        // ���𐔂Ƒ������J�E���g
        int correctCount = 0;
        int totalCount = panelDropAreas.Length;

        foreach (var area in panelDropAreas)
        {
            if (area != null && area.IsCorrect())
            {
                correctCount++;
            }
        }

        Debug.Log($"����(correctCount)={correctCount}, ����(totalCount)={totalCount}");

        // �S�Đ����Ȃ�A�C�R����ύX
        if (correctCount == totalCount)
        {
            ApplyCompletedSprite();
            if (debugMode)
            {
                Debug.Log($"�p�Y�����������o���܂���: correctCount={correctCount}, totalCount={totalCount}");
            }
        }
    }

    /// <summary>
    /// �C���X�y�N�^�[�Őݒ肳�ꂽ�J�X�^���h���b�v�G���A���`�F�b�N
    /// </summary>
    private void CheckCustomDropAreas()
    {
        // �����ȃG���A�����O
        dropAreas.RemoveAll(area => area == null);

        if (dropAreas.Count == 0) return;

        // ���𐔂Ƒ������J�E���g
        int correctCount = 0;
        int totalCount = dropAreas.Count;

        foreach (var area in dropAreas)
        {
            if (area != null && area.IsCorrect())
            {
                correctCount++;
            }
        }

        // �S�Đ����Ȃ�A�C�R����ύX
        if (correctCount == totalCount)
        {
            ApplyCompletedSprite();
            if (debugMode)
            {
                Debug.Log($"�J�X�^���ݒ肳�ꂽ�p�Y�����������o���܂���: correctCount={correctCount}, totalCount={totalCount}");
            }
        }
    }

    /// <summary>
    /// �������̃X�v���C�g��K�p
    /// </summary>
    private void ApplyCompletedSprite()
    {
        if (iconImage != null && completedSprite != null)
        {
            iconImage.sprite = completedSprite;
        }
    }

    /// <summary>
    /// �p�Y�������ʒm���󂯎�郁�\�b�h (SpeakerDropArea����Ăяo�����)
    /// </summary>
    /// <param name="fileName">���������t�@�C����</param>
    public void OnPuzzleCompleted(string fileName)
    {
        ApplyCompletedSprite();
        if (debugMode)
        {
            Debug.Log($"�p�Y�������ʒm���󂯎��܂���: {fileName}");
        }
    }

    /// <summary>
    /// �J�X�^���h���b�v�G���A�̒ǉ��i�X�N���v�g���瓮�I�ɒǉ�����ꍇ�j
    /// </summary>
    public void AddDropArea(SpeakerDropArea area)
    {
        if (area != null && !dropAreas.Contains(area))
        {
            dropAreas.Add(area);
            CheckPuzzleState(); // �ǉ���ɏ�Ԃ��ă`�F�b�N
        }
    }

    /// <summary>
    /// �J�X�^���h���b�v�G���A�̃��X�g���N���A
    /// </summary>
    public void ClearDropAreas()
    {
        dropAreas.Clear();
    }
}