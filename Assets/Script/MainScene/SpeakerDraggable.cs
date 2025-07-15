using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 発言者をドラッグするためのスクリプト
public class SpeakerDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private string speakerName; // 発言者の名前

    private Vector3 originalPosition;
    private Canvas draggingCanvas;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // ドラッグ用キャンバスの取得
        draggingCanvas = GameObject.Find("DraggingCanvas").GetComponent<Canvas>();
        if (draggingCanvas == null)
        {
            Debug.LogError("DraggingCanvasが見つかりません");
        }

        // 自身のCanvasGroupの取得または追加
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {

        // 元の位置を保存
        originalPosition = transform.position;

        // ドラッグ中はレイキャストを無効に
        canvasGroup.blocksRaycasts = false;

        // 半透明にして視覚的フィードバックを提供
        canvasGroup.alpha = 0.7f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // ドラッグ位置に追従
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {

        // 元の状態に戻す
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // Raycastですべてのオブジェクトを取得
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = eventData.position
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        bool droppedOnTarget = false;

        // ドロップエリアを検索
        foreach (var result in results)
        {
            SpeakerDropArea dropArea = result.gameObject.GetComponent<SpeakerDropArea>();
            if (dropArea != null)
            {
                // ドロップエリアに発言者をドロップした処理
                droppedOnTarget = dropArea.OnSpeakerDropped(this);
                break;
            }
        }

        // 常に元の位置に戻す
        transform.position = originalPosition;
    }

    public string GetSpeakerName()
    {
        return speakerName;
    }
}