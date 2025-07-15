using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class ExplorerController : MonoBehaviour, IDropHandler
{
    private CanvasGroup canvasGroup;
    private Transform lastParent;

    void Start()
    {
        // CanvasGroupコンポーネントを取得
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            Debug.LogError("CanvasGroup component not found!");
            return;
        }

        // 初期の親オブジェクトを記録
        lastParent = transform.parent;
    }

    // ドロップイベントを処理
    public void OnDrop(PointerEventData eventData)
    {
        // ドロップされたオブジェクトを取得
        GameObject droppedObject = eventData.pointerDrag;
        if (droppedObject != null)
        {
            // ドロップ後に親が変更されたかチェック
            CheckParentChange();
        }
    }

    private void CheckParentChange()
    {
        // 現在の親と記録されている親を比較
        if (transform.parent != lastParent)
        {
            // 親が変更されたらblocksRaycastsをtrueに
            canvasGroup.blocksRaycasts = true;

            // 新しい親を記録
            lastParent = transform.parent;

            Debug.Log("親オブジェクトの変更して、blocksRaycasts = trueにしたよ");
        }
    }

    // 必要に応じて親の変更を外部から手動でチェックするメソッド
    public void ManualParentCheck()
    {
        CheckParentChange();
    }
}