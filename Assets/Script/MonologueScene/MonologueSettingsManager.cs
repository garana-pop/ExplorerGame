using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// MonologueScene��p�̐ݒ�Ǘ��N���X
/// ���m���[�O�\�����ł��ݒ��ʂ��J���邪�A�w��̉��o�͌p��
/// </summary>
public class MonologueSettingsManager : BaseSettingsManager
{
    [Header("MonologueScene�ŗL�̎Q��")]
    [SerializeField] private MonologueDisplayManager monologueDisplayManager;
    [SerializeField] private GameObject animationArea;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private ContinueIndicatorAnimation continueIndicatorAnimation;

    private bool wasMonologuePaused = false;

    protected override void Start()
    {
        base.Start();

        if (monologueDisplayManager == null)
        {
            monologueDisplayManager = FindObjectOfType<MonologueDisplayManager>();
        }

        if (animationArea == null)
        {
            animationArea = GameObject.Find("AnimationArea");
        }

        if (dialogueText == null)
        {
            var dialogueArea = GameObject.Find("DialogueArea");
            if (dialogueArea != null)
            {
                dialogueText = dialogueArea.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        if (continueIndicatorAnimation == null)
        {
            continueIndicatorAnimation = FindObjectOfType<ContinueIndicatorAnimation>();
        }

        if (bgmAudioSource == null)
        {
            bgmAudioSource = GameObject.Find("BGMAudioSource")?.GetComponent<AudioSource>();
        }
        canOpenSettings = true;

        // ���ʐݒ�̓ǂݍ��݂ƓK�p
        LoadAndApplyVolumeSettings();
    }

    /// <summary>
    /// game_save.json����̉��ʐݒ�ǂݍ��݋@�\
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

                // �ݒ��K�p
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

        SaveVolumeToGameSave();// �ύX���ɑ����ɕۑ�
    }

    /// <summary>
    /// SE���ʂ��ύX���ꂽ���̏����i�I�[�o�[���C�h�j
    /// </summary>
    protected override void OnSeVolumeChanged(float volume)
    {
        base.OnSeVolumeChanged(volume);

        SaveVolumeToGameSave();// �ύX���ɑ����ɕۑ�
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
    /// �ݒ��ʂ��J���ꂽ���̏���
    /// </summary>
    protected override void OnSettingsOpened()
    {
        base.OnSettingsOpened();

        if (monologueDisplayManager != null)
        {
            monologueDisplayManager.SetSettingsOpen(true);
        }

        if (continueIndicatorAnimation != null)
        {
            continueIndicatorAnimation.enabled = false;
        }
    }

    /// <summary>
    /// �ݒ��ʂ�����ꂽ���̏���
    /// </summary>
    protected override void OnSettingsClosed()
    {
        base.OnSettingsClosed();

        if (monologueDisplayManager != null)
        {
            monologueDisplayManager.SetSettingsOpen(false);
        }

        if (continueIndicatorAnimation != null)
        {
            continueIndicatorAnimation.enabled = true;
        }
    }

    public void OnMonologueEnd()
    {
        canOpenSettings = false;
    }

    public void SetBackgroundAnimationPaused(bool paused)
    {
        if (animationArea != null)
        {
            var animators = animationArea.GetComponentsInChildren<Animator>();
            foreach (var animator in animators)
            {
                if (paused)
                {
                    animator.speed = 0f;
                }
                else
                {
                    animator.speed = 1f;
                }
            }
        }
    }
}