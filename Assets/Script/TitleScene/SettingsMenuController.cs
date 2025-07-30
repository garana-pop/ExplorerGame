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

    [Header("解像度プリセット")]
    // 16:9固定の解像度プリセット
    private readonly Vector2Int[] resolutionPresets = new Vector2Int[]
{
    new Vector2Int(1920, 1080),  // フルHD
    new Vector2Int(1600, 900),   // HD+
    new Vector2Int(1280, 720),   // HD
    new Vector2Int(960, 540)     // 小サイズ
};

    [Header("解像度ボタン")]
    [SerializeField] private Button resolution1920x1080Button;
    [SerializeField] private Button resolution1600x900Button;
    [SerializeField] private Button resolution1280x720Button;
    [SerializeField] private Button resolution960x540Button;

    [Header("現在設定されている値の色設定")]
    [SerializeField] private Color optionalColor;

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

    private void SetupResolutionButtons()
    {
        // 解像度ボタンのリスナー設定
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
    /// 解像度ボタン押下後の処理
    /// </summary>
    private void OnResolutionButtonClicked(int resolutionIndex)
    {
        if (resolutionIndex < 0 || resolutionIndex >= resolutionPresets.Length)
        {
            Debug.LogError($"無効な解像度インデックス: {resolutionIndex}");
            return;
        }

        Vector2Int selectedResolution = resolutionPresets[resolutionIndex];

        if (debugMode)
        {
            Debug.Log($"解像度選択: {selectedResolution.x}×{selectedResolution.y}");
        }

        // モニター解像度チェック
        int adjustedIndex = CheckAndAdjustResolutionForMonitor(resolutionIndex);

        // 調整が必要だった場合
        if (adjustedIndex != resolutionIndex)
        {
            selectedResolution = resolutionPresets[adjustedIndex];
            resolutionIndex = adjustedIndex;

            if (debugMode)
            {
                Debug.LogWarning($"モニター解像度制限により、解像度を自動調整: {selectedResolution.x}×{selectedResolution.y}");
            }
        }

        // エラーハンドリングと解像度変更処理
        try
        {
            // ウィンドウモード固定で解像度を変更
            Screen.SetResolution(selectedResolution.x, selectedResolution.y, false);

            // GameSaveManagerに解像度インデックスを保存
            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.SetResolutionIndex(resolutionIndex);
                GameSaveManager.Instance.SaveGame();
            }

            if (debugMode)
            {
                Debug.Log($"解像度を変更しました: {selectedResolution.x}×{selectedResolution.y} (ウィンドウモード)");
            }

            // 効果音再生
            if (SoundEffectManager.Instance != null)
            {
                SoundEffectManager.Instance.PlayClickSound();
            }
        }
        // 例外発生時
        catch (System.Exception ex)
        {
            Debug.LogError($"解像度変更エラー: {ex.Message}");

            // フォールバック処理: デフォルト解像度(1280×720)で試行
            try
            {
                Debug.LogWarning("デフォルト解像度(1280×720)にフォールバックします");

                // ウィンドウモード固定で解像度を1280×720に設定
                Screen.SetResolution(1280, 720, false);

                // デフォルトインデックスを保存
                if (GameSaveManager.Instance != null)
                {
                    GameSaveManager.Instance.SetResolutionIndex(2);
                    GameSaveManager.Instance.SaveGame();
                }
            }
            catch (System.Exception fallbackEx)
            {
                Debug.LogError($"フォールバック解像度変更も失敗: {fallbackEx.Message}");
            }
        }

        // UI更新処理を少し遅らせて実行（解像度変更が反映された後）
        StartCoroutine(DelayedUIUpdate());
    }

    /// <summary>
    /// 解像度変更後のUI更新を遅延実行
    /// </summary>
    private System.Collections.IEnumerator DelayedUIUpdate()
    {
        // 1フレーム待機して解像度変更が確実に反映されるのを待つ
        yield return null;

        // GraphicsPanel表示時の現在解像度確認処理
        UpdateGraphicsPanel();

        if (debugMode)
        {
            Debug.Log($"UI更新完了 - 現在の解像度: {Screen.width}×{Screen.height}");
        }
    }

    /// <summary>
    /// GraphicsPanelの表示時に現在の解像度を確認する処理
    /// </summary>
    private void UpdateGraphicsPanel()
    {
        // 現在の解像度を取得
        int currentWidth = Screen.width;
        int currentHeight = Screen.height;

        if (debugMode)
        {
            Debug.Log($"現在の解像度: {currentWidth}×{currentHeight}");
        }

        // 現在の解像度に対応するボタンのテキスト色を変更
        UpdateResolutionButtonTextColors(currentWidth, currentHeight);
    }

    /// <summary>
    /// 現在の解像度に対応するボタンのテキスト色を更新
    /// </summary>
    private void UpdateResolutionButtonTextColors(int width, int height)
    {
        // すべてのボタンのテキスト色をリセット
        ResetAllButtonTextColors();

        // 現在の解像度に一致するボタンを探してテキスト色を変更
        for (int i = 0; i < resolutionPresets.Length; i++)
        {
            if (resolutionPresets[i].x == width && resolutionPresets[i].y == height)
            {
                SetButtonTextColor(GetResolutionButton(i), optionalColor);

                if (debugMode)
                {
                    Debug.Log($"現在の解像度ボタンのテキスト色変更: {width}×{height}");
                }

                break;
            }
        }
    }

    /// <summary>
    /// モニターサイズをチェックし、必要に応じて解像度インデックスを調整
    /// </summary>
    private int CheckAndAdjustResolutionForMonitor(int desiredIndex)
    {
        // 現在のモニター解像度を取得
        int monitorWidth = Screen.currentResolution.width;
        int monitorHeight = Screen.currentResolution.height;

        if (debugMode)
        {
            Debug.Log($"モニター解像度: {monitorWidth}×{monitorHeight}");
        }

        // 選択された解像度がモニターサイズを超えているか確認
        for (int i = desiredIndex; i < resolutionPresets.Length; i++)
        {
            Vector2Int resolution = resolutionPresets[i];

            // タスクバーやウィンドウ枠の余裕を考慮
            if (resolution.x <= monitorWidth && resolution.y <= monitorHeight)
            {
                return i;
            }
        }

        // すべての解像度がモニターサイズを超える場合は最小解像度を返す
        return resolutionPresets.Length - 1;
    }

    // すべてのボタンのテキスト色をリセット
    private void ResetAllButtonTextColors()
    {
        SetButtonTextColor(resolution1920x1080Button, Color.white);
        SetButtonTextColor(resolution1600x900Button, Color.white);
        SetButtonTextColor(resolution1280x720Button, Color.white);
        SetButtonTextColor(resolution960x540Button, Color.white);
    }

    // ボタンのテキスト色を設定
    private void SetButtonTextColor(Button button, Color color)
    {
        if (button != null)
        {
            // ボタンの子オブジェクトからText (TMP)を探す
            TextMeshProUGUI textComponent = button.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null)
            {
                textComponent.color = color;
            }
            else if (debugMode)
            {
                Debug.LogWarning($"ボタン {button.name} にTextMeshProUGUIコンポーネントが見つかりません");
            }
        }
    }

    // インデックスから対応するボタンを取得
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

    /// <summary>
    /// ボタンのリスナー設定
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

        // 解像度ボタンの設定
        SetupResolutionButtons();
    }

    /// <summary>
    /// 音量スライダー設定
    /// </summary>
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

    /// <summary>
    /// 初期値（再ロード時）の設定：音量・解像度
    /// </summary>
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

        // 解像度を設定：保存された解像度インデックスを取得して適用
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.SaveDataExists())
        {
            int savedResolutionIndex = GameSaveManager.Instance.GetResolutionIndex();

            // 保存されたインデックスが有効な範囲内か確認
            if (savedResolutionIndex >= 0 && savedResolutionIndex < resolutionPresets.Length)
            {
                Vector2Int savedResolution = resolutionPresets[savedResolutionIndex];

                // 現在の解像度と異なる場合のみ変更
                if (Screen.width != savedResolution.x || Screen.height != savedResolution.y)
                {
                    Screen.SetResolution(savedResolution.x, savedResolution.y, false);

                    if (debugMode)
                    {
                        Debug.Log($"保存された解像度を適用: {savedResolution.x}×{savedResolution.y}");
                    }
                }

                // UI更新
                UpdateGraphicsPanel();
            }
        }
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

    /// <summary>
    /// パネルの初期化
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

        // GraphicsPanelを開いた時に現在の解像度を確認
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