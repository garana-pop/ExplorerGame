using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// MainScene内のすべての効果音(SE)を一元管理するマネージャークラス
/// 各スクリプトは個別にAudioSourceやAudioClipを管理する代わりに、このマネージャーを使用して効果音を再生します
/// </summary>
public class SoundEffectManager : MonoBehaviour
{
    #region シングルトンパターン実装
    private static SoundEffectManager _instance;
    public static SoundEffectManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SoundEffectManager>(FindObjectsInactive.Include);

                if (_instance == null && Application.isPlaying)
                {
                    GameObject go = new GameObject("SoundEffectManager");
                    _instance = go.AddComponent<SoundEffectManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
        private set { _instance = value; } // set アクセサを追加
    }

    #endregion

    [System.Serializable]
    public class SoundCategory
    {
        [Tooltip("カテゴリ名")]
        public string categoryName;

        [Tooltip("このカテゴリに含まれる効果音")]
        public List<AudioClip> clips;
    }

    [Header("共通設定")]
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private float defaultVolume = 0.7f;
    [SerializeField] private float minVolume = 0.0f;
    [SerializeField] private float maxVolume = 1.0f;
    [SerializeField] private string seVolumePrefsKey = "SEVolume";

    [Header("初期ミュート設定")]
    [Tooltip("ゲーム開始時にuiClickSound以外の効果音を無効にする秒数")]
    [SerializeField] private float initialMuteDuration = 1.5f;
    private float gameStartTime;

    [Header("効果音カテゴリ")]
    [SerializeField] private List<SoundCategory> soundCategories = new List<SoundCategory>();

    [Header("共通効果音")]
    [Tooltip("UI要素クリック時の効果音")]
    [SerializeField] private AudioClip uiClickSound;

    [Tooltip("情報表示/発見時の効果音")]
    [SerializeField] private AudioClip revealSound;

    [Tooltip("段階的進行完了時の効果音")]
    [SerializeField] private AudioClip progressCompletionSound;

    [Tooltip("最終完了/成功時の効果音")]
    [SerializeField] private AudioClip allRevealedSound;

    [Header("追加効果音")]
    [Tooltip("ファイル開く効果音")]
    [SerializeField] private AudioClip fileOpenSound;

    [Tooltip("ファイル閉じる効果音")]
    [SerializeField] private AudioClip fileCloseSound;

    [Tooltip("エラー/失敗時の効果音")]
    [SerializeField] private AudioClip errorSound;

    [Tooltip("タイプライター効果音")]
    [SerializeField] private AudioClip typeSound;

    [Tooltip("句読点タイプ効果音")]
    [SerializeField] private AudioClip punctuationTypeSound;

    private AudioSource audioSource;

    // 効果音ボリューム変更通知のデリゲート定義
    public delegate void VolumeChangedHandler(float newVolume);

    // 効果音ボリューム変更時に発行されるイベント
    public event VolumeChangedHandler OnVolumeChanged;

    // 効果音ボリューム
    private float currentSEVolume;

    // 効果音のキャッシュディクショナリ
    private Dictionary<string, AudioClip> soundCache = new Dictionary<string, AudioClip>();

    // 効果音キャッシュ用辞書
    private Dictionary<string, AudioClip> effectCache = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        // シングルトンパターンの実装
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // AudioSourceの初期化
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // sfxAudioSourceが設定されていない場合はaudioSourceを使用
            if (sfxAudioSource == null)
            {
                sfxAudioSource = audioSource;
            }

            // 【修正箇所】game_save.json -> PlayerPrefs -> デフォルト値の順で音量を読み込み
            LoadVolumeFromGameSave();

            // エフェクト音のキャッシュを作成
            effectCache = new Dictionary<string, AudioClip>();

