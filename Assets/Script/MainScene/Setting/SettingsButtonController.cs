using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// �ݒ�{�^���̃N���b�N�����m���āA�ݒ��ʂ�\������X�N���v�g
/// �{�^���ɃA�^�b�`���Ďg�p���܂�
/// </summary>
public class SettingsButtonController : MonoBehaviour
{
    [Header("�Q�Ɛݒ�")]
    [Tooltip("MainSceneSettingsManager�ւ̎Q��")]
    [SerializeField] private MainSceneSettingsManager settingsManager;

    // �{�^���R���|�[�l���g
    private Button button;

    private void Awake()
    {
        // �{�^���R���|�[�l���g���擾
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("SettingsButtonController��Button�R���|�[�l���g���A�^�b�`����Ă���GameObject�ɒǉ����Ă��������B");
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        // settingsManager���ݒ肳��Ă��Ȃ��ꍇ�͎�������
        if (settingsManager == null)
        {
            settingsManager = FindAnyObjectByType<MainSceneSettingsManager>();
            if (settingsManager == null)
            {
                Debug.LogError("MainSceneSettingsManager��������܂���B�V�[������MainSceneSettingsManager��ǉ����Ă��������B");
                return;
            }
        }

        // �{�^���N���b�N�C�x���g��ݒ�
        button.onClick.AddListener(OnSettingsButtonClicked);
    }

    /// <summary>
    /// �ݒ�{�^�����N���b�N���ꂽ�Ƃ��̏���
    /// </summary>
    private void OnSettingsButtonClicked()
    {
        // �ݒ�}�l�[�W���[��ʂ��Đݒ��ʂ�\��
        if (settingsManager != null)
        {
            settingsManager.ToggleSettings();
        }
    }

    private void OnDestroy()
    {
        // �C�x���g���X�i�[�̓o�^����
        if (button != null)
        {
            button.onClick.RemoveListener(OnSettingsButtonClicked);
        }
    }
}