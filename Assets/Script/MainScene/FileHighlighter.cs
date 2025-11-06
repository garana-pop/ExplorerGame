//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI;

//public class FileHighlighter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
//{
//    private Image fileImage;
//    private Color originalColor;

//    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.8f, 1f); // インスペクターで設定可能

//    private void Awake()
//    {
//        fileImage = GetComponent<Image>();
//        if (fileImage != null)
//        {
//            originalColor = fileImage.color; // 元の色を保存
//        }
//    }

//    public void OnPointerEnter(PointerEventData eventData)
//    {
//        if (fileImage != null)
//        {
//            fileImage.color = highlightColor; // ハイライト色に変更
//        }
//    }

//    public void OnPointerExit(PointerEventData eventData)
//    {
//        if (fileImage != null)
//        {
//            fileImage.color = originalColor; // 元の色に戻す
//        }
//    }
//}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FileHighlighter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Image fileImage;              // ファイルのImageコンポーネント
    private Color originalColor;          // 元の色を保存
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.8f, 1f); // ハイライト色（インスペクターで設定可能）

    private void Awake()
    {
        // Imageコンポーネントを取得
        fileImage = GetComponent<Image>();
        if (fileImage != null)
        {
            originalColor = fileImage.color; // 初期色を保存
        }
        else
        {
            Debug.LogError($"{gameObject.name} にImageコンポーネントがありません");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // ドラッグ中でない場合のみハイライト
        if (fileImage != null && !eventData.dragging)
        {
            fileImage.color = highlightColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // ドラッグ中でない場合のみ元の色に戻す
        if (fileImage != null && !eventData.dragging)
        {
            fileImage.color = originalColor;
        }
    }
}