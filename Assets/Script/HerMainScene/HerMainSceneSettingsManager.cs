using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HerMainScene専用の設定管理クラス
/// BaseSettingsManagerを継承して音量設定の連動機能を実装
/// </summary>
public class HerMainSceneSettingsManager : MonoBehaviour
{
    [Header("設定パネル")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private Canvas draggingCanvas;

    [Header("音量設定")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioClip buttonClickSound;

    [Header("ボタン")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button saveAndQuitButton;

    [Header("階層管理")]
    [SerializeField] private HierarchyOrderManager hierarchyManager;

    [Header("似顔絵制御設定")]
    [Tooltip("似顔絵表示中にESCキーでの設定画面表示を無効化するか")]
    [SerializeField] private bool disableEscapeWhenPortraitOpen = true;

    [Tooltip("似顔絵ファイルパネルへの参照")]
    [SerializeField] private GameObject portraitFilePanel;

    [Tooltip("デバッグログを表示するか")]
    [SerializeField] private bool debugMode = false;

    // 保護されたフィールド
    private float currentBgmVolume = 0.5f;
    private float currentSeVolume = 0.5f;
    private bool isSettingsVisible = false;
    private bool canOpenSettings = true;
    private bool isInitialized = false;
    private SoundEffectManager soundEffectManager;
    private Transform originalParent;

    // フェード用のコルーチン参照
    private Coroutine fadeCoroutine;

    #region Unity ライフサイクル

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

    #region 初期化メソッド

    /// <summary>
    /// コンポーネントの初期化
    /// </summary>
    private void InitializeComponents()
    {
        // 設定パネルが非表示であることを確認
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            isSettingsVisible = false;
            originalParent = settingsPanel.transform.parent;
        }

        // CanvasGroupの初期化
        if (panelCanvasGroup == null && settingsPanel != null)
        {
            panelCanvasGroup = settingsPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = settingsPanel.AddComponent<CanvasGroup>();
            }
        }

        // DraggingCanvasの初期化
        InitializeDraggingCanvas();

        // HierarchyOrderManagerの初期化
        InitializeHierarchyManager();

        // 似顔絵パネルの初期検索
        if (portraitFilePanel == null && disableEscapeWhenPortraitOpen)
        {
            portraitFilePanel = GameObject.Find("似顔絵.FilePanel");

            if (portraitFilePanel != null && debugMode)
            {
                Debug.Log("HerMainSceneSettingsManager: 似顔絵.FilePanelを自動検出しました");
            }
        }
    }

    /// <summary>
    /// DraggingCanvasの初期化
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
    /// HierarchyOrderManagerの初期化
    /// </summary>
    private void InitializeHierarchyManager()
    {
        if (hierarchyManager == null)
        {
            hierarchyManager = FindFirstObjectByType<HierarchyOrderManager>();
        }
    }

    /// <summary>
    /// サービスの初期化
    /// </summary>
    private void InitializeServices()
    {
        // SoundEffectManagerの取得を改善
        StartCoroutine(InitializeSoundEffectManager());

        // 階層管理の取得
        if (hierarchyManager == null)
        {
            hierarchyManager = FindFirstObjectByType<HierarchyOrderManager>(FindObjectsInactive.Include);
        }

        isInitialized = true;
        canOpenSettings = true;
    }

    private IEnumerator InitializeSoundEffectManager()
    {
        // フレーム待機してSoundEffectManagerの初期化を確実にする
        yield return null;

        soundEffectManager = SoundEffectManager.Instance;

        if (soundEffectManager == null)
        {
            // フォールバック: SoundEffectManagerを探す
            soundEffectManager = FindFirstObjectByType<SoundEffectManager>(FindObjectsInactive.Include);
        }

        // SoundEffectManagerが取得できた場合、現在のSE音量を適用
        if (soundEffectManager != null)
        {
            soundEffectManager.SetVolume(currentSeVolume);

            // イベントの購読
            soundEffectManager.OnVolumeChanged += OnSoundEffectVolumeChanged;
        }
    }


    /// <summary>
    /// スライダーの初期化
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
    /// ボタンイベントの設定
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
    /// イベントリスナーの登録
    /// </summary>
    private void SubscribeToEvents()
    {
        if (soundEffectManager != null)
        {
            soundEffectManager.OnVolumeChanged += OnSoundEffectVolumeChanged;
        }
    }

    #endregion

    #region 音量設定の読み込みと適用

    /// <summary>
    /// game_save.jsonから音量設定を読み込んで適用
    /// </summary>
    private void LoadAndApplyVolumeSettings()
    {
        // GameSaveManagerから音量設定を読み込む
        if (GameSaveManager.Instance != null)
        {
            var saveData = GameSaveManager.Instance.GetCurrentSaveData();
            if (saveData?.audioSettings != null)
            {
                currentBgmVolume = saveData.audioSettings.bgmVolume;
                currentSeVolume = saveData.audioSettings.seVolume;

                // masterVolumeも読み込み
                if (saveData.audioSettings.masterVolume > 0)
                {
                    AudioListener.volume = saveData.audioSettings.masterVolume;
                }

                // 設定を適用
                ApplyVolumeSettings();
                UpdateSliderValues();

                // PlayerPrefsにも保存（BGM/SE音量管理のみ使用許可）
                PlayerPrefs.SetFloat("BGMVolume", currentBgmVolume);
                PlayerPrefs.SetFloat("SEVolume", currentSeVolume);
                PlayerPrefs.Save();

                return;
            }
        }

        // game_save.jsonに設定がない場合はデフォルト値を使用
        LoadDefaultSettings();
    }

    /// <summary>
    /// デフォルト設定を読み込む
    /// </summary>
    private void LoadDefaultSettings()
    {
        currentBgmVolume = 0.5f;
        currentSeVolume = 0.5f;

        ApplyVolumeSettings();
        UpdateSliderValues();
    }

    /// <summary>
    /// 音量をAudioSourceに適用
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
    /// スライダーの値を更新
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

    #region 設定パネルの表示/非表示

    /// <summary>
    /// 設定画面の表示切り替え
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
    /// 設定画面を表示
    /// </summary>
    public void ShowSettings()
    {
        if (!canOpenSettings || !isInitialized || settingsPanel == null)
            return;

        // DraggingCanvasに設定パネルを移動
        MoveSettingsPanelToDraggingCanvas();

        settingsPanel.SetActive(true);

        // HierarchyOrderManagerで階層順序を調整
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

        // ボタンクリック音を再生
        PlayButtonClickSound();
    }

    /// <summary>
    /// 設定画面を非表示
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

        // 設定を保存
        SaveSettings();

        // ボタンクリック音を再生
        PlayButtonClickSound();
    }

