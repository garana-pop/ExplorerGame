using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class FolderButtonHighlighter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    private Image folderImage;
    private Color originalColor;
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f); // 黄色っぽい色

    private static FolderButtonHighlighter activeFolder; // 現在選択中のフォルダー

    private void Awake()
    {
        folderImage = GetComponent<Image>();
        if (folderImage != null)
        {
            originalColor = folderImage.color; // 元の色を保存
            //Debug.Log("元の色を保存");
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        //Debug.Log("OnPointerEnter: ハイライト");
        folderImage.color = highlightColor; // マウスオーバー時にハイライト
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        //  クリックされたフォルダーなら色を維持、それ以外は元に戻す
        if (this != activeFolder)
        {
            folderImage.color = originalColor;
            //Debug.Log("OnPointerExit: 元の色に戻す");
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        //Debug.Log("OnPointerClick: フォルダーを選択");

        //  以前のアクティブフォルダーの色をリセット
        if (activeFolder != null && activeFolder != this)
        {
            activeFolder.ResetColor();
        }

        //  現在のフォルダーをハイライト状態にする
        folderImage.color = highlightColor;
        activeFolder = this;
    }

    private void ResetColor()
    {
        folderImage.color = originalColor;
    }
}
