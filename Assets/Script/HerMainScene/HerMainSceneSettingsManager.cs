using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HerMainScene��p�̐ݒ�Ǘ��N���X
/// BaseSettingsManager���p�����ĉ��ʐݒ�̘A���@�\������
/// </summary>
public class HerMainSceneSettingsManager : MonoBehaviour
{
    [Header("�ݒ�p�l��")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private Canvas draggingCanvas;

    [Header("���ʐݒ�")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioClip buttonClickSound;

    [Header("�{�^��")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button saveAndQuitButton;

    [Header("�K�w�Ǘ�")]
    [SerializeField] private HierarchyOrderManager hierarchyManager;

    [Header("����G����ݒ�")]
    [Tooltip("����G�\������ESC�L�[�ł̐ݒ��ʕ\���𖳌������邩")]
    [SerializeField] private bool disableEscapeWhenPortraitOpen = true;

    [Tooltip("����G�t�@�C���p�l���ւ̎Q��")]
    [SerializeField] private GameObject portraitFilePanel;

    [Tooltip("�f�o�b�O���O��\�����邩")]
    [SerializeField] private bool debugMode = false;

    // �ی삳�ꂽ�t�B�[���h
    private float currentBgmVolume = 0.5f;
    private float currentSeVolume = 0.5f;
    private bool isSettingsVisible = false;
    private bool canOpenSettings = true;
    private bool isInitialized = false;
    private SoundEffectManager soundEffectManager;
    private Transform originalParent;

    // �t�F�[�h�p�̃R���[�`���Q��
    private Coroutine fadeCoroutine;

    #region Unity ���C�t�T�C�N��

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        InitializeServices();
        SetupButtonListeners();
        InitializeSliders();
        LoadAndApplyVolumeSettings();
        SubscribeToEvents();
    }

    private void Update()
    {
        CheckForEscapeKeyPress();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
        CleanupCoroutines();
    }

    #endregion

    #region ���������\�b�h

    /// <summary>
    /// �R���|�[�l���g�̏�����
    /// </summary>
    private void InitializeComponents()
    {
        // �ݒ�p�l������\���ł��邱�Ƃ��m�F
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            isSettingsVisible = false;
            originalParent = settingsPanel.transform.parent;
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

        // DraggingCanvas�̏�����
        InitializeDraggingCanvas();

        // HierarchyOrderManager�̏�����
        InitializeHierarchyManager();

        // ����G�p�l���̏�������
        if (portraitFilePanel == null && disableEscapeWhenPortraitOpen)
        {
            portraitFilePanel = GameObject.Find("����G.FilePanel");

            if (portraitFilePanel != null && debugMode)
            {
                Debug.Log("HerMainSceneSettingsManager: ����G.FilePanel���������o���܂���");
            }
        }
    }

    /// <summary>
    /// DraggingCanvas�̏�����
    /// </summary>
    private void InitializeDraggingCanvas()
    {
        if (draggingCanvas == null)
        {
            GameObject draggingCanvasObj = GameObject.Find("DraggingCanvas");
            if (draggingCanvasObj != null)
            {
                draggingCanvas = draggingCanvasObj.GetComponent<Canvas>();
            }
        }
    }

    /// <summary>
    /// HierarchyOrderManager�̏�����
    /// </summary>
    private void InitializeHierarchyManager()
    {
        if (hierarchyManager == null)
        {
            hierarchyManager = FindFirstObjectByType<HierarchyOrderManager>();
        }
    }

    /// <summary>
    /// �T�[�r�X�̏�����
    /// </summary>
    private void InitializeServices()
    {
        // SoundEffectManager�̎擾�����P
        StartCoroutine(InitializeSoundEffectManager());

        // �K�w�Ǘ��̎擾
        if (hierarchyManager == null)
        {
            hierarchyManager = FindFirstObjectByType<HierarchyOrderManager>(FindObjectsInactive.Include);
        }

        isInitialized = true;
        canOpenSettings = true;
    }

    private IEnumerator InitializeSoundEffectManager()
    {
        // �t���[���ҋ@����SoundEffectManager�̏��������m���ɂ���
        yield return null;

        soundEffectManager = SoundEffectManager.Instance;

        if (soundEffectManager == null)
        {
            // �t�H�[���o�b�N: SoundEffectManager��T��
            soundEffectManager = FindFirstObjectByType<SoundEffectManager>(FindObjectsInactive.Include);
        }

        // SoundEffectManager���擾�ł����ꍇ�A���݂�SE���ʂ�K�p
        if (soundEffectManager != null)
        {
            soundEffectManager.SetVolume(currentSeVolume);

            // �C�x���g�̍w��
            soundEffectManager.OnVolumeChanged += OnSoundEffectVolumeChanged;
        }
    }


    /// <summary>
    /// �X���C�_�[�̏�����
    /// </summary>
    private void InitializeSliders()
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

    #region ���ʐݒ�̓ǂݍ��݂ƓK�p

    /// <summary>
    /// game_save.json���特�ʐݒ��ǂݍ���œK�p
    /// </summary>
    private void LoadAndApplyVolumeSettings()
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

                // �ݒ��K�p
                ApplyVolumeSettings();
                UpdateSliderValues();

                // PlayerPrefs�ɂ��ۑ��iBGM/SE���ʊǗ��̂ݎg�p���j
                PlayerPrefs.SetFloat("BGMVolume", currentBgmVolume);
                PlayerPrefs.SetFloat("SEVolume", currentSeVolume);
                PlayerPrefs.Save();

                return;
            }
        }

