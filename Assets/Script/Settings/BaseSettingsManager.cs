using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// �ݒ��ʂ̊��N���X
/// �e�V�[���ŋ��ʂ̐ݒ�@�\���
/// </summary>
public abstract class BaseSettingsManager : MonoBehaviour
{
    [Header("�ݒ�p�l��")]
    [SerializeField] protected GameObject settingsPanel;
    [SerializeField] protected CanvasGroup panelCanvasGroup;
    [SerializeField] protected float fadeSpeed = 5f;

    [Header("���ʐݒ�")]
    [SerializeField] protected Slider bgmSlider;
    [SerializeField] protected Slider seSlider;
    [SerializeField] protected AudioSource bgmAudioSource;
    [SerializeField] protected AudioClip buttonClickSound;

    [Header("�{�^��")]
    [SerializeField] protected Button backButton;
    [SerializeField] protected Button saveAndQuitButton;

    // �ی삳�ꂽ�t�B�[���h�i�h���N���X����A�N�Z�X�\�j
    protected float currentBgmVolume = 0.5f;
    protected float currentSeVolume = 0.5f;
    protected bool isSettingsVisible = false;
    protected bool canOpenSettings = true;
    protected bool isInitialized = false;
    protected SoundEffectManager soundEffectManager;

    // �t�F�[�h�p�̃R���[�`���Q��
    private Coroutine fadeCoroutine;

    #region Unity ���C�t�T�C�N��

    protected virtual void Awake()
    {
        InitializeComponents();
    }

    protected virtual void Start()
    {
        InitializeServices();
        SetupButtonListeners();
        InitializeSliders();
        LoadSettings();
        SubscribeToEvents();
    }

    protected virtual void Update()
    {
        CheckForEscapeKeyPress();
    }

    protected virtual void OnDestroy()
    {
        UnsubscribeFromEvents();
        CleanupCoroutines();
    }

    #endregion

    #region ���������\�b�h

    /// <summary>
    /// �R���|�[�l���g�̏�����
    /// </summary>
    protected virtual void InitializeComponents()
    {
        // �ݒ�p�l������\���ł��邱�Ƃ��m�F
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            isSettingsVisible = false;
        }

