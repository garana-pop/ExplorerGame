using UnityEngine;

/// <summary>
/// �S�~���̌��ʉ��ݒ���Ǘ�����N���X
/// �t�@�C���h���b�v���ƃS�~���N���b�N���̌��ʉ��𐧌䂵�܂�
/// </summary>
public class TrashBoxSoundSetting : MonoBehaviour
{
    // SoundEffectManager�̎Q��
    private SoundEffectManager soundEffectManager;

    // TrashBoxDisplayManager�̎Q��
    private TrashBoxDisplayManager trashBoxDisplayManager;

    /// <summary>
    /// Start���\�b�h - �V�[���J�n��̏���
    /// </summary>
    private void Start()
    {
        // SoundEffectManager�̎Q�Ƃ��擾
        soundEffectManager = FindFirstObjectByType<SoundEffectManager>();

        // TrashBoxDisplayManager�̎Q�Ƃ��擾
        trashBoxDisplayManager = GetComponent<TrashBoxDisplayManager>();

        if (trashBoxDisplayManager != null)
        {
            //�C�x���g���w�ǁi�󂯎��j
            trashBoxDisplayManager.OnTrashBoxOpenedAndMouseReleased += HandleOpenedAndReleased;
        }
    }

    /// <summary>
    /// �V�[���J�ڂ�Q�[���I���A�蓮�� Destroy() �������ɌĂ΂�܂�
    /// �Q�Ɛ؂�G���[��h�����߁AHandleOpenedAndReleased ���\�b�h������
    /// </summary>
    private void OnDestroy()
    {
        if (trashBoxDisplayManager != null)
        {
            //�C�x���g������
            trashBoxDisplayManager.OnTrashBoxOpenedAndMouseReleased -= HandleOpenedAndReleased;

        }
    }

    /// <summary>
    /// �C�x���g�������������Ɏ��s����鏈��
    /// �S�~���Ƀt�@�C����������ʉ����Đ�
    /// </summary>
    private void HandleOpenedAndReleased()
    {
        SoundEffectManager.Instance.PlayCategorySound("TrashDestroySound", 0);
    }

}