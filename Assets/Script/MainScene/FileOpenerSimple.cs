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
            openPanel.transform.SetAsLastSibling(); // �ǉ�: �p�l�����q�G�����L�[�̈�ԉ��Ɉړ����čőO�ʂɕ\��
            Debug.Log("�t�@�C�����N���b�N�F�Ή������ʂ��J���čőO�ʂɈړ����܂�");
        }
        else
        {
            Debug.LogWarning("openPanel �����ݒ�ł��I");
        }
    }
}