            // soundCacheの初期化も必要
            BuildSoundCache();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// game_save.jsonから音量設定を読み込む
    /// </summary>
    private void LoadVolumeFromGameSave()
    {
        float loadedVolume = defaultVolume; // デフォルト値

        // まずPlayerPrefsから読み込み
        loadedVolume = PlayerPrefs.GetFloat(seVolumePrefsKey, defaultVolume);

        // GameSaveManagerから読み込みを試行
        if (GameSaveManager.Instance != null)
        {
            try
            {
                var saveData = GameSaveManager.Instance.GetCurrentSaveData();
                if (saveData?.audioSettings != null)
                {
                    loadedVolume = saveData.audioSettings.seVolume;
                    //Debug.Log($"SoundEffectManager: game_save.jsonからSE音量読み込み: {loadedVolume}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"SoundEffectManager: game_save.json読み込みエラー: {e.Message}");
            }
        }

        // 読み込んだ音量を設定
        currentSEVolume = loadedVolume;
        ApplyVolume();

        //Debug.Log($"SoundEffectManager: 最終SE音量: {currentSEVolume}");
    }


    private void OnDestroy()
    {
        // シーン変更イベントのリスナーを削除
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // シーン変更時に呼び出されるメソッド
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 音量設定を新しいシーンに適用
        ApplyVolumeToScene();

        // シーン変更時にもgame_save.jsonから最新の値を読み込み
        LoadVolumeFromGameSave();
    }


    // 新しいシーンに音量設定を適用するメソッド
    private void ApplyVolumeToScene()
    {
        // BGM用のAudioSourceを検索
        AudioSource[] audioSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AudioSource source in audioSources)
        {
            // 名前にBGMが含まれるものにボリュームを適用
            if (source != sfxAudioSource &&
                (source.gameObject.name.Contains("BGM") ||
                 source.gameObject.name.Contains("Background") ||
                 source.gameObject.name.Contains("Music")))
            {
                // PlayerPrefsから最新のBGM音量を取得
                float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
                source.volume = bgmVolume;
            }
        }

        // マスター音量を適用
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        AudioListener.volume = masterVolume;

        // SE音量を適用
        ApplyVolume();
    }

