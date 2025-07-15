//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

//public class FileHighlighter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
//{
//    private Image fileImage;
//    private Color originalColor;

//    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.8f, 1f); // �C���X�y�N�^�[�Őݒ�\

//    private void Awake()
//    {
//        fileImage = GetComponent<Image>();
//        if (fileImage != null)
//        {
//            originalColor = fileImage.color; // ���̐F��ۑ�
//        }
//    }

//    public void OnPointerEnter(PointerEventData eventData)
//    {
//        if (fileImage != null)
//        {
//            fileImage.color = highlightColor; // �n�C���C�g�F�ɕύX
//        }
//    }

//    public void OnPointerExit(PointerEventData eventData)
//    {
//        if (fileImage != null)
//        {
//            fileImage.color = originalColor; // ���̐F�ɖ߂�
//        }
//    }
//}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FileHighlighter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image fileImage;              // �t�@�C����Image�R���|�[�l���g
    private Color originalColor;          // ���̐F��ۑ�
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.8f, 1f); // �n�C���C�g�F�i�C���X�y�N�^�[�Őݒ�\�j

    private void Awake()
    {
        // Image�R���|�[�l���g���擾
        fileImage = GetComponent<Image>();
        if (fileImage != null)
        {
            originalColor = fileImage.color; // �����F��ۑ�
        }
        else
        {
            Debug.LogError($"{gameObject.name} ��Image�R���|�[�l���g������܂���");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // �h���b�O���łȂ��ꍇ�̂݃n�C���C�g
        if (fileImage != null && !eventData.dragging)
        {
            fileImage.color = highlightColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // �h���b�O���łȂ��ꍇ�̂݌��̐F�ɖ߂�
        if (fileImage != null && !eventData.dragging)
        {
            fileImage.color = originalColor;
        }
    }
}