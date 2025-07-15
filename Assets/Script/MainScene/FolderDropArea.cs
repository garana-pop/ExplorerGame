//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI; // Imageコンポーネントを使用するために追加


//public class FolderDropArea : MonoBehaviour, IDropHandler
//{
//    public void OnDrop(PointerEventData eventData)
//    {
//        //FolderButtonHighlighter.isDragging = false; // ドラッグ終了
//        DraggableFile droppedFile = eventData.pointerDrag.GetComponent<DraggableFile>();
//        if (droppedFile == null) return;

//        GameObject folder = eventData.pointerEnter; // ドロップされたオブジェクトを取得


//        // フォルダー名が "FolderButton" で始まるオブジェクトの上にドロップされたかチェック
//        while (folder != null && !folder.name.StartsWith("FolderButton"))
//        {
//            folder = folder.transform.parent?.gameObject;
//        }

//        if (folder != null)
//        {
//            FolderButtonScript folderScript = folder.GetComponent<FolderButtonScript>();
//            if (folderScript != null && folderScript.filePanel != null)
//            {
//                droppedFile.transform.SetParent(folderScript.filePanel.transform, false);
//                droppedFile.transform.localPosition = Vector3.zero;

//                // ドロップされたファイルのCanvasGroupのblocksRaycastsをtrueに設定
//                CanvasGroup canvasGroup = droppedFile.GetComponent<CanvasGroup>();
//                if (canvasGroup != null)
//                {
//                    canvasGroup.blocksRaycasts = true;
//                    //Debug.Log($"{droppedFile.name}のblocksRaycastsがtrueに設定されました");
//                    canvasGroup.alpha = 1f; // ドロップ後に透明度を元に戻す
//                    //Debug.Log($"{droppedFile.name}の透明度を元に戻しました。FolderDropAreaで");
//                }
//                else
//                {
//                    //Debug.LogWarning($"{droppedFile.name}にCanvasGroupがありません");
//                }
//            }
//        }

//        // ドロップされたファイルの色を白色に変更
//        Image fileImage = droppedFile.GetComponent<Image>();
//        if (fileImage != null)
//        {
//            fileImage.color = Color.white; // 白色に設定
//            //Debug.Log($"{droppedFile.name}の色を白色に変更しました");
//        }
//        else
//        {
//            Debug.LogWarning($"{droppedFile.name}にImageコンポーネントがありません");
//        }

//    }
//}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Imageコンポーネントを使用するために追加

public class FolderDropArea : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        //FolderButtonHighlighter.isDragging = false; // ドラッグ終了
        DraggableFile droppedFile = eventData.pointerDrag.GetComponent<DraggableFile>();
        if (droppedFile == null) return;

        GameObject folder = eventData.pointerEnter; // ドロップされたオブジェクトを取得

        // フォルダー名が "FolderButton" で始まるオブジェクトの上にドロップされたかチェック
        while (folder != null && !folder.name.StartsWith("FolderButton"))
        {
            folder = folder.transform.parent?.gameObject;
        }

        if (folder != null)
        {
            FolderButtonScript folderScript = folder.GetComponent<FolderButtonScript>();
            if (folderScript != null && folderScript.filePanel != null)
            {
                // 追加: 今表示しているフォルダーかどうかを判定
                bool isCurrentFolder = folderScript.filePanel.gameObject.activeSelf; // filePanelがアクティブなら「今表示している」とみなす

                droppedFile.transform.SetParent(folderScript.filePanel.transform, false);
                droppedFile.transform.localPosition = Vector3.zero;

                // 追加: 今表示しているフォルダーにドロップされた場合、元の位置に戻す
                if (isCurrentFolder)
                {
                    // DraggableFileから元の位置を取得して設定
                    Vector3 originalPosition = droppedFile.GetOriginalPosition(); // 元の位置はドラッグ開始時に記録されていると仮定
                    droppedFile.transform.localPosition = droppedFile.transform.InverseTransformPoint(originalPosition); // 元のローカル位置に戻す
                }

                // ドロップされたファイルのCanvasGroupのblocksRaycastsをtrueに設定
                CanvasGroup canvasGroup = droppedFile.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = true;
                    //Debug.Log($"{droppedFile.name}のblocksRaycastsがtrueに設定されました");
                    canvasGroup.alpha = 1f; // ドロップ後に透明度を元に戻す
                    //Debug.Log($"{droppedFile.name}の透明度を元に戻しました。FolderDropAreaで");
                }
                else
                {
                    //Debug.LogWarning($"{droppedFile.name}にCanvasGroupがありません");
                }
            }
        }

        // ドロップされたファイルの色を白色に変更
        Image fileImage = droppedFile.GetComponent<Image>();
        if (fileImage != null)
        {
            fileImage.color = Color.white; // 白色に設定
            //Debug.Log($"{droppedFile.name}の色を白色に変更しました");
        }
        else
        {
            Debug.LogWarning($"{droppedFile.name}にImageコンポーネントがありません");
        }
    }
}