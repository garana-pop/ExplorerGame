using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using OpeningScene;

public class OpeningSceneController : MonoBehaviour
{
    [System.Serializable]
    public class ContinueIndicatorSettings
    {
        [Header("アンカー設定")]
        [SerializeField] public Vector2 anchorMin = new Vector2(1, 0);
        [SerializeField] public Vector2 anchorMax = new Vector2(1, 0);
        [SerializeField] public Vector2 pivot = new Vector2(1, 0);

        [Header("位置設定")]
        [SerializeField] public Vector2 anchoredPosition = new Vector2(-10, 10);

        [Header("親設定")]
        [Tooltip("trueの場合は最新のダイアログエントリを親にします。falseの場合はScrollViewを親にします")]
        [SerializeField] public bool attachToLatestEntry = true;
    }

    [Header("Dialog Display")]
    [SerializeField] private ScrollRect dialogueScrollView;
    [SerializeField] private RectTransform contentPanel;
    [SerializeField] private GameObject dialogueEntryPrefab;
    [SerializeField] private GameObject continueIndicator;
    [SerializeField] private ContinueIndicatorSettings indicatorSettings = new ContinueIndicatorSettings();

    [Header("Display Effects")]
    [SerializeField] private float textSpeed = 0.05f;
    [SerializeField] private bool useTypewriterEffect = true;
    [SerializeField] private float autoScrollSpeed = 0.5f;

    [Header("Scene Transition")]
    [SerializeField] private string nextSceneName = "MainScene";
    [SerializeField] private float fadeSpeed = 1.0f;
    [SerializeField] private Image fadePanel;

    [Header("Dialog Data")]
    [SerializeField] private TextAsset dialogueTextAsset;

    [Header("Character Control")]
    [SerializeField] private CharacterExitController exitController;

    // Dialog system variables
    private List<DialogueEntry> dialogueEntries = new List<DialogueEntry>();
    private int currentDialogueIndex = 0;
    private bool isTyping = false;
    private bool isCompleted = false;
    private Coroutine typingCoroutine;
    private Coroutine scrollCoroutine;

    // Track the latest dialog entry
    private GameObject currentEntryObject;

    // Scene control variables
    private bool isTransitioning = false;
    private bool skipRequested = false;
    private bool isSettingsOpen = false;  // 設定画面が開いているかのフラグ

    // クラスのフィールドに以下を追加
    private SpeakerNameTransitionController speakerNameController;


    private void Awake()
    {
        // Validate components
        ValidateComponents();

        // Initialize dialog data
        InitializeDialogueData();

        // Setup initial UI state
        SetupInitialUI();
    }

    private void Start()
    {
        // GameSaveManagerをチェック
        if (GameSaveManager.Instance != null)
        {
            // endOpeningSceneFlagがfalseの場合のみ続行
            bool isOpeningCompleted = GameSaveManager.Instance.GetEndOpeningSceneFlag();
            if (isOpeningCompleted)
            {
                Debug.LogWarning("OpeningSceneController: 既にOpeningSceneは完了しています。MainSceneへ遷移します。");
                SceneManager.LoadScene("MainScene");
                return;
            }
        }

        // Start scene with fade in
        StartCoroutine(FadeIn());

        // Display first dialog
        StartDialogue();

        if (exitController == null)
        {
            exitController = GetComponent<CharacterExitController>();
            if (exitController == null)
            {
                exitController = gameObject.AddComponent<CharacterExitController>();
                Debug.Log("Exit Controllerが見つからなかったため、新しく追加しました。");
            }
        }

        // SpeakerNameTransitionControllerの参照を取得
        speakerNameController = FindFirstObjectByType<SpeakerNameTransitionController>();
        if (speakerNameController == null)
        {
            Debug.LogWarning("SpeakerNameTransitionControllerが見つかりません。");
        }
    }

    private void Update()
    {
        // Don't process input during transitions
        if (isTransitioning)
            return;

        // Process input
        HandleInput();
    }

