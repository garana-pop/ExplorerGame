using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

/// <summary>
/// MainSceneの設定メニューを制御するマネージャークラス
/// </summary>
public class MainSceneSettingsManager : MonoBehaviour, ISettingsManager
{
    #region SerializeFieldとプロパティ

    [Header("設定パネル")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private CanvasGroup panelCanvasGroup;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private Canvas draggingCanvas;

    [Header("音量設定")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private AudioClip buttonClickSound;

    [Header("ボタン")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button saveAndQuitButton;

    [Header("階層管理")]
    [SerializeField] private HierarchyOrderManager hierarchyManager;

    // プライベート変数
    private float currentBgmVolume;
    private float currentSeVolume;
    private bool isSettingsVisible = false;
    private Coroutine fadeCoroutine;
    private Transform originalParent;
    private SoundEffectManager soundEffectManager;
    private bool isInitialized = false;

    // キャッシュしたコンポーネント参照を保存
    private ISettingsService settingsService;

    #endregion

    #region Unity ライフサイクルメソッド

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

    #region 初期化メソッド

    /// <summary>
    /// コンポーネントの初期化
    /// </summary>
    private void InitializeComponents()
    {
        // 設定パネルの初期化
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            originalParent = settingsPanel.transform.parent;
        }

        // CanvasGroupの初期化
        InitializeCanvasGroup();

        // DraggingCanvasの初期化
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
                Debug.LogWarning("DraggingCanvasが見つかりません。設定パネルの最前面表示が機能しない可能性があります。");
            }
        }
    }

    /// <summary>
    /// サービスの初期化
    /// </summary>
    private void InitializeServices()
    {
        // シングルトンの代わりにサービスロケーターパターンを使用
        // 実際の実装ではDIコンテナなどを使うことが好ましい
        soundEffectManager = SoundEffectManager.Instance;
        settingsService = new SettingsService();

        // 階層管理の初期化
        if (hierarchyManager == null)
        {
            hierarchyManager = FindAnyObjectByType<HierarchyOrderManager>();
        }

        isInitialized = true;
    }

    /// <summary>
    /// スライダーの初期化
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

    #region クリーンアップメソッド

    /// <summary>
    /// イベントリスナーの登録解除
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

    #region 入力処理

