using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

/// <summary>
/// MainScene�̐ݒ胁�j���[�𐧌䂷��}�l�[�W���[�N���X
/// </summary>
public class MainSceneSettingsManager : MonoBehaviour, ISettingsManager
{
    #region SerializeField�ƃv���p�e�B

    [Header("�ݒ�p�l��")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private Canvas draggingCanvas;

    [Header("���ʐݒ�")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip buttonClickSound;

    [Header("�{�^��")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button saveAndQuitButton;

    [Header("�K�w�Ǘ�")]
    [SerializeField] private HierarchyOrderManager hierarchyManager;

    // �v���C�x�[�g�ϐ�
    private float currentBgmVolume;
    private float currentSeVolume;
    private bool isSettingsVisible = false;
    private Coroutine fadeCoroutine;
    private Transform originalParent;
    private SoundEffectManager soundEffectManager;
    private bool isInitialized = false;

    // �L���b�V�������R���|�[�l���g�Q�Ƃ�ۑ�
    private ISettingsService settingsService;

    #endregion

    #region Unity ���C�t�T�C�N�����\�b�h

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        InitializeServices();
        LoadSettingsFromGameSave();
        InitializeSliders();
        SetupButtonListeners();
        SubscribeToEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        CleanupCoroutines();
    }

    private void Update()
    {
        CheckForEscapeKeyPress();
    }

    #endregion

    #region ���������\�b�h

    /// <summary>
    /// �R���|�[�l���g�̏�����
    /// </summary>
    private void InitializeComponents()
    {
        // �ݒ�p�l���̏�����
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            originalParent = settingsPanel.transform.parent;
        }

        // CanvasGroup�̏�����
        InitializeCanvasGroup();

        // DraggingCanvas�̏�����
        InitializeDraggingCanvas();
    }

    private void InitializeCanvasGroup()
    {
        if (panelCanvasGroup == null && settingsPanel != null)
        {
            panelCanvasGroup = settingsPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = settingsPanel.AddComponent<CanvasGroup>();
            }
        }
    }

    private void InitializeDraggingCanvas()
    {
        if (draggingCanvas == null)
        {
            draggingCanvas = GameObject.Find("DraggingCanvas")?.GetComponent<Canvas>();
            if (draggingCanvas == null)
            {
                Debug.LogWarning("DraggingCanvas��������܂���B�ݒ�p�l���̍őO�ʕ\�����@�\���Ȃ��\��������܂��B");
            }
        }
    }

    /// <summary>
    /// �T�[�r�X�̏�����
    /// </summary>
    private void InitializeServices()
    {
        // �V���O���g���̑���ɃT�[�r�X���P�[�^�[�p�^�[�����g�p
        // ���ۂ̎����ł�DI�R���e�i�Ȃǂ��g�����Ƃ��D�܂���
        soundEffectManager = SoundEffectManager.Instance;
        settingsService = new SettingsService();

        // �K�w�Ǘ��̏�����
        if (hierarchyManager == null)
        {
            hierarchyManager = FindAnyObjectByType<HierarchyOrderManager>();
        }

        isInitialized = true;
    }

    /// <summary>
    /// �X���C�_�[�̏�����
    /// </summary>
    private void InitializeSliders()
    {
        if (bgmSlider != null)
        {
            bgmSlider.value = currentBgmVolume;
            bgmSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        }

        if (seSlider != null)
        {
            seSlider.value = currentSeVolume;
            seSlider.onValueChanged.AddListener(OnSeVolumeChanged);
        }
    }

    /// <summary>
    /// �{�^���C�x���g�̐ݒ�
    /// </summary>
    private void SetupButtonListeners()
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
    private void SubscribeToEvents()
    {
        if (soundEffectManager != null)
        {
            soundEffectManager.OnVolumeChanged += OnSoundEffectVolumeChanged;
        }
    }

    #endregion

    #region �N���[���A�b�v���\�b�h

