using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// afterChangeToHerMemory�t���O��true�̏ꍇ�̂݁A
/// �u�v���o���v�{�^���������ConversationFatherAndDaughterScene�Ɉڍs����R���g���[���[
/// MainMenuController�ƕ������ē��삵�܂�
/// </summary>
public class ConversationTransitionController : MonoBehaviour
{
    [Header("�V�[���J�ڐݒ�")]
    [Tooltip("��b�V�[���̖��O")]
    [SerializeField] private string conversationSceneName = "ConversationFatherAndDaughterScene";

    [Tooltip("�ʏ�̃Q�[���V�[���̖��O")]
    [SerializeField] private string normalGameSceneName = "MainScene";

    [Tooltip("�f�t�H���g�̃I�[�v�j���O�V�[����")]
    [SerializeField] private string defaultOpeningSceneName = "OpeningScene";

    [Header("�{�^���Q��")]
    [Tooltip("�u�v���o���v�{�^���ւ̎Q�Ɓi�����擾�\�j")]
    [SerializeField] private Button startButton;

    [Header("�t�F�[�h�ݒ�")]
    [Tooltip("�V�[���J�ڎ��Ƀt�F�[�h���ʂ��g�p���邩")]
    [SerializeField] private bool useFadeTransition = true;

    [Tooltip("�t�F�[�h���ԁi�b�j")]
    [SerializeField] private float fadeDuration = 1.0f;

    [Tooltip("�t�F�[�h�p�p�l���i�����擾�\�j")]
    [SerializeField] private CanvasGroup fadePanel;

    [Header("�����ݒ�")]
    [Tooltip("�V�[���J�ڎ��̌��ʉ�")]
    [SerializeField] private AudioClip transitionSound;

    [Tooltip("�����Đ��pAudioSource�i�����擾�\�j")]
    [SerializeField] private AudioSource audioSource;

    [Header("�Z�[�u�f�[�^���ؐݒ�")]
    [Tooltip("�Z�[�u�f�[�^�����݂��Ȃ��ꍇ�͒ʏ�̑J�ڃ��W�b�N���g�p���邩")]
    [SerializeField] private bool requireSaveDataForConversation = true;

    [Header("�f�o�b�O�ݒ�")]
    [Tooltip("�f�o�b�O���O��\�����邩")]
    [SerializeField] private bool debugMode = false;

    [Tooltip("�����I�ɉ�b�V�[���ɑJ�ځi�e�X�g�p�j")]
    [SerializeField] private bool forceConversationScene = false;

    [Header("�t���O����ݒ�")]
    [Tooltip("afterChangeToHerMemory�t���O�݂̂ŉ�b�V�[�����肷�邩")]
    [SerializeField] private bool useAfterChangeFlagOnly = true;

    [Tooltip("�f�o�b�O�p�F�t���O��Ԃ����O�ɏo��")]
    [SerializeField] private bool logFlagStatus = false;

    // �����ϐ�
    private GameSaveManager gameSaveManager;
    private MainMenuController mainMenuController;
    private bool isTransitioning = false;

    private void Awake()
    {
        // �K�v�ȃR���|�[�l���g�̎����擾
        InitializeComponents();
    }

    private void Start()
    {
        // GameSaveManager��MainMenuController�̎Q�Ƃ��擾
        InitializeManagers();

        // MainMenuController��Start�{�^���C�x���g���g��
        OverrideStartButtonBehavior();
    }

    //�擪�ɏd���h�~������ǉ�
    public void StartTransition()
    {
        // ���ɑJ�ڒ��̏ꍇ�͉������Ȃ�
        if (isTransitioning)
        {
            if (debugMode)
                Debug.Log("ConversationTransitionController: ���ɑJ�ڏ������ł��B�V�����J�ڂ��X�L�b�v���܂��B");
            return;
        }

        // ���̃V�[���J�ڂ��i�s���łȂ����m�F
        if (SceneManager.GetActiveScene().name != "TitleScene")
        {
            if (debugMode)
                Debug.LogWarning("ConversationTransitionController: ���ɕʂ̃V�[���ɑJ�ڂ��Ă��܂��B�����𒆒f���܂��B");
            return;
        }

        // �J�ڃt���O�𗧂Ă�
        isTransitioning = true;

        StartCoroutine(PerformSceneTransition());
    }

