using UnityEngine;

public class ConversationSettingsManager : BaseSettingsManager
{
    [Header("ConversationFatherAndDaughter固有の設定")]
    [SerializeField] private ConversationSceneController sceneController;

    protected override void Start()
    {
        base.Start();

        // SoundEffectManagerが存在しない場合は作成
        if (SoundEffectManager.Instance == null)
        {
            GameObject soundManagerObj = new GameObject("SoundEffectManager");
            soundManagerObj.AddComponent<SoundEffectManager>();
            DontDestroyOnLoad(soundManagerObj);
        }

        // シーンコントローラーの自動検索
        if (sceneController == null)
            sceneController = FindFirstObjectByType<ConversationSceneController>();

        // BGM AudioSourceの自動検索（BaseSettingsManagerのbgmAudioSourceを使用）
        if (bgmAudioSource == null)
        {
            bgmAudioSource = GameObject.Find("BGMAudioSource")?.GetComponent<AudioSource>();
        }

        // 音量設定の読み込みと適用
        LoadAndApplyVolumeSettings();
    }

    /// <summary>
    /// 音量設定を読み込んで適用
    /// </summary>
    private void LoadAndApplyVolumeSettings()
    {
        // GameSaveManagerから音量設定を読み込む
        if (GameSaveManager.Instance != null)
        {
            var saveData = GameSaveManager.Instance.GetCurrentSaveData();
            if (saveData?.audioSettings != null)
            {
                // BaseSettingsManagerの変数を直接更新
                currentBgmVolume = saveData.audioSettings.bgmVolume;
                currentSeVolume = saveData.audioSettings.seVolume;

                // 音量を適用
                ApplyVolumeSettings();
                UpdateSliderValues();

                return;
            }
        }

        // game_save.jsonに設定がない場合はデフォルト値を使用
        LoadDefaultSettings();
    }

    /// <summary>
    /// デフォルト設定を読み込む
    /// </summary>
    protected override void LoadDefaultSettings()
    {
        currentBgmVolume = 0.5f;
        currentSeVolume = 0.5f;

        ApplyVolumeSettings();
        UpdateSliderValues();
    }

    /// <summary>
    /// BGM音量が変更された時の処理（オーバーライド）
    /// </summary>
    protected override void OnBgmVolumeChanged(float volume)
    {
        base.OnBgmVolumeChanged(volume);

        // game_save.jsonに保存
        SaveVolumeToGameSave();
    }

    /// <summary>
    /// SE音量が変更された時の処理（オーバーライド）
    /// </summary>
    protected override void OnSeVolumeChanged(float volume)
    {
        base.OnSeVolumeChanged(volume);

        // game_save.jsonに保存
        SaveVolumeToGameSave();
    }

    /// <summary>
    /// 音量設定をgame_save.jsonに保存
    /// </summary>
    private void SaveVolumeToGameSave()
    {
        if (GameSaveManager.Instance != null)
        {
            // まずPlayerPrefsに現在の音量を保存（GameSaveManagerが参照するため）
            PlayerPrefs.SetFloat("BGMVolume", currentBgmVolume);
            PlayerPrefs.SetFloat("SEVolume", currentSeVolume);
            PlayerPrefs.Save();

            // 現在の音量設定を収集（BaseSettingsManagerの値を使用）
            GameSaveManager.Instance.UpdateAudioSettings(currentBgmVolume, currentSeVolume);

            // 音量設定のみを保存
            GameSaveManager.Instance.SaveAudioSettingsOnly();
        }
    }

    /// <summary>
    /// 設定を保存（オーバーライド）
    /// </summary>
    protected override void SaveSettings()
    {
        // まずBGM音量をPlayerPrefsに保存
        PlayerPrefs.SetFloat("BGMVolume", currentBgmVolume);

        // SE音量はSoundEffectManagerに任せる
        if (soundEffectManager != null)
        {
            soundEffectManager.SetVolume(currentSeVolume);
            soundEffectManager.SaveVolumeSettingsWithGameSaveManager();
        }
        else
        {
            // SoundEffectManagerがない場合は直接保存
            PlayerPrefs.SetFloat("SEVolume", currentSeVolume);
        }
        // PlayerPrefsを確実に保存
        PlayerPrefs.Save();

        // BGM音量をgame_save.jsonに保存
        SaveVolumeToGameSave();
    }

    /// <summary>
    /// 設定画面の表示/非表示を切り替える
    /// </summary>
    public override void ToggleSettings()
    {
        // 基底クラスのToggleSettingsを呼び出す前に、現在の状態を保存
        bool wasVisible = isSettingsVisible;

        base.ToggleSettings();

        // ConversationSceneControllerに設定画面の状態を通知
        if (sceneController != null)
        {
            // 状態が変更された場合のみ通知
            if (wasVisible != isSettingsVisible)
            {
                sceneController.SetSettingsOpen(isSettingsVisible);
            }
        }
    }

    /// <summary>
    /// 設定画面を表示
    /// </summary>
    public override void ShowSettings()
    {
        base.ShowSettings();

        // ConversationSceneControllerに設定画面が開いたことを通知
        if (sceneController != null)
        {
            sceneController.SetSettingsOpen(true);
        }
    }

    /// <summary>
    /// 設定画面を非表示
    /// </summary>
    public override void HideSettings()
    {
        base.HideSettings();

        // ConversationSceneControllerに設定画面が閉じたことを通知
        if (sceneController != null)
        {
            sceneController.SetSettingsOpen(false);
        }
    }

    /// <summary>
    /// 設定画面が開かれた時の処理
    /// </summary>
    protected override void OnSettingsOpened()
    {
        base.OnSettingsOpened();

        // 追加の処理が必要な場合はここに記述
    }

    /// <summary>
    /// 設定画面が閉じられた時の処理
    /// </summary>
    protected override void OnSettingsClosed()
    {
        base.OnSettingsClosed();

        // 追加の処理が必要な場合はここに記述
    }
}