using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TrashBoxDisplayManager : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("�S�~�����J�������̉摜")]
    public Sprite mouseOverSprite; // �C���X�y�N�^�[�Őݒ肷��A�}�E�X�J�[�\����������Ƃ��̉摜

    [Header("�S�~�����J�������̕\���̈�̊g����")]
    [Tooltip("Rect Transform�R���|�[�l���g��Height�̒l")]
    [SerializeField] private int ImageDisplayHeightValue = 10;

    private Image image;
    private Sprite originalSprite;
    private RectTransform rectTransform;
    private bool FileDragging = false; //�h���b�O��������
    private bool TrashBoxOpen = false; //�S�~���̊W���J����������

    /// <summary>
    /// Start���\�b�h - �V�[���J�n���̏���
    /// </summary>
    private void Start()
    {
        // �e�I�u�W�F�N�g��Transform���擾���A��ԉ��ɔz�u���܂��B
        if (transform.parent != null)
        {
            transform.SetAsLastSibling();
        }

        // image�R���|�[�l���g���擾���܂��B
        image = GetComponent<Image>();

        //RectTransform�R���|�[�l���g���擾���܂��B
        rectTransform = GetComponent<RectTransform>();

        // ���̉摜��ۑ����܂��B
        if (image != null)
        {
            originalSprite = image.sprite;
        }
    }

    /// <summary>
    /// �}�E�X�J�[�\�����I�u�W�F�N�g��ɓ���ƌĂяo�����
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        //image�ɉ摜���ݒ肳��Ă���@���A�}�E�X�J�[�\�����I�u�W�F�N�g��ɂ���@���A�h���b�N���ł���ꍇ
        if (image != null && mouseOverSprite != null && FileDragging) 
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
            TrashBoxOpen = true;

            Debug.Log("�S�~���̊W���J����");
        }
    }

    /// <summary>
    /// �}�E�X�J�[�\�����I�u�W�F�N�g�ォ��o��ƌĂяo�����
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        //image�ɉ摜���ݒ肳��Ă���ꍇ�@���A�S�~�̊W���J���Ă��邩
        if (image != null && TrashBoxOpen)
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
            TrashBoxOpen = false;

            Debug.Log("�S�~���̊W��߂�");
        }
    }
    /// <summary>
    /// �A�^�b�`���ꂽ�I�u�W�F�N�g���N���b�N���ꂽ���ɁA�Ăяo�����
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("�N���b�N���ꂽ��");
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
    /// <param name="isDragging"></param>
    private void HandleFileDragging(bool isDragging)
    {
        FileDragging = isDragging; // ��Ԃ𔽉f
        Debug.Log("FileDragging" + FileDragging);
    }

}
