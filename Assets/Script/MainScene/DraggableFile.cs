using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DraggableFile : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    private Vector3 originalPosition;
    private Transform originalParent;
    private CanvasGroup canvasGroup;
    private Canvas draggingCanvas; // 最前面用Canvas
    public bool FileDragging = false; // ドラッグ中か判定
    private bool isBeingDeleted = false; // 削除処理中フラグ

    [SerializeField] private float draggingAlpha = 0.01f; // ドラッグ中の透明度（インスペクターで設定可能）
    private float originalAlpha; // 元の透明度を保存

    public delegate void FileDragEvent(bool isDragging);
    public static event FileDragEvent OnFileDragging;

    public Vector3 GetOriginalPosition() { return originalPosition; }

    /// <summary>
    /// 削除処理中フラグを設定
    /// </summary>
    /// <param name="deleting">削除処理中かどうか</param>
    public void SetDeleting(bool deleting)
    {
        isBeingDeleted = deleting;
    }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        // CanvasGroupが存在しない場合は追加
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 元の透明度を保存
        originalAlpha = canvasGroup.alpha;

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

        // ドラッグ中はDraggingCanvasに移動して最前面に
        if (draggingCanvas != null)
        {
            transform.SetParent(draggingCanvas.transform, false);
        }

        // ドラッグ中の透明度を設定
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

        // 透明度を元に戻す
        canvasGroup.alpha = originalAlpha;

        // 削除処理中の場合は位置リセットを行わない
        if (isBeingDeleted)
        {
            canvasGroup.blocksRaycasts = true;
            return;
        }

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

        // 元から入っているフォルダーにドロップした際にblocksRaycasts = trueに変更
        canvasGroup.blocksRaycasts = true;
    }
}