using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// MonologueScene�̃Z���t�\�����Ǘ�����N���X
/// </summary>
public class MonologueDisplayManager : MonoBehaviour
{
    [Header("UI�Q��")]
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject continueIndicator;

    [Header("�\���ݒ�")]
    [SerializeField] private float textSpeed = 0.05f;
    [SerializeField] private float continueIndicatorDelay = 0.5f;

    [Header("���͐ݒ�")]
    [SerializeField] private KeyCode continueKey = KeyCode.Space;
    [SerializeField] private bool enableMouseClick = true;

    [Header("�A�j���[�V�����ݒ�")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private VideoClip[] monologueAnimations;

    [Header("�V�[���J�ڐݒ�")]
    [SerializeField] private string nextSceneName = "TitleScene";
    [SerializeField] private float fadeSpeed = 1.0f;
    [SerializeField] private UnityEngine.UI.Image fadePanel;
    [SerializeField] private bool waitForInputAfterCompletion = true;

    private MonologueDataLoader dataLoader;
    private List<string> dialogues;
    private int currentDialogueIndex = 0;
    private bool isDisplaying = false;
    private bool canContinue = false;
    private Coroutine displayCoroutine;
    private bool allDialoguesCompleted = false;
    private bool isWaitingForFinalInput = false;
    private bool isSettingsOpen = false;  // �ݒ��ʂ��J���Ă��邩�̃t���O

    /// <summary>
    /// �ݒ��ʂ̊J��Ԃ�ݒ肷��
    /// </summary>
    /// <param name="isOpen">�ݒ��ʂ��J���Ă��邩</param>
    public void SetSettingsOpen(bool isOpen)
    {
        isSettingsOpen = isOpen;
    }

    private void Awake()
    {
        // MonologueDataLoader���擾
        dataLoader = GetComponent<MonologueDataLoader>();
        if (dataLoader == null)
        {
            dataLoader = gameObject.AddComponent<MonologueDataLoader>();
        }

        // ContinueIndicator��������Ԃł͔�\����
        if (continueIndicator != null)
        {
            continueIndicator.SetActive(false);
        }
    }

    private void Start()
    {
        // �Z���t�f�[�^��ǂݍ���
        dialogues = dataLoader.LoadDialogueData();

        if (dialogues.Count == 0)
        {
            Debug.LogError("�Z���t�f�[�^���ǂݍ��܂�܂���ł����B");
            return;
        }

        // �ŏ��̃Z���t��\��
        DisplayCurrentDialogue();
    }

    private void Update()
    {
        // �ݒ��ʂ��J���Ă���ꍇ�͓��͂��������Ȃ�
        if (isSettingsOpen)
        {
            return;
        }

        // ���̓`�F�b�N
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
            // �e�L�X�g�\�����̃X�L�b�v����
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

        // ���ׂẴZ���t�\��������̓��͑ҋ@
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
    /// ���݂̃Z���t��\��
    /// </summary>
    private void DisplayCurrentDialogue()
    {
        if (currentDialogueIndex >= dialogues.Count)
        {
            Debug.Log("���ׂẴZ���t��\�����܂����B");
            return;
        }

        // ContinueIndicator���\��
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
    /// �e�L�X�g��1�������\��
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

        // ContinueIndicator��\��
        yield return new WaitForSeconds(continueIndicatorDelay);
        if (continueIndicator != null && currentDialogueIndex < dialogues.Count - 1)
        {
            continueIndicator.SetActive(true);
        }

        canContinue = true;
    }

    /// <summary>
    /// �e�L�X�g�\�����X�L�b�v
    /// </summary>
    private void SkipTextDisplay()
    {
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }

        dialogueText.text = dialogues[currentDialogueIndex];
        isDisplaying = false;

        // ContinueIndicator��\��
        if (continueIndicator != null && currentDialogueIndex < dialogues.Count - 1)
        {
            continueIndicator.SetActive(true);
        }

        canContinue = true;
    }

    /// <summary>
    /// ���̃Z���t�֐i��
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
            // �K�v�ɉ����ăV�[���J�ڂȂǂ̏�����ǉ�
            OnAllDialoguesCompleted();
        }
    }

    /// <summary>
    /// ���ׂẴZ���t�\���������̏���
    /// </summary>
    private void OnAllDialoguesCompleted()
    {
        allDialoguesCompleted = true;

        Debug.Log("���ׂẴZ���t�̕\�����������܂����B�V�[���J�ڂ̏�����...");

        // ContinueIndicator���\��
        if (continueIndicator != null)
        {
            continueIndicator.SetActive(false);
        }

        if (waitForInputAfterCompletion)
        {
            // ���͑ҋ@��ԂɈڍs
            isWaitingForFinalInput = true;

            Debug.Log("�V�[���J�ڂ̂��߂̓��͂�ҋ@��...");
        }
        else
        {
            // �����ɃV�[���J��
            StartCoroutine(FadeOutAndLoadScene());
        }
    }

    /// <summary>
    /// �V�[���ڍs���̏���
    /// </summary>
    private IEnumerator FadeOutAndLoadScene()
    {
        Debug.Log($"�V�[�� '{nextSceneName}' �ւ̑J�ڂ��J�n���܂��B");

        // �t�F�[�h�p�l��������ꍇ�̓t�F�[�h�A�E�g
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
            // �t�F�[�h�p�l�����Ȃ��ꍇ�͏����ҋ@
            yield return new WaitForSeconds(0.5f);
        }

        // MonologueScene����̑J�ڃt���O��ݒ�i�V�K�ǉ��j
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.SetFromMonologueSceneFlag(true);
            GameSaveManager.Instance.SaveGame();

            Debug.Log("MonologueDisplayManager: MonologueScene����̑J�ڃt���O��ݒ肵�܂���");
        }
        else
        {
            Debug.LogWarning("MonologueDisplayManager: GameSaveManager��������Ȃ����߁AfromMonologueScene�t���O��ݒ�ł��܂���ł���");
        }

        // TitleTextChangerForMonologueScene�Ƀt���O��ݒ�i�V�K�ǉ��j
        TitleTextChangerForMonologueScene.SetTransitionFlag();

        // �t�ϊ��t���O�̐ݒ�
        RememberButtonTextChangerForHer.SetReverseTransitionFlag();

        // �V�[���J��
        SceneManager.LoadScene(nextSceneName);
    }
}