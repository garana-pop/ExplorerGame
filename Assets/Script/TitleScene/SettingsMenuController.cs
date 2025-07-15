using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsMenuController : MonoBehaviour
{
    [Header("設定カテゴリ")]
    [SerializeField] private Button languageButton;
    [SerializeField] private Button soundButton;
    [SerializeField] private Button graphicsButton;
    [SerializeField] private Button resetDataButton;
    [SerializeField] private Button backButton;

    [Header("パネル")]
    [SerializeField] private GameObject languagePanel;
    [SerializeField] private GameObject soundPanel;
    [SerializeField] private GameObject graphicsPanel;
    [SerializeField] private GameObject resetConfirmationPanel;

    [Header("言語設定")]
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private List<string> availableLanguages = new List<string>() { "日本語", "English" };

    [Header("サウンド設定")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider bgmVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("グラフィック設定")]
    [SerializeField] private TMP_Dropdown qualityDropdown;

    [Header("マネージャー参照")]
    private MainMenuController mainMenuController;
    private TitleSceneSettingsManager titleSceneSettingsManager;

    [Header("デバッグ")]
    [SerializeField] private bool debugMode = false;

    private void Start()
    {
        mainMenuController = GetComponentInParent<MainMenuController>();

        // パネルの初期化
        CloseSubPanels();

        // ボタンのリスナー設定
        SetupButtonListeners();

        // スライダーの初期設定
        SetupSliders();

        // 初期値をロード
        LoadInitialSettings();
    }

    /// <summary>
    /// TitleSceneSettingsManagerを設定
    /// </summary>
    public void SetTitleSceneSettingsManager(TitleSceneSettingsManager manager)
    {
        titleSceneSettingsManager = manager;

        // 現在の音量設定を取得して反映
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
        // BGMスライダーの設定
        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.minValue = 0f;
            bgmVolumeSlider.maxValue = 1f;
            bgmVolumeSlider.wholeNumbers = false;
            bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        }

        // SEスライダーの設定
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.minValue = 0f;
            sfxVolumeSlider.maxValue = 1f;
            sfxVolumeSlider.wholeNumbers = false;
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        // マスターボリュームスライダーの設定
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
        // 初期音量を設定
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
    /// スライダーの値を更新（外部から呼び出し可能）
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
    /// クリック音再生（SoundEffectManager経由）
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
    /// BGM音量が変更された時の処理
    /// </summary>
    private void OnBgmVolumeChanged(float value)
    {
        // TitleSceneSettingsManagerに通知
        if (titleSceneSettingsManager != null)
        {
            // 現在のSE音量とマスター音量を取得
            float currentBgmVolume, currentSeVolume;
            titleSceneSettingsManager.GetCurrentVolumeSettings(out currentBgmVolume, out currentSeVolume);
            float masterVolume = AudioListener.volume;

            // BGM音量のみを更新
            titleSceneSettingsManager.UpdateVolumeFromSettingsMenu(value, currentSeVolume, masterVolume);
        }
        else
        {
            // TitleSceneSettingsManagerが無い場合の直接更新（フォールバック）
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
            Debug.Log($"BGM音量変更: {value}");
    }

    /// <summary>
    /// SE音量が変更された時の処理（SoundEffectManager経由）
    /// </summary>
    private void OnSfxVolumeChanged(float value)
    {
        // SoundEffectManagerに直接音量を設定
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.SetVolume(value);
        }

        // TitleSceneSettingsManagerに通知
        if (titleSceneSettingsManager != null)
        {
            // 現在のBGM音量とマスター音量を取得
            float currentBgmVolume, currentSeVolume;
            titleSceneSettingsManager.GetCurrentVolumeSettings(out currentBgmVolume, out currentSeVolume);
            float masterVolume = AudioListener.volume;

            // SE音量のみを更新
            titleSceneSettingsManager.UpdateVolumeFromSettingsMenu(currentBgmVolume, value, masterVolume);
        }
        else
        {
            // フォールバック処理
            PlayerPrefs.SetFloat("SFXVolume", value);
            PlayerPrefs.Save();
        }

        if (debugMode)
            Debug.Log($"SE音量変更: {value}");
    }

    /// <summary>
    /// マスター音量が変更された時の処理
    /// </summary>
    private void OnMasterVolumeChanged(float value)
    {
        // AudioListenerの音量を直接設定
        AudioListener.volume = value;

        // TitleSceneSettingsManagerに通知
        if (titleSceneSettingsManager != null)
        {
            // 現在のBGMとSE音量を取得
            float currentBgmVolume, currentSeVolume;
            titleSceneSettingsManager.GetCurrentVolumeSettings(out currentBgmVolume, out currentSeVolume);

            // マスター音量のみを更新
            titleSceneSettingsManager.UpdateVolumeFromSettingsMenu(currentBgmVolume, currentSeVolume, value);
        }
        else
        {
            // フォールバック処理
            PlayerPrefs.SetFloat("MasterVolume", value);
            PlayerPrefs.Save();
        }

        if (debugMode)
            Debug.Log($"マスター音量変更: {value}");
    }
}