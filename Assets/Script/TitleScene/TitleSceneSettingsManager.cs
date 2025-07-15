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
    /// SoundEffectManagerとGameSaveManagerの初期化を待ってから設定を読み込む
    /// </summary>
    private System.Collections.IEnumerator DelayedInitialization()
    {
        // 1フレーム待機してSoundEffectManagerとGameSaveManagerの初期化を確実にする
        yield return null;

        // 基底クラスの初期化を実行（但しLoadSettingsは後で行う）
        InitializeServices();
        SetupButtonListeners();
        InitializeSliders();
        SubscribeToEvents();

        // 音量設定の読み込みと適用（遅延実行）
        LoadAndApplyVolumeSettings();
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
            Debug.Log("BGM AudioSource: MainMenuControllerのbackgroundAudioSourceを使用");
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
                Debug.Log("BGM AudioSource: 新規作成");
            }
            else
            {
                bgmAudioSource = bgmObj.GetComponent<AudioSource>();
                Debug.Log("BGM AudioSource: 既存のBGMAudioSourceを使用");
            }
        }
    }

    /// <summary>
    /// 音量設定を読み込んで適用（修正版）
    /// </summary>
    private void LoadAndApplyVolumeSettings()
    {
        Debug.Log("TitleSceneSettingsManager: 音量設定読み込み開始");

        // 段階的読み込み：PlayerPrefs -> game_save.json
        float loadedBgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.8f);
        float loadedSeVolume = PlayerPrefs.GetFloat("SEVolume", 0.8f);
        float loadedMasterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);

        Debug.Log($"PlayerPrefsから読み込み - BGM: {loadedBgmVolume}, SE: {loadedSeVolume}, Master: {loadedMasterVolume}");

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
                    Debug.Log($"game_save.jsonから読み込み - BGM: {loadedBgmVolume}, SE: {loadedSeVolume}, Master: {loadedMasterVolume}");
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
            Debug.Log($"SoundEffectManagerにSE音量を設定: {loadedSeVolume}");
        }

        // 現在の音量として保存
        currentBgmVolume = loadedBgmVolume;
        currentSeVolume = loadedSeVolume;

        // 音量を適用
        ApplyVolumeSettings();

        // Master Volumeを適用
        AudioListener.volume = loadedMasterVolume;
        Debug.Log($"AudioListener.volumeを設定: {loadedMasterVolume}");

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

        Debug.Log($"最終音量設定 - BGM: {currentBgmVolume}, SE: {currentSeVolume}, Master: {AudioListener.volume}");
    }

    /// <summary>
    /// 音量設定をAudioSourceに適用（オーバーライド）
    /// </summary>
    protected override void ApplyVolumeSettings()
    {
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = currentBgmVolume;
            Debug.Log($"BGM音量適用: {currentBgmVolume} (AudioSource: {bgmAudioSource.name})");
        }
        else
        {
            Debug.LogWarning("BGM AudioSourceが見つかりません！");
        }

        // MainMenuControllerのbackgroundAudioSourceも直接更新（確実性のため）
        if (mainMenuController != null && mainMenuController.backgroundAudioSource != null)
        {
            mainMenuController.backgroundAudioSource.volume = currentBgmVolume;
            Debug.Log($"MainMenuController BGM音量適用: {currentBgmVolume}");
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

        Debug.Log($"音量設定更新: BGM={bgmVolume}, SE={seVolume}, Master={masterVolume}");

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
        Debug.Log("TitleSceneSettingsManager: LoadSettings()をスキップ（LoadAndApplyVolumeSettingsで処理済み）");
    }
}