using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// TitleScene�̃^�C�g���e�L�X�g���ꕶ�����ύX����R���|�[�l���g
/// DaughterRequestScene����J�ڂ��Ă����ꍇ�̂ݓ���
/// </summary>
public class TitleTextChanger : MonoBehaviour
{
    [Header("�e�L�X�g�ݒ�")]
    [Tooltip("�ύX�Ώۂ�TextMeshPro�R���|�[�l���g")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("�ύX��̃e�L�X�g")]
    [SerializeField] private string newTitleText = "�u�ޏ��v�̋L��";

    [Tooltip("���̃e�L�X�g�i�����p�j")]
    [SerializeField] private string originalTitleText = "�u�肢�v�̋L��";

    [Header("�A�j���[�V�����ݒ�")]
    [Tooltip("1�����ύX�ɂ����鎞�ԁi�b�j")]
    [SerializeField] private float changeInterval = 0.3f;

    [Tooltip("�ύX�J�n�܂ł̒x�����ԁi�b�j")]
    [SerializeField] private float startDelay = 1.0f;

    [Tooltip("�����ύX���̃G�t�F�N�g�i�t�F�[�h�A�O���b�`�Ȃǁj")]
    [SerializeField] private bool useGlitchEffect = true;

    [Tooltip("�O���b�`�G�t�F�N�g�̎������ԁi�b�j")]
    [SerializeField] private float glitchDuration = 0.1f;

    [Header("���ʉ��ݒ�")]
    [Tooltip("�����ύX���̌��ʉ�")]
    [SerializeField] private AudioClip changeSound;

    [Tooltip("�������̌��ʉ�")]
    [SerializeField] private AudioClip completeSound;

    [Header("�V�[���J�ڃ`�F�b�N")]
    [Tooltip("����̃V�[������J�ڂ����ꍇ�̂ݓ��삷�邩")]
    [SerializeField] private bool checkPreviousScene = true;

    [Tooltip("�O�̃V�[�����i���̖��O����J�ڂ����ꍇ�̂ݓ���j")]
    [SerializeField] private string previousSceneName = "DaughterRequest";

