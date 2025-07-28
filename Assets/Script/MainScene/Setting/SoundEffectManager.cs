using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// MainScene���̂��ׂĂ̌��ʉ�(SE)���ꌳ�Ǘ�����}�l�[�W���[�N���X
/// �e�X�N���v�g�͌ʂ�AudioSource��AudioClip���Ǘ��������ɁA���̃}�l�[�W���[���g�p���Č��ʉ����Đ����܂�
/// </summary>
public class SoundEffectManager : MonoBehaviour
{
    #region �V���O���g���p�^�[������
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
        private set { _instance = value; } // set �A�N�Z�T��ǉ�
    }

    #endregion

    [System.Serializable]
    public class SoundCategory
    {
        [Tooltip("�J�e�S����")]
        public string categoryName;

        [Tooltip("���̃J�e�S���Ɋ܂܂����ʉ�")]
        public List<AudioClip> clips;
    }

    [Header("���ʐݒ�")]
    [SerializeField] private AudioSource sfxAudioSource;
    [SerializeField] private float defaultVolume = 0.7f;
    [SerializeField] private float minVolume = 0.0f;
    [SerializeField] private float maxVolume = 1.0f;
    [SerializeField] private string seVolumePrefsKey = "SEVolume";

    [Header("�����~���[�g�ݒ�")]
    [Tooltip("�Q�[���J�n����uiClickSound�ȊO�̌��ʉ��𖳌��ɂ���b��")]
    [SerializeField] private float initialMuteDuration = 1.5f;
    private float gameStartTime;

    [Header("���ʉ��J�e�S��")]
    [SerializeField] private List<SoundCategory> soundCategories = new List<SoundCategory>();

    [Header("���ʌ��ʉ�")]
    [Tooltip("UI�v�f�N���b�N���̌��ʉ�")]
    [SerializeField] private AudioClip uiClickSound;

    [Tooltip("���\��/�������̌��ʉ�")]
    [SerializeField] private AudioClip revealSound;

    [Tooltip("�i�K�I�i�s�������̌��ʉ�")]
    [SerializeField] private AudioClip progressCompletionSound;

    [Tooltip("�ŏI����/�������̌��ʉ�")]
    [SerializeField] private AudioClip allRevealedSound;

    [Header("�ǉ����ʉ�")]
    [Tooltip("�t�@�C���J�����ʉ�")]
    [SerializeField] private AudioClip fileOpenSound;

    [Tooltip("�t�@�C��������ʉ�")]
    [SerializeField] private AudioClip fileCloseSound;

    [Tooltip("�G���[/���s���̌��ʉ�")]
    [SerializeField] private AudioClip errorSound;

    [Tooltip("�^�C�v���C�^�[���ʉ�")]
    [SerializeField] private AudioClip typeSound;

    [Tooltip("��Ǔ_�^�C�v���ʉ�")]
    [SerializeField] private AudioClip punctuationTypeSound;

    private AudioSource audioSource;

    // ���ʉ��{�����[���ύX�ʒm�̃f���Q�[�g��`
    public delegate void VolumeChangedHandler(float newVolume);

    // ���ʉ��{�����[���ύX���ɔ��s�����C�x���g
    public event VolumeChangedHandler OnVolumeChanged;

    // ���ʉ��{�����[��
    private float currentSEVolume;

    // ���ʉ��̃L���b�V���f�B�N�V���i��
    private Dictionary<string, AudioClip> soundCache = new Dictionary<string, AudioClip>();

    // ���ʉ��L���b�V���p����
    private Dictionary<string, AudioClip> effectCache = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        // �V���O���g���p�^�[���̎���
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);

            // AudioSource�̏�����
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // sfxAudioSource���ݒ肳��Ă��Ȃ��ꍇ��audioSource���g�p
            if (sfxAudioSource == null)
            {
                sfxAudioSource = audioSource;
            }

            // �y�C���ӏ��zgame_save.json -> PlayerPrefs -> �f�t�H���g�l�̏��ŉ��ʂ�ǂݍ���
            LoadVolumeFromGameSave();

            // �G�t�F�N�g���̃L���b�V�����쐬
            effectCache = new Dictionary<string, AudioClip>();

            // soundCache�̏��������K�v
            BuildSoundCache();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// game_save.json���特�ʐݒ��ǂݍ���
    /// </summary>
    private void LoadVolumeFromGameSave()
    {
        float loadedVolume = defaultVolume; // �f�t�H���g�l

        // �܂�PlayerPrefs����ǂݍ���
        loadedVolume = PlayerPrefs.GetFloat(seVolumePrefsKey, defaultVolume);

        // GameSaveManager����ǂݍ��݂����s
        if (GameSaveManager.Instance != null)
        {
            try
            {
                var saveData = GameSaveManager.Instance.GetCurrentSaveData();
                if (saveData?.audioSettings != null)
                {
                    loadedVolume = saveData.audioSettings.seVolume;
                    //Debug.Log($"SoundEffectManager: game_save.json����SE���ʓǂݍ���: {loadedVolume}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"SoundEffectManager: game_save.json�ǂݍ��݃G���[: {e.Message}");
            }
        }

        // �ǂݍ��񂾉��ʂ�ݒ�
        currentSEVolume = loadedVolume;
        ApplyVolume();

        //Debug.Log($"SoundEffectManager: �ŏISE����: {currentSEVolume}");
    }


    private void OnDestroy()
    {
        // �V�[���ύX�C�x���g�̃��X�i�[���폜
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // �V�[���ύX���ɌĂяo����郁�\�b�h
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ���ʐݒ��V�����V�[���ɓK�p
        ApplyVolumeToScene();

        // �V�[���ύX���ɂ�game_save.json����ŐV�̒l��ǂݍ���
        LoadVolumeFromGameSave();
    }


    // �V�����V�[���ɉ��ʐݒ��K�p���郁�\�b�h
    private void ApplyVolumeToScene()
    {
        // BGM�p��AudioSource������
        AudioSource[] audioSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AudioSource source in audioSources)
        {
            // ���O��BGM���܂܂����̂Ƀ{�����[����K�p
            if (source != sfxAudioSource &&
                (source.gameObject.name.Contains("BGM") ||
                 source.gameObject.name.Contains("Background") ||
                 source.gameObject.name.Contains("Music")))
            {
                // PlayerPrefs����ŐV��BGM���ʂ��擾
                float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
                source.volume = bgmVolume;
            }
        }

        // �}�X�^�[���ʂ�K�p
        float masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        AudioListener.volume = masterVolume;

        // SE���ʂ�K�p
        ApplyVolume();
    }

    // GameSaveManager�ƘA�g���ĉ��ʐݒ��ۑ����郁�\�b�h
    public void SaveVolumeSettingsWithGameSaveManager()
    {
        // �ݒ��ۑ�
        PlayerPrefs.SetFloat(seVolumePrefsKey, currentSEVolume);
        PlayerPrefs.Save();

        // GameSaveManager������Ή��ʐݒ��ۑ�
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.SaveAudioSettingsOnly();
        }
    }

    private void Start()
    {
        // game_save.json���特�ʂ��ēǂݍ���
        LoadVolumeFromGameSave();

        // �{�����[�������ݒ�
        ApplyVolume();

        // �e�X�g����ቹ�ʂōĐ����ď������m�F�i�I�v�V�����j
        PlayClickSound(0.2f);
    }

    /// <summary>
    /// �T�E���h�̃L���b�V�����\�z
    /// </summary>
    private void BuildSoundCache()
    {
        // ���ʌ��ʉ��̓o�^
        RegisterSound("Click", uiClickSound);
        RegisterSound("Reveal", revealSound);
        RegisterSound("Completion", progressCompletionSound);
        RegisterSound("AllRevealed", allRevealedSound);
        RegisterSound("FileOpen", fileOpenSound);
        RegisterSound("FileClose", fileCloseSound);
        RegisterSound("Error", errorSound);
        RegisterSound("Type", typeSound);
        RegisterSound("PunctuationType", punctuationTypeSound);

        // �J�e�S�����Ƃ̌��ʉ���o�^
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
    /// ���ʉ����L���b�V���ɓo�^
    /// </summary>
    private void RegisterSound(string key, AudioClip clip)
    {
        if (clip != null && !soundCache.ContainsKey(key))
        {
            soundCache[key] = clip;
        }
    }

    /// <summary>
    /// �ۑ�����Ă���{�����[���ݒ�����[�h
    /// </summary>
    private void LoadVolume()
    {
        currentSEVolume = PlayerPrefs.GetFloat(seVolumePrefsKey, defaultVolume);
    }

    /// <summary>
    /// �{�����[����K�p
    /// </summary>
    private void ApplyVolume()
    {
        if (sfxAudioSource != null)
        {
            sfxAudioSource.volume = currentSEVolume;
        }
    }

    /// <summary>
    /// ���ʉ��̃{�����[����ݒ�
    /// </summary>
    public void SetVolume(float volume)
    {
        // �ύX�O�̃{�����[����ۑ�
        float previousVolume = currentSEVolume;

        // �l�͈̔͂𐧌�
        currentSEVolume = Mathf.Clamp(volume, minVolume, maxVolume);

        // ���ۂɒl���ς�����ꍇ�̂ݏ����𑱍s
        if (previousVolume != currentSEVolume)
        {
            // AudioSource�ɓK�p
            ApplyVolume();

            // �ݒ��ۑ�
            PlayerPrefs.SetFloat(seVolumePrefsKey, currentSEVolume);
            PlayerPrefs.Save();

            // �ύX�ʒm�C�x���g�𔭍s
            OnVolumeChanged?.Invoke(currentSEVolume);

            // GameSaveManager�ɂ��ۑ�
            if (GameSaveManager.Instance != null)
            {
                try
                {
                    // ���݂�BGM���ʂƃ}�X�^�[���ʂ��擾
                    float bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.5f);
                    float masterVolume = AudioListener.volume;

                    // game_save.json�ɕۑ�
                    GameSaveManager.Instance.UpdateAudioSettings(bgmVolume, currentSEVolume, masterVolume);
                    GameSaveManager.Instance.SaveAudioSettingsOnly();
                    //Debug.Log($"SoundEffectManager: SE���ʂ�game_save.json�ɕۑ�: {currentSEVolume}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"SoundEffectManager: game_save.json�ւ̕ۑ��G���[: {e.Message}");
                }
            }
        }
    }

    /// <summary>
    /// ���݂̌��ʉ��{�����[�����擾
    /// </summary>
    public float GetVolume()
    {
        return currentSEVolume;
    }

    /// <summary>
    /// ���݂̌��ʉ��{�����[����PlayerPrefs����擾�i�܂����[�h����Ă��Ȃ��ꍇ�j
    /// </summary>
    public float GetSavedVolume()
    {
        return PlayerPrefs.HasKey(seVolumePrefsKey)
            ? PlayerPrefs.GetFloat(seVolumePrefsKey, defaultVolume)
            : defaultVolume;
    }

    #region ���ʉ��Đ����\�b�h - ���ʌ��ʉ�

    /// <summary>
    /// �N���b�N���ʉ����Đ�
    /// </summary>
    public void PlayClickSound()
    {
        PlaySound("Click");
    }

    /// <summary>
    /// �N���b�N���ʉ����w��{�����[���ōĐ�
    /// </summary>
    public void PlayClickSound(float volumeScale)
    {
        PlaySound("Click", volumeScale);
    }

    /// <summary>
    /// ����/�\�����ʉ����Đ�
    /// </summary>
    public void PlayRevealSound()
    {
        PlaySound("Reveal");
    }

    /// <summary>
    /// ����/�\�����ʉ����w��{�����[���ōĐ�
    /// </summary>
    public void PlayRevealSound(float volumeScale)
    {
        PlaySound("Reveal", volumeScale);
    }

    /// <summary>
    /// �i�s�������ʉ����Đ�
    /// </summary>
    public void PlayCompletionSound()
    {
        PlaySound("Completion");
    }

    /// <summary>
    /// �i�s�������ʉ����w��{�����[���ōĐ�
    /// </summary>
    public void PlayCompletionSound(float volumeScale)
    {
        PlaySound("Completion", volumeScale);
    }

    /// <summary>
    /// �S�̊������ʉ����Đ�
    /// </summary>
    public void PlayAllRevealedSound()
    {
        PlaySound("AllRevealed");
    }

    /// <summary>
    /// �S�̊������ʉ����w��{�����[���ōĐ�
    /// </summary>
    public void PlayAllRevealedSound(float volumeScale)
    {
        PlaySound("AllRevealed", volumeScale);
    }

    /// <summary>
    /// �t�@�C�����J�����ʉ����Đ�
    /// </summary>
    public void PlayFileOpenSound()
    {
        PlaySound("FileOpen");
    }

    /// <summary>
    /// �t�@�C�����J�����ʉ����w��{�����[���ōĐ�
    /// </summary>
    public void PlayFileOpenSound(float volumeScale)
    {
        PlaySound("FileOpen", volumeScale);
    }

    /// <summary>
    /// �t�@�C���������ʉ����Đ�
    /// </summary>
    public void PlayFileCloseSound()
    {
        PlaySound("FileClose");
    }

    /// <summary>
    /// �t�@�C���������ʉ����w��{�����[���ōĐ�
    /// </summary>
    public void PlayFileCloseSound(float volumeScale)
    {
        PlaySound("FileClose", volumeScale);
    }

    /// <summary>
    /// �G���[���ʉ����Đ�
    /// </summary>
    public void PlayErrorSound()
    {
        PlaySound("Error");
    }

    /// <summary>
    /// �G���[���ʉ����w��{�����[���ōĐ�
    /// </summary>
    public void PlayErrorSound(float volumeScale)
    {
        PlaySound("Error", volumeScale);
    }

    /// <summary>
    /// �^�C�v���C�^�[���ʉ����Đ�
    /// </summary>
    public void PlayTypeSound()
    {
        PlaySound("Type");
    }

    /// <summary>
    /// �^�C�v���C�^�[���ʉ����w��{�����[���ōĐ�
    /// </summary>
    public void PlayTypeSound(float volumeScale)
    {
        PlaySound("Type", volumeScale);
    }

    /// <summary>
    /// ��Ǔ_�^�C�v���ʉ����Đ�
    /// </summary>
    public void PlayPunctuationTypeSound()
    {
        PlaySound("PunctuationType");
    }

    /// <summary>
    /// ��Ǔ_�^�C�v���ʉ����w��{�����[���ōĐ�
    /// </summary>
    public void PlayPunctuationTypeSound(float volumeScale)
    {
        PlaySound("PunctuationType", volumeScale);
    }

    #endregion

    #region ���ʉ��Đ����\�b�h - �ėp

    /// <summary>
    /// �w�肳�ꂽ�L�[�̌��ʉ����Đ�
    /// </summary>
    public void PlaySound(string soundKey)
    {
        if (sfxAudioSource == null)
        {
            Debug.LogWarning("AudioSource ���Z�b�g����Ă��܂���");
            return;
        }

        // clickSound�ȊO�̌��ʉ����Q�[���J�n�w�莞�ԓ��͍Đ�����Ȃ��悤�ɂ���
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
            Debug.LogWarning($"���ʉ� '{soundKey}' ��������܂���");
        }
    }

    /// <summary>
    /// �w�肳�ꂽ�L�[�̌��ʉ����w��{�����[���ōĐ�
    /// </summary>
    public void PlaySound(string soundKey, float volumeScale)
    {
        if (sfxAudioSource == null)
        {
            Debug.LogWarning("AudioSource ���Z�b�g����Ă��܂���");
            return;
        }

        // clickSound�ȊO�̌��ʉ����Q�[���J�n�w�莞�ԓ��͍Đ�����Ȃ��悤�ɂ���
        if (soundKey != "Click" && Time.time - gameStartTime < initialMuteDuration)
        {
            return;
        }

        if (soundCache.TryGetValue(soundKey, out AudioClip clip) && clip != null)
        {
            // �X�P�[���������ꂽ�{�����[���ōĐ��i���݂�SE�{�����[���ɃX�P�[�����|����j
            sfxAudioSource.PlayOneShot(clip, currentSEVolume * volumeScale);
        }
        else
        {
            Debug.LogWarning($"���ʉ� '{soundKey}' ��������܂���");
        }
    }

    /// <summary>
    /// �w�肳�ꂽAudioClip���Đ�
    /// </summary>
    public void PlaySound(AudioClip clip)
    {
        if (sfxAudioSource == null)
        {
            Debug.LogWarning("AudioSource ���Z�b�g����Ă��܂���");
            return;
        }

        // uiClickSound�ȊO�̌��ʉ����Q�[���J�n�w�莞�ԓ��͍Đ�����Ȃ��悤�ɂ���
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
    /// �w�肳�ꂽAudioClip���w��{�����[���ōĐ�
    /// </summary>
    public void PlaySound(AudioClip clip, float volumeScale)
    {
        if (sfxAudioSource == null)
        {
            Debug.LogWarning("AudioSource ���Z�b�g����Ă��܂���");
            return;
        }

        // uiClickSound�ȊO�̌��ʉ����Q�[���J�n�w�莞�ԓ��͍Đ�����Ȃ��悤�ɂ���
        if (clip != uiClickSound && Time.time - gameStartTime < initialMuteDuration)
        {
            return;
        }

        if (clip != null)
        {
            // �X�P�[���������ꂽ�{�����[���ōĐ�
            sfxAudioSource.PlayOneShot(clip, currentSEVolume * volumeScale);
        }
    }

    /// <summary>
    /// �J�e�S�����̌��ʉ����Đ� (�J�e�S�����ƃC���f�b�N�X�Ŏw��)
    /// </summary>
    public void PlayCategorySound(string categoryName, int index)
    {
        string soundKey = categoryName + "_" + index;
        PlaySound(soundKey);
    } 

    /// <summary>
    /// ���ׂĂ̌��ʉ����~
    /// </summary>
    public void StopAllSounds()
    {
        if (sfxAudioSource != null)
        {
            sfxAudioSource.Stop();
        }
    }

    /// <summary>
    /// �V�[���J�ڎ��ȂǂɌĂяo���āA�ݒ��ۑ�����
    /// </summary>
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(seVolumePrefsKey, currentSEVolume);
        PlayerPrefs.Save();
    }
    #endregion
}