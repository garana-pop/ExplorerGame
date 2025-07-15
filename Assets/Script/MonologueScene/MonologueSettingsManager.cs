using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// MonologueScene専用の設定管理クラス
/// モノローグ表示中でも設定画面を開けるが、背後の演出は継続
/// </summary>
public class MonologueSettingsManager : BaseSettingsManager
{
    [Header("MonologueScene固有の参照")]
    [SerializeField] private MonologueDisplayManager monologueDisplayManager;
    [SerializeField] private GameObject animationArea;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private ContinueIndicatorAnimation continueIndicatorAnimation;

    private bool wasMonologuePaused = false;

    protected override void Start()
    {
        base.Start();

        if (monologueDisplayManager == null)
        {
            monologueDisplayManager = FindObjectOfType<MonologueDisplayManager>();
        }

        if (animationArea == null)
        {
            animationArea = GameObject.Find("AnimationArea");
        }

        if (dialogueText == null)
        {
            var dialogueArea = GameObject.Find("DialogueArea");
            if (dialogueArea != null)
            {
                dialogueText = dialogueArea.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        if (continueIndicatorAnimation == null)
        {
            continueIndicatorAnimation = FindObjectOfType<ContinueIndicatorAnimation>();
        }

        if (bgmAudioSource == null)
        {
            bgmAudioSource = GameObject.Find("BGMAudioSource")?.GetComponent<AudioSource>();
        }
        canOpenSettings = true;

        // 音量設定の読み込みと適用
        LoadAndApplyVolumeSettings();
    }

    /// <summary>
    /// game_save.jsonからの音量設定読み込み機能
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

                // 設定を適用
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

        SaveVolumeToGameSave();// 変更時に即座に保存
    }

    /// <summary>
    /// SE音量が変更された時の処理（オーバーライド）
    /// </summary>
    protected override void OnSeVolumeChanged(float volume)
    {
        base.OnSeVolumeChanged(volume);

        SaveVolumeToGameSave();// 変更時に即座に保存
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
    /// 設定画面が開かれた時の処理
    /// </summary>
    protected override void OnSettingsOpened()
    {
        base.OnSettingsOpened();

        if (monologueDisplayManager != null)
        {
            monologueDisplayManager.SetSettingsOpen(true);
        }

        if (continueIndicatorAnimation != null)
        {
            continueIndicatorAnimation.enabled = false;
        }
    }

    /// <summary>
    /// 設定画面が閉じられた時の処理
    /// </summary>
    protected override void OnSettingsClosed()
    {
        base.OnSettingsClosed();

        if (monologueDisplayManager != null)
        {
            monologueDisplayManager.SetSettingsOpen(false);
        }

        if (continueIndicatorAnimation != null)
        {
            continueIndicatorAnimation.enabled = true;
        }
    }

    public void OnMonologueEnd()
    {
        canOpenSettings = false;
    }

    public void SetBackgroundAnimationPaused(bool paused)
    {
        if (animationArea != null)
        {
            var animators = animationArea.GetComponentsInChildren<Animator>();
            foreach (var animator in animators)
            {
                if (paused)
                {
                    animator.speed = 0f;
                }
                else
                {
                    animator.speed = 1f;
                }
            }
        }
    }
}