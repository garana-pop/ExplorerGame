using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DraggableFile : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private Vector3 originalPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private Canvas draggingCanvas; // 最前面用Canvas
    public Vector3 GetOriginalPosition() { return originalPosition; }
    public bool FileDragging = false; //ドラッグ中か判定

    public delegate void FileDragEvent(bool isDragging);
    public static event FileDragEvent OnFileDragging;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        draggingCanvas = GameObject.Find("DraggingCanvas").GetComponent<Canvas>(); // シーンから取得
        if (draggingCanvas == null)
        {
            Debug.LogError("DraggingCanvasが見つかりません");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        FileDragging = true;
        OnFileDragging?.Invoke(true);

        originalPosition = transform.position;
        originalParent = transform.parent;

        //ドラッグ中はDraggingCanvasに移動して最前面に
        if (draggingCanvas != null)
        {
            transform.SetParent(draggingCanvas.transform, false);
        }

        canvasGroup.blocksRaycasts = false;
        //Debug.Log("ドラッグ開始時の親は　" + transform.parent.name);
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        FileDragging = false;
        OnFileDragging?.Invoke(false);

        // Raycast ですべてのオブジェクトを取得
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

        //元から入っているフォルダーにドロップした際に、blocksRaycasts = trueに変更
        //（別のフォルダーにドロップした際はFolderDropArea.csで変更）
        canvasGroup.blocksRaycasts = true;

        //元から入っているフォルダーにドロップした際に、透明度を元に戻す
        //（別のフォルダーにドロップした際はFolderDropArea.csで変更）
        //canvasGroup.alpha = 1f;

    }
}
