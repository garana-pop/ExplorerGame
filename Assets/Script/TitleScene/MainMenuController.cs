using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    [Header("メニュー項目")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("パネル")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject quitConfirmationPanel;

    [Header("エフェクト")]
    [SerializeField] private BlinkEffect blinkEffect;

    [Header("オーディオ")]
    [SerializeField] public AudioSource backgroundAudioSource; // publicに変更してTitleSceneSettingsManagerからアクセス可能に
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private float sfxVolume = 0.8f;

    [Header("セーブ管理")]
    [SerializeField] private GameSaveManager gameSaveManager;

    // シーン名を明示的に指定できるようにする
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("思い出すボタン制御")]
    [Tooltip("思い出すボタンのGameObject（未設定の場合は自動検索）")]
    [SerializeField] private GameObject rememberButton;

    private void Start()
    {
        // メニューの初期化
        InitializeMenu();

        // オーディオの設定は後で行う（TitleSceneSettingsManagerが管理）

        // GameSaveManagerが設定されていない場合は自動的に取得
        if (gameSaveManager == null)
        {
            gameSaveManager = GameSaveManager.Instance;
        }

        // ボタンイベントの登録
        startButton.onClick.AddListener(OnStartButtonClicked);
        settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        quitButton.onClick.AddListener(OnQuitButtonClicked);

        // 思い出すボタンの初期化とMonologueSceneフラグのチェック
        InitializeRememberButton();
    }

    /// <summary>
    /// Start メソッドの最後に追加する処理
    /// 思い出すボタンの初期化とMonologueSceneフラグのチェックを行う
    /// </summary>
    private void InitializeRememberButton()
    {
        // 思い出すボタンの自動検出
        if (rememberButton == null)
        {
            GameObject menuContainer = GameObject.Find("MenuContainer");
            if (menuContainer != null)
            {
                Transform button = menuContainer.transform.Find("思い出すボタン");
                if (button != null)
                {
                    rememberButton = button.gameObject;
                }
            }
        }

        // MonologueSceneからの遷移をチェックして思い出すボタンを制御
        CheckAndHideRememberButton();
    }

    /// <summary>
    /// MonologueSceneからの遷移をチェックして思い出すボタンを制御
    /// </summary>
    private void CheckAndHideRememberButton()
    {
        if (rememberButton == null) return;

        GameSaveManager saveManager = GameSaveManager.Instance;
        if (saveManager == null)
        {
            Debug.LogWarning("MainMenuController: GameSaveManagerが見つかりません");
            return;
        }

        bool fromMonologue = saveManager.GetFromMonologueSceneFlag();

        if (fromMonologue)
        {
            rememberButton.SetActive(false);
            Debug.Log("MainMenuController: 思い出すボタンを非表示にしました");

            saveManager.SaveGame();
        }
        else
        {
            rememberButton.SetActive(true);
        }
    }

    private void InitializeMenu()
    {
        // メインメニューを表示し、他を非表示に
        mainMenuPanel.SetActive(true);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (quitConfirmationPanel != null)
            quitConfirmationPanel.SetActive(false);
    }

    /// <summary>
    /// BGM音量を更新するパブリックメソッド（TitleSceneSettingsManagerから呼び出し可能）
    /// </summary>
    public void UpdateBgmVolume(float volume)
    {
        if (backgroundAudioSource != null)
        {
            backgroundAudioSource.volume = volume;
            Debug.Log($"MainMenuController: BGM音量更新 {volume}");

            // BGMが再生されていない場合は再生開始
            if (!backgroundAudioSource.isPlaying && backgroundAudioSource.clip != null)
            {
                backgroundAudioSource.Play();
            }
        }
    }

    /// <summary>
    /// 効果音音量を更新するパブリックメソッド（SettingsMenuControllerから呼び出し可能）
    /// </summary>
    public void ApplySfxVolume(float volume)
    {
        sfxVolume = volume;

        // SoundEffectManagerにも音量変更を通知
        SoundEffectManager soundManager = SoundEffectManager.Instance;
        if (soundManager != null)
        {
            soundManager.SetVolume(volume);
        }
        else
        {
            PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
            PlayerPrefs.Save();
        }
    }

    public void OnStartButtonClicked()
    {
        PlayClickSound();

        // ConversationTransitionControllerを使用する場合
        ConversationTransitionController transitionController = FindFirstObjectByType<ConversationTransitionController>();
        if (transitionController != null)
        {
            // ConversationTransitionControllerに遷移を委託
            transitionController.StartTransition();
        }
        else
        {
            // ConversationTransitionControllerが見つからない場合のフォールバック
            Debug.LogWarning("MainMenuController: ConversationTransitionControllerが見つかりません。直接シーン遷移を行います。");

            // セーブデータの存在をチェック
            if (GameSaveManager.Instance != null)
            {
                bool saveExists = GameSaveManager.Instance.SaveDataExists();
                if (!saveExists)
                {
                    // セーブデータが存在しない場合はOpeningSceneへ
                    StartCoroutine(DelayedSceneLoad("OpeningScene"));
                }
                else
                {
                    // セーブデータが存在する場合は、endOpeningSceneFlagをチェック
                    GameSaveManager.Instance.LoadGame();
                    bool isOpeningCompleted = GameSaveManager.Instance.GetEndOpeningSceneFlag();

                    if (isOpeningCompleted)
                    {
                        // OpeningScene完了済みならMainSceneへ
                        StartCoroutine(DelayedSceneLoad("MainScene"));
                    }
                    else
                    {
                        // OpeningScene未完了ならOpeningSceneへ
                        StartCoroutine(DelayedSceneLoad("OpeningScene"));
                    }
                }
            }
            else
            {
                // GameSaveManagerが見つからない場合はOpeningSceneへ
                StartCoroutine(DelayedSceneLoad("OpeningScene"));
            }
        }
    }

    private IEnumerator DelayedSceneLoad(string sceneName)
    {
        yield return new WaitForSeconds(0.1f); // 少し待ってからシーン遷移
        SceneManager.LoadScene(sceneName);
    }

    public void OnSettingsButtonClicked()
    {
        PlayClickSound();

        // TitleSceneSettingsManagerに設定画面表示を依頼
        TitleSceneSettingsManager titleSettingsManager = FindFirstObjectByType<TitleSceneSettingsManager>();
        if (titleSettingsManager != null)
        {
            titleSettingsManager.ShowSettings();
        }

        // 設定パネルを表示
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    public void OnQuitButtonClicked()
    {
        PlayClickSound();

        if (quitConfirmationPanel != null)
        {
            quitConfirmationPanel.SetActive(true);
        }
    }

    public void ReturnToMainMenu()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        if (quitConfirmationPanel != null)
        {
            quitConfirmationPanel.SetActive(false);
        }

        mainMenuPanel.SetActive(true);
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
}