    /// <summary>
    /// 設定パネルをDraggingCanvasに移動
    /// </summary>
    private void MoveSettingsPanelToDraggingCanvas()
    {
        if (draggingCanvas != null && settingsPanel != null)
        {
            // 元の親を保存
            if (originalParent == null)
            {
                originalParent = settingsPanel.transform.parent;
            }

            // DraggingCanvasに移動
            settingsPanel.transform.SetParent(draggingCanvas.transform, false);

            // 画面中央に配置
            CenterSettingsPanel();
        }
    }

    /// <summary>
    /// 設定パネルを元の親に戻す
    /// </summary>
    private void RestoreSettingsPanelParent()
    {
        if (originalParent != null && settingsPanel != null)
        {
            settingsPanel.transform.SetParent(originalParent, false);
        }

        // HierarchyOrderManagerでOverlayを非表示
        if (hierarchyManager != null)
        {
            hierarchyManager.SetOverlayActive(false);
        }
    }

    /// <summary>
    /// 設定パネルを画面中央に配置
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
    /// 設定を保存してアプリケーションを終了
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

    #region フェードアニメーション

    /// <summary>
    /// 設定パネルのフェードイン
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
    /// 設定パネルのフェードアウト
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

        // 設定パネルを元の親に戻す
        RestoreSettingsPanelParent();
    }

    #endregion

    #region 音量変更処理

    /// <summary>
    /// BGM音量が変更されたときの処理
    /// </summary>
    private void OnBgmVolumeChanged(float volume)
    {
        currentBgmVolume = volume;

        // BGMの音量を即時反映
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = volume;
        }

        // PlayerPrefsに保存（互換性のため）
        PlayerPrefs.SetFloat("BGMVolume", volume);
        PlayerPrefs.Save();

        // game_save.jsonに保存
        SaveVolumeToGameSave();
    }

    /// <summary>
    /// SE音量が変更されたときの処理
    /// </summary>
    private void OnSeVolumeChanged(float volume)
    {
        currentSeVolume = volume;

        // SoundEffectManagerに音量を適用
        if (soundEffectManager != null)
        {
            soundEffectManager.SetVolume(volume);
        }

        // PlayerPrefsに保存（互換性のため）
        PlayerPrefs.SetFloat("SEVolume", volume);
        PlayerPrefs.Save();

        // game_save.jsonに保存
        SaveVolumeToGameSave();
    }

    /// <summary>
    /// SoundEffectManagerからの音量変更通知を受け取るハンドラ
    /// </summary>
    private void OnSoundEffectVolumeChanged(float newVolume)
    {
        // スライダーの値を更新（無限ループを防ぐため差分があるときのみ更新）
        if (seSlider != null && !Mathf.Approximately(seSlider.value, newVolume))
        {
            seSlider.value = newVolume;
            currentSeVolume = newVolume;
        }
    }

    /// <summary>
    /// 音量設定をgame_save.jsonに保存
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

    #region ユーティリティメソッド

    /// <summary>
    /// ESCキー入力のチェック
    /// </summary>
    private void CheckForEscapeKeyPress()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 似顔絵表示中のチェック
            if (disableEscapeWhenPortraitOpen && IsPortraitOpen())
            {
                if (debugMode)
                {
                    Debug.Log("HerMainSceneSettingsManager: 似顔絵表示中のため設定画面を開きません");
                }
                return;
            }

            ToggleSettings();
        }
    }


    /// <summary>
    /// クリック音を再生
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
    /// 設定を保存
    /// </summary>
    private void SaveSettings()
    {
        // SEは自動的にSoundEffectManagerが保存するため
        // BGMのみ保存処理を行う
        PlayerPrefs.SetFloat("BGMVolume", currentBgmVolume);
        PlayerPrefs.Save();

        // game_save.jsonに保存
        SaveVolumeToGameSave();
    }

    /// <summary>
    /// イベントリスナーの解除
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
    /// アクティブなコルーチンを停止
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

    #region 似顔絵制御機能

    /// <summary>
    /// 似顔絵が開かれているかチェック
    /// </summary>
    private bool IsPortraitOpen()
    {
        // 似顔絵パネルが未設定の場合は自動検索
        if (portraitFilePanel == null)
        {
            portraitFilePanel = GameObject.Find("似顔絵.FilePanel");

            if (portraitFilePanel == null && debugMode)
            {
                Debug.LogWarning("HerMainSceneSettingsManager: 似顔絵.FilePanelが見つかりません");
            }

            if (portraitFilePanel != null && debugMode)
            {
                Debug.Log("HerMainSceneSettingsManager: 似顔絵.FilePanelを自動検出しました");
            }
        }

        // 似顔絵パネルのアクティブ状態を返す
        bool isPortraitPanelActive = portraitFilePanel != null && portraitFilePanel.activeSelf;

        if (debugMode)
        {
            if (portraitFilePanel != null)
            {
                Debug.Log($"HerMainSceneSettingsManager: 似顔絵パネル状態チェック - アクティブ: {isPortraitPanelActive}");
            }
            else
            {
                Debug.Log("HerMainSceneSettingsManager: 似顔絵パネルが見つからないため、false を返します");
            }
        }

        return isPortraitPanelActive;
    }

    /// <summary>
    /// 似顔絵パネルの参照を設定（外部から設定する場合用）
    /// </summary>
    public void SetPortraitFilePanel(GameObject panel)
    {
        portraitFilePanel = panel;

        if (debugMode)
        {
            Debug.Log($"HerMainSceneSettingsManager: 似顔絵パネルが設定されました: {panel?.name}");
        }
    }

    #endregion
}