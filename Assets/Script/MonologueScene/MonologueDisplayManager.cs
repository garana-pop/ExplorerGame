using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// MonologueSceneのセリフ表示を管理するクラス
/// </summary>
public class MonologueDisplayManager : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject continueIndicator;

    [Header("表示設定")]
    [SerializeField] private float textSpeed = 0.05f;
    [SerializeField] private float continueIndicatorDelay = 0.5f;

    [Header("入力設定")]
    [SerializeField] private KeyCode continueKey = KeyCode.Space;
    [SerializeField] private bool enableMouseClick = true;

    [Header("アニメーション設定")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip[] monologueAnimations;

    [Header("シーン遷移設定")]
    [SerializeField] private string nextSceneName = "TitleScene";
    [SerializeField] private float fadeSpeed = 1.0f;
    [SerializeField] private UnityEngine.UI.Image fadePanel;
    [SerializeField] private bool waitForInputAfterCompletion = true;

    [Header("Steam実績設定")]
    [Tooltip("MonologueScene完了時に解除するSteam実績のAPI名")]
    [SerializeField] private string achievementApiName = "SelfAwareness_8_ACHIEVEMENTS_UNLOCK";

    [Header("デバッグ")]
    [Tooltip("デバッグモードの有効化")]
    [SerializeField] private bool debugMode = true;

    private MonologueDataLoader dataLoader;
    private List<string> dialogues;
    private int currentDialogueIndex = 0;
    private bool isDisplaying = false;
    private bool canContinue = false;
    private Coroutine displayCoroutine;
    private bool allDialoguesCompleted = false;
    private bool isWaitingForFinalInput = false;
    private bool isSettingsOpen = false;  // 設定画面が開いているかのフラグ
    private bool isAchievementUnlocked = false; // 実績解除済みフラグ

    /// <summary>
    /// 設定画面の開閉状態を設定する
    /// </summary>
    /// <param name="isOpen">設定画面が開いているか</param>
    public void SetSettingsOpen(bool isOpen)
    {
        isSettingsOpen = isOpen;
    }

    private void Awake()
    {
        // MonologueDataLoaderを取得
        dataLoader = GetComponent<MonologueDataLoader>();
        if (dataLoader == null)
        {
            dataLoader = gameObject.AddComponent<MonologueDataLoader>();
        }

        // ContinueIndicatorを初期状態では非表示に
        if (continueIndicator != null)
        {
            continueIndicator.SetActive(false);
        }
    }

    private void Start()
    {
        // セリフデータを読み込む
        dialogues = dataLoader.LoadDialogueData();

        if (dialogues.Count == 0)
        {
            Debug.LogError("セリフデータが読み込まれませんでした。");
            return;
        }

        // 最初のセリフを表示
        DisplayCurrentDialogue();
    }

    private void Update()
    {
        // 設定画面が開いている場合は入力を処理しない
        if (isSettingsOpen)
        {
            return;
        }

        // 入力チェック
        if (canContinue && !isDisplaying)
        {
            bool shouldContinue = Input.GetKeyDown(continueKey);

            if (enableMouseClick && Input.GetMouseButtonDown(0))
            {
                shouldContinue = true;
            }

            if (shouldContinue)
            {
                NextDialogue();
            }
        }
        else if (isDisplaying)
        {
            // テキスト表示中のスキップ処理
            bool shouldSkip = Input.GetKeyDown(continueKey);

            if (enableMouseClick && Input.GetMouseButtonDown(0))
            {
                shouldSkip = true;
            }

            if (shouldSkip)
            {
                SkipTextDisplay();
            }
        }

        // すべてのセリフ表示完了後の入力待機
        if (isWaitingForFinalInput)
        {
            bool shouldTransition = Input.GetKeyDown(continueKey);

            if (enableMouseClick && Input.GetMouseButtonDown(0))
            {
                shouldTransition = true;
            }

            if (shouldTransition)
            {
                isWaitingForFinalInput = false;
                StartCoroutine(FadeOutAndLoadScene());
            }
        }
    }

    /// <summary>
    /// 現在のセリフを表示
    /// </summary>
    private void DisplayCurrentDialogue()
    {
        if (currentDialogueIndex >= dialogues.Count)
        {
            Debug.Log("すべてのセリフを表示しました。");
            return;
        }

        // ContinueIndicatorを非表示
        if (continueIndicator != null)
        {
            continueIndicator.SetActive(false);
        }

        canContinue = false;
        string dialogue = dialogues[currentDialogueIndex];

        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }

        displayCoroutine = StartCoroutine(DisplayTextCoroutine(dialogue));
    }

    /// <summary>
    /// テキストを1文字ずつ表示
    /// </summary>
    private IEnumerator DisplayTextCoroutine(string text)
    {
        isDisplaying = true;
        dialogueText.text = "";

        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(textSpeed);
        }

        isDisplaying = false;

        // ContinueIndicatorを表示
        yield return new WaitForSeconds(continueIndicatorDelay);
        if (continueIndicator != null && currentDialogueIndex < dialogues.Count - 1)
        {
            continueIndicator.SetActive(true);
        }

        canContinue = true;
    }

    /// <summary>
    /// テキスト表示をスキップ
    /// </summary>
    private void SkipTextDisplay()
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }

        dialogueText.text = dialogues[currentDialogueIndex];
        isDisplaying = false;

        // ContinueIndicatorを表示
        if (continueIndicator != null && currentDialogueIndex < dialogues.Count - 1)
        {
            continueIndicator.SetActive(true);
        }

        canContinue = true;
    }

    /// <summary>
    /// 次のセリフへ進む
    /// </summary>
    private void NextDialogue()
    {
        currentDialogueIndex++;

        if (currentDialogueIndex < dialogues.Count)
        {
            DisplayCurrentDialogue();
        }
        else
        {
            // 必要に応じてシーン遷移などの処理を追加
            OnAllDialoguesCompleted();
        }
    }

    /// <summary>
    /// すべてのセリフ表示完了時の処理
    /// </summary>
    private void OnAllDialoguesCompleted()
    {
        if (allDialoguesCompleted) return;

        allDialoguesCompleted = true;

        // GameSaveManagerのフラグを設定
        if (GameSaveManager.Instance != null)
        {
            // afterChangeToLastフラグを設定（重要）
            GameSaveManager.Instance.SetAfterChangeToLastFlag(true);

            // MonologueScene完了フラグも設定
            GameSaveManager.Instance.SetFromMonologueSceneFlag(true);

            // 即座に保存
            GameSaveManager.Instance.SaveGame();

            Debug.Log($"{nameof(MonologueDisplayManager)}: afterChangeToLastフラグを設定しました");

        }
        // Steam実績解除処理を呼び出し
        UnlockSteamAchievement();

        // フェードアウトとシーン遷移
        StartCoroutine(FadeOutAndLoadScene());
    }

    #region Steam実績処理

    /// <summary>
    /// Steam実績を解除する
    /// </summary>
    private void UnlockSteamAchievement()
    {
        // 既に解除済みの場合は処理をスキップ
        if (isAchievementUnlocked)
        {
            LogDebug("実績は既に解除済みです");
            return;
        }

        // SteamAchievementManagerの存在確認
        if (SteamAchievementManager.Instance == null)
        {
            Debug.LogError($"{nameof(MonologueDisplayManager)}: SteamAchievementManagerが見つかりません");
            return;
        }

        // Steam実績を解除
        bool success = SteamAchievementManager.Instance.UnlockAchievement(achievementApiName);

        if (success)
        {
            LogDebug($"Steam実績「{achievementApiName}」を正常に解除しました");
            isAchievementUnlocked = true;
        }
        else
        {
            Debug.LogError($"{nameof(MonologueDisplayManager)}: Steam実績「{achievementApiName}」の解除に失敗しました");
        }
    }

    #endregion


    /// <summary>
    /// シーン移行時の処理
    /// </summary>
    private IEnumerator FadeOutAndLoadScene()
    {
        Debug.Log($"シーン '{nextSceneName}' への遷移を開始します。");

        // フェードパネルがある場合はフェードアウト
        if (fadePanel != null)
        {
            fadePanel.gameObject.SetActive(true);
            float timer = 0f;
            Color panelColor = fadePanel.color;
            panelColor.a = 0f;
            fadePanel.color = panelColor;

            while (timer < fadeSpeed)
            {
                timer += Time.deltaTime;
                panelColor.a = Mathf.Lerp(0f, 1f, timer / fadeSpeed);
                fadePanel.color = panelColor;
                yield return null;
            }

            panelColor.a = 1f;
            fadePanel.color = panelColor;
        }
        else
        {
            // フェードパネルがない場合は少し待機
            yield return new WaitForSeconds(0.5f);
        }

        // MonologueSceneからの遷移フラグを設定（新規追加）
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.SetFromMonologueSceneFlag(true);
            GameSaveManager.Instance.SaveGame();

            Debug.Log("MonologueDisplayManager: MonologueSceneからの遷移フラグを設定しました");
        }
        else
        {
            Debug.LogWarning("MonologueDisplayManager: GameSaveManagerが見つからないため、fromMonologueSceneフラグを設定できませんでした");
        }

        // TitleTextChangerForMonologueSceneにフラグを設定（新規追加）
        TitleTextChangerForMonologueScene.SetTransitionFlag();

        // 逆変換フラグの設定
        RememberButtonTextChangerForHer.SetReverseTransitionFlag();

        // シーン遷移
        SceneManager.LoadScene(nextSceneName);
    }

    #region デバッグ

    /// <summary>
    /// デバッグログ出力
    /// </summary>
    /// <param name="message">ログメッセージ</param>
    private void LogDebug(string message)
    {
        if (debugMode)
        {
            Debug.Log($"{nameof(MonologueDisplayManager)}: {message}");
        }
    }

    #endregion
}