using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 設定画面の基底クラス
/// 各シーンで共通の設定機能を提供
/// </summary>
public abstract class BaseSettingsManager : MonoBehaviour
{
    [Header("設定パネル")]
    [SerializeField] protected GameObject settingsPanel;
    [SerializeField] protected CanvasGroup panelCanvasGroup;
    [SerializeField] protected float fadeSpeed = 5f;

    [Header("音量設定")]
    [SerializeField] protected Slider bgmSlider;
    [SerializeField] protected Slider seSlider;
    [SerializeField] protected AudioSource bgmAudioSource;
    [SerializeField] protected AudioClip buttonClickSound;

    [Header("ボタン")]
    [SerializeField] protected Button backButton;
    [SerializeField] protected Button saveAndQuitButton;

    // 保護されたフィールド（派生クラスからアクセス可能）
    protected float currentBgmVolume = 0.5f;
    protected float currentSeVolume = 0.5f;
    protected bool isSettingsVisible = false;
    protected bool canOpenSettings = true;
    protected bool isInitialized = false;
    protected SoundEffectManager soundEffectManager;

    // フェード用のコルーチン参照
    private Coroutine fadeCoroutine;

    #region Unity ライフサイクル

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

    #region 初期化メソッド

    /// <summary>
    /// コンポーネントの初期化
    /// </summary>
    protected virtual void InitializeComponents()
    {
        // 設定パネルが非表示であることを確認
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            isSettingsVisible = false;
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
    }

    /// <summary>
    /// サービスの初期化
    /// </summary>
    protected virtual void InitializeServices()
    {
        soundEffectManager = SoundEffectManager.Instance;
        isInitialized = true;
    }

    /// <summary>
    /// スライダーの初期化
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
    /// ボタンイベントの設定
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
    /// イベントリスナーの登録
    /// </summary>
    protected virtual void SubscribeToEvents()
    {
        if (soundEffectManager != null)
        {
            soundEffectManager.OnVolumeChanged += OnSoundEffectVolumeChanged;
        }
    }

    #endregion

    #region 設定の読み込みと保存

    /// <summary>
    /// 保存された設定値を読み込む
    /// </summary>
    protected virtual void LoadSettings()
    {
        // SE音量はSoundEffectManagerから取得
        if (soundEffectManager != null)
        {
            currentSeVolume = soundEffectManager.GetVolume();
        }
        else
        {
            currentSeVolume = 0.5f;
        }

        // BGM音量はデフォルト値を使用（派生クラスでオーバーライド）
        currentBgmVolume = 0.5f;

        // AudioSourceに音量を適用
        ApplyVolumeSettings();

        // スライダーの値を更新
        UpdateSliderValues();
    }

    /// <summary>
    /// デフォルト設定を読み込む
    /// </summary>
    protected virtual void LoadDefaultSettings()
    {
        currentBgmVolume = 0.5f;

        // SE音量はSoundEffectManagerから取得
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
    /// 設定を保存
    /// </summary>
    protected virtual void SaveSettings()
    {
        // SE音量はSoundEffectManagerに任せる
        if (soundEffectManager != null)
        {
            soundEffectManager.SetVolume(currentSeVolume);
            soundEffectManager.SaveVolumeSettingsWithGameSaveManager();
        }

        // BGM音量は派生クラスで実装
    }

    #endregion

    #region 設定パネルの表示制御

    /// <summary>
    /// 設定画面の表示/非表示を切り替え
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
    /// 設定画面を表示
    /// </summary>
    public virtual void ShowSettings()
    {
        if (!isInitialized || settingsPanel == null || isSettingsVisible || !canOpenSettings)
            return;

        settingsPanel.SetActive(true);
        isSettingsVisible = true;

        // フェードイン効果
        StartFadeIn();

        // 表示時に最新の設定値をスライダーに反映
        UpdateSliderValues();

        // シーン固有の処理
        OnSettingsOpened();
    }

    /// <summary>
    /// 設定画面を非表示
    /// </summary>
    public virtual void HideSettings()
    {
        if (!isInitialized || settingsPanel == null || !isSettingsVisible)
            return;

        // 設定を保存
        SaveSettings();

        // フェードアウト効果
        StartFadeOut();

        // シーン固有の処理
        OnSettingsClosed();
    }

    /// <summary>
    /// 設定画面が開かれた時の処理
    /// </summary>
    protected virtual void OnSettingsOpened()
    {
        // 派生クラスでオーバーライド
    }

    /// <summary>
    /// 設定画面が閉じられた時の処理
    /// </summary>
    protected virtual void OnSettingsClosed()
    {
        // 派生クラスでオーバーライド
    }

    #endregion

    #region フェード効果

    /// <summary>
    /// フェードイン開始
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
    /// フェードアウト開始
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
    /// フェードイン処理
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
    /// フェードアウト処理
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

    #region 音量変更処理

    /// <summary>
    /// BGM音量が変更された時の処理
    /// </summary>
    protected virtual void OnBgmVolumeChanged(float volume)
    {
        currentBgmVolume = volume;

        // BGMの音量を即時反映
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = volume;
        }
    }

    /// <summary>
    /// SE音量が変更された時の処理
    /// </summary>
    protected virtual void OnSeVolumeChanged(float volume)
    {
        currentSeVolume = volume;

        // SoundEffectManagerに音量を適用
        if (soundEffectManager != null)
        {
            soundEffectManager.SetVolume(volume);
        }
    }

    /// <summary>
    /// SoundEffectManagerからの音量変更通知を受け取るハンドラ
    /// </summary>
    protected virtual void OnSoundEffectVolumeChanged(float newVolume)
    {
        // スライダーの値を更新（無限ループを防ぐため差分があるときのみ更新）
        if (seSlider != null && !Mathf.Approximately(seSlider.value, newVolume))
        {
            seSlider.value = newVolume;
            currentSeVolume = newVolume;
        }
    }

    /// <summary>
    /// 音量をAudioSourceに適用
    /// </summary>
    protected virtual void ApplyVolumeSettings()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = currentBgmVolume;
        }
    }

    /// <summary>
    /// スライダーの値を更新
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

    #region ユーティリティメソッド

    /// <summary>
    /// ESCキー入力のチェック
    /// </summary>
    protected virtual void CheckForEscapeKeyPress()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettings();
        }
    }

    /// <summary>
    /// クリック音を再生
    /// </summary>
    protected virtual void PlayButtonClickSound()
    {
        if (soundEffectManager != null)
        {
            soundEffectManager.PlayClickSound();
        }
        else if (buttonClickSound != null)
        {
            // AudioSourceがない場合は作成して再生
            AudioSource.PlayClipAtPoint(buttonClickSound, Camera.main.transform.position, currentSeVolume);
        }
    }

    /// <summary>
    /// セーブして終了
    /// </summary>
    protected virtual void SaveAndQuit()
    {
        // 設定を保存
        SaveSettings();

        // ゲーム状態の保存
        SaveGameState();

        // アプリケーションを終了
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// ゲームの状態を保存するメソッド
    /// </summary>
    protected virtual void SaveGameState()
    {
        // GameSaveManagerを使用してゲーム状態を保存
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.SaveGame();
        }
    }

    /// <summary>
    /// 設定画面を開けるかどうかを設定
    /// </summary>
    public virtual void SetCanOpenSettings(bool canOpen)
    {
        canOpenSettings = canOpen;
    }

    #endregion

    #region クリーンアップメソッド

    /// <summary>
    /// イベントリスナーの登録解除
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
    /// アクティブなコルーチンを停止
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