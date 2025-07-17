using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// MonologueScene����J�ڂ��Ă����ۂɃ^�C�g����"Thanks for playing the game."�ɕύX����R���|�[�l���g
/// �^�C�g���e�L�X�g�̕\���Ǘ���TitleTextLoaderForMonologueScene���s��
/// </summary>
public class TitleTextChangerForMonologueScene : MonoBehaviour
{
    [Header("�e�L�X�g�ݒ�")]
    [Tooltip("�ύX�Ώۂ�TextMeshPro�R���|�[�l���g")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("�ύX��̃e�L�X�g")]
    [SerializeField] private string newTitleText = "Thanks for playing the game.";

    [Header("�A�j���[�V�����ݒ�")]
    [Tooltip("1�����ύX�ɂ����鎞�ԁi�b�j")]
    [SerializeField] private float changeInterval = 0.2f;

    [Tooltip("�ύX�J�n�܂ł̒x�����ԁi�b�j")]
    [SerializeField] private float startDelay = 1.0f;

    [Tooltip("�����ύX���̃G�t�F�N�g�i�t�F�[�h�A�O���b�`�Ȃǁj")]
    [SerializeField] private bool useGlitchEffect = true;

    [Tooltip("�O���b�`�G�t�F�N�g�̎������ԁi�b�j")]
    [SerializeField] private float glitchDuration = 0.1f;

    [Header("�{�^������ݒ�")]
    [Tooltip("�^�C�g���ύX����MenuContainer�̃{�^���𖳌������邩")]
    [SerializeField] private bool disableButtonsDuringChange = true;

    [Tooltip("MenuContainer�ւ̎Q�Ɓi���ݒ�̏ꍇ�͎��������j")]
    [SerializeField] private GameObject menuContainer;

    [Header("�t���O�Ǘ��ݒ�")]
    [Tooltip("�^�C�g���ύX��Ƀt���O��ݒ肷�邩")]
    [SerializeField] private bool setCompletionFlag = true;

