//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.UI; // Image�R���|�[�l���g���g�p���邽�߂ɒǉ�


//public class FolderDropArea : MonoBehaviour, IDropHandler
//{
//    public void OnDrop(PointerEventData eventData)
//    {
//        //FolderButtonHighlighter.isDragging = false; // �h���b�O�I��
//        DraggableFile droppedFile = eventData.pointerDrag.GetComponent<DraggableFile>();
//        if (droppedFile == null) return;

//        GameObject folder = eventData.pointerEnter; // �h���b�v���ꂽ�I�u�W�F�N�g���擾


//        // �t�H���_�[���� "FolderButton" �Ŏn�܂�I�u�W�F�N�g�̏�Ƀh���b�v���ꂽ���`�F�b�N
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

//                // �h���b�v���ꂽ�t�@�C����CanvasGroup��blocksRaycasts��true�ɐݒ�
//                CanvasGroup canvasGroup = droppedFile.GetComponent<CanvasGroup>();
//                if (canvasGroup != null)
//                {
//                    canvasGroup.blocksRaycasts = true;
//                    //Debug.Log($"{droppedFile.name}��blocksRaycasts��true�ɐݒ肳��܂���");
//                    canvasGroup.alpha = 1f; // �h���b�v��ɓ����x�����ɖ߂�
//                    //Debug.Log($"{droppedFile.name}�̓����x�����ɖ߂��܂����BFolderDropArea��");
//                }
//                else
//                {
//                    //Debug.LogWarning($"{droppedFile.name}��CanvasGroup������܂���");
//                }
//            }
//        }

//        // �h���b�v���ꂽ�t�@�C���̐F�𔒐F�ɕύX
//        Image fileImage = droppedFile.GetComponent<Image>();
//        if (fileImage != null)
//        {
//            fileImage.color = Color.white; // ���F�ɐݒ�
//            //Debug.Log($"{droppedFile.name}�̐F�𔒐F�ɕύX���܂���");
//        }
//        else
//        {
//            Debug.LogWarning($"{droppedFile.name}��Image�R���|�[�l���g������܂���");
//        }

//    }
//}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI; // Image�R���|�[�l���g���g�p���邽�߂ɒǉ�

public class FolderDropArea : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        //FolderButtonHighlighter.isDragging = false; // �h���b�O�I��
        DraggableFile droppedFile = eventData.pointerDrag.GetComponent<DraggableFile>();
        if (droppedFile == null) return;

        GameObject folder = eventData.pointerEnter; // �h���b�v���ꂽ�I�u�W�F�N�g���擾

        // �t�H���_�[���� "FolderButton" �Ŏn�܂�I�u�W�F�N�g�̏�Ƀh���b�v���ꂽ���`�F�b�N
        while (folder != null && !folder.name.StartsWith("FolderButton"))
        {
            folder = folder.transform.parent?.gameObject;
        }

        if (folder != null)
        {
            FolderButtonScript folderScript = folder.GetComponent<FolderButtonScript>();
            if (folderScript != null && folderScript.filePanel != null)
            {
                // �ǉ�: ���\�����Ă���t�H���_�[���ǂ����𔻒�
                bool isCurrentFolder = folderScript.filePanel.gameObject.activeSelf; // filePanel���A�N�e�B�u�Ȃ�u���\�����Ă���v�Ƃ݂Ȃ�

                droppedFile.transform.SetParent(folderScript.filePanel.transform, false);
                droppedFile.transform.localPosition = Vector3.zero;

                // �ǉ�: ���\�����Ă���t�H���_�[�Ƀh���b�v���ꂽ�ꍇ�A���̈ʒu�ɖ߂�
                if (isCurrentFolder)
                {
                    // DraggableFile���猳�̈ʒu���擾���Đݒ�
                    Vector3 originalPosition = droppedFile.GetOriginalPosition(); // ���̈ʒu�̓h���b�O�J�n���ɋL�^����Ă���Ɖ���
                    droppedFile.transform.localPosition = droppedFile.transform.InverseTransformPoint(originalPosition); // ���̃��[�J���ʒu�ɖ߂�
                }

                // �h���b�v���ꂽ�t�@�C����CanvasGroup��blocksRaycasts��true�ɐݒ�
                CanvasGroup canvasGroup = droppedFile.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.blocksRaycasts = true;
                    //Debug.Log($"{droppedFile.name}��blocksRaycasts��true�ɐݒ肳��܂���");
                    canvasGroup.alpha = 1f; // �h���b�v��ɓ����x�����ɖ߂�
                    //Debug.Log($"{droppedFile.name}�̓����x�����ɖ߂��܂����BFolderDropArea��");
                }
                else
                {
                    //Debug.LogWarning($"{droppedFile.name}��CanvasGroup������܂���");
                }
            }
        }

        // �h���b�v���ꂽ�t�@�C���̐F�𔒐F�ɕύX
        Image fileImage = droppedFile.GetComponent<Image>();
        if (fileImage != null)
        {
            fileImage.color = Color.white; // ���F�ɐݒ�
            //Debug.Log($"{droppedFile.name}�̐F�𔒐F�ɕύX���܂���");
        }
        else
        {
            Debug.LogWarning($"{droppedFile.name}��Image�R���|�[�l���g������܂���");
        }
    }
}