    /// <summary>
    /// �K�v�ȃR���|�[�l���g��������
    /// </summary>
    private void InitializeComponents()
    {
        // �u�v���o���v�{�^���̎����擾
        if (startButton == null)
        {
            startButton = GameObject.Find("�v���o���{�^��")?.GetComponent<Button>();

            if (startButton == null)
            {
                // �p�ꖼ�ł̌��������s
                startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
            }
        }

        // AudioSource�̎����擾�܂��͍쐬
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        // �t�F�[�h�p�p�l���̎����擾
        if (fadePanel == null && useFadeTransition)
        {
            GameObject fadeObj = GameObject.Find("FadePanel");
            if (fadeObj != null)
            {
                fadePanel = fadeObj.GetComponent<CanvasGroup>();
                if (fadePanel == null)
                {
                    fadePanel = fadeObj.AddComponent<CanvasGroup>();
                }
            }
            else
            {
                // �t�F�[�h�p�l����������Ȃ��ꍇ�͍쐬
                CreateFadePanel();
            }
        }
    }

    /// <summary>
    /// GameSaveManager��MainMenuController�̎Q�Ƃ�������
    /// </summary>
    private void InitializeManagers()
    {
        // GameSaveManager�̎擾
        gameSaveManager = GameSaveManager.Instance;
        if (gameSaveManager == null)
        {
            Debug.LogWarning("ConversationTransitionController: GameSaveManager��������܂���");
        }

        // MainMenuController�̎擾
        mainMenuController = FindFirstObjectByType<MainMenuController>();
        if (mainMenuController == null)
        {
            Debug.LogWarning("ConversationTransitionController: MainMenuController��������܂���");
        }
    }

    /// <summary>
    /// MainMenuController�̃X�^�[�g�{�^���̓�����g��
    /// </summary>
    private void OverrideStartButtonBehavior()
    {
        if (startButton == null)
        {
            Debug.LogError("ConversationTransitionController: �u�v���o���v�{�^����������܂���");
            return;
        }

        // �����̃��X�i�[�݂̂��폜�i�d���o�^�h�~�j
        startButton.onClick.RemoveListener(OnStartButtonClicked);

        // �V�����C�x���g��o�^
        startButton.onClick.AddListener(OnStartButtonClicked);

        if (debugMode)
        {
            Debug.Log("ConversationTransitionController: �u�v���o���v�{�^���̃C�x���g���g�����܂���");
        }
    }