    private void HandleInput()
    {
        // 設定画面が開いている場合は入力を処理しない
        if (isSettingsOpen)
            return;

        // 話者名変更中は入力を処理しない
        if (speakerNameController != null && speakerNameController.IsTransitioning)
            return;

        // On click or press enter/space
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {

            // Play click sound
            SoundEffectManager.Instance.PlayClickSound();

            // Skip text if currently typing
            if (isTyping)
            {
                skipRequested = true;
                return;
            }

            // Go to next scene if all dialogs completed
            if (isCompleted)
            {
                StartCoroutine(FadeOutAndLoadScene());
                return;
            }

            // Go to next dialog
            DisplayNextDialogue();
        }
    }

    /// <summary>
    /// 設定画面の開閉状態を設定する
    /// </summary>
    ///// <param name="isOpen">設定画面が開いているか</param>
    public void SetSettingsOpen(bool isOpen)
    {
        isSettingsOpen = isOpen;

        Debug.Log($"Settings panel is now {(isOpen ? "open" : "closed")}");
    }

    private void ValidateComponents()
    {
        if (dialogueScrollView == null || contentPanel == null)
        {
            Debug.LogError("DialogueScrollView or ContentPanel not set");
        }

        // Add warning for unset ContinueIndicator
        if (continueIndicator == null)
        {
            Debug.LogError("ContinueIndicator not set");
        }
    }

    private void InitializeDialogueData()
    {
        // Load dialog data
        if (dialogueTextAsset != null)
        {
            DialogueDataLoader loader = GetComponent<DialogueDataLoader>();
            if (loader == null)
            {
                loader = gameObject.AddComponent<DialogueDataLoader>();
            }

            dialogueEntries = loader.LoadDialogueFromTextAsset(dialogueTextAsset);
        }
        else
        {
            // Use sample dialog if no text asset
            SetupSampleDialogue();
        }
    }

    private void SetupSampleDialogue()
    {
        // Sample dialog setup
        dialogueEntries.Add(new DialogueEntry("", "[Screen brightens slowly - white ceiling and IV stand visible]", DialogueType.Narration));
        dialogueEntries.Add(new DialogueEntry("Protagonist", "...Where am I...?", DialogueType.Normal));
        dialogueEntries.Add(new DialogueEntry("", "[Vision gradually expands, hospital room becomes visible]", DialogueType.Narration));
        dialogueEntries.Add(new DialogueEntry("???", "You're finally awake.", DialogueType.Normal));
    }

    private void SetupInitialUI()
    {
        // Clear content panel
        foreach (Transform child in contentPanel)
        {
            Destroy(child.gameObject);
        }

        // Initialize layout settings
        InitializeLayout();

        // Hide continue indicator
        if (continueIndicator != null)
        {
            continueIndicator.SetActive(false);
        }

        // Set up fade panel
        if (fadePanel != null)
        {
            Color color = fadePanel.color;
            color.a = 1f; // Start fully opaque (black)
            fadePanel.color = color;
            fadePanel.gameObject.SetActive(true);
        }
    }