    [Header("�f�o�b�O�ݒ�")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceExecute = false; // �f�o�b�O�p�F�������s

    [Header("�t���O�Ǘ��ݒ�")]
    [Tooltip("hasChanged��true�̏ꍇ�̂�AfterChangeToHerMemory�t���O��ݒ肷�邩")]
    [SerializeField] private bool onlySetFlagWhenChanged = true;

    [Tooltip("�蓮�Ńt���O�����Z�b�g����i�f�o�b�O�p�j")]
    [SerializeField] private bool manualResetFlag = false;

    [Header("�Z�[�u�f�[�^���ؐݒ�")]
    [Tooltip("�Z�[�u�f�[�^�����݂��Ȃ��ꍇ��hasChanged��false�ɂ��邩")]
    [SerializeField] private bool requireSaveDataForChange = true;

    [Header("�J�ڌ�̎������s�ݒ�")]
    [Tooltip("DaughterRequestScene����J�ڂ����ꍇ�Ɏ������s���邩�ǂ���")]
    [SerializeField] private bool autoExecuteOnTransition = true;

    [Header("�{�^������ݒ�")]
    [Tooltip("�^�C�g���ύX����MenuContainer�̃{�^���𖳌������邩")]
    [SerializeField] private bool disableButtonsDuringChange = true;

    [Tooltip("MenuContainer�ւ̎Q�Ɓi���ݒ�̏ꍇ�͎��������j")]
    [SerializeField] private GameObject menuContainer;

    [Tooltip("�J�ڃt���O��ێ����鎞�ԁi�b�j")]
    [SerializeField] private float transitionFlagDuration = 1.0f;

    // �v���C�x�[�g�ϐ�
    private AudioSource audioSource;
    private string currentText;
    private bool isChanging = false;
    private bool hasChanged = false;
    private static bool shouldExecuteOnNextLoad = false;
    private float transitionFlagTimer = 0f;
    private bool soundEnabled = true;

    // �O���b�`�p�̕���
    private readonly string glitchChars = "!#$%&'()*+,-./0123456789:;<=>?@[]^_`{|}~";

    private void Awake()
    {
        // TextMeshPro�R���|�[�l���g�̎擾
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
            Debug.LogError("TitleTextChanger: TextMeshPro�R���|�[�l���g��������܂���B");
            enabled = false;
            return;
        }

        // AudioSource�̎擾�܂��͍쐬
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (changeSound != null || completeSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // ���݂̃e�L�X�g��ۑ�
        currentText = titleText.text;
        if (string.IsNullOrEmpty(originalTitleText))
        {
            originalTitleText = currentText;
        }

        // �J�ڃt���O�̃`�F�b�N
        if (shouldExecuteOnNextLoad)
        {
            transitionFlagTimer = transitionFlagDuration;
            if (debugMode)
                Debug.Log("TitleTextChanger: �J�ڃt���O���ݒ肳��Ă��܂��B");
        }
    }

    private void Start()
    {
        // afterChangeToHerMemory�t���O�̏�Ԃ��m�F
        bool afterChangeFlag = GetAfterChangeToHerMemoryFlag();

        if (debugMode)
        {
            Debug.Log($"TitleTextChanger: Start����afterChangeToHerMemory�t���O = {afterChangeFlag}");
        }

        // �t���O��true�̏ꍇ�͉������Ȃ��i���ɕύX�ς݁j
        if (afterChangeFlag)
        {
            if (debugMode)
                Debug.Log("TitleTextChanger: ���Ƀe�L�X�g�ύX�ς݂̂��߁A�������X�L�b�v���܂�");
            return;
        }

        // �J�ڃt���O���`�F�b�N
        if (shouldExecuteOnNextLoad)
        {
            shouldExecuteOnNextLoad = false; // �t���O�����Z�b�g

            if (debugMode)
                Debug.Log("TitleTextChanger: DaughterRequestScene����̑J�ڂ����o�B�e�L�X�g�ύX���J�n���܂��B");

            StartCoroutine(StartTextChange());
        }
    }

    /// <summary>
    /// ����TitleScene�ǂݍ��ݎ��Ƀe�L�X�g�ύX�����s����t���O��ݒ�
    /// </summary>
    public static void SetExecuteOnNextLoad()
    {
        shouldExecuteOnNextLoad = true;
        Debug.Log("TitleTextChanger: ����ǂݍ��ݎ��̎��s�t���O��ݒ肵�܂����B");
    }

    /// <summary>
    /// �O�����狭���I�Ƀe�L�X�g�ύX���J�n
    /// </summary>
    public void ForceStartTextChange()
    {
        if (!isChanging && !hasChanged)
        {
            // afterChangeFlag�̊m�F
            bool afterChangeFlag = GetAfterChangeToHerMemoryFlag();
            if (!afterChangeFlag)
            {
                if (debugMode)
                    Debug.Log("TitleTextChanger: afterChangeToHerMemory��false�̂��߁A�e�L�X�g�ύX���X�L�b�v���܂�");
                return;
            }

            // �Z�[�u�f�[�^�̑��݃`�F�b�N
            bool saveDataExists = CheckSaveDataExists();
            if (requireSaveDataForChange && !saveDataExists)
            {
                if (debugMode)
                    Debug.Log("TitleTextChanger: �Z�[�u�f�[�^�����݂��Ȃ����߁AhasChanged��false�ɐݒ肵�܂��B");
                hasChanged = false;
                return;
            }

            if (debugMode)
                Debug.Log("TitleTextChanger: �����I�Ƀe�L�X�g�ύX���J�n���܂��B");

            StartCoroutine(StartTextChange());
        }
    }

    // Update���\�b�h��ǉ�
    private void Update()
    {
        // �J�ڃt���O�̃^�C�}�[����
        if (transitionFlagTimer > 0)
        {
            transitionFlagTimer -= Time.deltaTime;
            if (transitionFlagTimer <= 0)
            {
                shouldExecuteOnNextLoad = false;
                if (debugMode)
                    Debug.Log("TitleTextChanger: �J�ڃt���O���^�C���A�E�g���܂����B");
            }
        }
    }

    /// <summary>
    /// �Z�[�u�f�[�^�̑��݂��`�F�b�N�i�V�K�ǉ��j
    /// </summary>
    private bool CheckSaveDataExists()
    {
        try
        {
            // GameSaveManager ����Z�[�u�f�[�^�̑��݂��m�F
            if (GameSaveManager.Instance != null)
            {
                bool exists = GameSaveManager.Instance.SaveDataExists();
                if (debugMode)
                    Debug.Log($"TitleTextChanger: GameSaveManager �ł̃Z�[�u�f�[�^���݊m�F: {exists}");
                return exists;
            }

            if (debugMode)
                Debug.Log($"TitleTextChanger: GameSaveManager�����݂��Ȃ����߁A�Z�[�u�f�[�^�Ȃ��Ɣ���");

            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"TitleTextChanger: �Z�[�u�f�[�^���݃`�F�b�N���ɃG���[: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// �e�L�X�g�ύX���J�n����R���[�`��
    /// </summary>
    private IEnumerator StartTextChange()
    {
        // �ύX�J�n���Ƀ{�^���𖳌���
        SetMenuButtonsInteractable(false);

        // �J�n�x��
        if (startDelay > 0)
        {
            yield return new WaitForSeconds(startDelay);
        }

        isChanging = true;

        // ����������ύX
        for (int i = 0; i < newTitleText.Length; i++)
        {
            if (i < currentText.Length)
            {
                // �����̕�����u������
                yield return StartCoroutine(ChangeCharacterAt(i, newTitleText[i]));
            }
            else
            {
                // �V����������ǉ�
                currentText += newTitleText[i];
                titleText.text = currentText;
                PlayChangeSound();
            }

            yield return new WaitForSeconds(changeInterval);
        }

        // ���̃e�L�X�g�̕��������ꍇ�A�]���ȕ������폜
        if (currentText.Length > newTitleText.Length)
        {
            currentText = newTitleText;
            titleText.text = currentText;
        }

        // �ύX�������Ƀ{�^����L����
        SetMenuButtonsInteractable(true);

        // ��������
        OnChangeComplete();
    }

    /// <summary>
    /// �w��ʒu�̕�����ύX����R���[�`��
    /// </summary>
    private IEnumerator ChangeCharacterAt(int index, char newChar)
    {
        if (useGlitchEffect)
        {
            // �O���b�`�G�t�F�N�g
            float elapsedTime = 0;
            while (elapsedTime < glitchDuration)
            {
                // �����_���ȕ����Ɉꎞ�I�ɕύX
                char[] chars = currentText.ToCharArray();
                chars[index] = glitchChars[Random.Range(0, glitchChars.Length)];
                titleText.text = new string(chars);

                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        // �ŏI�I�ȕ����ɕύX
        char[] finalChars = currentText.ToCharArray();
        finalChars[index] = newChar;
        currentText = new string(finalChars);
        titleText.text = currentText;

        // ���ʉ��Đ�
        PlayChangeSound();
    }

    /// <summary>
    /// �����ύX���̌��ʉ����Đ�
    /// </summary>
    private void PlayChangeSound()
    {
        // ���ʉ��������̏ꍇ�͍Đ����Ȃ�
        if (!soundEnabled) return;

        if (changeSound != null)
        {
            // SoundEffectManager��D��g�p
            if (SoundEffectManager.Instance != null)
            {
                SoundEffectManager.Instance.PlaySound(changeSound);
            }
            else if (audioSource != null)
            {
                audioSource.PlayOneShot(changeSound);
            }
        }
    }

    /// <summary>
    /// �ύX�������̏���
    /// </summary>
    private void OnChangeComplete()
    {
        isChanging = false;
        hasChanged = true;

        // ���ʉ����L���ȏꍇ�̂݊��������Đ�
        if (soundEnabled && completeSound != null)
        {
            if (SoundEffectManager.Instance != null)
            {
                SoundEffectManager.Instance.PlaySound(completeSound);
            }
            else if (audioSource != null)
            {
                audioSource.PlayOneShot(completeSound);
            }
        }

        if (debugMode)
        {
            Debug.Log("TitleTextChanger: �e�L�X�g�ύX���������܂����B");
        }

        // GameSaveManager��afterChangeToHerMemory�t���O��ݒ�
        SetAfterChangeToHerMemoryFlag();
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
            Debug.LogWarning($"{GetType().Name}: MenuContainer��������܂���");
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

    private void SetAfterChangeToHerMemoryFlag()
    {
        try
        {
            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.SetAfterChangeToHerMemoryFlag(true);

                if (debugMode)
                    Debug.Log("TitleTextChanger: AfterChangeToHerMemory�t���O��true�ɐݒ肵�܂���");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"TitleTextChanger: �t���O�ݒ蒆�ɃG���[: {ex.Message}");
        }
    }


    /// <summary>
    /// �^�C�g����"�u�ޏ��v�̋L��"�ɕύX������GameSaveManager�ɒʒm
    /// </summary>
    private void NotifyTitleChangeCompleted()
    {
        try
        {
            if (GameSaveManager.Instance != null)
            {
                // hasChanged�Ɋ֌W�Ȃ��A�ύX������������afterChangeToHerMemory�t���O��true�ɐݒ�
                GameSaveManager.Instance.SetAfterChangeToHerMemoryFlag(true);

                if (debugMode)
                    Debug.Log("TitleTextChanger: AfterChangeToHerMemory�t���O��true�ɐݒ肵�܂���");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"TitleTextChanger: �t���O�ݒ蒆�ɃG���[: {ex.Message}");
        }
    }



    /// <summary>
    /// �e�L�X�g�����ɖ߂��i�f�o�b�O�p�j
    /// </summary>
    public void ResetText()
    {
        if (!isChanging)
        {
            titleText.text = originalTitleText;
            currentText = originalTitleText;
            hasChanged = false;

            if (debugMode)
            {
                Debug.Log("TitleTextChanger: �e�L�X�g�����Z�b�g���܂����B");
            }
        }
    }

    /// <summary>
    /// �ύX�𑦍��Ɋ���������i�f�o�b�O�p�j
    /// </summary>
    public void CompleteImmediately()
    {
        StopAllCoroutines();
        titleText.text = newTitleText;
        currentText = newTitleText;
        OnChangeComplete();
    }

    /// <summary>
    /// �����ύX�������������ǂ������擾����
    /// </summary>
    public bool HasChanged
    {
        get
        {
            // afterChangeToHerMemory�t���O��D�悵�ĕԂ�
            if (GameSaveManager.Instance != null)
            {
                bool afterChangeFlag = GameSaveManager.Instance.GetAfterChangeToHerMemoryFlag();
                if (afterChangeFlag)
                {
                    return true;
                }
            }

            // �Z�[�u�f�[�^���݃`�F�b�N���L���ŁA�Z�[�u�f�[�^�����݂��Ȃ��ꍇ��false
            if (requireSaveDataForChange && !CheckSaveDataExists())
            {
                if (debugMode)
                    Debug.Log("TitleTextChanger: �Z�[�u�f�[�^�����݂��Ȃ����߁AHasChanged = false ��Ԃ��܂��B");
                return false;
            }

            return hasChanged;
        }
    }

    /// <summary>
    /// ���ݕύX�����ǂ������擾����
    /// </summary>
    public bool IsChanging => isChanging;

    /// <summary>
    /// AfterChangeToHerMemory�t���O�̏�Ԃ��擾�i�O������m�F�p�j
    /// </summary>
    public bool GetAfterChangeToHerMemoryFlag()
    {
        if (GameSaveManager.Instance != null)
        {
            return GameSaveManager.Instance.GetAfterChangeToHerMemoryFlag();
        }
        return false;
    }


    /// <summary>
    /// �e�X�g�p�F�^�C�g���֘A�̃t���O�����ׂď�����
    /// </summary>
    [ContextMenu("Debug: Reset Title Flags")]
    public void ResetAllTitleFlags()
    {
        // �e�L�X�g�����ɖ߂�
        titleText.text = originalTitleText;
        currentText = originalTitleText;
        hasChanged = false;
        isChanging = false;

        // GameSaveManager�̃t���O�����Z�b�g
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.SetAfterChangeToHerMemoryFlag(false);
        }

        Debug.Log("TitleTextChanger: �^�C�g���֘A�t���O�����������܂���");
    }

    /// <summary>
    /// �ύX��̃e�L�X�g���擾�iTitleTextLoader����Q�Ɨp�j
    /// </summary>
    public string NewTitleText => newTitleText;

    /// <summary>
    /// ���̃e�L�X�g���擾�iTitleTextLoader����Q�Ɨp�j
    /// </summary>
    public string OriginalTitleText => originalTitleText;

    /// <summary>
    /// ���ʉ��̗L��/������ݒ�
    /// </summary>
    /// <param name="enabled">�L���ɂ���ꍇ��true</param>
    public void SetSoundEnabled(bool enabled)
    {
        soundEnabled = enabled;
        if (debugMode) Debug.Log($"TitleTextChanger: ���ʉ���{(enabled ? "�L��" : "����")}�ɂ��܂���");
    }
}