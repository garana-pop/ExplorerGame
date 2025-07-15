using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using OpeningScene;

/// <summary>
/// ConversationFatherAndDaughterシーンのメインコントローラー
/// 父親と私の会話を制御します
/// </summary>
public class ConversationSceneController : MonoBehaviour
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
    [SerializeField] private string nextSceneName = "TitleScene";
    [SerializeField] private float fadeSpeed = 1.0f;
    [SerializeField] private Image fadePanel;
    [SerializeField] private bool deletePortraitOnComplete = true; // 会話終了時に似顔絵を削除するか

    [Header("Dialog Data")]
    [SerializeField] private TextAsset dialogueTextAsset;

    [Header("Character Control")]
    [SerializeField] private CharacterExitController exitController;
    [SerializeField] private CharacterDisplayController characterController;

    [Header("Character Settings")]
    [SerializeField] private GameObject leftCharacter;  // 父親
    [SerializeField] private GameObject rightCharacter; // 私
    [SerializeField] private string leftCharacterName = "父親";
    [SerializeField] private string rightCharacterName = "私";

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
    private bool isSettingsOpen = false;

    // GameSaveManager reference
    private GameSaveManager gameSaveManager;

    private void Awake()
    {
        // Validate components
        ValidateComponents();

        // Initialize dialog data
        InitializeDialogueData();

        // Setup initial UI state
        SetupInitialUI();

        // Get GameSaveManager
        gameSaveManager = GameSaveManager.Instance;
    }

    private void Start()
    {
        // Setup character display
        SetupCharacterDisplay();

        // Start scene with fade in
        StartCoroutine(FadeIn());

        // Display first dialog
        StartDialogue();

        // Setup exit controller
        SetupExitController();
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

        // On click or press enter/space
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            // Play click sound
            if (SoundEffectManager.Instance != null)
            {
                SoundEffectManager.Instance.PlayClickSound();
            }

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
    /// <param name="isOpen">設定画面が開いているか</param>
    public void SetSettingsOpen(bool isOpen)
    {
        isSettingsOpen = isOpen;
        Debug.Log($"ConversationSceneController: Settings panel is now {(isOpen ? "open" : "closed")}");
    }

    private void ValidateComponents()
    {
        if (dialogueScrollView == null || contentPanel == null)
        {
            Debug.LogError("DialogueScrollView or ContentPanel not set");
        }

        if (continueIndicator == null)
        {
            Debug.LogError("ContinueIndicator not set");
        }

        // CharacterDisplayController
        if (characterController == null)
        {
            characterController = GetComponent<CharacterDisplayController>();
            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterDisplayController>();
            }
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
            Debug.LogError("Dialogue text asset not set!");
        }
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

    private void SetupCharacterDisplay()
    {
        if (characterController == null) return;

        // Register event listener
        DialogueEventNotifier.OnDialogueDisplayed += OnDialogueDisplayed;

        // Setup characters
        if (leftCharacter != null)
        {
            RegisterCharacter(leftCharacter, leftCharacterName);
        }

        if (rightCharacter != null)
        {
            RegisterCharacter(rightCharacter, rightCharacterName);
        }
    }

    private void RegisterCharacter(GameObject characterObject, string characterName)
    {
        if (characterController == null || characterObject == null) return;

        if (!characterController.HasCharacterForSpeaker(characterName))
        {
            var characterImage = characterObject.GetComponent<UnityEngine.UI.Image>();

            var nameArea = characterObject.transform.Find("LeftNameArea") ??
                          characterObject.transform.Find("RightNameArea");

            TMPro.TextMeshProUGUI nameText = null;
            if (nameArea != null)
            {
                nameText = nameArea.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            }

            characterController.AddCharacter(characterName, characterObject, characterImage, nameText);
        }
    }

    private void SetupExitController()
    {
        if (exitController == null)
        {
            exitController = GetComponent<CharacterExitController>();
            if (exitController == null)
            {
                exitController = gameObject.AddComponent<CharacterExitController>();
                Debug.Log("Exit Controllerが見つからなかったため、新しく追加しました。");
            }
        }
    }

    private void StartDialogue()
    {
        currentDialogueIndex = 0;
        isCompleted = false;
        currentEntryObject = null;

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

        // Handle command entries
        if (entry.isCommand || entry.type == DialogueType.Command)
        {
            // Notify event for command processing
            DialogueEventNotifier.NotifyDialogueDisplayed(entry);

            // Move to next dialogue immediately
            DisplayNextDialogue();
            return;
        }

        // Hide continue indicator from previous dialog
        HideContinueIndicator();

        // Create dialog entry
        GameObject entryObject = Instantiate(dialogueEntryPrefab, contentPanel);
        currentEntryObject = entryObject;

        // Get positioner for layout processing
        DialoguePanelPositioner positioner = contentPanel.GetComponent<DialoguePanelPositioner>();
        if (positioner == null)
        {
            positioner = contentPanel.gameObject.AddComponent<DialoguePanelPositioner>();
        }

        // Initialize entry
        InitializeDialogueEntry(entryObject, entry);

        // Apply positioning based on speaker
        if (positioner != null)
        {
            positioner.OnDialogueEntryAdded(entryObject, entry);
        }

        // Notify dialogue displayed event
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

        // Notify dialogue completion
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
        Canvas.ForceUpdateCanvases();

        if (scrollCoroutine != null)
        {
            StopCoroutine(scrollCoroutine);
        }

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

        dialogueScrollView.normalizedPosition = new Vector2(0, 0);
        scrollCoroutine = null;
    }

    private void HideContinueIndicator()
    {
        if (continueIndicator != null)
        {
            continueIndicator.SetActive(false);

            if (continueIndicator.transform.parent != dialogueScrollView.transform)
            {
                continueIndicator.transform.SetParent(dialogueScrollView.transform, false);
            }
        }
    }

    private void ShowContinueIndicator()
    {
        if (continueIndicator == null)
            return;

        // Set parent based on settings
        if (indicatorSettings.attachToLatestEntry && currentEntryObject != null)
        {
            continueIndicator.transform.SetParent(currentEntryObject.transform, false);
        }
        else
        {
            continueIndicator.transform.SetParent(dialogueScrollView.transform, false);
        }

        // Apply position settings
        RectTransform indicatorRect = continueIndicator.GetComponent<RectTransform>();
        if (indicatorRect != null)
        {
            indicatorRect.anchorMin = indicatorSettings.anchorMin;
            indicatorRect.anchorMax = indicatorSettings.anchorMax;
            indicatorRect.pivot = indicatorSettings.pivot;
            indicatorRect.anchoredPosition = indicatorSettings.anchoredPosition;
        }

        continueIndicator.SetActive(true);
    }

    private void CompleteDialogue()
    {
        // Notify scene ending event
        DialogueEventNotifier.NotifySceneEnding();
        isCompleted = true;

        // Delete portrait if setting is enabled
        if (deletePortraitOnComplete && gameSaveManager != null)
        {
            StartCoroutine(DeletePortraitAndTransition());
        }
        else
        {
            StartCoroutine(FadeOutAndLoadScene());
        }
    }

    private IEnumerator DeletePortraitAndTransition()
    {
        // 似顔絵.pngファイルを削除
        if (gameSaveManager != null)
        {
            Debug.Log("似顔絵.pngを削除します");
            //gameSaveManager.DeletePortraitFile();
            // メインシーンの似顔絵ファイルを削除
            // これはGameSaveManagerで処理する必要がある
            // 実際の実装では、MainSceneControllerと連携して削除処理を行う

            yield return new WaitForSeconds(0.5f); // 短い待機
        }

        // シーン遷移
        yield return StartCoroutine(FadeOutAndLoadScene());
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

        color.a = 0;
        fadePanel.color = color;
        fadePanel.gameObject.SetActive(false);

        isTransitioning = false;
    }

    private IEnumerator FadeOutAndLoadScene()
    {
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

    private void OnDialogueDisplayed(DialogueEntry entry)
    {
        if (characterController == null || entry == null) return;

        // Don't highlight for narration
        if (entry.type == DialogueType.Narration)
        {
            characterController.ResetAllCharacters();
            return;
        }

        // Highlight character
        characterController.HighlightCharacter(entry.speaker);
    }

    private void OnDestroy()
    {
        // Unregister event listeners
        DialogueEventNotifier.OnDialogueDisplayed -= OnDialogueDisplayed;

        // Stop coroutines
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