    private void InitializeLayout()
    {
        // Content panel layout settings
        VerticalLayoutGroup layoutGroup = contentPanel.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup == null)
        {
            layoutGroup = contentPanel.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        // Layout settings
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.spacing = 150f;  // Space between lines

        // Content Size Fitter settings
        ContentSizeFitter contentFitter = contentPanel.GetComponent<ContentSizeFitter>();
        if (contentFitter == null)
        {
            contentFitter = contentPanel.gameObject.AddComponent<ContentSizeFitter>();
        }
        contentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void StartDialogue()
    {
        currentDialogueIndex = 0;
        isCompleted = false;
        currentEntryObject = null; // Added: reset current entry

        // Display first dialog
        DisplayDialogue(currentDialogueIndex);
    }

    private void DisplayNextDialogue()
    {
        currentDialogueIndex++;

        // Check if all dialogs have been displayed
        if (currentDialogueIndex >= dialogueEntries.Count)
        {
            CompleteDialogue();
            return;
        }

        // Display next dialog
        DisplayDialogue(currentDialogueIndex);
    }

    private void DisplayDialogue(int index)
    {
        if (index < 0 || index >= dialogueEntries.Count)
            return;

        DialogueEntry entry = dialogueEntries[index];

        // コマンド行は表示せず次のダイアログへ進む
        if (entry.isCommand || entry.type == DialogueType.Command)
        {
            // ダイアログ表示イベントは通知する（SpeakerNameTransitionControllerが処理するため）
            DialogueEventNotifier.NotifyDialogueDisplayed(entry);

            // 次のダイアログを表示（すぐに次へ進む）
            DisplayNextDialogue();
            return;
        }

        // Hide continue indicator from previous dialog
        HideContinueIndicator();

        // Create dialog entry
        GameObject entryObject = Instantiate(dialogueEntryPrefab, contentPanel);
        currentEntryObject = entryObject; // Save the latest entry

        // Get positioner for layout processing
        DialoguePanelPositioner positioner = contentPanel.GetComponent<DialoguePanelPositioner>();

        // Initialize entry
        InitializeDialogueEntry(entryObject, entry);

        // Apply positioning based on speaker
        if (positioner != null)
        {
            positioner.OnDialogueEntryAdded(entryObject, entry);
        }
        // ダイアログ表示イベントを通知
        DialogueEventNotifier.NotifyDialogueDisplayed(entry);
    }

    private void InitializeDialogueEntry(GameObject entryObject, DialogueEntry entry)
    {
        DialogueEntryController entryController = entryObject.GetComponent<DialogueEntryController>();
        if (entryController != null)
        {
            // Set typing speed
            entryController.typingSpeed = textSpeed;

            // Initialize dialog and speaker
            entryController.Initialize(entry.speaker, entry.dialogue, entry.type);

            if (useTypewriterEffect)
            {
                // Start typing effect
                entryController.StartTyping();
                isTyping = true;
                skipRequested = false;

                // Monitor typing completion
                StartCoroutine(MonitorTyping(entryController));
            }
            else
            {
                // Instant display
                entryController.CompleteTyping();
                ShowContinueIndicator();
            }
        }
        else
        {
            // Fallback: direct text setting
            TextMeshProUGUI[] texts = entryObject.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0)
            {
                foreach (var text in texts)
                {
                    if (text.name.Contains("Speaker") || text.name.Contains("Name"))
                    {
                        text.text = entry.speaker;
                    }
                    else if (text.name.Contains("Dialogue") || text.name.Contains("Text"))
                    {
                        text.text = entry.dialogue;
                    }
                }
            }
        }
    }

    private IEnumerator MonitorTyping(DialogueEntryController controller)
    {
        // Hide continue icon
        HideContinueIndicator();

        // Wait for typing to complete
        while (controller.IsTyping())
        {
            // Skip typing if requested
            if (skipRequested)
            {
                controller.CompleteTyping();
                skipRequested = false;
            }

            yield return null;
        }

        isTyping = false;
        // ダイアログ完了イベントを通知
        DialogueEventNotifier.NotifyDialogueCompleted(dialogueEntries[currentDialogueIndex]);

        // Scroll to bottom after display completed
        DialoguePanelPositioner positioner = contentPanel.GetComponent<DialoguePanelPositioner>();
        if (positioner != null)
        {
            positioner.ScrollToBottom();
        }
        else
        {
            ScrollToBottom();
        }

        // Show continue icon
        ShowContinueIndicator();
    }

    private void ScrollToBottom()
    {
        // Force update canvases before scrolling
        Canvas.ForceUpdateCanvases();

        // Stop any in-progress scrolling
        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
        }

