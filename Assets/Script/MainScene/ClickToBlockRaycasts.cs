using UnityEngine;
using UnityEngine.EventSystems;

public class ClickToBlockRaycasts : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private CanvasGroup canvasGroup;

    // Start is called before the first frame update
    void Start()
    {
        // ���̃I�u�W�F�N�g��CanvasGroup���擾
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // �}�E�X�{�^���������ꂽ�Ƃ�
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("�|�C���^�[�_�E�����m");
        // �{�^���������ꂽ����blocksRaycasts��true�ɐݒ�
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;  // ���C�L���X�g�𖳌��ɂ���
        }
    }

    // �}�E�X�{�^���������ꂽ�Ƃ�
    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("�|�C���^�[�A�b�v���m");
        // �{�^���������ꂽ����blocksRaycasts��true�ɖ߂�
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;  // ���C�L���X�g��L���ɂ���
        }
    }
}