    private void OnDestroy()
    {
        // �����̃��X�i�[�݂̂��폜
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }
    }

    /// <summary>
    /// �X�^�[�g�{�^�����N���b�N���ꂽ���̏���
    /// </summary>
    private void OnStartButtonClicked()
    {
        if (isTransitioning)
        {
            if (debugMode)
                Debug.Log("ConversationTransitionController: ���ɑJ�ڒ��ł�");
            return;
        }

        // RememberButtonTextChangerForHer�̃t���O���`�F�b�N
        RememberButtonTextChangerForHer textChanger = FindFirstObjectByType<RememberButtonTextChangerForHer>();
        if (textChanger != null && textChanger.DataResetPanelControllerBoot)
        {
            if (debugMode) Debug.Log("ConversationTransitionController: DataResetPanelControllerBoot�t���O��true�̂��߁A�X�^�[�g�{�^���������X�L�b�v���܂�");
            return; // �������^�[���őJ�ڏ������~
        }

        // afterChangeToLast�t���O���`�F�b�N�i�ǉ��j
        if (gameSaveManager != null && gameSaveManager.GetAfterChangeToLastFlag())
        {
            if (debugMode) Debug.Log("ConversationTransitionController: afterChangeToLast�t���O��true�̂��߁ATitleTextChanger�Ƀ{�^���������Ϗ����܂�");
            // TitleTextChanger�ɏ������Ϗ����A���̃��\�b�h�͏I��
            return;
        }

        // �J�ڃt���O�𑦍��ɐݒ肵�āA�d�����s��h��
        isTransitioning = true;

        // �{�^���𖳌������Č둀���h��
        if (startButton != null)
        {
            startButton.interactable = false;
        }

        // �J�ڏ��������s
        StartCoroutine(PerformSceneTransition());
    }


    /// <summary>
    /// ���ۂ̃V�[���J�ڏ������s���R���[�`���i�C���Łj
    /// </summary>
    private IEnumerator PerformSceneTransition()
    {
        // RememberButtonTextChangerForHer�̃t���O���Ċm�F
        RememberButtonTextChangerForHer textChanger = FindFirstObjectByType<RememberButtonTextChangerForHer>();
        if (textChanger != null && textChanger.DataResetPanelControllerBoot)
        {
            if (debugMode) Debug.Log("ConversationTransitionController: DataResetPanelControllerBoot�t���O��true�̂��߁A�J�ڏ����𒆎~���܂�");
            isTransitioning = false;
            if (startButton != null) startButton.interactable = true;
            yield break; // �R���[�`�����I��
        }

        string targetScene = defaultOpeningSceneName;
        bool transitionSuccessful = false;

        try
        {
            bool saveExists = gameSaveManager != null && gameSaveManager.SaveDataExists();

            // �Z�[�u�f�[�^���݃`�F�b�N
            if (requireSaveDataForConversation && !saveExists)
            {
                if (debugMode)
                    Debug.Log("ConversationTransitionController: �Z�[�u�f�[�^�����݂��Ȃ����߁A�ʏ�̑J�ڃ��W�b�N���g�p���܂�");

                // �V�K�Q�[���̏ꍇ�AendOpeningSceneFlag��false�ɏ�����
                if (gameSaveManager != null)
                {
                    gameSaveManager.InitializeSaveData();
                    gameSaveManager.SetEndOpeningSceneFlag(false);
                    // �Z�[�u�͍s��Ȃ��iOpeningScene�������ɍs�����߁j

                    if (debugMode)
                        Debug.Log("ConversationTransitionController: �V�K�Q�[���Ƃ��ď��������AendOpeningSceneFlag��false�ɐݒ�");
                }

                targetScene = defaultOpeningSceneName;
                transitionSuccessful = true;
            }
            else if (saveExists)
            {
                // �Z�[�u�f�[�^��ǂݍ���
                bool loadSuccess = gameSaveManager.LoadGame();

                if (loadSuccess)
                {
                    // afterChangeToHerMemory�t���O���ēx�m�F�i�ŐV�̏�Ԃ��擾�j
                    bool afterChangeFlag = gameSaveManager.GetAfterChangeToHerMemoryFlag();

                    // endOpeningScene�t���O���擾
                    bool isOpeningCompleted = gameSaveManager.GetEndOpeningSceneFlag();

                    if (debugMode)
                    {
                        Debug.Log($"ConversationTransitionController: �Z�[�u�f�[�^�ǂݍ��ݐ���");
                        Debug.Log($"ConversationTransitionController: afterChangeToHerMemory�t���O = {afterChangeFlag}");
                        Debug.Log($"ConversationTransitionController: endOpeningScene�t���O = {isOpeningCompleted}");
                    }

                    // afterChangeToHerMemory�t���O��true�̏ꍇ�͉�b�V�[����
                    if (afterChangeFlag || forceConversationScene)
                    {
                        targetScene = conversationSceneName;
                        if (debugMode)
                            Debug.Log("ConversationTransitionController: �t���O��true�̂��߁A��b�V�[���ɑJ�ڂ��܂�");
                    }
                    // afterChangeToHerMemory�t���O��false�̏ꍇ
                    else
                    {
                        // endOpeningScene�t���O�Ŕ���
                        if (isOpeningCompleted)
                        {
                            // OpeningScene�����ς݂Ȃ�MainScene��
                            targetScene = normalGameSceneName; // MainScene
                            if (debugMode)
                                Debug.Log("ConversationTransitionController: OpeningScene�����ς݁BMainScene�֑J��");
                        }
                        else
                        {
                            // OpeningScene�������Ȃ�OpeningScene��
                            targetScene = defaultOpeningSceneName;
                            if (debugMode)
                                Debug.Log("ConversationTransitionController: OpeningScene�������BOpeningScene�֑J��");
                        }
                    }

                    transitionSuccessful = true;
                }
                else
                {
                    if (debugMode)
                        Debug.Log("ConversationTransitionController: �Z�[�u�f�[�^�̓ǂݍ��݂Ɏ��s�A�I�[�v�j���O�V�[���ɑJ��");
                    targetScene = defaultOpeningSceneName;
                    transitionSuccessful = true;
                }
            }
            else
            {
                if (debugMode)
                    Debug.Log("ConversationTransitionController: �Z�[�u�f�[�^�����݂��܂���A�I�[�v�j���O�V�[���ɑJ��");
                targetScene = defaultOpeningSceneName;
                transitionSuccessful = true;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ConversationTransitionController: �V�[�����蒆�ɃG���[: {ex.Message}");
            targetScene = defaultOpeningSceneName;
            transitionSuccessful = false;
        }

        // �t�F�[�h�ƃV�[���J��
        if (useFadeTransition && fadePanel != null)
        {
            yield return StartCoroutine(FadeOut());
        }

        // �ŏI�I�ȃV�[�������Ċm�F�i�O�̂��߁j
        if (debugMode)
            Debug.Log($"ConversationTransitionController: �ŏI�I�ȑJ�ڐ�: {targetScene}");

        yield return StartCoroutine(LoadSceneAsync(targetScene));

        isTransitioning = false;

        if (debugMode)
        {
            string status = transitionSuccessful ? "����" : "���s";
            Debug.Log($"ConversationTransitionController: �V�[���J�ڊ��� ({status})");
            Debug.Log($"ConversationTransitionController: �ŏI�I�ȑJ�ڐ� = {targetScene}");
        }
    }


    // ConversationTransitionController - �x���t���O�`�F�b�N�p�R���[�`���i�V�K�ǉ��j
    private IEnumerator CheckAfterChangeToLastFlagAndTransition()
    {
        // GameSaveManager�̃��[�h������҂�
        yield return new WaitForSeconds(0.5f);

        // afterChangeToLast�t���O���`�F�b�N
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.GetAfterChangeToLastFlag())
        {
            if (debugMode) Debug.Log("ConversationTransitionController: afterChangeToLast�t���O��true�̂��߁A�V�[���J�ڂ��X�L�b�v���܂�");
            yield break; // afterChangeToLast��true�̏ꍇ�̓V�[���J�ڂ��~
        }

        // �t���O��false�܂��͎擾�ł��Ȃ��ꍇ�͊����̃V�[���J�ڏ������p��
        StartCoroutine(CheckFlagAndTransition());
    }

    /// <summary>
    /// �t���O���`�F�b�N���ēK�؂ȃV�[���ɑJ��
    /// </summary>
    private IEnumerator CheckFlagAndTransition()
    {
        // �R���[�`���J�n���ɂ��ēx�t���O�`�F�b�N
        RememberButtonTextChangerForHer textChanger = FindFirstObjectByType<RememberButtonTextChangerForHer>();
        if (textChanger != null && textChanger.DataResetPanelControllerBoot)
        {
            if (debugMode) Debug.Log("ConversationTransitionController: CheckFlagAndTransition�J�n����DataResetPanelControllerBoot�t���O��true�̂��߁A�������~���܂�");
            yield break; // �R���[�`�����I��
        }

        // afterChangeToLast�t���O�̍ă`�F�b�N
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.GetAfterChangeToLastFlag())
        {
            if (debugMode) Debug.Log("ConversationTransitionController: CheckFlagAndTransition�J�n����afterChangeToLast�t���O��true�̂��߁A�������~���܂�");
            yield break; // �R���[�`�����I��
        }

        isTransitioning = true;
        string targetScene = defaultOpeningSceneName;
        bool transitionSuccessful = false;

        try
        {
            bool saveExists = gameSaveManager != null && gameSaveManager.SaveDataExists();

            // �Z�[�u�f�[�^���݃`�F�b�N
            if (requireSaveDataForConversation && !saveExists)
            {
                if (debugMode)
                    Debug.Log("ConversationTransitionController: �Z�[�u�f�[�^�����݂��Ȃ����߁A�ʏ�̑J�ڃ��W�b�N���g�p���܂�");

                targetScene = defaultOpeningSceneName;
                transitionSuccessful = true;
            }
            else if (saveExists)
            {
                // �Z�[�u�f�[�^��ǂݍ���
                bool loadSuccess = gameSaveManager.LoadGame();

                if (loadSuccess)
                {
                    // afterChangeToHerMemory�t���O���ēx�m�F�i�ŐV�̏�Ԃ��擾�j
                    bool afterChangeFlag = gameSaveManager.GetAfterChangeToHerMemoryFlag();

                    if (debugMode)
                    {
                        Debug.Log($"ConversationTransitionController: �Z�[�u�f�[�^�ǂݍ��ݐ���");
                        Debug.Log($"ConversationTransitionController: afterChangeToHerMemory�t���O = {afterChangeFlag}");
                    }

                    // �t���O��false�̏ꍇ�͕K���ʏ�̑J�ڂ��s��
                    if (!afterChangeFlag && !forceConversationScene)
                    {
                        // endOpeningScene�t���O���`�F�b�N
                        bool isOpeningCompleted = gameSaveManager.GetEndOpeningSceneFlag();

                        if (isOpeningCompleted)
                        {
                            targetScene = normalGameSceneName; // MainScene
                            if (debugMode)
                                Debug.Log("ConversationTransitionController: OpeningScene�����ς݁BMainScene�ֈڍs");
                        }
                        else
                        {
                            targetScene = defaultOpeningSceneName;
                            if (debugMode)
                                Debug.Log("ConversationTransitionController: �t���O��false�̂��߁A�I�[�v�j���O�V�[���ɑJ�ڂ��܂�");
                        }
                    }
                    else if (afterChangeFlag || forceConversationScene)
                    {
                        targetScene = conversationSceneName;
                        if (debugMode)
                            Debug.Log("ConversationTransitionController: �t���O��true�̂��߁A��b�V�[���ɑJ�ڂ��܂�");
                    }
                    else
                    {
                        // �ǂ���̏����ɂ����Ă͂܂�Ȃ��ꍇ�̃t�H�[���o�b�N
                        targetScene = defaultOpeningSceneName;
                        if (debugMode)
                            Debug.Log("ConversationTransitionController: �f�t�H���g�ŃI�[�v�j���O�V�[���ɑJ�ڂ��܂�");
                    }

                    transitionSuccessful = true;
                }
                else
                {
                    if (debugMode)
                        Debug.Log("ConversationTransitionController: �Z�[�u�f�[�^�̓ǂݍ��݂Ɏ��s�A�I�[�v�j���O�V�[���ɑJ��");
                    targetScene = defaultOpeningSceneName;
                    transitionSuccessful = true;
                }
            }
            else
            {
                if (debugMode)
                    Debug.Log("ConversationTransitionController: �Z�[�u�f�[�^�����݂��܂���A�I�[�v�j���O�V�[���ɑJ��");
                targetScene = defaultOpeningSceneName;
                transitionSuccessful = true;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ConversationTransitionController: �V�[�����蒆�ɃG���[: {ex.Message}");
            targetScene = defaultOpeningSceneName;
            transitionSuccessful = false;
        }

        // �t�F�[�h�ƃV�[���J��
        if (useFadeTransition && fadePanel != null)
        {
            yield return StartCoroutine(FadeOut());
        }

        yield return StartCoroutine(LoadSceneAsync(targetScene));

        isTransitioning = false;

        if (debugMode)
        {
            string status = transitionSuccessful ? "����" : "�G���[��";
            Debug.Log($"ConversationTransitionController: �V�[���J�ڊ��� ({status})");
            Debug.Log($"ConversationTransitionController: �ŏI�I�ȑJ�ڐ� = {targetScene}");
        }
    }

    /// <summary>
    /// �Z�[�u�f�[�^�̑��݂��`�F�b�N�i�V�K�ǉ��j
    /// </summary>
    private bool CheckSaveDataExists()
    {
        try
        {
            // GameSaveManager����Z�[�u�f�[�^�̑��݂��m�F
            if (gameSaveManager != null)
            {
                bool exists = gameSaveManager.SaveDataExists();
                if (debugMode)
                    Debug.Log($"ConversationTransitionController: GameSaveManager�ł̃Z�[�u�f�[�^���݊m�F: {exists}");
                return exists;
            }

            // GameSaveManager�����݂��Ȃ��ꍇ��false
            if (debugMode)
                Debug.Log("ConversationTransitionController: GameSaveManager�����݂��Ȃ����߁A�Z�[�u�f�[�^�Ȃ��Ɣ���");
            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ConversationTransitionController: �Z�[�u�f�[�^���݃`�F�b�N���ɃG���[: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// �t�F�[�h�A�E�g����
    /// </summary>
    private IEnumerator FadeOut()
    {
        if (fadePanel == null) yield break;

        fadePanel.gameObject.SetActive(true);
        fadePanel.alpha = 0f;

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            yield return null;
        }

        fadePanel.alpha = 1f;
    }

    /// <summary>
    /// �񓯊��ŃV�[����ǂݍ���
    /// </summary>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        if (debugMode)
            Debug.Log($"ConversationTransitionController: �V�[�� '{sceneName}' ��ǂݍ��ݒ�...");

        // �񓯊����[�h���J�n
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // asyncLoad��null�łȂ����Ƃ��m�F
        if (asyncLoad == null)
        {
            Debug.LogError($"ConversationTransitionController: �V�[�� '{sceneName}' �̓ǂݍ��݂Ɏ��s���܂���");
            yield break;
        }

        // �����I�ɃV�[�����A�N�e�B�u�ɂȂ�Ȃ��悤�ɂ���
        asyncLoad.allowSceneActivation = false;

        // ���[�h����������܂őҋ@
        while (!asyncLoad.isDone)
        {
            // �i����\���i0�`0.9�͈̔́j
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            if (debugMode)
                Debug.Log($"�ǂݍ��ݐi��: {progress * 100:F0}%");

            // ���[�h��90%����������i���ۂɂ͊������Ă���j
            if (asyncLoad.progress >= 0.9f)
            {
                // �t�F�[�h�A�E�g���������Ă��邱�Ƃ��m�F
                if (useFadeTransition && fadePanel != null)
                {
                    // �t�F�[�h�A�E�g����������܂ŏ����ҋ@
                    yield return new WaitForSeconds(0.1f);
                }

                // �V�[�����A�N�e�B�u��
                asyncLoad.allowSceneActivation = true;

                if (debugMode)
                    Debug.Log($"ConversationTransitionController: �V�[�� '{sceneName}' ���A�N�e�B�u�����܂���");
            }

            yield return null;
        }

        if (debugMode)
            Debug.Log($"ConversationTransitionController: �V�[�� '{sceneName}' �̓ǂݍ��݂��������܂���");
    }

    /// <summary>
    /// �J�ڎ��̌��ʉ����Đ�
    /// </summary>
    private void PlayTransitionSound()
    {
        if (transitionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(transitionSound);
        }
    }

    /// <summary>
    /// �t�F�[�h�p�p�l���𓮓I�ɍ쐬
    /// </summary>
    private void CreateFadePanel()
    {
        // Canvas��T��
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("ConversationTransitionController: Canvas��������Ȃ����߁A�t�F�[�h�p�l�����쐬�ł��܂���");
            return;
        }

        // �t�F�[�h�p�l���pGameObject���쐬
        GameObject fadeObj = new GameObject("FadePanel");
        fadeObj.transform.SetParent(canvas.transform, false);

        // RectTransform��ݒ�i�S��ʂ��J�o�[�j
        RectTransform rectTransform = fadeObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // Image�R���|�[�l���g��ǉ��i���w�i�j
        Image image = fadeObj.AddComponent<Image>();
        image.color = Color.black;

        // CanvasGroup��ǉ�
        fadePanel = fadeObj.AddComponent<CanvasGroup>();
        fadePanel.alpha = 0f;

        // �őO�ʂɔz�u
        fadeObj.transform.SetAsLastSibling();

        // ������Ԃł͔�A�N�e�B�u
        fadeObj.SetActive(false);

        if (debugMode)
            Debug.Log("ConversationTransitionController: �t�F�[�h�p�l���𓮓I�ɍ쐬���܂���");
    }

    /// <summary>
    /// �O������afterChangeToHerMemory�t���O�̏�Ԃ��m�F
    /// </summary>
    public bool IsConversationSceneEligible()
    {
        if (forceConversationScene) return true;

        // �Z�[�u�f�[�^���݃`�F�b�N
        if (requireSaveDataForConversation && !CheckSaveDataExists())
        {
            if (debugMode)
                Debug.Log("ConversationTransitionController: �Z�[�u�f�[�^�����݂��Ȃ����߁A��b�V�[���ΏۊO");
            return false;
        }

        if (gameSaveManager == null) return false;

        // afterChangeToHerMemory�t���O�݂̂��`�F�b�N
        return gameSaveManager.GetAfterChangeToHerMemoryFlag();
    }

    /// <summary>
    /// �e�X�g�p�F�����I�ɉ�b�V�[���t���O��ݒ�
    /// </summary>
    [ContextMenu("Debug: Force Conversation Scene")]
    public void ForceConversationSceneFlag()
    {
        forceConversationScene = true;
        if (debugMode)
            Debug.Log("ConversationTransitionController: ������b�V�[���t���O��L���ɂ��܂���");
    }

    /// <summary>
    /// �e�X�g�p�F�ʏ�V�[���t���O�Ƀ��Z�b�g
    /// </summary>
    [ContextMenu("Debug: Reset to Normal Scene")]
    public void ResetToNormalScene()
    {
        forceConversationScene = false;
        if (debugMode)
            Debug.Log("ConversationTransitionController: �ʏ�V�[���t���O�Ƀ��Z�b�g���܂���");
    }

    /// <summary>
    /// ���݂̑J�ڏ�Ԃ��擾
    /// </summary>
    public bool IsTransitioning => isTransitioning;

    /// <summary>
    /// �O������V�[�����𓮓I�ɕύX
    /// </summary>
    public void SetConversationSceneName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            conversationSceneName = sceneName;
            if (debugMode)
                Debug.Log($"ConversationTransitionController: ��b�V�[������ '{sceneName}' �ɐݒ肵�܂���");
        }
    }

    /// <summary>
    /// �O������ʏ�Q�[���V�[�����𓮓I�ɕύX
    /// </summary>
    public void SetNormalGameSceneName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            normalGameSceneName = sceneName;
            if (debugMode)
                Debug.Log($"ConversationTransitionController: �ʏ�Q�[���V�[������ '{sceneName}' �ɐݒ肵�܂���");
        }
    }

    /// <summary>
    /// �f�o�b�O�p�F���݂̏�Ԃ�\��
    /// </summary>
    [ContextMenu("Debug: Show Current Status")]
    public void ShowCurrentStatus()
    {
        bool saveDataExists = CheckSaveDataExists();
        bool afterChangeFlag = gameSaveManager?.GetAfterChangeToHerMemoryFlag() ?? false;
        bool eligible = IsConversationSceneEligible();

        Debug.Log($"=== ConversationTransitionController ��� ===");
        Debug.Log($"�Z�[�u�f�[�^����: {saveDataExists}");
        Debug.Log($"AfterChange�t���O: {afterChangeFlag}");
        Debug.Log($"��b�V�[���Ώ�: {eligible}");
        Debug.Log($"������b�V�[��: {forceConversationScene}");
        Debug.Log($"�J�ڒ�: {isTransitioning}");
        Debug.Log($"==============================");
    }
}