        // Start smooth scrolling
        scrollCoroutine = StartCoroutine(SmoothScrollToBottom());
    }

    private IEnumerator SmoothScrollToBottom()
    {
        float targetPosition = 0;
        float currentPosition = dialogueScrollView.normalizedPosition.y;
        float elapsedTime = 0;

        while (currentPosition > targetPosition + 0.01f)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime * autoScrollSpeed);
            currentPosition = Mathf.Lerp(currentPosition, targetPosition, t);
            dialogueScrollView.normalizedPosition = new Vector2(0, currentPosition);
            yield return null;
        }

        // Set final position exactly
        dialogueScrollView.normalizedPosition = new Vector2(0, 0);
        scrollCoroutine = null;
    }

    // Hide continue indicator method
    private void HideContinueIndicator()
    {
        if (continueIndicator != null)
        {
            // Hide the indicator
            continueIndicator.SetActive(false);

            // Return to original parent if needed
            if (continueIndicator.transform.parent != dialogueScrollView.transform)
            {
                continueIndicator.transform.SetParent(dialogueScrollView.transform, false);
            }
        }
    }

    // Show continue indicator method (modified to use indicatorSettings)
    private void ShowContinueIndicator()
    {
        if (continueIndicator == null)
            return;

        // 指定された親に設定
        if (indicatorSettings.attachToLatestEntry && currentEntryObject != null)
        {
            // 最新のダイアログエントリを親に設定
            continueIndicator.transform.SetParent(currentEntryObject.transform, false);
        }
        else
        {
            // ScrollViewを親に設定
            continueIndicator.transform.SetParent(dialogueScrollView.transform, false);
        }

        // インスペクターで設定された位置に調整
        RectTransform indicatorRect = continueIndicator.GetComponent<RectTransform>();
        if (indicatorRect != null)
        {
            indicatorRect.anchorMin = indicatorSettings.anchorMin;
            indicatorRect.anchorMax = indicatorSettings.anchorMax;
            indicatorRect.pivot = indicatorSettings.pivot;
            indicatorRect.anchoredPosition = indicatorSettings.anchoredPosition;
        }

        // 表示する
        continueIndicator.SetActive(true);
    }

    private void CompleteDialogue()
    {
        // シーン終了イベントを通知
        DialogueEventNotifier.NotifySceneEnding();
        isCompleted = true;
        StartCoroutine(FadeOutAndLoadScene());
    }

    private IEnumerator FadeIn()
    {
        if (fadePanel == null)
            yield break;

        isTransitioning = true;

        Color color = fadePanel.color;

        // Fade from opaque to transparent
        while (color.a > 0)
        {
            color.a -= Time.deltaTime * fadeSpeed;
            fadePanel.color = color;
            yield return null;
        }

        // Ensure fully transparent at the end
        color.a = 0;
        fadePanel.color = color;
        fadePanel.gameObject.SetActive(false);

        isTransitioning = false;
    }

    private IEnumerator FadeOutAndLoadScene()
    {
        // GameSaveManagerでフラグ設定と自動セーブ
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.SetEndOpeningSceneFlag(true);
            GameSaveManager.Instance.SaveOnMainSceneEntry();
            Debug.Log("OpeningSceneController: endOpeningSceneフラグを設定し、セーブしました");
        }
        else
        {
            Debug.LogError("OpeningSceneController: GameSaveManagerが見つかりません");
        }

        if (fadePanel == null)
        {
            SceneManager.LoadScene(nextSceneName);
            yield break;
        }

        isTransitioning = true;
        fadePanel.gameObject.SetActive(true);

        Color color = fadePanel.color;
        color.a = 0;
        fadePanel.color = color;

        // Fade from transparent to opaque
        while (color.a < 1)
        {
            color.a += Time.deltaTime * fadeSpeed;
            fadePanel.color = color;
            yield return null;
        }

        // Load next scene when fully opaque
        SceneManager.LoadScene(nextSceneName);
    }

    private void OnDestroy()
    {
        // Ensure all coroutines are stopped
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
            scrollCoroutine = null;
        }
    }
}