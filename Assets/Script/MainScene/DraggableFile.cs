using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DraggableFile : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private Vector3 originalPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private Canvas draggingCanvas; // �őO�ʗpCanvas
    public bool FileDragging = false; // �h���b�O��������
    private bool isBeingDeleted = false; // �폜�������t���O

    [SerializeField] private float draggingAlpha = 0.01f; // �h���b�O���̓����x�i�C���X�y�N�^�[�Őݒ�\�j
    private float originalAlpha; // ���̓����x��ۑ�

    public delegate void FileDragEvent(bool isDragging);
    public static event FileDragEvent OnFileDragging;

    public Vector3 GetOriginalPosition() { return originalPosition; }

    /// <summary>
    /// �폜�������t���O��ݒ�
    /// </summary>
    /// <param name="deleting">�폜���������ǂ���</param>
    public void SetDeleting(bool deleting)
    {
        isBeingDeleted = deleting;
    }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        // CanvasGroup�����݂��Ȃ��ꍇ�͒ǉ�
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // ���̓����x��ۑ�
        originalAlpha = canvasGroup.alpha;

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

        // �h���b�O����DraggingCanvas�Ɉړ����čőO�ʂ�
        if (draggingCanvas != null)
        {
            transform.SetParent(draggingCanvas.transform, false);
        }

        // �h���b�O���̓����x��ݒ�
        canvasGroup.alpha = draggingAlpha;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        FileDragging = false;
        OnFileDragging?.Invoke(false);

        // �����x�����ɖ߂�
        canvasGroup.alpha = originalAlpha;

        // �폜�������̏ꍇ�͈ʒu���Z�b�g���s��Ȃ�
        if (isBeingDeleted)
        {
            canvasGroup.blocksRaycasts = true;
            return;
        }

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

        if (dropTarget != null)
        {
            Transform filePanel = dropTarget.transform.parent.Find("FilePanel");
            if (filePanel != null)
            {
                transform.SetParent(filePanel, false);
            }
        }
        else
        {
            transform.position = originalPosition;
            transform.SetParent(originalParent, false);
        }

        // ����������Ă���t�H���_�[�Ƀh���b�v�����ۂ�blocksRaycasts = true�ɕύX
        canvasGroup.blocksRaycasts = true;
    }
}