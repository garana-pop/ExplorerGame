using UnityEngine;
using UnityEngine.UI;

public class FolderPanelBorder : MonoBehaviour
{
    void Start()
    {
        // "BorderContainer" を作成し、FolderPanel の親に設定（Grid Layout Group の影響を受けない）
        GameObject borderContainer = new GameObject("BorderContainer");
        borderContainer.transform.SetParent(transform.parent, false); // FolderPanel の親に設定
        borderContainer.transform.SetAsLastSibling(); // 最前面に配置

        // 線のオブジェクトを作成
        GameObject border = new GameObject("RightBorder");
        border.transform.SetParent(borderContainer.transform, false);

        // Image コンポーネントを追加
        Image borderImage = border.AddComponent<Image>();
        borderImage.color = Color.white; // 白い線

        // RectTransform 設定
        RectTransform rectTransform = border.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 0f); // 右側
        rectTransform.anchorMax = new Vector2(1f, 1f); // 上下いっぱい
        rectTransform.pivot = new Vector2(1f, 0.5f); // 右端を基準
        rectTransform.sizeDelta = new Vector2(1f, 0f); // 幅 1px、高さは自動

        // Layout の影響を受けないようにする
        LayoutElement layoutElement = borderContainer.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true; // Grid Layout Group の影響を受けない

        // 右側に配置
        borderContainer.transform.position = transform.position; // FolderPanel の位置に配置
    }
}
