using UnityEngine;
using UnityEngine.EventSystems;

public class FileOpenerSimple : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject openPanel;

    public void OnPointerClick(PointerEventData eventData)
    {
        OpenFile();
    }

    private void OpenFile()
    {
        if (openPanel != null)
        {
            openPanel.SetActive(true);
            openPanel.transform.SetAsLastSibling(); // 追加: パネルをヒエラルキーの一番下に移動して最前面に表示
            Debug.Log("ファイルをクリック：対応する画面を開いて最前面に移動します");
        }
        else
        {
            Debug.LogWarning("openPanel が未設定です！");
        }
    }
}
