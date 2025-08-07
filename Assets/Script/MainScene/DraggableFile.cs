using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DraggableFile : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private Vector3 originalPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private Canvas draggingCanvas; // �őO�ʗpCanvas
    public Vector3 GetOriginalPosition() { return originalPosition; }
    public bool FileDragging = false; //�h���b�O��������

    public delegate void FileDragEvent(bool isDragging);
    public static event FileDragEvent OnFileDragging;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        draggingCanvas = GameObject.Find("DraggingCanvas").GetComponent<Canvas>(); // �V�[������擾
        if (draggingCanvas == null)
        {
            Debug.LogError("DraggingCanvas��������܂���");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        FileDragging = true;
        OnFileDragging?.Invoke(true);

        originalPosition = transform.position;
        originalParent = transform.parent;

        //�h���b�O����DraggingCanvas�Ɉړ����čőO�ʂ�
        if (draggingCanvas != null)
        {
            transform.SetParent(draggingCanvas.transform, false);
        }

        canvasGroup.blocksRaycasts = false;
        //Debug.Log("�h���b�O�J�n���̐e�́@" + transform.parent.name);
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        FileDragging = false;
        OnFileDragging?.Invoke(false);

        // Raycast �ł��ׂẴI�u�W�F�N�g���擾
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = eventData.position
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        GameObject dropTarget = null;
        foreach (var result in results)
        {
            if (result.gameObject.name.StartsWith("FolderButton"))
            {
                dropTarget = result.gameObject;
                break;
            }
        }

        //Debug.Log("OnEndDrag: Drop Target = " + (dropTarget != null ? dropTarget.name : "null"));

        if (dropTarget != null)
        {
            Transform filePanel = dropTarget.transform.parent.Find("FilePanel");
            if (filePanel != null)
            {
                transform.SetParent(filePanel, false);
                //Debug.Log("Moved to Folder: " + dropTarget.name);
            }
        }
        else
        {
            transform.position = originalPosition;
            transform.SetParent(originalParent, false);
            //Debug.Log("Moved back to original position");
        }

        //����������Ă���t�H���_�[�Ƀh���b�v�����ۂɁAblocksRaycasts = true�ɕύX
        //�i�ʂ̃t�H���_�[�Ƀh���b�v�����ۂ�FolderDropArea.cs�ŕύX�j
        canvasGroup.blocksRaycasts = true;

        //����������Ă���t�H���_�[�Ƀh���b�v�����ۂɁA�����x�����ɖ߂�
        //�i�ʂ̃t�H���_�[�Ƀh���b�v�����ۂ�FolderDropArea.cs�ŕύX�j
        //canvasGroup.alpha = 1f;

    }
}
