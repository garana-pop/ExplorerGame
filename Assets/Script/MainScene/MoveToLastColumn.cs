using UnityEngine;
using UnityEngine.EventSystems;

public class MoveToLastColumn : MonoBehaviour, IPointerClickHandler
{
    // ボタンがクリックされたときに呼ばれる
    public void OnPointerClick(PointerEventData eventData)
    {
        Transform parentTransform = transform.parent;
        if (parentTransform != null)
        {
            // 親の子供の数 - 1 に設定することで、最後列に配置
            transform.SetSiblingIndex(parentTransform.childCount - 1);
            Debug.Log("最後列に配置");
        }
    }
}
