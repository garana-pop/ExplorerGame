using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FolderButtonHighlighter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Image folderImage;
    private Color originalColor;
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f); // ���F���ۂ��F

    private static FolderButtonHighlighter activeFolder; // ���ݑI�𒆂̃t�H���_�[

    private void Awake()
    {
        folderImage = GetComponent<Image>();
        if (folderImage != null)
        {
            originalColor = folderImage.color; // ���̐F��ۑ�
            //Debug.Log("���̐F��ۑ�");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //Debug.Log("OnPointerEnter: �n�C���C�g");
        folderImage.color = highlightColor; // �}�E�X�I�[�o�[���Ƀn�C���C�g
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //  �N���b�N���ꂽ�t�H���_�[�Ȃ�F���ێ��A����ȊO�͌��ɖ߂�
        if (this != activeFolder)
        {
            folderImage.color = originalColor;
            //Debug.Log("OnPointerExit: ���̐F�ɖ߂�");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //Debug.Log("OnPointerClick: �t�H���_�[��I��");

        //  �ȑO�̃A�N�e�B�u�t�H���_�[�̐F�����Z�b�g
        if (activeFolder != null && activeFolder != this)
        {
            activeFolder.ResetColor();
        }

        //  ���݂̃t�H���_�[���n�C���C�g��Ԃɂ���
        folderImage.color = highlightColor;
        activeFolder = this;
    }

    private void ResetColor()
    {
        folderImage.color = originalColor;
    }
}
