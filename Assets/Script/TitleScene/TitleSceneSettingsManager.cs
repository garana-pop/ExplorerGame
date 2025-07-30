using UnityEngine;

/// <summary>
/// TitleSceneの設定管理クラス
/// BGM/SE音量設定をgame_save.jsonに保存し、シーン遷移前後で設定を維持する
/// </summary>
public class TitleSceneSettingsManager : BaseSettingsManager
{
    [Header("TitleScene固有の設定")]
    [SerializeField] private MainMenuController mainMenuController;
    [SerializeField] private SettingsMenuController settingsMenuController;

    protected override void Start()
    {
        // SoundEffectManagerが存在しない場合は作成
        if (SoundEffectManager.Instance == null)
        {
            GameObject soundManagerObj = new GameObject("SoundEffectManager");
            soundManagerObj.AddComponent<SoundEffectManager>();
            DontDestroyOnLoad(soundManagerObj);
        }

        // MainMenuControllerの自動検索
        if (mainMenuController == null)
            mainMenuController = FindFirstObjectByType<MainMenuController>();

        // SettingsMenuControllerの自動検索
        if (settingsMenuController == null)
            settingsMenuController = FindFirstObjectByType<SettingsMenuController>();

        // TitleSceneSettingsManagerの参照をSettingsMenuControllerに設定
        if (settingsMenuController != null)
        {
            settingsMenuController.SetTitleSceneSettingsManager(this);
        }

        // BGM AudioSourceの統一設定
        SetupBGMAudioSource();

        // 重要：SoundEffectManagerの初期化を待ってから基底クラスの初期化を実行
        StartCoroutine(DelayedInitialization());
    }