    /// <summary>
    /// ESCキー入力のチェック
    /// </summary>
    private void CheckForEscapeKeyPress()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
        }
    }

    #endregion

    #region 設定の読み込みと保存

    /// <summary>
    /// 保存された設定値を読み込む
    /// </summary>
    public void LoadSettings()
    {
        // BGM音量を読み込み
        currentBgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);

        // SE音量 - SoundEffectManagerから取得
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

        // AudioSourceに音量を適用
        ApplyVolumeSettings();

        // スライダーの値を更新
        UpdateSliderValues();
    }

    /// <summary>
    /// 読み込んだ音量設定をAudioSourceに適用
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
    /// スライダーの値を更新
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

    #region 設定パネルの表示制御

    /// <summary>
    /// 設定画面の表示/非表示を切り替え
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
    /// 設定画面を表示
    /// </summary>
    public void ShowSettings()
    {
        if (!isInitialized || settingsPanel == null || isSettingsVisible)
            return;

        // 階層順序を調整
        AdjustHierarchyForSettings();

        // 設定パネルをDraggingCanvasに移動（最前面に表示するため）
        MoveSettingsPanelToFront();

        settingsPanel.SetActive(true);
        isSettingsVisible = true;

        // フェードイン効果
        StartFadeIn();

        // 表示時に最新の設定値をスライダーに反映
        UpdateSliderValues();
    }

    /// <summary>
    /// 設定画面を非表示
    /// </summary>
    public void HideSettings()
    {
        if (settingsPanel == null || !isSettingsVisible)
            return;

        // 設定を保存
        SaveSettings();

        // フェードアウト効果
        StartFadeOut();

        // HierarchyOrderManagerにも通知して、Overlayを非表示に
        if (hierarchyManager != null)
        {
            hierarchyManager.OnSettingsClosed();
        }

        // 効果音を再生（設定を閉じる音）
        PlayButtonClickSound();
    }

    /// <summary>
    /// 階層順序を設定表示用に調整
    /// </summary>
    private void AdjustHierarchyForSettings()
    {
        if (hierarchyManager != null)
        {
            hierarchyManager.AdjustHierarchyOrderForSettings();
        }
    }

    /// <summary>
    /// 設定パネルを最前面に移動
    /// </summary>
    private void MoveSettingsPanelToFront()
    {
        if (draggingCanvas != null)
        {
            // 現在の親が元の親と異なる場合は、元の親を保存しないようにする
            if (settingsPanel.transform.parent == originalParent)
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

    #endregion

    #region フェードアニメーション

    /// <summary>
    /// フェードイン処理を開始
    /// </summary>
    private void StartFadeIn()
    {
        // 実行中のフェードを停止
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeIn());
    }

    /// <summary>
    /// フェードアウト処理を開始
    /// </summary>
    private void StartFadeOut()
    {
        // 実行中のフェードを停止
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeOut());
    }

    /// <summary>
    /// フェードイン処理
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
    /// フェードアウト処理
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

        // 元の親に戻す
        RestoreSettingsPanelParent();
    }

    /// <summary>
    /// 設定パネルを元の親に戻す
    /// </summary>
    private void RestoreSettingsPanelParent()
    {
        if (originalParent != null)
        {
            settingsPanel.transform.SetParent(originalParent, false);
        }
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
        else
        {
            // SoundEffectManagerがない場合の代替処理
            if (sfxAudioSource != null)
            {
                sfxAudioSource.volume = volume;
            }
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

    #endregion

    #region ユーティリティメソッド

    /// <summary>
    /// クリック音を再生
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
    /// game_save.jsonから音量設定を読み込む
    /// </summary>
    private void LoadSettingsFromGameSave()
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

                // 音量を適用
                ApplyVolumeSettings();
                return;
            }
            else
            {
                Debug.Log("LoadSettingsFromGameSave: game_save.jsonにaudioSettingsが存在しません");
            }
        }
        else
        {
            Debug.LogWarning("LoadSettingsFromGameSave: GameSaveManager.Instanceがnullです");
        }

        // game_save.jsonにデータがない場合はPlayerPrefsから読み込み
        Debug.Log("LoadSettingsFromGameSave: PlayerPrefsから読み込みます");
        LoadSettings();
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
        else
        {
            Debug.LogWarning("SaveVolumeToGameSave: GameSaveManager.Instanceがnullです");
        }
    }

    /// <summary>
    /// 設定を保存
    /// </summary>
    // SaveSettingsメソッドを修正
    private void SaveSettings()
    {
        // BGM音量を保存
        PlayerPrefs.SetFloat("BGMVolume", currentBgmVolume);

        // SE音量はSoundEffectManagerに任せる
        if (soundEffectManager != null)
        {
            soundEffectManager.SetVolume(currentSeVolume);
            // GameSaveManagerとの連携も含めた保存
            soundEffectManager.SaveVolumeSettingsWithGameSaveManager(); // soundManagerからsoundEffectManagerに修正
        }
        else
        {
            // 直接PlayerPrefsに保存
            PlayerPrefs.SetFloat("SEVolume", currentSeVolume);
        }

        // 確実に保存
        PlayerPrefs.Save();
    }

    /// <summary>
    /// セーブして終了
    /// </summary>
    private void SaveAndQuit()
    {
        // 設定を保存
        SaveSettings();

        // 必要に応じてゲームの状態をさらに保存
        SaveGameState();

        // アプリケーションを終了
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// ゲームの状態を保存するメソッド（必要に応じて実装）
    /// </summary>
    private void SaveGameState()
    {
        // ゲーム状態の保存（GameSaveManagerを使用）
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.SaveGame();
            Debug.Log("ゲームの状態を保存しました");
        }
        else
        {
            Debug.LogWarning("GameSaveManagerが見つかりません。ゲームの状態を保存できませんでした。");
        }
    }

    #endregion
}

/// <summary>
/// テスト容易性のためのインターフェース
/// </summary>
public interface ISettingsManager
{
    void LoadSettings();
    void ToggleSettings();
    void ShowSettings();
    void HideSettings();
}

/// <summary>
/// 設定サービスのインターフェース
/// </summary>
public interface ISettingsService
{
    float GetBgmVolume();
    float GetSeVolume();
    void SaveBgmVolume(float volume);
    void SaveSeVolume(float volume);
}

/// <summary>
/// 設定サービスの実装
/// </summary>
public class SettingsService : ISettingsService
{
    public float GetBgmVolume() => PlayerPrefs.GetFloat("BGMVolume", 0.5f);
    public float GetSeVolume() => PlayerPrefs.GetFloat("SEVolume", 0.5f);

    public void SaveBgmVolume(float volume) => PlayerPrefs.SetFloat("BGMVolume", volume);
    public void SaveSeVolume(float volume) => PlayerPrefs.SetFloat("SEVolume", volume);
}