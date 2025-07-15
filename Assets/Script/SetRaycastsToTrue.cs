using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ExplorerController : MonoBehaviour, IDropHandler
{
    private CanvasGroup canvasGroup;
    private Transform lastParent;

    void Start()
    {
        // CanvasGroup�R���|�[�l���g���擾
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("CanvasGroup component not found!");
            return;
        }

        // �����̐e�I�u�W�F�N�g���L�^
        lastParent = transform.parent;
    }

    // �h���b�v�C�x���g������
    public void OnDrop(PointerEventData eventData)
    {
        // �h���b�v���ꂽ�I�u�W�F�N�g���擾
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject != null)
        {
            // �h���b�v��ɐe���ύX���ꂽ���`�F�b�N
            CheckParentChange();
        }
    }

    private void CheckParentChange()
    {
        // ���݂̐e�ƋL�^����Ă���e���r
        if (transform.parent != lastParent)
        {
            // �e���ύX���ꂽ��blocksRaycasts��true��
            canvasGroup.blocksRaycasts = true;

            // �V�����e���L�^
            lastParent = transform.parent;

            Debug.Log("�e�I�u�W�F�N�g�̕ύX���āAblocksRaycasts = true�ɂ�����");
        }
    }

    // �K�v�ɉ����Đe�̕ύX���O������蓮�Ń`�F�b�N���郁�\�b�h
    public void ManualParentCheck()
    {
        CheckParentChange();
    }
}