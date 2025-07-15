using UnityEngine;
using UnityEngine.EventSystems;

public class FolderDropZone : MonoBehaviour, IPointerClickHandler
{
    private FolderButtonScript folderScript;

    private void Start()
    {
        folderScript = GetComponentInParent<FolderButtonScript>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (folderScript != null)
        {
            folderScript.ToggleFolder(); // フォルダーを開く
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 何もしない（必要ならフォルダーを閉じる処理を追加）
    }
}

//using UnityEngine;
//using UnityEngine.EventSystems;

//public class FolderDropZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
//{
//    private FolderButtonScript folderScript;

//    private void Start()
//    {
//        folderScript = GetComponentInParent<FolderButtonScript>();
//    }

//    public void OnPointerEnter(PointerEventData eventData)
//    {
//        if (folderScript != null)
//        {
//            folderScript.ToggleFolder(); // フォルダーを開く
//        }
//    }

//    public void OnPointerExit(PointerEventData eventData)
//    {
//        // 何もしない（必要ならフォルダーを閉じる処理を追加）
//    }
//}
