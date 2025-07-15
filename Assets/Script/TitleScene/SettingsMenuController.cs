using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsMenuController : MonoBehaviour
{
    [Header("�ݒ�J�e�S��")]
    [SerializeField] private Button languageButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button graphicsButton;
    [SerializeField] private Button resetDataButton;
    [SerializeField] private Button backButton;

    [Header("�p�l��")]
    [SerializeField] private GameObject languagePanel;
    [SerializeField] private GameObject soundPanel;
    [SerializeField] private GameObject graphicsPanel;
    [SerializeField] private GameObject resetConfirmationPanel;

    [Header("����ݒ�")]
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private List<string> availableLanguages = new List<string>() { "���{��", "English" };

    [Header("�T�E���h�ݒ�")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("�O���t�B�b�N�ݒ�")]
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("�}�l�[�W���[�Q��")]
    private MainMenuController mainMenuController;
    private TitleSceneSettingsManager titleSceneSettingsManager;

    [Header("�f�o�b�O")]
    [SerializeField] private bool debugMode = false;

    private void Start()
    {
        mainMenuController = GetComponentInParent<MainMenuController>();

        // �p�l���̏�����
        CloseSubPanels();

        // �{�^���̃��X�i�[�ݒ�
        SetupButtonListeners();

        // �X���C�_�[�̏����ݒ�
        SetupSliders();

        // �����l�����[�h
        LoadInitialSettings();
    }

    /// <summary>
    /// TitleSceneSettingsManager��ݒ�
    /// </summary>
    public void SetTitleSceneSettingsManager(TitleSceneSettingsManager manager)
    {
        titleSceneSettingsManager = manager;

        // ���݂̉��ʐݒ���擾���Ĕ��f
        if (titleSceneSettingsManager != null)
        {
            float bgmVolume, seVolume;
            titleSceneSettingsManager.GetCurrentVolumeSettings(out bgmVolume, out seVolume);
            UpdateSliderValues(bgmVolume, seVolume);
        }
    }

    private void SetupButtonListeners()
    {
        if (languageButton != null)
            languageButton.onClick.AddListener(OnLanguageButtonClicked);

        if (soundButton != null)
            soundButton.onClick.AddListener(OnSoundButtonClicked);

        if (graphicsButton != null)
            graphicsButton.onClick.AddListener(OnGraphicsButtonClicked);

        if (resetDataButton != null)
            resetDataButton.onClick.AddListener(OnResetDataButtonClicked);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackButtonClicked);
    }

    private void SetupSliders()
    {
        // BGM�X���C�_�[�̐ݒ�
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.minValue = 0f;
            bgmVolumeSlider.maxValue = 1f;
            bgmVolumeSlider.wholeNumbers = false;
            bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        }

        // SE�X���C�_�[�̐ݒ�
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.wholeNumbers = false;
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        // �}�X�^�[�{�����[���X���C�_�[�̐ݒ�
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.minValue = 0f;
            masterVolumeSlider.maxValue = 1f;
            masterVolumeSlider.wholeNumbers = false;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
    }

    private void LoadInitialSettings()
    {
        // �������ʂ�ݒ�
        if (bgmVolumeSlider != null)
            bgmVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("BGMVolume", 0.8f));

        if (sfxVolumeSlider != null)
        {
            float seVolume = 0.8f;
            if (SoundEffectManager.Instance != null)
            {
                seVolume = SoundEffectManager.Instance.GetVolume();
            }
            else
            {
                seVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
            }
            sfxVolumeSlider.SetValueWithoutNotify(seVolume);
        }

        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat("MasterVolume", 0.8f));
    }

    /// <summary>
    /// �X���C�_�[�̒l���X�V�i�O������Ăяo���\�j
    /// </summary>
    public void UpdateSliderValues(float bgmVolume, float seVolume, float masterVolume = -1f)
    {
        if (bgmVolumeSlider != null)
            bgmVolumeSlider.SetValueWithoutNotify(bgmVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(seVolume);

        if (masterVolume >= 0f && masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(masterVolume);
    }

    /// <summary>
    /// �N���b�N���Đ��iSoundEffectManager�o�R�j
    /// </summary>
    private void PlayClickSound()
    {
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlayClickSound();
        }
    }

    private void CloseSubPanels()
    {
        if (languagePanel != null)
            languagePanel.SetActive(false);

        if (soundPanel != null)
            soundPanel.SetActive(false);

        if (graphicsPanel != null)
            graphicsPanel.SetActive(false);

        if (resetConfirmationPanel != null)
            resetConfirmationPanel.SetActive(false);
    }

    private void OnLanguageButtonClicked()
    {
        PlayClickSound();
        CloseSubPanels();
        languagePanel.SetActive(true);
    }

    private void OnSoundButtonClicked()
    {
        PlayClickSound();
        CloseSubPanels();
        soundPanel.SetActive(true);
    }

    private void OnGraphicsButtonClicked()
    {
        PlayClickSound();
        CloseSubPanels();
        graphicsPanel.SetActive(true);
    }

    private void OnResetDataButtonClicked()
    {
        PlayClickSound();
        CloseSubPanels();
        resetConfirmationPanel.SetActive(true);
    }

    private void OnBackButtonClicked()
    {
        PlayClickSound();
        if (mainMenuController != null)
        {
            mainMenuController.ReturnToMainMenu();
        }
    }

    /// <summary>
    /// BGM���ʂ��ύX���ꂽ���̏���
    /// </summary>
    private void OnBgmVolumeChanged(float value)
    {
        // TitleSceneSettingsManager�ɒʒm
        if (titleSceneSettingsManager != null)
        {
            // ���݂�SE���ʂƃ}�X�^�[���ʂ��擾
            float currentBgmVolume, currentSeVolume;
            titleSceneSettingsManager.GetCurrentVolumeSettings(out currentBgmVolume, out currentSeVolume);
            float masterVolume = AudioListener.volume;

            // BGM���ʂ݂̂��X�V
            titleSceneSettingsManager.UpdateVolumeFromSettingsMenu(value, currentSeVolume, masterVolume);
        }
        else
        {
            // TitleSceneSettingsManager�������ꍇ�̒��ڍX�V�i�t�H�[���o�b�N�j
            if (mainMenuController != null)
            {
                var bgmSource = GameObject.Find("BGMAudioSource")?.GetComponent<AudioSource>();
                if (bgmSource != null)
                {
                    bgmSource.volume = value;
                }
            }

            PlayerPrefs.SetFloat("BGMVolume", value);
            PlayerPrefs.Save();
        }

        if (debugMode)
            Debug.Log($"BGM���ʕύX: {value}");
    }

    /// <summary>
    /// SE���ʂ��ύX���ꂽ���̏����iSoundEffectManager�o�R�j
    /// </summary>
    private void OnSfxVolumeChanged(float value)
    {
        // SoundEffectManager�ɒ��ډ��ʂ�ݒ�
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.SetVolume(value);
        }

        // TitleSceneSettingsManager�ɒʒm
        if (titleSceneSettingsManager != null)
        {
            // ���݂�BGM���ʂƃ}�X�^�[���ʂ��擾
            float currentBgmVolume, currentSeVolume;
            titleSceneSettingsManager.GetCurrentVolumeSettings(out currentBgmVolume, out currentSeVolume);
            float masterVolume = AudioListener.volume;

            // SE���ʂ݂̂��X�V
            titleSceneSettingsManager.UpdateVolumeFromSettingsMenu(currentBgmVolume, value, masterVolume);
        }
        else
        {
            // �t�H�[���o�b�N����
            PlayerPrefs.SetFloat("SFXVolume", value);
            PlayerPrefs.Save();
        }

        if (debugMode)
            Debug.Log($"SE���ʕύX: {value}");
    }

    /// <summary>
    /// �}�X�^�[���ʂ��ύX���ꂽ���̏���
    /// </summary>
    private void OnMasterVolumeChanged(float value)
    {
        // AudioListener�̉��ʂ𒼐ڐݒ�
        AudioListener.volume = value;

        // TitleSceneSettingsManager�ɒʒm
        if (titleSceneSettingsManager != null)
        {
            // ���݂�BGM��SE���ʂ��擾
            float currentBgmVolume, currentSeVolume;
            titleSceneSettingsManager.GetCurrentVolumeSettings(out currentBgmVolume, out currentSeVolume);

            // �}�X�^�[���ʂ݂̂��X�V
            titleSceneSettingsManager.UpdateVolumeFromSettingsMenu(currentBgmVolume, currentSeVolume, value);
        }
        else
        {
            // �t�H�[���o�b�N����
            PlayerPrefs.SetFloat("MasterVolume", value);
            PlayerPrefs.Save();
        }

        if (debugMode)
            Debug.Log($"�}�X�^�[���ʕύX: {value}");
    }
}