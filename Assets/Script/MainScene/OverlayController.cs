using UnityEngine;

/// <summary>
/// DraggingCanvas�ɁuOverlay�v�ȊO�̎q�I�u�W�F�N�g��1�ȏ゠��ꍇ��
/// Overlay�I�u�W�F�N�g���A�N�e�B�u�ɂ���X�N���v�g
/// </summary>
public class OverlayController : MonoBehaviour
{
    [Tooltip("�A�N�e�B�u�ɂ���I�[�o�[���C�I�u�W�F�N�g")]
    [SerializeField] private GameObject overlayObject;

    private void Awake()
    {
        // Overlay�I�u�W�F�N�g�̎��������i�ݒ肳��Ă��Ȃ��ꍇ�j
        if (overlayObject == null)
        {
            overlayObject = transform.Find("Overlay")?.gameObject;
            if (overlayObject == null)
            {
                Debug.LogWarning("OverlayController: Overlay�I�u�W�F�N�g��������܂���B�C���X�y�N�^�[�Őݒ肵�Ă��������B");
                enabled = false; // �X�N���v�g�𖳌���
                return;
            }
        }

        // ������Ԃ̃`�F�b�N
        CheckOverlayStatus();
    }

    private void OnTransformChildrenChanged()
    {
        // �q�I�u�W�F�N�g���ύX���ꂽ�Ƃ��ɌĂяo�����
        CheckOverlayStatus();
    }

    /// <summary>
    /// �uOverlay�v�ȊO�̎q�I�u�W�F�N�g�����݂��邩�`�F�b�N���A
    /// Overlay�̕\���E��\�����X�V
    /// </summary>
    private void CheckOverlayStatus()
    {
        if (overlayObject == null) return;

        // �uOverlay�v�ȊO�̎q�I�u�W�F�N�g�����v�Z
        int childCount = 0;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            // Overlay�I�u�W�F�N�g���g�͏��O
            if (child.gameObject != overlayObject)
            {
                childCount++;
            }
        }

        // �uOverlay�v�ȊO�̎q�I�u�W�F�N�g��1�ȏ゠��΃A�N�e�B�u��
        overlayObject.SetActive(childCount > 0);
    }
}