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

    [Header("�𑜓x�v���Z�b�g")]
    // 16:9�Œ�̉𑜓x�v���Z�b�g
    private readonly Vector2Int[] resolutionPresets = new Vector2Int[]
{
    new Vector2Int(1920, 1080),  // �t��HD
    new Vector2Int(1600, 900),   // HD+
    new Vector2Int(1280, 720),   // HD
    new Vector2Int(960, 540)     // ���T�C�Y
};

    [Header("�𑜓x�{�^��")]
    [SerializeField] private Button resolution1920x1080Button;
    [SerializeField] private Button resolution1600x900Button;
    [SerializeField] private Button resolution1280x720Button;
    [SerializeField] private Button resolution960x540Button;

    [Header("���ݐݒ肳��Ă���l�̐F�ݒ�")]
    [SerializeField] private Color optionalColor;

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

    private void SetupResolutionButtons()
    {
        // �𑜓x�{�^���̃��X�i�[�ݒ�
        if (resolution1920x1080Button != null)
            resolution1920x1080Button.onClick.AddListener(() => OnResolutionButtonClicked(0));

        if (resolution1600x900Button != null)
            resolution1600x900Button.onClick.AddListener(() => OnResolutionButtonClicked(1));

        if (resolution1280x720Button != null)
            resolution1280x720Button.onClick.AddListener(() => OnResolutionButtonClicked(2));

        if (resolution960x540Button != null)
            resolution960x540Button.onClick.AddListener(() => OnResolutionButtonClicked(3));
    }

    /// <summary>
    /// �𑜓x�{�^��������̏���
    /// </summary>
    private void OnResolutionButtonClicked(int resolutionIndex)
    {
        if (resolutionIndex < 0 || resolutionIndex >= resolutionPresets.Length)
        {
            Debug.LogError($"�����ȉ𑜓x�C���f�b�N�X: {resolutionIndex}");
            return;
        }

        Vector2Int selectedResolution = resolutionPresets[resolutionIndex];

        if (debugMode)
        {
            Debug.Log($"�𑜓x�I��: {selectedResolution.x}�~{selectedResolution.y}");
        }

        // ���j�^�[�𑜓x�`�F�b�N
        int adjustedIndex = CheckAndAdjustResolutionForMonitor(resolutionIndex);

        // �������K�v�������ꍇ
        if (adjustedIndex != resolutionIndex)
        {
            selectedResolution = resolutionPresets[adjustedIndex];
            resolutionIndex = adjustedIndex;

            if (debugMode)
            {
                Debug.LogWarning($"���j�^�[�𑜓x�����ɂ��A�𑜓x����������: {selectedResolution.x}�~{selectedResolution.y}");
            }
        }

        // �G���[�n���h�����O�Ɖ𑜓x�ύX����
        try
        {
            // �E�B���h�E���[�h�Œ�ŉ𑜓x��ύX
            Screen.SetResolution(selectedResolution.x, selectedResolution.y, false);

            // GameSaveManager�ɉ𑜓x�C���f�b�N�X��ۑ�
            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.SetResolutionIndex(resolutionIndex);
                GameSaveManager.Instance.SaveGame();
            }

            if (debugMode)
            {
                Debug.Log($"�𑜓x��ύX���܂���: {selectedResolution.x}�~{selectedResolution.y} (�E�B���h�E���[�h)");
            }

            // ���ʉ��Đ�
            if (SoundEffectManager.Instance != null)
            {
                SoundEffectManager.Instance.PlayClickSound();
            }
        }
        // ��O������
        catch (System.Exception ex)
        {
            Debug.LogError($"�𑜓x�ύX�G���[: {ex.Message}");

            // �t�H�[���o�b�N����: �f�t�H���g�𑜓x(1280�~720)�Ŏ��s
            try
            {
                Debug.LogWarning("�f�t�H���g�𑜓x(1280�~720)�Ƀt�H�[���o�b�N���܂�");

                // �E�B���h�E���[�h�Œ�ŉ𑜓x��1280�~720�ɐݒ�
                Screen.SetResolution(1280, 720, false);

                // �f�t�H���g�C���f�b�N�X��ۑ�
                if (GameSaveManager.Instance != null)
                {
                    GameSaveManager.Instance.SetResolutionIndex(2);
                    GameSaveManager.Instance.SaveGame();
                }
            }
            catch (System.Exception fallbackEx)
            {
                Debug.LogError($"�t�H�[���o�b�N�𑜓x�ύX�����s: {fallbackEx.Message}");
            }
        }

        // UI�X�V�����������x�点�Ď��s�i�𑜓x�ύX�����f���ꂽ��j
        StartCoroutine(DelayedUIUpdate());
    }

    /// <summary>
    /// �𑜓x�ύX���UI�X�V��x�����s
    /// </summary>
    private System.Collections.IEnumerator DelayedUIUpdate()
    {
        // 1�t���[���ҋ@���ĉ𑜓x�ύX���m���ɔ��f�����̂�҂�
        yield return null;

        // GraphicsPanel�\�����̌��݉𑜓x�m�F����
        UpdateGraphicsPanel();

        if (debugMode)
        {
            Debug.Log($"UI�X�V���� - ���݂̉𑜓x: {Screen.width}�~{Screen.height}");
        }
    }

    /// <summary>
    /// GraphicsPanel�̕\�����Ɍ��݂̉𑜓x���m�F���鏈��
    /// </summary>
    private void UpdateGraphicsPanel()
    {
        // ���݂̉𑜓x���擾
        int currentWidth = Screen.width;
        int currentHeight = Screen.height;

        if (debugMode)
        {
            Debug.Log($"���݂̉𑜓x: {currentWidth}�~{currentHeight}");
        }

        // ���݂̉𑜓x�ɑΉ�����{�^���̃e�L�X�g�F��ύX
        UpdateResolutionButtonTextColors(currentWidth, currentHeight);
    }

    /// <summary>
    /// ���݂̉𑜓x�ɑΉ�����{�^���̃e�L�X�g�F���X�V
    /// </summary>
    private void UpdateResolutionButtonTextColors(int width, int height)
    {
        // ���ׂẴ{�^���̃e�L�X�g�F�����Z�b�g
        ResetAllButtonTextColors();

        // ���݂̉𑜓x�Ɉ�v����{�^����T���ăe�L�X�g�F��ύX
        for (int i = 0; i < resolutionPresets.Length; i++)
        {
            if (resolutionPresets[i].x == width && resolutionPresets[i].y == height)
            {
                SetButtonTextColor(GetResolutionButton(i), optionalColor);

                if (debugMode)
                {
                    Debug.Log($"���݂̉𑜓x�{�^���̃e�L�X�g�F�ύX: {width}�~{height}");
                }

                break;
            }
        }
    }

    /// <summary>
    /// ���j�^�[�T�C�Y���`�F�b�N���A�K�v�ɉ����ĉ𑜓x�C���f�b�N�X�𒲐�
    /// </summary>
    private int CheckAndAdjustResolutionForMonitor(int desiredIndex)
    {
        // ���݂̃��j�^�[�𑜓x���擾
        int monitorWidth = Screen.currentResolution.width;
        int monitorHeight = Screen.currentResolution.height;

        if (debugMode)
        {
            Debug.Log($"���j�^�[�𑜓x: {monitorWidth}�~{monitorHeight}");
        }

        // �I�����ꂽ�𑜓x�����j�^�[�T�C�Y�𒴂��Ă��邩�m�F
        for (int i = desiredIndex; i < resolutionPresets.Length; i++)
        {
            Vector2Int resolution = resolutionPresets[i];

            // �^�X�N�o�[��E�B���h�E�g�̗]�T���l��
            if (resolution.x <= monitorWidth && resolution.y <= monitorHeight)
            {
                return i;
            }
        }

        // ���ׂẲ𑜓x�����j�^�[�T�C�Y�𒴂���ꍇ�͍ŏ��𑜓x��Ԃ�
        return resolutionPresets.Length - 1;
    }

    // ���ׂẴ{�^���̃e�L�X�g�F�����Z�b�g
    private void ResetAllButtonTextColors()
    {
        SetButtonTextColor(resolution1920x1080Button, Color.white);
        SetButtonTextColor(resolution1600x900Button, Color.white);
        SetButtonTextColor(resolution1280x720Button, Color.white);
        SetButtonTextColor(resolution960x540Button, Color.white);
    }

    // �{�^���̃e�L�X�g�F��ݒ�
    private void SetButtonTextColor(Button button, Color color)
    {
        if (button != null)
        {
            // �{�^���̎q�I�u�W�F�N�g����Text (TMP)��T��
            TextMeshProUGUI textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.color = color;
            }
            else if (debugMode)
            {
                Debug.LogWarning($"�{�^�� {button.name} ��TextMeshProUGUI�R���|�[�l���g��������܂���");
            }
        }
    }

    // �C���f�b�N�X����Ή�����{�^�����擾
    private Button GetResolutionButton(int index)
    {
        switch (index)
        {
            case 0:
                return resolution1920x1080Button;
            case 1:
                return resolution1600x900Button;
            case 2:
                return resolution1280x720Button;
            case 3:
                return resolution960x540Button;
            default:
                return null;
        }
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

    /// <summary>
    /// �{�^���̃��X�i�[�ݒ�
    /// </summary>
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

        // �𑜓x�{�^���̐ݒ�
        SetupResolutionButtons();
    }

    /// <summary>
    /// ���ʃX���C�_�[�ݒ�
    /// </summary>
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

    /// <summary>
    /// �����l�i�ă��[�h���j�̐ݒ�F���ʁE�𑜓x
    /// </summary>
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

        // �𑜓x��ݒ�F�ۑ����ꂽ�𑜓x�C���f�b�N�X���擾���ēK�p
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.SaveDataExists())
        {
            int savedResolutionIndex = GameSaveManager.Instance.GetResolutionIndex();

            // �ۑ����ꂽ�C���f�b�N�X���L���Ȕ͈͓����m�F
            if (savedResolutionIndex >= 0 && savedResolutionIndex < resolutionPresets.Length)
            {
                Vector2Int savedResolution = resolutionPresets[savedResolutionIndex];

                // ���݂̉𑜓x�ƈقȂ�ꍇ�̂ݕύX
                if (Screen.width != savedResolution.x || Screen.height != savedResolution.y)
                {
                    Screen.SetResolution(savedResolution.x, savedResolution.y, false);

                    if (debugMode)
                    {
                        Debug.Log($"�ۑ����ꂽ�𑜓x��K�p: {savedResolution.x}�~{savedResolution.y}");
                    }
                }

                // UI�X�V
                UpdateGraphicsPanel();
            }
        }
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

    /// <summary>
    /// �p�l���̏�����
    /// </summary>
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

        // GraphicsPanel���J�������Ɍ��݂̉𑜓x���m�F
        UpdateGraphicsPanel();

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