        // CanvasGroup�̏�����
        if (panelCanvasGroup == null && settingsPanel != null)
        {
            panelCanvasGroup = settingsPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = settingsPanel.AddComponent<CanvasGroup>();
            }
        }
    }

    /// <summary>
    /// �T�[�r�X�̏�����
    /// </summary>
    protected virtual void InitializeServices()
    {
        soundEffectManager = SoundEffectManager.Instance;
        isInitialized = true;
    }

    /// <summary>
    /// �X���C�_�[�̏�����
    /// </summary>
    protected virtual void InitializeSliders()
    {
        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.wholeNumbers = false;
            bgmSlider.value = currentBgmVolume;
            bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        }

        if (seSlider != null)
        {
            seSlider.minValue = 0f;
            seSlider.maxValue = 1f;
            seSlider.wholeNumbers = false;
            seSlider.value = currentSeVolume;
            seSlider.onValueChanged.AddListener(OnSeVolumeChanged);
        }
    }

    /// <summary>
    /// �{�^���C�x���g�̐ݒ�
    /// </summary>
    protected virtual void SetupButtonListeners()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(HideSettings);
        }

        if (saveAndQuitButton != null)
        {
            saveAndQuitButton.onClick.AddListener(SaveAndQuit);
        }
    }

    /// <summary>
    /// �C�x���g���X�i�[�̓o�^
    /// </summary>
    protected virtual void SubscribeToEvents()
    {
        if (soundEffectManager != null)
        {
            soundEffectManager.OnVolumeChanged += OnSoundEffectVolumeChanged;
        }
    }

    #endregion

    #region �ݒ�̓ǂݍ��݂ƕۑ�

    /// <summary>
    /// �ۑ����ꂽ�ݒ�l��ǂݍ���
    /// </summary>
    protected virtual void LoadSettings()
    {
        // SE���ʂ�SoundEffectManager����擾
        if (soundEffectManager != null)
        {
            currentSeVolume = soundEffectManager.GetVolume();
        }
        else
        {
            currentSeVolume = 0.5f;
        }

        // BGM���ʂ̓f�t�H���g�l���g�p�i�h���N���X�ŃI�[�o�[���C�h�j
        currentBgmVolume = 0.5f;

        // AudioSource�ɉ��ʂ�K�p
        ApplyVolumeSettings();

        // �X���C�_�[�̒l���X�V
        UpdateSliderValues();
    }

    /// <summary>
    /// �f�t�H���g�ݒ��ǂݍ���
    /// </summary>
    protected virtual void LoadDefaultSettings()
    {
        currentBgmVolume = 0.5f;

        // SE���ʂ�SoundEffectManager����擾
        if (soundEffectManager != null)
        {
            currentSeVolume = soundEffectManager.GetVolume();
        }
        else
        {
            currentSeVolume = 0.5f;
        }
    }

    /// <summary>
    /// �ݒ��ۑ�
    /// </summary>
    protected virtual void SaveSettings()
    {
        // SE���ʂ�SoundEffectManager�ɔC����
        if (soundEffectManager != null)
        {
            soundEffectManager.SetVolume(currentSeVolume);
            soundEffectManager.SaveVolumeSettingsWithGameSaveManager();
        }

        // BGM���ʂ͔h���N���X�Ŏ���
    }

    #endregion

    #region �ݒ�p�l���̕\������

    /// <summary>
    /// �ݒ��ʂ̕\��/��\����؂�ւ�
    /// </summary>
    public virtual void ToggleSettings()
    {
        if (!canOpenSettings)
        {
            return;
        }

        if (isSettingsVisible)
        {
            HideSettings();
        }
        else
        {
            ShowSettings();
        }
    }

    /// <summary>
    /// �ݒ��ʂ�\��
    /// </summary>
    public virtual void ShowSettings()
    {
        if (!isInitialized || settingsPanel == null || isSettingsVisible || !canOpenSettings)
            return;

        settingsPanel.SetActive(true);
        isSettingsVisible = true;

        // �t�F�[�h�C������
        StartFadeIn();

        // �\�����ɍŐV�̐ݒ�l���X���C�_�[�ɔ��f
        UpdateSliderValues();

        // �V�[���ŗL�̏���
        OnSettingsOpened();
    }

    /// <summary>
    /// �ݒ��ʂ��\��
    /// </summary>
    public virtual void HideSettings()
    {
        if (!isInitialized || settingsPanel == null || !isSettingsVisible)
            return;

        // �ݒ��ۑ�
        SaveSettings();

        // �t�F�[�h�A�E�g����
        StartFadeOut();

        // �V�[���ŗL�̏���
        OnSettingsClosed();
    }

    /// <summary>
    /// �ݒ��ʂ��J���ꂽ���̏���
    /// </summary>
    protected virtual void OnSettingsOpened()
    {
        // �h���N���X�ŃI�[�o�[���C�h
    }

    /// <summary>
    /// �ݒ��ʂ�����ꂽ���̏���
    /// </summary>
    protected virtual void OnSettingsClosed()
    {
        // �h���N���X�ŃI�[�o�[���C�h
    }

    #endregion

    #region �t�F�[�h����

    /// <summary>
    /// �t�F�[�h�C���J�n
    /// </summary>
    protected virtual void StartFadeIn()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeIn());
    }

    /// <summary>
    /// �t�F�[�h�A�E�g�J�n
    /// </summary>
    protected virtual void StartFadeOut()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeOut());
    }

    /// <summary>
    /// �t�F�[�h�C������
    /// </summary>
    protected virtual IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        float duration = 1f / fadeSpeed;

        panelCanvasGroup.alpha = 0f;

        while (elapsedTime < duration)
        {
            panelCanvasGroup.alpha = Mathf.Clamp01(elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        panelCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// �t�F�[�h�A�E�g����
    /// </summary>
    protected virtual IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        float duration = 1f / fadeSpeed;

        panelCanvasGroup.alpha = 1f;

        while (elapsedTime < duration)
        {
            panelCanvasGroup.alpha = Mathf.Clamp01(1f - (elapsedTime / duration));
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        panelCanvasGroup.alpha = 0f;
        settingsPanel.SetActive(false);
        isSettingsVisible = false;
    }

    #endregion

    #region ���ʕύX����

    /// <summary>
    /// BGM���ʂ��ύX���ꂽ���̏���
    /// </summary>
    protected virtual void OnBgmVolumeChanged(float volume)
    {
        currentBgmVolume = volume;

        // BGM�̉��ʂ𑦎����f
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = volume;
        }
    }

    /// <summary>
    /// SE���ʂ��ύX���ꂽ���̏���
    /// </summary>
    protected virtual void OnSeVolumeChanged(float volume)
    {
        currentSeVolume = volume;

        // SoundEffectManager�ɉ��ʂ�K�p
        if (soundEffectManager != null)
        {
            soundEffectManager.SetVolume(volume);
        }
    }

    /// <summary>
    /// SoundEffectManager����̉��ʕύX�ʒm���󂯎��n���h��
    /// </summary>
    protected virtual void OnSoundEffectVolumeChanged(float newVolume)
    {
        // �X���C�_�[�̒l���X�V�i�������[�v��h�����ߍ���������Ƃ��̂ݍX�V�j
        if (seSlider != null && !Mathf.Approximately(seSlider.value, newVolume))
        {
            seSlider.value = newVolume;
            currentSeVolume = newVolume;
        }
    }

    /// <summary>
    /// ���ʂ�AudioSource�ɓK�p
    /// </summary>
    protected virtual void ApplyVolumeSettings()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = currentBgmVolume;
        }
    }

    /// <summary>
    /// �X���C�_�[�̒l���X�V
    /// </summary>
    protected virtual void UpdateSliderValues()
    {
        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(currentBgmVolume);
        }

        if (seSlider != null)
        {
            seSlider.SetValueWithoutNotify(currentSeVolume);
        }
    }

    #endregion

    #region ���[�e�B���e�B���\�b�h

    /// <summary>
    /// ESC�L�[���͂̃`�F�b�N
    /// </summary>
    protected virtual void CheckForEscapeKeyPress()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
        }
    }

    /// <summary>
    /// �N���b�N�����Đ�
    /// </summary>
    protected virtual void PlayButtonClickSound()
    {
        if (soundEffectManager != null)
        {
            soundEffectManager.PlayClickSound();
        }
        else if (buttonClickSound != null)
        {
            // AudioSource���Ȃ��ꍇ�͍쐬���čĐ�
            AudioSource.PlayClipAtPoint(buttonClickSound, Camera.main.transform.position, currentSeVolume);
        }
    }

    /// <summary>
    /// �Z�[�u���ďI��
    /// </summary>
    protected virtual void SaveAndQuit()
    {
        // �ݒ��ۑ�
        SaveSettings();

        // �Q�[����Ԃ̕ۑ�
        SaveGameState();

        // �A�v���P�[�V�������I��
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// �Q�[���̏�Ԃ�ۑ����郁�\�b�h
    /// </summary>
    protected virtual void SaveGameState()
    {
        // GameSaveManager���g�p���ăQ�[����Ԃ�ۑ�
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.SaveGame();
        }
    }

    /// <summary>
    /// �ݒ��ʂ��J���邩�ǂ�����ݒ�
    /// </summary>
    public virtual void SetCanOpenSettings(bool canOpen)
    {
        canOpenSettings = canOpen;
    }

    #endregion

    #region �N���[���A�b�v���\�b�h

    /// <summary>
    /// �C�x���g���X�i�[�̓o�^����
    /// </summary>
    protected virtual void UnsubscribeFromEvents()
    {
        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
        }

        if (seSlider != null)
        {
            seSlider.onValueChanged.RemoveListener(OnSeVolumeChanged);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(HideSettings);
        }

        if (saveAndQuitButton != null)
        {
            saveAndQuitButton.onClick.RemoveListener(SaveAndQuit);
        }

        if (soundEffectManager != null)
        {
            soundEffectManager.OnVolumeChanged -= OnSoundEffectVolumeChanged;
        }
    }

    /// <summary>
    /// �A�N�e�B�u�ȃR���[�`�����~
    /// </summary>
    protected virtual void CleanupCoroutines()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    #endregion
}