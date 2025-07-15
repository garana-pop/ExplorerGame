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
            folderScript.ToggleFolder(); // �t�H���_�[���J��
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // �������Ȃ��i�K�v�Ȃ�t�H���_�[����鏈����ǉ��j
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
//            folderScript.ToggleFolder(); // �t�H���_�[���J��
//        }
//    }

//    public void OnPointerExit(PointerEventData eventData)
//    {
//        // �������Ȃ��i�K�v�Ȃ�t�H���_�[����鏈����ǉ��j
//    }
//}
