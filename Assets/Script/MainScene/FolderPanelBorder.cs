using UnityEngine;
using UnityEngine.UI;

public class FolderPanelBorder : MonoBehaviour
{
    void Start()
    {
        // "BorderContainer" ���쐬���AFolderPanel �̐e�ɐݒ�iGrid Layout Group �̉e�����󂯂Ȃ��j
        GameObject borderContainer = new GameObject("BorderContainer");
        borderContainer.transform.SetParent(transform.parent, false); // FolderPanel �̐e�ɐݒ�
        borderContainer.transform.SetAsLastSibling(); // �őO�ʂɔz�u

        // ���̃I�u�W�F�N�g���쐬
        GameObject border = new GameObject("RightBorder");
        border.transform.SetParent(borderContainer.transform, false);

        // Image �R���|�[�l���g��ǉ�
        Image borderImage = border.AddComponent<Image>();
        borderImage.color = Color.white; // ������

        // RectTransform �ݒ�
        RectTransform rectTransform = border.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 0f); // �E��
        rectTransform.anchorMax = new Vector2(1f, 1f); // �㉺�����ς�
        rectTransform.pivot = new Vector2(1f, 0.5f); // �E�[���
        rectTransform.sizeDelta = new Vector2(1f, 0f); // �� 1px�A�����͎���

        // Layout �̉e�����󂯂Ȃ��悤�ɂ���
        LayoutElement layoutElement = borderContainer.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true; // Grid Layout Group �̉e�����󂯂Ȃ�

        // �E���ɔz�u
        borderContainer.transform.position = transform.position; // FolderPanel �̈ʒu�ɔz�u
    }
}