    [Header("�f�o�b�O�ݒ�")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceExecute = false;

    private string currentText;
    private bool isChanging = false;

    // �ÓI�ϐ��ɂ���ԊǗ�
    private static bool shouldExecuteOnNextLoad = false;
    private static bool titleChangedToLast = false; // �����t���O�̑��
    private bool soundEnabled = true; // ���ʉ��̗L��/�����t���O

    private readonly string glitchChars = "!#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`{|}~";

    private void Awake()
    {
        if (titleText == null)
        {
            titleText = GetComponent<TMP_Text>();
            if (titleText == null)
            {
                titleText = GetComponentInChildren<TMP_Text>();
            }
        }

        if (titleText == null)
        {
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        currentText = titleText.text;

        if (ShouldExecuteTitleChange())
        {
            if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: �^�C�g���ύX���J�n���܂�");
            // StartCoroutine�̑O�ɁA������xsoundEnabled�̏�Ԃ��m�F
            if (debugMode) Debug.Log($"TitleTextChangerForMonologueScene: soundEnabled = {soundEnabled}");
            StartCoroutine(StartTitleChange());
        }
    }

    private bool ShouldExecuteTitleChange()
    {
        if (debugMode && forceExecute)
        {
            Debug.Log("TitleTextChangerForMonologueScene: �������s���[�h�Ń^�C�g���ύX�����s");
            return true;
        }

        if (titleChangedToLast)
        {
            if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: ���Ƀ^�C�g���ύX�ς݂ł�");
            return false;
        }

        if (shouldExecuteOnNextLoad)
        {
            shouldExecuteOnNextLoad = false;
            if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: MonologueScene����̑J�ڂ����o");
            return true;
        }

        // MonologueDisplayManager��allDialoguesCompleted�t���O���`�F�b�N
        GameSaveManager saveManager = GameSaveManager.Instance;
        if (saveManager != null && saveManager.GetAllDialoguesCompletedFlag())
        {
            if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: �S�_�C�A���O�����t���O�����o");
            return true;
        }

        return false;
    }

    public static void SetTransitionFlag()
    {
        shouldExecuteOnNextLoad = true;
    }

    private IEnumerator StartTitleChange()
    {
        yield return new WaitForSeconds(startDelay);

        // �ύX�J�n���Ƀ{�^���𖳌���
        SetMenuButtonsInteractable(false);

        isChanging = true;

        // ���݂̃e�L�X�g�ƐV�����e�L�X�g�̒����𒲐�
        int maxLength = Mathf.Max(currentText.Length, newTitleText.Length);

        for (int i = 0; i < maxLength; i++)
        {
            if (useGlitchEffect)
            {
                yield return StartCoroutine(ChangeCharacterWithGlitch(i));
            }
            else
            {
                yield return StartCoroutine(ChangeCharacter(i));
            }

            // �f�o�b�O���O��ǉ�
            if (debugMode)
            {
                Debug.Log($"TitleTextChangerForMonologueScene: ���ʉ��`�F�b�N - soundEnabled={soundEnabled}");
            }

            // SoundEffectManager���g�p�������ʉ��Đ�
            if (soundEnabled && SoundEffectManager.Instance != null)
            {
                if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: ���ʉ����Đ����܂�");
                SoundEffectManager.Instance.PlayTypeSound();
            }

            yield return new WaitForSeconds(changeInterval);
        }

        // �ŏI�I�Ƀe�L�X�g�����S�ɒu������
        titleText.text = newTitleText;

        isChanging = false;

        //SoundEffectManager���g�p�����������Đ�
        if (soundEnabled && SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlayCompletionSound();
        }

        if (setCompletionFlag)
        {
            titleChangedToLast = true;

            GameSaveManager saveManager = GameSaveManager.Instance;
            if (saveManager != null)
            {
                saveManager.SetAfterChangeToLastFlag(true);
                saveManager.SaveGame();
            }

            if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: �����t���O��ݒ肵�܂���");
        }

        // �ύX�������Ƀ{�^����L����
        SetMenuButtonsInteractable(true);

    }

    /// <summary>
    /// MenuContainer���̃{�^���̗L��/������؂�ւ���
    /// </summary>
    private void SetMenuButtonsInteractable(bool interactable)
    {
        if (!disableButtonsDuringChange) return;

        // MenuContainer�̎擾
        if (menuContainer == null)
        {
            menuContainer = GameObject.Find("MenuContainer");
        }

        if (menuContainer == null)
        {
            if (debugMode) Debug.LogWarning($"{GetType().Name}: MenuContainer��������܂���");
            return;
        }

        // �S�Ă�Button�R���|�[�l���g���擾���Đ���
        Button[] buttons = menuContainer.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            button.interactable = interactable;
        }

        if (debugMode)
        {
            Debug.Log($"{GetType().Name}: MenuContainer�̃{�^����{(interactable ? "�L��" : "����")}�����܂���");
        }
    }

    private IEnumerator ChangeCharacter(int index)
    {
        char[] chars = titleText.text.ToCharArray();

        // ��������������ꍇ�̑Ή�
        if (index >= chars.Length)
        {
            string newText = titleText.text;
            while (newText.Length <= index)
            {
                newText += " ";
            }
            chars = newText.ToCharArray();
        }

        if (index < newTitleText.Length)
        {
            chars[index] = newTitleText[index];
        }
        else if (index < chars.Length)
        {
            chars[index] = ' '; // �]���ȕ������󔒂�
        }

        titleText.text = new string(chars);
        yield return null;
    }

    private IEnumerator ChangeCharacterWithGlitch(int index)
    {
        char[] chars = titleText.text.ToCharArray();

        // ��������������ꍇ�̑Ή�
        if (index >= chars.Length)
        {
            string newText = titleText.text;
            while (newText.Length <= index)
            {
                newText += " ";
            }
            chars = newText.ToCharArray();
        }

        char targetChar = index < newTitleText.Length ? newTitleText[index] : ' ';

        float glitchTimer = 0f;

        while (glitchTimer < glitchDuration)
        {
            chars[index] = glitchChars[Random.Range(0, glitchChars.Length)];
            titleText.text = new string(chars);

            glitchTimer += Time.deltaTime;
            yield return null;
        }

        chars[index] = targetChar;
        titleText.text = new string(chars);
    }

    [ContextMenu("Execute Title Change")]
    public void ExecuteTitleChange()
    {
        if (!isChanging)
        {
            StartCoroutine(StartTitleChange());
        }
    }

    [ContextMenu("Reset Completion Flag")]
    public void ResetCompletionFlag()
    {
        titleChangedToLast = false;

        GameSaveManager saveManager = GameSaveManager.Instance;
        if (saveManager != null)
        {
            saveManager.SetAfterChangeToLastFlag(false);
            saveManager.SaveGame();
        }

        if (debugMode) Debug.Log("TitleTextChangerForMonologueScene: �����t���O�����Z�b�g���܂���");
    }

    /// <summary>
    /// ������Ԃ��擾
    /// </summary>
    public static bool IsTitleChanged()
    {
        return titleChangedToLast;
    }

    /// <summary>
    /// ���ʉ��̗L��/������ݒ�
    /// </summary>
    /// <param name="enabled">�L���ɂ���ꍇ��true</param>
    public void SetSoundEnabled(bool enabled)
    {
        soundEnabled = enabled;
        if (debugMode) Debug.Log($"TitleTextChangerForMonologueScene: ���ʉ���{(enabled ? "�L��" : "����")}�ɂ��܂���");
    }
}