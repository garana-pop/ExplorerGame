using UnityEngine;
using UnityEngine.EventSystems;

public class ClickToBlockRaycasts : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private CanvasGroup canvasGroup;

    // Start is called before the first frame update
    void Start()
    {
        // このオブジェクトのCanvasGroupを取得
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // マウスボタンが押されたとき
    public void OnPointerDown(PointerEventData eventData)
    {
        DebugLogger.Log("ポインターダウン検知");
        // ボタンが押された時にblocksRaycastsをtrueに設定
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;  // レイキャストを無効にする
        }
    }

    // マウスボタンが離されたとき
    public void OnPointerUp(PointerEventData eventData)
    {
        DebugLogger.Log("ポインターアップ検知");
        // ボタンが離された時にblocksRaycastsをtrueに戻す
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;  // レイキャストを有効にする
        }
    }
}
