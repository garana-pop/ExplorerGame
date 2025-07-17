using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// HerMainScene����J�ڂ��Ă����ۂɁu�v���o���v�{�^���̃e�L�X�g���u���ӂ���v�ɕύX����R���|�[�l���g
/// �e�L�X�g�̕\���Ǘ���RememberButtonTextLoaderForHer���s��
/// </summary>
public class RememberButtonTextChangerForHer : MonoBehaviour
{
    [Header("�e�L�X�g�ݒ�")]
    [Tooltip("�ύX�Ώۂ�TextMeshPro�R���|�[�l���g")]
    [SerializeField] private TMP_Text buttonText;

    [Tooltip("�ύX��̃e�L�X�g")]
    [SerializeField] private string newButtonText = "���ӂ���";

    [Header("�{�^������ݒ�")]
    [Tooltip("�v���o���{�^����Button �R���|�[�l���g")]
    [SerializeField] private Button rememberButton;

    [Header("�V�[���J�ڐݒ�")]
    [Tooltip("�J�ڐ�̃V�[����")]
    [SerializeField] private string targetSceneName = "MonologueScene";

    [Header("�t�ϊ��ݒ�")]
    [Tooltip("�t�ϊ����̃^�[�Q�b�g�e�L�X�g")]
    [SerializeField] private string reverseTargetText = "�v���o��";

    [Tooltip("�V�[���J�ڎ��̃t�F�[�h���ԁi�b�j")]
    [SerializeField] private float transitionDelay = 0.5f;

    [Header("�A�j���[�V�����ݒ�")]
    [Tooltip("1�����ύX�ɂ����鎞�ԁi�b�j")]
    [SerializeField] private float changeInterval = 0.25f;

    [Tooltip("�ύX�J�n�܂ł̒x�����ԁi�b�j")]
    [SerializeField] private float startDelay = 0.8f;

    [Tooltip("�����ύX���̃G�t�F�N�g�i�t�F�[�h�A�O���b�`�Ȃǁj")]
    [SerializeField] private bool useGlitchEffect = true;

    [Tooltip("�O���b�`�G�t�F�N�g�̎������ԁi�b�j")]
    [SerializeField] private float glitchDuration = 0.08f;

    [Header("���ʉ��ݒ�")]
    [Tooltip("�����ύX���̌��ʉ�")]
    [SerializeField] private AudioClip changeSound;

    [Tooltip("�������̌��ʉ�")]
    [SerializeField] private AudioClip completeSound;

    [Tooltip("AudioSource�inull �̏ꍇ�͎����擾�j")]
    [SerializeField] private AudioSource audioSource;

    [Header("�t���O�Ǘ��ݒ�")]
    [Tooltip("�e�L�X�g�ύX��Ƀt���O��ݒ肷�邩")]
    [SerializeField] private bool setCompletionFlag = true;

