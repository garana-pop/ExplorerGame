using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class TrashBoxDisplayManager : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IDropHandler
{
    [Header("�S�~�����J�������̉摜")]
    public Sprite mouseOverSprite; // �C���X�y�N�^�[�Őݒ肷��A�}�E�X�J�[�\����������Ƃ��̉摜

    [Header("�S�~�����J�������̕\���̈�̊g����")]
    [Tooltip("Rect Transform�R���|�[�l���g��Height�̒l")]
    [SerializeField] private int ImageDisplayHeightValue = 10;

    // UI�R���|�[�l���g
    private Image image;
    private Sprite originalSprite;
    private RectTransform rectTransform;

    // ��ԊǗ�
    private bool fileDragging = false; //�h���b�O��������
    private bool trashBoxOpen = false; //�S�~���̊W���J����������
    private bool waitingForMouseUp = false; //�u�J���Ă�����Ƀ}�E�X�A�b�v�����m����v�p

    // �S�~����Ń}�E�X�A�b�v�����ۂɔ��΂���C�x���g��錾
    public event Action OnTrashBoxOpenedAndMouseReleased;

    // ���̃R���|�[�l���g�Q��
    private TrashBoxSoundSetting soundSetting;
    //private TrashBoxTips tips;
    //private TrashBoxDeletionManagement deletionManagement;

    /// <summary>
    /// Start���\�b�h - �V�[���J�n���̏���
    /// </summary>
    private void Start()
    {
        // image�R���|�[�l���g���擾���܂��B
        image = GetComponent<Image>();

        //RectTransform�R���|�[�l���g���擾���܂��B
        rectTransform = GetComponent<RectTransform>();

        // ���̉摜��ۑ����܂��B
        if (image != null)
        {
            originalSprite = image.sprite;
        }

        // ���̃R���|�[�l���g���擾
        soundSetting = GetComponent<TrashBoxSoundSetting>();
        //tips = GetComponent<TrashBoxTips>();
        //deletionManagement = GetComponent<TrashBoxDeletionManagement>();

    }

    /// <summary>
    /// �}�E�X�J�[�\�����I�u�W�F�N�g��ɓ���ƌĂяo�����
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        //image�ɉ摜���ݒ肳��Ă���@���A�}�E�X�J�[�\�����I�u�W�F�N�g��ɂ���@���A�h���b�N���ł���ꍇ
        if (image != null && mouseOverSprite != null && fileDragging) 
        {
            //Rect Transform�R���|�[�l���g��Height�̒l��+20
            if (rectTransform != null)
            {
                Vector2 size = rectTransform.sizeDelta;
                size.y += ImageDisplayHeightValue; //Height�̒l��ύX
                rectTransform.sizeDelta = size;
            }

            //�摜��ύX�F�S�~���̊W���J����
            image.sprite = mouseOverSprite;

            //�S�~���̊W���J����
            trashBoxOpen = true;

            //�}�E�X�A�b�v�ҋ@�J�n
            waitingForMouseUp = true;
        }
    }

    /// <summary>
    /// �h���b�O�A�C�e�����S�~����Ńh���b�v���ꂽ�Ƃ��ɌĂ΂��iIDropHandler�j
    /// </summary>
    /// <param name="eventData"></param>
    public void OnDrop(PointerEventData eventData)
    {
        if (waitingForMouseUp && trashBoxOpen)
        {
            //�C�x���g�𔭉�
            OnTrashBoxOpenedAndMouseReleased?.Invoke();
        }
        waitingForMouseUp = false;
    }

    /// <summary>
    /// �}�E�X�J�[�\�����I�u�W�F�N�g�ォ��o��ƌĂяo�����
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        //image�ɉ摜���ݒ肳��Ă���ꍇ�@���A�S�~�̊W���J���Ă��邩
        if (image != null && trashBoxOpen)
        {
            //Rect Transform�R���|�[�l���g��Height�̒l��-20
            if (rectTransform != null)
            {
                Vector2 size = rectTransform.sizeDelta;
                size.y -= ImageDisplayHeightValue; //Height�̒l�����ɖ߂�
                rectTransform.sizeDelta = size;
            }

            // ���̉摜�ɖ߂��F�S�~���̊W��߂�
            image.sprite = originalSprite;

            //�S�~���̊W���܂���
            trashBoxOpen = false;
        }
    }
    /// <summary>
    /// �A�^�b�`���ꂽ�I�u�W�F�N�g���N���b�N���ꂽ���ɁA�Ăяo�����
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData)
    {
        // �q���g���b�Z�[�W�\��
        //if (tips != null)
        //{
        //    tips.ShowClickMessage();
        //}
    }

    /// <summary>
    /// TrashBoxDisplayManager�L�����Ƀh���b�O���ꂽ�����󂯎��
    /// </summary>
    private void OnEnable()
    {
        DraggableFile.OnFileDragging += HandleFileDragging; // �C�x���g�ɓo�^
    }

    /// <summary>
    /// TrashBoxDisplayManager�������Ƀh���b�O�C�x���g�̒ʒmOFF
    /// </summary>
    private void OnDisable()
    {
        DraggableFile.OnFileDragging -= HandleFileDragging; // �C�x���g�������
    }

    /// <summary>
    /// DraggableFile�N���X����isDragging�i�h���b�O����t���O�j�̒l���擾
    /// </summary>
    /// <param name="isDragging">�h���b�O�����ǂ���</param>
    private void HandleFileDragging(bool isDragging)
    {
        fileDragging = isDragging; // ��Ԃ𔽉f
    }

    #region �p�u���b�N���\�b�h

    /// <summary>
    /// �S�~�����J���Ă��邩�ǂ������擾
    /// </summary>
    /// <returns>�J���Ă���ꍇ��true</returns>
    public bool IsTrashBoxOpen()
    {
        return trashBoxOpen;
    }

    /// <summary>
    /// �t�@�C�����h���b�O�����ǂ������擾
    /// </summary>
    /// <returns>�h���b�O���̏ꍇ��true</returns>
    public bool IsFileDragging()
    {
        return fileDragging;
    }

    #endregion
}
