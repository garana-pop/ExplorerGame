using UnityEngine;
using UnityEngine.EventSystems;

public class MoveToLastColumn : MonoBehaviour, IPointerClickHandler
{
    // �{�^�����N���b�N���ꂽ�Ƃ��ɌĂ΂��
    public void OnPointerClick(PointerEventData eventData)
    {
        Transform parentTransform = transform.parent;
        if (parentTransform != null)
        {
            // �e�̎q���̐� - 1 �ɐݒ肷�邱�ƂŁA�Ō��ɔz�u
            transform.SetSiblingIndex(parentTransform.childCount - 1);
            Debug.Log("�Ō��ɔz�u");
        }
    }
}