    [Header("�f�o�b�O�ݒ�")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceExecute = false;

    private string currentText;
    private bool isChanging = false;

    // �ÓI�ϐ��ɂ���ԊǗ�
    private static bool shouldExecuteOnNextLoad = false;
    private static bool buttonTextChangedForHer = false; // �����t���O
    private static bool shouldExecuteReverseChangeOnNextLoad = false;
    private static bool isReverseMode = false; // �t�ϊ����[�h���ǂ������Ǘ�

    public bool DataResetPanelControllerBoot = false; // DataResetPanelController�N���X�̋N���Ǘ��t���O

    private readonly string glitchChars = "!#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`{|}~";

    private void Awake()
    {

        if (buttonText == null)
        {
            // MenuContainer�̎v���o���{�^����T��
            GameObject menuContainer = GameObject.Find("MenuContainer");
            if (menuContainer != null)
            {
                Transform rememberButton = menuContainer.transform.Find("�v���o���{�^��");
                if (rememberButton != null)
                {
                    buttonText = rememberButton.GetComponentInChildren<TMP_Text>();
                }
            }
        }

        if (buttonText == null)
        {
            Debug.LogError("RememberButtonTextChangerForHer: �v���o���{�^����TextMeshPro�R���|�[�l���g��������܂���");
            enabled = false;
            return;
        }

        // StartTextChange�R���[�`���̍Ō�A�����t���O�ݒ��ɒǉ�
        if (rememberButton != null)
        {
            // RemoveAllListeners���g�킸�A�����̃��X�i�[�̂݊Ǘ�
            rememberButton.onClick.RemoveListener(OnRememberButtonClicked);
            rememberButton.onClick.AddListener(OnRememberButtonClicked);

        }

        // AudioSource
        if (audioSource == null && (changeSound != null || completeSound != null))
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
    }

    private void Start()
    {
        currentText = buttonText.text;

        if (ShouldExecuteTextChange())
        {
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: �{�^���e�L�X�g�ύX���J�n���܂�");
            StartCoroutine(StartTextChange());
        }
    }

    private bool ShouldExecuteTextChange()
    {
        if (debugMode)
        {
            Debug.Log($"RememberButtonTextChangerForHer�FShouldExecuteTextChange���\�b�h�͌Ă΂�Ă�");
            Debug.Log($"RememberButtonTextChangerForHer: shouldExecuteOnNextLoad�t���O:{shouldExecuteOnNextLoad}");
            Debug.Log($"RememberButtonTextChangerForHer: shouldExecuteReverseChangeOnNextLoad�t���O:{shouldExecuteReverseChangeOnNextLoad}");
            Debug.Log($"RememberButtonTextChangerForHer: buttonTextChangedForHer�t���O:{buttonTextChangedForHer}");
            Debug.Log($"RememberButtonTextChangerForHer: isReverseMode�t���O:{isReverseMode}");
        }

        // �f�o�b�O���[�h�������s
        if (debugMode && forceExecute)
        {
            Debug.Log("RememberButtonTextChangerForHer: �������s���[�h�Ńe�L�X�g�ύX�����s");
            return true;
        }

        // �t�ϊ��̗D��`�F�b�N�iMonologueScene �� TitleScene�j
        if (shouldExecuteReverseChangeOnNextLoad)
        {
            shouldExecuteReverseChangeOnNextLoad = false;
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: �t�ϊ��iMonologueScene �� TitleScene�j�����s");
            return true;
        }

        // �ʏ�ϊ��iHerMainScene �� TitleScene�j
        if (shouldExecuteOnNextLoad)
        {
            shouldExecuteOnNextLoad = false;
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: �ʏ�ϊ��iHerMainScene �� TitleScene�j�����s");
            return true;
        }

        // ���łɕύX�ς݂̏ꍇ�̓X�L�b�v
        if (buttonTextChangedForHer && !isReverseMode)
        {
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: ���łɃe�L�X�g�ύX�ς݂ł�");
            return false;
        }

        return false;
    }


    public static void SetTransitionFlag()
    {
        shouldExecuteOnNextLoad = true;
        isReverseMode = false; // �ʏ�ϊ����[�h�ɐݒ�
        Debug.Log("RememberButtonTextChangerForHer: �ʏ�ϊ��t���O��ݒ肵�܂���");
    }

    private IEnumerator StartTextChange()
    {
        yield return new WaitForSeconds(startDelay);

        isChanging = true;

        // �ϊ������ƃ^�[�Q�b�g�e�L�X�g������
        string targetText = DetermineTargetText();

        // ���݂̃e�L�X�g�ƃ^�[�Q�b�g�e�L�X�g�̒����𒲐�
        int maxLength = Mathf.Max(currentText.Length, targetText.Length);

        for (int i = 0; i < maxLength; i++)
        {
            if (useGlitchEffect)
            {
                yield return StartCoroutine(ChangeCharacterWithGlitch(i, targetText));
            }
            else
            {
                yield return StartCoroutine(ChangeCharacter(i, targetText));
            }

            yield return new WaitForSeconds(changeInterval);
        }

        // �ŏI�I�Ƀe�L�X�g�����S�ɒu������
        buttonText.text = targetText;
        currentText = targetText;

        isChanging = false;

        // �����t���O�̐ݒ�i�C���Łj
        if (targetText == reverseTargetText)
        {
            // �t�ϊ��̏ꍇ�̓t���O�����Z�b�g
            buttonTextChangedForHer = false;
            isReverseMode = false; // �t�ϊ����[�h������
            DataResetPanelControllerBoot = true; //
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: �t�ϊ��ɂ�芮���t���O�ƃ��[�h�����Z�b�g���܂���");
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: DataResetPanelControllerBoot�t���O" + DataResetPanelControllerBoot + "�ɕύX");
        }
        else if (setCompletionFlag && targetText == newButtonText)
        {
            // �ʏ�ϊ��̏ꍇ�̓t���O��ݒ�
            buttonTextChangedForHer = true;
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: �����t���O��ݒ肵�܂���");
        }

        if (debugMode) Debug.Log("RememberButtonTextChangerForHer: �e�L�X�g�ύX���������܂���");
    }

    private IEnumerator ChangeCharacter(int index, string targetText)
    {
        char[] chars = currentText.ToCharArray();

        // �z��̃T�C�Y�𒲐�
        if (chars.Length < targetText.Length)
        {
            char[] newChars = new char[targetText.Length];
            for (int i = 0; i < chars.Length; i++)
            {
                newChars[i] = chars[i];
            }
            for (int i = chars.Length; i < targetText.Length; i++)
            {
                newChars[i] = ' ';
            }
            chars = newChars;
        }

        if (index < chars.Length && index < targetText.Length)
        {
            chars[index] = targetText[index];
            buttonText.text = new string(chars);
            currentText = new string(chars);
        }

        yield return null;
    }


    private IEnumerator ChangeCharacterWithGlitch(int index, string targetText)
    {
        char[] chars = currentText.ToCharArray();

        // �z��̃T�C�Y�𒲐�
        if (chars.Length < targetText.Length)
        {
            char[] newChars = new char[targetText.Length];
            for (int i = 0; i < chars.Length; i++)
            {
                newChars[i] = chars[i];
            }
            for (int i = chars.Length; i < targetText.Length; i++)
            {
                newChars[i] = ' ';
            }
            chars = newChars;
        }

        char targetChar = index < targetText.Length ? targetText[index] : ' ';

        float glitchTimer = 0f;

        while (glitchTimer < glitchDuration)
        {
            if (index < chars.Length)
            {
                chars[index] = glitchChars[Random.Range(0, glitchChars.Length)];
                buttonText.text = new string(chars);
            }

            glitchTimer += Time.deltaTime;
            yield return null;
        }

        if (index < chars.Length)
        {
            chars[index] = targetChar;
            buttonText.text = new string(chars);
            currentText = new string(chars);
        }
    }


    [ContextMenu("Execute Text Change")]
    public void ExecuteTextChange()
    {
        if (!isChanging)
        {
            StartCoroutine(StartTextChange());
        }
    }

    [ContextMenu("Reset Completion Flag")]
    public void ResetCompletionFlag()
    {
        buttonTextChangedForHer = false;

        if (debugMode) Debug.Log("RememberButtonTextChangerForHer: �����t���O�����Z�b�g���܂���");
    }

    /// <summary>
    /// ������Ԃ��擾�iRememberButtonTextLoaderForHer����Q�Ɖ\�j
    /// </summary>
    public static bool IsTextChanged()
    {
        return buttonTextChangedForHer;
    }

    /// <summary>
    /// �v���o���{�^�����N���b�N���ꂽ���̏���
    /// </summary>
    private void OnRememberButtonClicked()
    {
        // DataResetPanelControllerBoot�t���O�`�F�b�N���ŗD��Ŏ��s
        if (DataResetPanelControllerBoot)
        {
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: DataResetPanelControllerBoot�t���O��true�̂��߁A�V�[���J�ڂ��X�L�b�v���܂�");
            return; // �������^�[���ŃV�[���J�ڏ������~CheckAfterChangeToLastFlagAndProceed
        }

        // �x������afterChangeToLast�t���O���`�F�b�N
        StartCoroutine(CheckAfterChangeToLastFlagAndProceed());
    }

    private IEnumerator CheckAfterChangeToLastFlagAndProceed()
    {
        // GameSaveManager�̃��[�h������҂�
        yield return new WaitForSeconds(0.5f);

        // afterChangeToLast�t���O���`�F�b�N
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.GetAfterChangeToLastFlag())
        {
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: afterChangeToLast�t���O��true�̂��߁A�V�[���J�ڂ��X�L�b�v���܂�");
            yield break; // afterChangeToLast��true�̏ꍇ�̓V�[���J�ڂ��~
        }

        // �t���O��false�܂��͎擾�ł��Ȃ��ꍇ�͊����̃V�[���J�ڏ������p��
        if (debugMode) Debug.Log($"RememberButtonTextChangerForHer: {targetSceneName}�֑J�ڂ��܂�");
        StartCoroutine(TransitionToMonologue());
    }


    /// <summary>
    /// MonologueScene�ւ̑J�ڏ���
    /// </summary>
    private IEnumerator TransitionToMonologue()
    {
        // �{�^���𖳌����i��d�N���b�N�h�~�j
        if (rememberButton != null)
        {
            rememberButton.interactable = false;
        }

        // �R���[�`���J�n���ɂ��ēx�t���O�`�F�b�N
        if (DataResetPanelControllerBoot)
        {
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: TransitionToMonologue�J�n����DataResetPanelControllerBoot�t���O��true�̂��߁A�������~���܂�");
            yield break; // �R���[�`�����I��
        }

        // afterChangeToLast�t���O�̍ă`�F�b�N
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.GetAfterChangeToLastFlag())
        {
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: TransitionToMonologue�J�n����afterChangeToLast�t���O��true�̂��߁A�������~���܂�");
            yield break; // �R���[�`�����I��
        }

        if (debugMode) Debug.Log("RememberButtonTextChangerForHer: �V�[���J�ڏ������J�n���܂�");

        // �J�ڒx���i�K�v�ɉ����ăt�F�[�h�����Ȃǂ�ǉ��\�j
        yield return new WaitForSeconds(transitionDelay);

        // �V�[���J��
        SceneManager.LoadScene(targetSceneName);
    }

    /// <summary>
    /// MonologueScene����̋t�ϊ��t���O��ݒ�
    /// </summary>
    public static void SetReverseTransitionFlag()
    {
        shouldExecuteReverseChangeOnNextLoad = true;
        isReverseMode = true; // �t�ϊ����[�h�ɐݒ�
        Debug.Log("RememberButtonTextChangerForHer-SetReverseTransitionFlag():shouldExecuteReverseChangeOnNextLoad�t���O�F" + shouldExecuteReverseChangeOnNextLoad);
        Debug.Log("RememberButtonTextChangerForHer: �t�ϊ����[�h�ɐݒ肵�܂���");
    }

    /// <summary>
    /// �ϊ������ɉ����ă^�[�Q�b�g�e�L�X�g������
    /// </summary>
    private string DetermineTargetText()
    {
        if (debugMode)
        {
            Debug.Log($"RememberButtonTextChangerForHer: isReverseMode�t���O: {isReverseMode}");
        }

        // �t���O�x�[�X�Ŕ���
        if (isReverseMode)
        {
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: �t�ϊ����[�h - �^�[�Q�b�g: " + reverseTargetText);
            return reverseTargetText; // "�v���o��"
        }
        else
        {
            if (debugMode) Debug.Log("RememberButtonTextChangerForHer: �ʏ�ϊ����[�h - �^�[�Q�b�g: " + newButtonText);
            return newButtonText; // "���ӂ���"
        }
    }

}