    /// <summary>
    /// �C�x���g���X�i�[�̓o�^����
    /// </summary>
    private void UnsubscribeFromEvents()
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
    private void CleanupCoroutines()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }

    #endregion

    #region ���͏���

    /// <summary>
    /// ESC�L�[���͂̃`�F�b�N
    /// </summary>
    private void CheckForEscapeKeyPress()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
        }
    }

    #endregion

    #region �ݒ�̓ǂݍ��݂ƕۑ�

    /// <summary>
    /// �ۑ����ꂽ�ݒ�l��ǂݍ���
    /// </summary>
    public void LoadSettings()
    {
        // BGM���ʂ�ǂݍ���
        currentBgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);

        // SE���� - SoundEffectManager����擾
        if (soundEffectManager != null)
        {
            currentSeVolume = soundEffectManager.GetVolume();
        }
        else
        {
            currentSeVolume = PlayerPrefs.GetFloat("SEVolume", 0.5f);
        }

        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1.0f);
        AudioListener.volume = masterVolume;

        // AudioSource�ɉ��ʂ�K�p
        ApplyVolumeSettings();

        // �X���C�_�[�̒l���X�V
        UpdateSliderValues();
    }

    /// <summary>
    /// �ǂݍ��񂾉��ʐݒ��AudioSource�ɓK�p
    /// </summary>
    private void ApplyVolumeSettings()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = currentBgmVolume;
        }

        if (soundEffectManager != null)
        {
            soundEffectManager.SetVolume(currentSeVolume);
        }
        else if (sfxAudioSource != null)
        {
            sfxAudioSource.volume = currentSeVolume;
        }
    }

    /// <summary>
    /// �X���C�_�[�̒l���X�V
    /// </summary>
    private void UpdateSliderValues()
    {
        if (bgmSlider != null)
        {
            bgmSlider.value = currentBgmVolume;
        }

        if (seSlider != null)
        {
            seSlider.value = currentSeVolume;
        }
    }

    #endregion

    #region �ݒ�p�l���̕\������

    /// <summary>
    /// �ݒ��ʂ̕\��/��\����؂�ւ�
    /// </summary>
    public void ToggleSettings()
    {
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
    public void ShowSettings()
    {
        if (!isInitialized || settingsPanel == null || isSettingsVisible)
            return;

        // �K�w�����𒲐�
        AdjustHierarchyForSettings();

        // �ݒ�p�l����DraggingCanvas�Ɉړ��i�őO�ʂɕ\�����邽�߁j
        MoveSettingsPanelToFront();

        settingsPanel.SetActive(true);
        isSettingsVisible = true;

        // �t�F�[�h�C������
        StartFadeIn();

        // �\�����ɍŐV�̐ݒ�l���X���C�_�[�ɔ��f
        UpdateSliderValues();
    }

    /// <summary>
    /// �ݒ��ʂ��\��
    /// </summary>
    public void HideSettings()
    {
        if (settingsPanel == null || !isSettingsVisible)
            return;

        // �ݒ��ۑ�
        SaveSettings();

        // �t�F�[�h�A�E�g����
        StartFadeOut();

        // HierarchyOrderManager�ɂ��ʒm���āAOverlay���\����
        if (hierarchyManager != null)
        {
            hierarchyManager.OnSettingsClosed();
        }

        // ���ʉ����Đ��i�ݒ����鉹�j
        PlayButtonClickSound();
    }

    /// <summary>
    /// �K�w������ݒ�\���p�ɒ���
    /// </summary>
    private void AdjustHierarchyForSettings()
    {
        if (hierarchyManager != null)
        {
            hierarchyManager.AdjustHierarchyOrderForSettings();
        }
    }

    /// <summary>
    /// �ݒ�p�l�����őO�ʂɈړ�
    /// </summary>
    private void MoveSettingsPanelToFront()
    {
        if (draggingCanvas != null)
        {
            // ���݂̐e�����̐e�ƈقȂ�ꍇ�́A���̐e��ۑ����Ȃ��悤�ɂ���
            if (settingsPanel.transform.parent == originalParent)
            {
                originalParent = settingsPanel.transform.parent;
            }

            // DraggingCanvas�Ɉړ�
            settingsPanel.transform.SetParent(draggingCanvas.transform, false);

            // ��ʒ����ɔz�u
            CenterSettingsPanel();
        }
    }

    /// <summary>
    /// �ݒ�p�l������ʒ����ɔz�u
    /// </summary>
    private void CenterSettingsPanel()
    {
        RectTransform panelRect = settingsPanel.GetComponent<RectTransform>();
        if (panelRect != null)
        {
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    #endregion

    #region �t�F�[�h�A�j���[�V����

    /// <summary>
    /// �t�F�[�h�C���������J�n
    /// </summary>
    private void StartFadeIn()
    {
        // ���s���̃t�F�[�h���~
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeIn());
    }

    /// <summary>
    /// �t�F�[�h�A�E�g�������J�n
    /// </summary>
    private void StartFadeOut()
    {
        // ���s���̃t�F�[�h���~
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeOut());
    }

    /// <summary>
    /// �t�F�[�h�C������
    /// </summary>
    private IEnumerator FadeIn()
    {
        if (panelCanvasGroup == null)
            yield break;

        panelCanvasGroup.alpha = 0f;

        float elapsedTime = 0f;
        float duration = 1f / fadeSpeed; // fadeDuration = 1/fadeSpeed for consistent timing

        while (elapsedTime < duration)
        {
            panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        panelCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// �t�F�[�h�A�E�g����
    /// </summary>
    private IEnumerator FadeOut()
    {
        if (panelCanvasGroup == null)
        {
            settingsPanel.SetActive(false);
            isSettingsVisible = false;
            RestoreSettingsPanelParent();
            yield break;
        }

        float elapsedTime = 0f;
        float duration = 1f / fadeSpeed; // fadeDuration = 1/fadeSpeed for consistent timing

        while (elapsedTime < duration)
        {
            panelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        panelCanvasGroup.alpha = 0f;
        settingsPanel.SetActive(false);
        isSettingsVisible = false;

        // ���̐e�ɖ߂�
        RestoreSettingsPanelParent();
    }

    /// <summary>
    /// �ݒ�p�l�������̐e�ɖ߂�
    /// </summary>
    private void RestoreSettingsPanelParent()
    {
        if (originalParent != null)
        {
            settingsPanel.transform.SetParent(originalParent, false);
        }
    }

    #endregion

    #region ���ʕύX����

    /// <summary>
    /// BGM���ʂ��ύX���ꂽ�Ƃ��̏���
    /// </summary>
    private void OnBgmVolumeChanged(float volume)
    {
        currentBgmVolume = volume;

        // BGM�̉��ʂ𑦎����f
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = volume;
        }

        // PlayerPrefs�ɕۑ��i�݊����̂��߁j
        PlayerPrefs.SetFloat("BGMVolume", volume);
        PlayerPrefs.Save();

        // game_save.json�ɕۑ�
        SaveVolumeToGameSave();
    }

    /// <summary>
    /// SE���ʂ��ύX���ꂽ�Ƃ��̏���
    /// </summary>
    private void OnSeVolumeChanged(float volume)
    {
        currentSeVolume = volume;

        // SoundEffectManager�ɉ��ʂ�K�p
        if (soundEffectManager != null)
        {
            soundEffectManager.SetVolume(volume);
        }
        else
        {
            // SoundEffectManager���Ȃ��ꍇ�̑�֏���
            if (sfxAudioSource != null)
            {
                sfxAudioSource.volume = volume;
            }
        }

        // PlayerPrefs�ɕۑ��i�݊����̂��߁j
        PlayerPrefs.SetFloat("SEVolume", volume);
        PlayerPrefs.Save();

        // game_save.json�ɕۑ�
        SaveVolumeToGameSave();
    }

    /// <summary>
    /// SoundEffectManager����̉��ʕύX�ʒm���󂯎��n���h��
    /// </summary>
    private void OnSoundEffectVolumeChanged(float newVolume)
    {
        // �X���C�_�[�̒l���X�V�i�������[�v��h�����ߍ���������Ƃ��̂ݍX�V�j
        if (seSlider != null && !Mathf.Approximately(seSlider.value, newVolume))
        {
            seSlider.value = newVolume;
            currentSeVolume = newVolume;
        }
    }

    #endregion

    #region ���[�e�B���e�B���\�b�h

    /// <summary>
    /// �N���b�N�����Đ�
    /// </summary>
    private void PlayButtonClickSound()
    {
        if (soundEffectManager != null)
        {
            soundEffectManager.PlayClickSound();
        }
        else if (sfxAudioSource != null && buttonClickSound != null)
        {
            sfxAudioSource.PlayOneShot(buttonClickSound, currentSeVolume);
        }
    }

    /// <summary>
    /// game_save.json���特�ʐݒ��ǂݍ���
    /// </summary>
    private void LoadSettingsFromGameSave()
    {
        // GameSaveManager���特�ʐݒ��ǂݍ���
        if (GameSaveManager.Instance != null)
        {
            var saveData = GameSaveManager.Instance.GetCurrentSaveData();
            if (saveData?.audioSettings != null)
            {
                currentBgmVolume = saveData.audioSettings.bgmVolume;
                currentSeVolume = saveData.audioSettings.seVolume;

                // masterVolume���ǂݍ���
                if (saveData.audioSettings.masterVolume > 0)
                {
                    AudioListener.volume = saveData.audioSettings.masterVolume;
                }

                // ���ʂ�K�p
                ApplyVolumeSettings();
                return;
            }
            else
            {
                Debug.Log("LoadSettingsFromGameSave: game_save.json��audioSettings�����݂��܂���");
            }
        }
        else
        {
            Debug.LogWarning("LoadSettingsFromGameSave: GameSaveManager.Instance��null�ł�");
        }

        // game_save.json�Ƀf�[�^���Ȃ��ꍇ��PlayerPrefs����ǂݍ���
        Debug.Log("LoadSettingsFromGameSave: PlayerPrefs����ǂݍ��݂܂�");
        LoadSettings();
    }

    /// <summary>
    /// ���ʐݒ��game_save.json�ɕۑ�
    /// </summary>
    private void SaveVolumeToGameSave()
    {
        if (GameSaveManager.Instance != null)
        {
            float masterVolume = AudioListener.volume;
            GameSaveManager.Instance.UpdateAudioSettings(currentBgmVolume, currentSeVolume, masterVolume);
            GameSaveManager.Instance.SaveAudioSettingsOnly();
        }
        else
        {
            Debug.LogWarning("SaveVolumeToGameSave: GameSaveManager.Instance��null�ł�");
        }
    }

    /// <summary>
    /// �ݒ��ۑ�
    /// </summary>
    // SaveSettings���\�b�h���C��
    private void SaveSettings()
    {
        // BGM���ʂ�ۑ�
        PlayerPrefs.SetFloat("BGMVolume", currentBgmVolume);

        // SE���ʂ�SoundEffectManager�ɔC����
        if (soundEffectManager != null)
        {
            soundEffectManager.SetVolume(currentSeVolume);
            // GameSaveManager�Ƃ̘A�g���܂߂��ۑ�
            soundEffectManager.SaveVolumeSettingsWithGameSaveManager(); // soundManager����soundEffectManager�ɏC��
        }
        else
        {
            // ����PlayerPrefs�ɕۑ�
            PlayerPrefs.SetFloat("SEVolume", currentSeVolume);
        }

        // �m���ɕۑ�
        PlayerPrefs.Save();
    }

    /// <summary>
    /// �Z�[�u���ďI��
    /// </summary>
    private void SaveAndQuit()
    {
        // �ݒ��ۑ�
        SaveSettings();

        // �K�v�ɉ����ăQ�[���̏�Ԃ�����ɕۑ�
        SaveGameState();

        // �A�v���P�[�V�������I��
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// �Q�[���̏�Ԃ�ۑ����郁�\�b�h�i�K�v�ɉ����Ď����j
    /// </summary>
    private void SaveGameState()
    {
        // �Q�[����Ԃ̕ۑ��iGameSaveManager���g�p�j
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.SaveGame();
            Debug.Log("�Q�[���̏�Ԃ�ۑ����܂���");
        }
        else
        {
            Debug.LogWarning("GameSaveManager��������܂���B�Q�[���̏�Ԃ�ۑ��ł��܂���ł����B");
        }
    }

    #endregion
}

/// <summary>
/// �e�X�g�e�Ր��̂��߂̃C���^�[�t�F�[�X
/// </summary>
public interface ISettingsManager
{
    void LoadSettings();
    void ToggleSettings();
    void ShowSettings();
    void HideSettings();
}

/// <summary>
/// �ݒ�T�[�r�X�̃C���^�[�t�F�[�X
/// </summary>
public interface ISettingsService
{
    float GetBgmVolume();
    float GetSeVolume();
    void SaveBgmVolume(float volume);
    void SaveSeVolume(float volume);
}

/// <summary>
/// �ݒ�T�[�r�X�̎���
/// </summary>
public class SettingsService : ISettingsService
{
    public float GetBgmVolume() => PlayerPrefs.GetFloat("BGMVolume", 0.5f);
    public float GetSeVolume() => PlayerPrefs.GetFloat("SEVolume", 0.5f);

    public void SaveBgmVolume(float volume) => PlayerPrefs.SetFloat("BGMVolume", volume);
    public void SaveSeVolume(float volume) => PlayerPrefs.SetFloat("SEVolume", volume);
}