    /// <summary>
    /// 起動時の解像度適用処理
    /// </summary>
    private void ApplyStartupResolution()
    {
        // GameSaveManagerからセーブデータが存在するか確認
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.SaveDataExists())
        {
            // 保存された解像度インデックスを取得
            int savedResolutionIndex = GameSaveManager.Instance.GetResolutionIndex();

            // 解像度プリセット配列を定義（SettingsMenuControllerと同じ設定）
            Vector2Int[] resolutionPresets = new Vector2Int[]
            {
                new Vector2Int(1920, 1080),  // フルHD
                new Vector2Int(1600, 900),   // 中間
                new Vector2Int(1280, 720),   // HD
                new Vector2Int(960, 540)     // 小サイズ
            };

            // インデックスが有効範囲内かチェック
            if (savedResolutionIndex >= 0 && savedResolutionIndex < resolutionPresets.Length)
            {
                // モニターサイズをチェックし、必要に応じて調整
                int adjustedIndex = CheckAndAdjustResolutionForMonitor(savedResolutionIndex, resolutionPresets);

                if (adjustedIndex != savedResolutionIndex)
                {
                    // 調整された解像度を保存
                    GameSaveManager.Instance.SetResolutionIndex(adjustedIndex);
                    GameSaveManager.Instance.SaveGame();

                    Debug.LogWarning($"モニターサイズ制限により起動時解像度を調整: インデックス {savedResolutionIndex} → {adjustedIndex}");
                }

                Vector2Int targetResolution = resolutionPresets[adjustedIndex];

                // 現在の解像度と異なる場合のみ変更
                if (Screen.width != targetResolution.x || Screen.height != targetResolution.y)
                {
                    Screen.SetResolution(targetResolution.x, targetResolution.y, false);
                    Debug.Log($"起動時解像度を適用: {targetResolution.x}×{targetResolution.y}");

                    // AspectRatioManagerに最新のウィンドウサイズを通知
                    if (AspectRatioManager.Instance != null)
                    {
                        AspectRatioManager.Instance.UpdateLastWindowSize();
                    }
                }
            }
            else
            {
                // 無効なインデックスの場合はデフォルト解像度を適用
                ApplyDefaultResolution();
            }
        }
        else
        {
            // セーブデータが存在しない場合はデフォルト解像度を適用
            ApplyDefaultResolution();
        }
    }

    /// <summary>
    /// デフォルト解像度（1280×720）を適用
    /// </summary>
    private void ApplyDefaultResolution()
    {
        const int DEFAULT_WIDTH = 1280;
        const int DEFAULT_HEIGHT = 720;
        const int DEFAULT_INDEX = 2;

        if (Screen.width != DEFAULT_WIDTH || Screen.height != DEFAULT_HEIGHT)
        {
            Screen.SetResolution(DEFAULT_WIDTH, DEFAULT_HEIGHT, false);

            // デフォルトインデックスを保存
            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.SetResolutionIndex(DEFAULT_INDEX);
                GameSaveManager.Instance.SaveGame();
            }

            // AspectRatioManagerに最新のウィンドウサイズを通知
            if (AspectRatioManager.Instance != null)
            {
                AspectRatioManager.Instance.UpdateLastWindowSize();
            }
        }
    }

    /// <summary>
    /// SoundEffectManagerとGameSaveManagerの初期化を待ってから設定を読み込む
    /// </summary>
    private System.Collections.IEnumerator DelayedInitialization()
    {
        // 1フレーム待機してSoundEffectManagerとGameSaveManagerの初期化を確実にする
        yield return null;

        // 起動時の解像度適用処理を追加
        ApplyStartupResolution();

        // 基底クラスの初期化を実行（但しLoadSettingsは後で行う）
        InitializeServices();
        SetupButtonListeners();
        InitializeSliders();
        SubscribeToEvents();

        // 音量設定の読み込みと適用（遅延実行）
        LoadAndApplyVolumeSettings();
    }

    /// <summary>
    /// モニターサイズをチェックし、必要に応じて解像度インデックスを調整
    /// </summary>
    private int CheckAndAdjustResolutionForMonitor(int desiredIndex, Vector2Int[] resolutionPresets)
    {
        // 現在のモニター解像度を取得
        int monitorWidth = Screen.currentResolution.width;
        int monitorHeight = Screen.currentResolution.height;

        Debug.Log($"モニター解像度: {monitorWidth}×{monitorHeight}");

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

    /// <summary>
    /// BGM AudioSourceの統一設定
    /// </summary>
    private void SetupBGMAudioSource()
    {
        // MainMenuControllerのbackgroundAudioSourceを優先的に使用
        if (mainMenuController != null && mainMenuController.backgroundAudioSource != null)
        {
            bgmAudioSource = mainMenuController.backgroundAudioSource;
        }
        else
        {
            // MainMenuControllerにない場合は既存の方法で検索/作成
            GameObject bgmObj = GameObject.Find("BGMAudioSource");
            if (bgmObj == null)
            {
                bgmObj = new GameObject("BGMAudioSource");
                bgmAudioSource = bgmObj.AddComponent<AudioSource>();
                bgmAudioSource.loop = true;
                bgmAudioSource.playOnAwake = false;
            }
            else
            {
                bgmAudioSource = bgmObj.GetComponent<AudioSource>();
            }
        }
    }

    /// <summary>
    /// 音量設定を読み込んで適用（修正版）
    /// </summary>
    private void LoadAndApplyVolumeSettings()
    {

        // 段階的読み込み：PlayerPrefs -> game_save.json
        float loadedBgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.8f);
        float loadedSeVolume = PlayerPrefs.GetFloat("SEVolume", 0.8f);
        float loadedMasterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);

        // GameSaveManagerから読み込みを試行
        if (GameSaveManager.Instance != null)
        {
            try
            {
                var saveData = GameSaveManager.Instance.GetCurrentSaveData();
                if (saveData?.audioSettings != null)
                {
                    loadedBgmVolume = saveData.audioSettings.bgmVolume;
                    loadedSeVolume = saveData.audioSettings.seVolume;
                    loadedMasterVolume = saveData.audioSettings.masterVolume;
                }
                else
                {
                    Debug.Log("game_save.jsonにaudioSettingsがありません");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"game_save.json読み込みエラー: {e.Message}");
            }
        }

        // SoundEffectManagerが存在する場合は、読み込んだ値を設定
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.SetVolume(loadedSeVolume);
        }

        // 現在の音量として保存
        currentBgmVolume = loadedBgmVolume;
        currentSeVolume = loadedSeVolume;

        // 音量を適用
        ApplyVolumeSettings();

        // Master Volumeを適用
        AudioListener.volume = loadedMasterVolume;

        // MainMenuControllerにBGM音量を適用
        if (mainMenuController != null)
        {
            mainMenuController.UpdateBgmVolume(currentBgmVolume);
        }

        // SettingsMenuControllerに現在の音量を反映
        if (settingsMenuController != null)
        {
            settingsMenuController.UpdateSliderValues(currentBgmVolume, currentSeVolume, loadedMasterVolume);
        }

    }

    /// <summary>
    /// 音量設定をAudioSourceに適用（オーバーライド）
    /// </summary>
    protected override void ApplyVolumeSettings()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = currentBgmVolume;
        }
        else
        {
            Debug.LogWarning("BGM AudioSourceが見つかりません！");
        }

        // MainMenuControllerのbackgroundAudioSourceも直接更新（確実性のため）
        if (mainMenuController != null && mainMenuController.backgroundAudioSource != null)
        {
            mainMenuController.backgroundAudioSource.volume = currentBgmVolume;
        }
    }

    /// <summary>
    /// SettingsMenuControllerから音量更新を受け取るメソッド
    /// </summary>
    public void UpdateVolumeFromSettingsMenu(float bgmVolume, float seVolume, float masterVolume)
    {
        currentBgmVolume = bgmVolume;
        currentSeVolume = seVolume;

        // マスター音量を適用
        AudioListener.volume = masterVolume;

        // 音量を適用
        ApplyVolumeSettings();

        // SoundEffectManagerにも設定
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.SetVolume(currentSeVolume);
        }

        // PlayerPrefsに保存
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.SetFloat("SEVolume", seVolume);
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.Save();

        // game_save.jsonに保存
        SaveVolumeToGameSave(masterVolume);
    }

    /// <summary>
    /// game_save.jsonに音量設定を保存
    /// </summary>
    private void SaveVolumeToGameSave(float masterVolume)
    {
        if (GameSaveManager.Instance != null)
        {
            try
            {
                GameSaveManager.Instance.UpdateAudioSettings(currentBgmVolume, currentSeVolume, masterVolume);
                GameSaveManager.Instance.SaveAudioSettingsOnly();
                Debug.Log("game_save.jsonに音量設定を保存");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"game_save.jsonへの保存エラー: {e.Message}");
            }
        }
    }

    /// <summary>
    /// 現在の音量設定を取得
    /// </summary>
    public void GetCurrentVolumeSettings(out float bgmVolume, out float seVolume)
    {
        bgmVolume = currentBgmVolume;
        seVolume = currentSeVolume;
    }

    /// <summary>
    /// 設定画面を表示（オーバーライド）
    /// </summary>
    public override void ShowSettings()
    {
        // BaseSettingsManagerの設定パネルは使用せず、
        // SettingsMenuControllerの設定パネルを使用する
        if (settingsMenuController != null)
        {
            // 現在の音量設定を取得
            float masterVolume = AudioListener.volume;

            // SettingsMenuControllerに現在の音量を設定
            settingsMenuController.UpdateSliderValues(currentBgmVolume, currentSeVolume, masterVolume);
        }
    }

    /// <summary>
    /// 設定画面を非表示（オーバーライド）
    /// </summary>
    public override void HideSettings()
    {
        // SettingsMenuControllerの設定パネルを閉じる処理は
        // SettingsMenuController側で行う
    }

    /// <summary>
    /// ESCキーでの設定画面開閉を無効化（TitleSceneでは別の方法で開く）
    /// </summary>
    protected override void CheckForEscapeKeyPress()
    {
        // TitleSceneではESCキーでの設定画面開閉は行わない
        // 設定ボタンからのみ開く
    }

    /// <summary>
    /// BaseSettingsManagerのLoadSettingsをオーバーライドして無効化
    /// </summary>
    protected override void LoadSettings()
    {
        // 何もしない（LoadAndApplyVolumeSettingsで処理済み）
    }
}