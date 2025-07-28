using UnityEngine;

/// <summary>
/// TitleScene�̐ݒ�Ǘ��N���X
/// BGM/SE���ʐݒ��game_save.json�ɕۑ����A�V�[���J�ڑO��Őݒ���ێ�����
/// </summary>
public class TitleSceneSettingsManager : BaseSettingsManager
{
    [Header("TitleScene�ŗL�̐ݒ�")]
    [SerializeField] private MainMenuController mainMenuController;
    [SerializeField] private SettingsMenuController settingsMenuController;

    protected override void Start()
    {
        // SoundEffectManager�����݂��Ȃ��ꍇ�͍쐬
        if (SoundEffectManager.Instance == null)
        {
            GameObject soundManagerObj = new GameObject("SoundEffectManager");
            soundManagerObj.AddComponent<SoundEffectManager>();
            DontDestroyOnLoad(soundManagerObj);
        }

        // MainMenuController�̎�������
        if (mainMenuController == null)
            mainMenuController = FindFirstObjectByType<MainMenuController>();

        // SettingsMenuController�̎�������
        if (settingsMenuController == null)
            settingsMenuController = FindFirstObjectByType<SettingsMenuController>();

        // TitleSceneSettingsManager�̎Q�Ƃ�SettingsMenuController�ɐݒ�
        if (settingsMenuController != null)
        {
            settingsMenuController.SetTitleSceneSettingsManager(this);
        }

        // BGM AudioSource�̓���ݒ�
        SetupBGMAudioSource();

        // �d�v�FSoundEffectManager�̏�������҂��Ă�����N���X�̏����������s
        StartCoroutine(DelayedInitialization());
    }

    /// <summary>
    /// SoundEffectManager��GameSaveManager�̏�������҂��Ă���ݒ��ǂݍ���
    /// </summary>
    private System.Collections.IEnumerator DelayedInitialization()
    {
        // 1�t���[���ҋ@����SoundEffectManager��GameSaveManager�̏��������m���ɂ���
        yield return null;

        // ���N���X�̏����������s�i�A��LoadSettings�͌�ōs���j
        InitializeServices();
        SetupButtonListeners();
        InitializeSliders();
        SubscribeToEvents();

        // ���ʐݒ�̓ǂݍ��݂ƓK�p�i�x�����s�j
        LoadAndApplyVolumeSettings();
    }

    /// <summary>
    /// BGM AudioSource�̓���ݒ�
    /// </summary>
    private void SetupBGMAudioSource()
    {
        // MainMenuController��backgroundAudioSource��D��I�Ɏg�p
        if (mainMenuController != null && mainMenuController.backgroundAudioSource != null)
        {
            bgmAudioSource = mainMenuController.backgroundAudioSource;
        }
        else
        {
            // MainMenuController�ɂȂ��ꍇ�͊����̕��@�Ō���/�쐬
            GameObject bgmObj = GameObject.Find("BGMAudioSource");
            if (bgmObj == null)
            {
                bgmObj = new GameObject("BGMAudioSource");
                bgmAudioSource = bgmObj.AddComponent<AudioSource>();
                bgmAudioSource.loop = true;
                bgmAudioSource.playOnAwake = false;
            }
            else
            {
                bgmAudioSource = bgmObj.GetComponent<AudioSource>();
            }
        }
    }