    // GameSaveManagerと連携して音量設定を保存するメソッド
    public void SaveVolumeSettingsWithGameSaveManager()
    {
        // 設定を保存
        PlayerPrefs.SetFloat(seVolumePrefsKey, currentSEVolume);
        PlayerPrefs.Save();

        // GameSaveManagerがあれば音量設定を保存
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.SaveAudioSettingsOnly();
        }
    }

    private void Start()
    {
        // game_save.jsonから音量を再読み込み
        LoadVolumeFromGameSave();

        // ボリューム初期設定
        ApplyVolume();

        // テスト音を低音量で再生して初期化確認（オプション）
        PlayClickSound(0.2f);
    }

    /// <summary>
    /// サウンドのキャッシュを構築
    /// </summary>
    private void BuildSoundCache()
    {
        // 共通効果音の登録
        RegisterSound("Click", uiClickSound);
        RegisterSound("Reveal", revealSound);
        RegisterSound("Completion", progressCompletionSound);
        RegisterSound("AllRevealed", allRevealedSound);
        RegisterSound("FileOpen", fileOpenSound);
        RegisterSound("FileClose", fileCloseSound);
        RegisterSound("Error", errorSound);
        RegisterSound("Type", typeSound);
        RegisterSound("PunctuationType", punctuationTypeSound);

        // カテゴリごとの効果音を登録
        foreach (var category in soundCategories)
        {
            if (string.IsNullOrEmpty(category.categoryName)) continue;

            for (int i = 0; i < category.clips.Count; i++)
            {
                if (category.clips[i] == null) continue;

                string key = category.categoryName + "_" + i;
                RegisterSound(key, category.clips[i]);
            }
        }
    }

    /// <summary>
    /// 効果音をキャッシュに登録
    /// </summary>
    private void RegisterSound(string key, AudioClip clip)
    {
        if (clip != null && !soundCache.ContainsKey(key))
        {
            soundCache[key] = clip;
        }
    }

    /// <summary>
    /// 保存されているボリューム設定をロード
    /// </summary>
    private void LoadVolume()
    {
        currentSEVolume = PlayerPrefs.GetFloat(seVolumePrefsKey, defaultVolume);
    }

    /// <summary>
    /// ボリュームを適用
    /// </summary>
    private void ApplyVolume()
    {
        if (sfxAudioSource != null)
        {
            sfxAudioSource.volume = currentSEVolume;
        }
    }

    /// <summary>
    /// 効果音のボリュームを設定
    /// </summary>
    public void SetVolume(float volume)
    {
        // 変更前のボリュームを保存
        float previousVolume = currentSEVolume;

        // 値の範囲を制限
        currentSEVolume = Mathf.Clamp(volume, minVolume, maxVolume);

        // 実際に値が変わった場合のみ処理を続行
        if (previousVolume != currentSEVolume)
        {
            // AudioSourceに適用
            ApplyVolume();

            // 設定を保存
            PlayerPrefs.SetFloat(seVolumePrefsKey, currentSEVolume);
            PlayerPrefs.Save();

            // 変更通知イベントを発行
            OnVolumeChanged?.Invoke(currentSEVolume);

            // GameSaveManagerにも保存
            if (GameSaveManager.Instance != null)
            {
                try
                {
                    // 現在のBGM音量とマスター音量も取得
                    float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
                    float masterVolume = AudioListener.volume;

                    // game_save.jsonに保存
                    GameSaveManager.Instance.UpdateAudioSettings(bgmVolume, currentSEVolume, masterVolume);
                    GameSaveManager.Instance.SaveAudioSettingsOnly();
                    //Debug.Log($"SoundEffectManager: SE音量をgame_save.jsonに保存: {currentSEVolume}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"SoundEffectManager: game_save.jsonへの保存エラー: {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// 現在の効果音ボリュームを取得
    /// </summary>
    public float GetVolume()
    {
        return currentSEVolume;
    }

    /// <summary>
    /// 現在の効果音ボリュームをPlayerPrefsから取得（まだロードされていない場合）
    /// </summary>
    public float GetSavedVolume()
    {
        return PlayerPrefs.HasKey(seVolumePrefsKey)
            ? PlayerPrefs.GetFloat(seVolumePrefsKey, defaultVolume)
            : defaultVolume;
    }

    #region 効果音再生メソッド - 共通効果音

    /// <summary>
    /// クリック効果音を再生
    /// </summary>
    public void PlayClickSound()
    {
        PlaySound("Click");
    }

    /// <summary>
    /// クリック効果音を指定ボリュームで再生
    /// </summary>
    public void PlayClickSound(float volumeScale)
    {
        PlaySound("Click", volumeScale);
    }

    /// <summary>
    /// 発見/表示効果音を再生
    /// </summary>
    public void PlayRevealSound()
    {
        PlaySound("Reveal");
    }

    /// <summary>
    /// 発見/表示効果音を指定ボリュームで再生
    /// </summary>
    public void PlayRevealSound(float volumeScale)
    {
        PlaySound("Reveal", volumeScale);
    }

    /// <summary>
    /// 進行完了効果音を再生
    /// </summary>
    public void PlayCompletionSound()
    {
        PlaySound("Completion");
    }

    /// <summary>
    /// 進行完了効果音を指定ボリュームで再生
    /// </summary>
    public void PlayCompletionSound(float volumeScale)
    {
        PlaySound("Completion", volumeScale);
    }

    /// <summary>
    /// 全体完了効果音を再生
    /// </summary>
    public void PlayAllRevealedSound()
    {
        PlaySound("AllRevealed");
    }

    /// <summary>
    /// 全体完了効果音を指定ボリュームで再生
    /// </summary>
    public void PlayAllRevealedSound(float volumeScale)
    {
        PlaySound("AllRevealed", volumeScale);
    }

    /// <summary>
    /// ファイルを開く効果音を再生
    /// </summary>
    public void PlayFileOpenSound()
    {
        PlaySound("FileOpen");
    }

    /// <summary>
    /// ファイルを開く効果音を指定ボリュームで再生
    /// </summary>
    public void PlayFileOpenSound(float volumeScale)
    {
        PlaySound("FileOpen", volumeScale);
    }

    /// <summary>
    /// ファイルを閉じる効果音を再生
    /// </summary>
    public void PlayFileCloseSound()
    {
        PlaySound("FileClose");
    }

    /// <summary>
    /// ファイルを閉じる効果音を指定ボリュームで再生
    /// </summary>
    public void PlayFileCloseSound(float volumeScale)
    {
        PlaySound("FileClose", volumeScale);
    }

    /// <summary>
    /// エラー効果音を再生
    /// </summary>
    public void PlayErrorSound()
    {
        PlaySound("Error");
    }

    /// <summary>
    /// エラー効果音を指定ボリュームで再生
    /// </summary>
    public void PlayErrorSound(float volumeScale)
    {
        PlaySound("Error", volumeScale);
    }

    /// <summary>
    /// タイプライター効果音を再生
    /// </summary>
    public void PlayTypeSound()
    {
        PlaySound("Type");
    }

    /// <summary>
    /// タイプライター効果音を指定ボリュームで再生
    /// </summary>
    public void PlayTypeSound(float volumeScale)
    {
        PlaySound("Type", volumeScale);
    }

    /// <summary>
    /// 句読点タイプ効果音を再生
    /// </summary>
    public void PlayPunctuationTypeSound()
    {
        PlaySound("PunctuationType");
    }

    /// <summary>
    /// 句読点タイプ効果音を指定ボリュームで再生
    /// </summary>
    public void PlayPunctuationTypeSound(float volumeScale)
    {
        PlaySound("PunctuationType", volumeScale);
    }

    #endregion

    #region 効果音再生メソッド - 汎用

    /// <summary>
    /// 指定されたキーの効果音を再生
    /// </summary>
    public void PlaySound(string soundKey)
    {
        if (sfxAudioSource == null)
        {
            Debug.LogWarning("AudioSource がセットされていません");
            return;
        }

        // clickSound以外の効果音がゲーム開始指定時間内は再生されないようにする
        if (soundKey != "Click" && Time.time - gameStartTime < initialMuteDuration)
        {
            return;
        }

        if (soundCache.TryGetValue(soundKey, out AudioClip clip) && clip != null)
        {
            sfxAudioSource.PlayOneShot(clip, currentSEVolume);
        }
        else
        {
            Debug.LogWarning($"効果音 '{soundKey}' が見つかりません");
        }
    }

    /// <summary>
    /// 指定されたキーの効果音を指定ボリュームで再生
    /// </summary>
    public void PlaySound(string soundKey, float volumeScale)
    {
        if (sfxAudioSource == null)
        {
            Debug.LogWarning("AudioSource がセットされていません");
            return;
        }

        // clickSound以外の効果音がゲーム開始指定時間内は再生されないようにする
        if (soundKey != "Click" && Time.time - gameStartTime < initialMuteDuration)
        {
            return;
        }

        if (soundCache.TryGetValue(soundKey, out AudioClip clip) && clip != null)
        {
            // スケール調整されたボリュームで再生（現在のSEボリュームにスケールを掛ける）
            sfxAudioSource.PlayOneShot(clip, currentSEVolume * volumeScale);
        }
        else
        {
            Debug.LogWarning($"効果音 '{soundKey}' が見つかりません");
        }
    }

    /// <summary>
    /// 指定されたAudioClipを再生
    /// </summary>
    public void PlaySound(AudioClip clip)
    {
        if (sfxAudioSource == null)
        {
            Debug.LogWarning("AudioSource がセットされていません");
            return;
        }

        // uiClickSound以外の効果音がゲーム開始指定時間内は再生されないようにする
        if (clip != uiClickSound && Time.time - gameStartTime < initialMuteDuration)
        {
            return;
        }

        if (clip != null)
        {
            sfxAudioSource.PlayOneShot(clip, currentSEVolume);
        }
    }

    /// <summary>
    /// 指定されたAudioClipを指定ボリュームで再生
    /// </summary>
    public void PlaySound(AudioClip clip, float volumeScale)
    {
        if (sfxAudioSource == null)
        {
            Debug.LogWarning("AudioSource がセットされていません");
            return;
        }

        // uiClickSound以外の効果音がゲーム開始指定時間内は再生されないようにする
        if (clip != uiClickSound && Time.time - gameStartTime < initialMuteDuration)
        {
            return;
        }

        if (clip != null)
        {
            // スケール調整されたボリュームで再生
            sfxAudioSource.PlayOneShot(clip, currentSEVolume * volumeScale);
        }
    }

    /// <summary>
    /// カテゴリ内の効果音を再生 (カテゴリ名とインデックスで指定)
    /// </summary>
    public void PlayCategorySound(string categoryName, int index)
    {
        string soundKey = categoryName + "_" + index;
        PlaySound(soundKey);
    } 

    /// <summary>
    /// すべての効果音を停止
    /// </summary>
    public void StopAllSounds()
    {
        if (sfxAudioSource != null)
        {
            sfxAudioSource.Stop();
        }
    }

    /// <summary>
    /// シーン遷移時などに呼び出して、設定を保存する
    /// </summary>
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(seVolumePrefsKey, currentSEVolume);
        PlayerPrefs.Save();
    }
    #endregion
}