using UnityEngine;

public class ConversationSettingsManager : BaseSettingsManager
{
    [Header("ConversationFatherAndDaughter�ŗL�̐ݒ�")]
    [SerializeField] private ConversationSceneController sceneController;

    protected override void Start()
    {
        base.Start();

        // SoundEffectManager�����݂��Ȃ��ꍇ�͍쐬
        if (SoundEffectManager.Instance == null)
        {
            GameObject soundManagerObj = new GameObject("SoundEffectManager");
            soundManagerObj.AddComponent<SoundEffectManager>();
            DontDestroyOnLoad(soundManagerObj);
        }

        // �V�[���R���g���[���[�̎�������
        if (sceneController == null)
            sceneController = FindFirstObjectByType<ConversationSceneController>();

        // BGM AudioSource�̎��������iBaseSettingsManager��bgmAudioSource���g�p�j
        if (bgmAudioSource == null)
        {
            bgmAudioSource = GameObject.Find("BGMAudioSource")?.GetComponent<AudioSource>();
        }

        // ���ʐݒ�̓ǂݍ��݂ƓK�p
        LoadAndApplyVolumeSettings();
    }

    /// <summary>
    /// ���ʐݒ��ǂݍ���œK�p
    /// </summary>
    private void LoadAndApplyVolumeSettings()
    {
        // GameSaveManager���特�ʐݒ��ǂݍ���
        if (GameSaveManager.Instance != null)
        {
            var saveData = GameSaveManager.Instance.GetCurrentSaveData();
            if (saveData?.audioSettings != null)
            {
                // BaseSettingsManager�̕ϐ��𒼐ڍX�V
                currentBgmVolume = saveData.audioSettings.bgmVolume;
                currentSeVolume = saveData.audioSettings.seVolume;

                // ���ʂ�K�p
                ApplyVolumeSettings();
                UpdateSliderValues();

                return;
            }
        }

        // game_save.json�ɐݒ肪�Ȃ��ꍇ�̓f�t�H���g�l���g�p
        LoadDefaultSettings();
    }

    /// <summary>
    /// �f�t�H���g�ݒ��ǂݍ���
    /// </summary>
    protected override void LoadDefaultSettings()
    {
        currentBgmVolume = 0.5f;
        currentSeVolume = 0.5f;

        ApplyVolumeSettings();
        UpdateSliderValues();
    }

    /// <summary>
    /// BGM���ʂ��ύX���ꂽ���̏����i�I�[�o�[���C�h�j
    /// </summary>
    protected override void OnBgmVolumeChanged(float volume)
    {
        base.OnBgmVolumeChanged(volume);

        // game_save.json�ɕۑ�
        SaveVolumeToGameSave();
    }

    /// <summary>
    /// SE���ʂ��ύX���ꂽ���̏����i�I�[�o�[���C�h�j
    /// </summary>
    protected override void OnSeVolumeChanged(float volume)
    {
        base.OnSeVolumeChanged(volume);

        // game_save.json�ɕۑ�
        SaveVolumeToGameSave();
    }

    /// <summary>
    /// ���ʐݒ��game_save.json�ɕۑ�
    /// </summary>
    private void SaveVolumeToGameSave()
    {
        if (GameSaveManager.Instance != null)
        {
            // �܂�PlayerPrefs�Ɍ��݂̉��ʂ�ۑ��iGameSaveManager���Q�Ƃ��邽�߁j
            PlayerPrefs.SetFloat("BGMVolume", currentBgmVolume);
            PlayerPrefs.SetFloat("SEVolume", currentSeVolume);
            PlayerPrefs.Save();

            // ���݂̉��ʐݒ�����W�iBaseSettingsManager�̒l���g�p�j
            GameSaveManager.Instance.UpdateAudioSettings(currentBgmVolume, currentSeVolume);

            // ���ʐݒ�݂̂�ۑ�
            GameSaveManager.Instance.SaveAudioSettingsOnly();
        }
    }

    /// <summary>
    /// �ݒ��ۑ��i�I�[�o�[���C�h�j
    /// </summary>
    protected override void SaveSettings()
    {
        // �܂�BGM���ʂ�PlayerPrefs�ɕۑ�
        PlayerPrefs.SetFloat("BGMVolume", currentBgmVolume);

        // SE���ʂ�SoundEffectManager�ɔC����
        if (soundEffectManager != null)
        {
            soundEffectManager.SetVolume(currentSeVolume);
            soundEffectManager.SaveVolumeSettingsWithGameSaveManager();
        }
        else
        {
            // SoundEffectManager���Ȃ��ꍇ�͒��ڕۑ�
            PlayerPrefs.SetFloat("SEVolume", currentSeVolume);
        }
        // PlayerPrefs���m���ɕۑ�
        PlayerPrefs.Save();

        // BGM���ʂ�game_save.json�ɕۑ�
        SaveVolumeToGameSave();
    }

    /// <summary>
    /// �ݒ��ʂ̕\��/��\����؂�ւ���
    /// </summary>
    public override void ToggleSettings()
    {
        // ���N���X��ToggleSettings���Ăяo���O�ɁA���݂̏�Ԃ�ۑ�
        bool wasVisible = isSettingsVisible;

        base.ToggleSettings();

        // ConversationSceneController�ɐݒ��ʂ̏�Ԃ�ʒm
        if (sceneController != null)
        {
            // ��Ԃ��ύX���ꂽ�ꍇ�̂ݒʒm
            if (wasVisible != isSettingsVisible)
            {
                sceneController.SetSettingsOpen(isSettingsVisible);
            }
        }
    }

    /// <summary>
    /// �ݒ��ʂ�\��
    /// </summary>
    public override void ShowSettings()
    {
        base.ShowSettings();

        // ConversationSceneController�ɐݒ��ʂ��J�������Ƃ�ʒm
        if (sceneController != null)
        {
            sceneController.SetSettingsOpen(true);
        }
    }

    /// <summary>
    /// �ݒ��ʂ��\��
    /// </summary>
    public override void HideSettings()
    {
        base.HideSettings();

        // ConversationSceneController�ɐݒ��ʂ��������Ƃ�ʒm
        if (sceneController != null)
        {
            sceneController.SetSettingsOpen(false);
        }
    }

    /// <summary>
    /// �ݒ��ʂ��J���ꂽ���̏���
    /// </summary>
    protected override void OnSettingsOpened()
    {
        base.OnSettingsOpened();

        // �ǉ��̏������K�v�ȏꍇ�͂����ɋL�q
    }

    /// <summary>
    /// �ݒ��ʂ�����ꂽ���̏���
    /// </summary>
    protected override void OnSettingsClosed()
    {
        base.OnSettingsClosed();

        // �ǉ��̏������K�v�ȏꍇ�͂����ɋL�q
    }
}