    /// <summary>
    /// ���ʐݒ��ǂݍ���œK�p�i�C���Łj
    /// </summary>
    private void LoadAndApplyVolumeSettings()
    {

        // �i�K�I�ǂݍ��݁FPlayerPrefs -> game_save.json
        float loadedBgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.8f);
        float loadedSeVolume = PlayerPrefs.GetFloat("SEVolume", 0.8f);
        float loadedMasterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);

        // GameSaveManager����ǂݍ��݂����s
        if (GameSaveManager.Instance != null)
        {
            try
            {
                var saveData = GameSaveManager.Instance.GetCurrentSaveData();
                if (saveData?.audioSettings != null)
                {
                    loadedBgmVolume = saveData.audioSettings.bgmVolume;
                    loadedSeVolume = saveData.audioSettings.seVolume;
                    loadedMasterVolume = saveData.audioSettings.masterVolume;
                }
                else
                {
                    Debug.Log("game_save.json��audioSettings������܂���");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"game_save.json�ǂݍ��݃G���[: {e.Message}");
            }
        }

        // SoundEffectManager�����݂���ꍇ�́A�ǂݍ��񂾒l��ݒ�
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.SetVolume(loadedSeVolume);
        }

        // ���݂̉��ʂƂ��ĕۑ�
        currentBgmVolume = loadedBgmVolume;
        currentSeVolume = loadedSeVolume;

        // ���ʂ�K�p
        ApplyVolumeSettings();

        // Master Volume��K�p
        AudioListener.volume = loadedMasterVolume;

        // MainMenuController��BGM���ʂ�K�p
        if (mainMenuController != null)
        {
            mainMenuController.UpdateBgmVolume(currentBgmVolume);
        }

        // SettingsMenuController�Ɍ��݂̉��ʂ𔽉f
        if (settingsMenuController != null)
        {
            settingsMenuController.UpdateSliderValues(currentBgmVolume, currentSeVolume, loadedMasterVolume);
        }

    }

    /// <summary>
    /// ���ʐݒ��AudioSource�ɓK�p�i�I�[�o�[���C�h�j
    /// </summary>
    protected override void ApplyVolumeSettings()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = currentBgmVolume;
        }
        else
        {
            Debug.LogWarning("BGM AudioSource��������܂���I");
        }

        // MainMenuController��backgroundAudioSource�����ڍX�V�i�m�����̂��߁j
        if (mainMenuController != null && mainMenuController.backgroundAudioSource != null)
        {
            mainMenuController.backgroundAudioSource.volume = currentBgmVolume;
        }
    }

    /// <summary>
    /// SettingsMenuController���特�ʍX�V���󂯎�郁�\�b�h
    /// </summary>
    public void UpdateVolumeFromSettingsMenu(float bgmVolume, float seVolume, float masterVolume)
    {
        currentBgmVolume = bgmVolume;
        currentSeVolume = seVolume;

        // �}�X�^�[���ʂ�K�p
        AudioListener.volume = masterVolume;

        // ���ʂ�K�p
        ApplyVolumeSettings();

        // SoundEffectManager�ɂ��ݒ�
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.SetVolume(currentSeVolume);
        }

        // PlayerPrefs�ɕۑ�
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.SetFloat("SEVolume", seVolume);
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.Save();

        // game_save.json�ɕۑ�
        SaveVolumeToGameSave(masterVolume);
    }

    /// <summary>
    /// game_save.json�ɉ��ʐݒ��ۑ�
    /// </summary>
    private void SaveVolumeToGameSave(float masterVolume)
    {
        if (GameSaveManager.Instance != null)
        {
            try
            {
                GameSaveManager.Instance.UpdateAudioSettings(currentBgmVolume, currentSeVolume, masterVolume);
                GameSaveManager.Instance.SaveAudioSettingsOnly();
                Debug.Log("game_save.json�ɉ��ʐݒ��ۑ�");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"game_save.json�ւ̕ۑ��G���[: {e.Message}");
            }
        }
    }

    /// <summary>
    /// ���݂̉��ʐݒ���擾
    /// </summary>
    public void GetCurrentVolumeSettings(out float bgmVolume, out float seVolume)
    {
        bgmVolume = currentBgmVolume;
        seVolume = currentSeVolume;
    }

    /// <summary>
    /// �ݒ��ʂ�\���i�I�[�o�[���C�h�j
    /// </summary>
    public override void ShowSettings()
    {
        // BaseSettingsManager�̐ݒ�p�l���͎g�p�����A
        // SettingsMenuController�̐ݒ�p�l�����g�p����
        if (settingsMenuController != null)
        {
            // ���݂̉��ʐݒ���擾
            float masterVolume = AudioListener.volume;

            // SettingsMenuController�Ɍ��݂̉��ʂ�ݒ�
            settingsMenuController.UpdateSliderValues(currentBgmVolume, currentSeVolume, masterVolume);
        }
    }

    /// <summary>
    /// �ݒ��ʂ��\���i�I�[�o�[���C�h�j
    /// </summary>
    public override void HideSettings()
    {
        // SettingsMenuController�̐ݒ�p�l������鏈����
        // SettingsMenuController���ōs��
    }

    /// <summary>
    /// ESC�L�[�ł̐ݒ��ʊJ�𖳌����iTitleScene�ł͕ʂ̕��@�ŊJ���j
    /// </summary>
    protected override void CheckForEscapeKeyPress()
    {
        // TitleScene�ł�ESC�L�[�ł̐ݒ��ʊJ�͍s��Ȃ�
        // �ݒ�{�^������̂݊J��
    }

    /// <summary>
    /// BaseSettingsManager��LoadSettings���I�[�o�[���C�h���Ė�����
    /// </summary>
    protected override void LoadSettings()
    {
        // �������Ȃ��iLoadAndApplyVolumeSettings�ŏ����ς݁j
    }
}