        // game_save.json�ɐݒ肪�Ȃ��ꍇ�̓f�t�H���g�l���g�p
        LoadDefaultSettings();
    }

    /// <summary>
    /// �f�t�H���g�ݒ��ǂݍ���
    /// </summary>
    private void LoadDefaultSettings()
    {
        currentBgmVolume = 0.5f;
        currentSeVolume = 0.5f;

        ApplyVolumeSettings();
        UpdateSliderValues();
    }

    /// <summary>
    /// ���ʂ�AudioSource�ɓK�p
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
    }

    /// <summary>
    /// �X���C�_�[�̒l���X�V
    /// </summary>
    private void UpdateSliderValues()
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

    #region �ݒ�p�l���̕\��/��\��

    /// <summary>
    /// �ݒ��ʂ̕\���؂�ւ�
    /// </summary>
    public void ToggleSettings()
    {
        if (!canOpenSettings || !isInitialized)
            return;

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
        if (!canOpenSettings || !isInitialized || settingsPanel == null)
            return;

        // DraggingCanvas�ɐݒ�p�l�����ړ�
        MoveSettingsPanelToDraggingCanvas();

        settingsPanel.SetActive(true);

        // HierarchyOrderManager�ŊK�w�����𒲐�
        if (hierarchyManager != null)
        {
            hierarchyManager.AdjustHierarchyOrderForSettings();
            hierarchyManager.SetOverlayActive(true);
        }

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeInSettings());

        // �{�^���N���b�N�����Đ�
        PlayButtonClickSound();
    }

    /// <summary>
    /// �ݒ��ʂ��\��
    /// </summary>
    public void HideSettings()
    {
        if (settingsPanel == null || !isSettingsVisible)
            return;

        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeOutSettings());

        // �ݒ��ۑ�
        SaveSettings();

        // �{�^���N���b�N�����Đ�
        PlayButtonClickSound();
    }

    /// <summary>
    /// �ݒ�p�l����DraggingCanvas�Ɉړ�
    /// </summary>
    private void MoveSettingsPanelToDraggingCanvas()
    {
        if (draggingCanvas != null && settingsPanel != null)
        {
            // ���̐e��ۑ�
            if (originalParent == null)
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
    /// �ݒ�p�l�������̐e�ɖ߂�
    /// </summary>
    private void RestoreSettingsPanelParent()
    {
        if (originalParent != null && settingsPanel != null)
        {
            settingsPanel.transform.SetParent(originalParent, false);
        }

        // HierarchyOrderManager��Overlay���\��
        if (hierarchyManager != null)
        {
            hierarchyManager.SetOverlayActive(false);
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

    /// <summary>
    /// �ݒ��ۑ����ăA�v���P�[�V�������I��
    /// </summary>
    private void SaveAndQuit()
    {
        SaveSettings();
        PlayButtonClickSound();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    #endregion

    #region �t�F�[�h�A�j���[�V����

    /// <summary>
    /// �ݒ�p�l���̃t�F�[�h�C��
    /// </summary>
    private IEnumerator FadeInSettings()
    {
        isSettingsVisible = true;
        canOpenSettings = false;

        if (panelCanvasGroup == null)
            yield break;

        panelCanvasGroup.alpha = 0f;
        float elapsedTime = 0f;
        float duration = 1f / fadeSpeed;

        while (elapsedTime < duration)
        {
            panelCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        panelCanvasGroup.alpha = 1f;
        canOpenSettings = true;
    }

    /// <summary>
    /// �ݒ�p�l���̃t�F�[�h�A�E�g
    /// </summary>
    private IEnumerator FadeOutSettings()
    {
        canOpenSettings = false;

        if (panelCanvasGroup == null)
            yield break;

        panelCanvasGroup.alpha = 1f;
        float elapsedTime = 0f;
        float duration = 1f / fadeSpeed;

        while (elapsedTime < duration)
        {
            panelCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        panelCanvasGroup.alpha = 0f;
        settingsPanel.SetActive(false);
        isSettingsVisible = false;
        canOpenSettings = true;

        // �ݒ�p�l�������̐e�ɖ߂�
        RestoreSettingsPanelParent();
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
    }

    #endregion

    #region ���[�e�B���e�B���\�b�h

    /// <summary>
    /// ESC�L�[���͂̃`�F�b�N
    /// </summary>
    private void CheckForEscapeKeyPress()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // ����G�\�����̃`�F�b�N
            if (disableEscapeWhenPortraitOpen && IsPortraitOpen())
            {
                if (debugMode)
                {
                    Debug.Log("HerMainSceneSettingsManager: ����G�\�����̂��ߐݒ��ʂ��J���܂���");
                }
                return;
            }

            ToggleSettings();
        }
    }


    /// <summary>
    /// �N���b�N�����Đ�
    /// </summary>
    private void PlayButtonClickSound()
    {
        if (soundEffectManager != null)
        {
            soundEffectManager.PlayClickSound();
        }
        else if (bgmAudioSource != null && buttonClickSound != null)
        {
            bgmAudioSource.PlayOneShot(buttonClickSound, currentSeVolume);
        }
    }

    /// <summary>
    /// �ݒ��ۑ�
    /// </summary>
    private void SaveSettings()
    {
        // SE�͎����I��SoundEffectManager���ۑ����邽��
        // BGM�̂ݕۑ��������s��
        PlayerPrefs.SetFloat("BGMVolume", currentBgmVolume);
        PlayerPrefs.Save();

        // game_save.json�ɕۑ�
        SaveVolumeToGameSave();
    }

    /// <summary>
    /// �C�x���g���X�i�[�̉���
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

    #region ����G����@�\

    /// <summary>
    /// ����G���J����Ă��邩�`�F�b�N
    /// </summary>
    private bool IsPortraitOpen()
    {
        // ����G�p�l�������ݒ�̏ꍇ�͎�������
        if (portraitFilePanel == null)
        {
            portraitFilePanel = GameObject.Find("����G.FilePanel");

            if (portraitFilePanel == null && debugMode)
            {
                Debug.LogWarning("HerMainSceneSettingsManager: ����G.FilePanel��������܂���");
            }

            if (portraitFilePanel != null && debugMode)
            {
                Debug.Log("HerMainSceneSettingsManager: ����G.FilePanel���������o���܂���");
            }
        }

        // ����G�p�l���̃A�N�e�B�u��Ԃ�Ԃ�
        bool isPortraitPanelActive = portraitFilePanel != null && portraitFilePanel.activeSelf;

        if (debugMode)
        {
            if (portraitFilePanel != null)
            {
                Debug.Log($"HerMainSceneSettingsManager: ����G�p�l����ԃ`�F�b�N - �A�N�e�B�u: {isPortraitPanelActive}");
            }
            else
            {
                Debug.Log("HerMainSceneSettingsManager: ����G�p�l����������Ȃ����߁Afalse ��Ԃ��܂�");
            }
        }

        return isPortraitPanelActive;
    }

    /// <summary>
    /// ����G�p�l���̎Q�Ƃ�ݒ�i�O������ݒ肷��ꍇ�p�j
    /// </summary>
    public void SetPortraitFilePanel(GameObject panel)
    {
        portraitFilePanel = panel;

        if (debugMode)
        {
            Debug.Log($"HerMainSceneSettingsManager: ����G�p�l�����ݒ肳��܂���: {panel?.name}");
        }
    }

    #endregion
}