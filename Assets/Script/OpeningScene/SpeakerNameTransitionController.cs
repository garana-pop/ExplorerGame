using System.Collections;
using UnityEngine;
using TMPro;
using OpeningScene;

/// <summary>
/// �I�[�v�j���O�V�[���ɂ����Ęb�Җ����ꕶ�����ω�������A�j���[�V�����𐧌䂷��N���X
/// </summary>
public class SpeakerNameTransitionController : MonoBehaviour
{
    [System.Serializable]
    public class SpeakerTransitionSetting
    {
        [Tooltip("�ύX�R�}���h���iSpeakerChange_XXX�́uXXX�v�����j")]
        public string commandKey;

        [Tooltip("�ύX�O�̘b�Җ�")]
        public string beforeName;

        [Tooltip("�ύX��̘b�Җ�")]
        public string afterName;

        [Tooltip("�����L�����N�^�[�̕ύX�Ȃ�true�A�E���Ȃ�false")]
        public bool isLeftCharacter;
    }

    [Header("��{�ݒ�")]
    [SerializeField] private TextMeshProUGUI leftNameText;
    [SerializeField] private TextMeshProUGUI rightNameText;

    [Header("�g�����W�V�����ݒ�")]
    [SerializeField] private float characterChangeInterval = 0.1f; // 1�������\������Ԋu
    [SerializeField] private float pauseBeforeTransition = 0.5f;   // �ύX�J�n�O�̑ҋ@����
    [SerializeField] private Color transitionHighlightColor = new Color(1f, 0.8f, 0.4f); // �ύX���̃n�C���C�g�F
    [SerializeField] private AudioClip typeSound; // �����ύX���̌��ʉ�

    [Header("�b�ҕύX�ݒ�")]
    [SerializeField]
    private SpeakerTransitionSetting[] transitionSettings = new SpeakerTransitionSetting[]
    {
        new SpeakerTransitionSetting {
            commandKey = "father",
            beforeName = "�H�H�H",
            afterName = "���e",
            isLeftCharacter = true
        },
        new SpeakerTransitionSetting {
            commandKey = "amnesiac",
            beforeName = "�j��",
            afterName = "�L���r����",
            isLeftCharacter = false
        }
    };

    [Header("�f�o�b�O")]
    [SerializeField] private bool debugMode = false;

    // �b�Җ��ύX���t���O
    public bool IsTransitioning { get; private set; } = false;

    // �����ϐ�
    private AudioSource audioSource;
    private Color originalLeftNameColor;
    private Color originalRightNameColor;
    private Coroutine leftNameTransition;
    private Coroutine rightNameTransition;

    private void Awake()
    {
        // AudioSource�̎擾
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // �e�L�X�g�R���|�[�l���g�̊m�F
        ValidateComponents();
    }

    private void Start()
    {
        // ���̕����F��ۑ�
        if (leftNameText != null)
            originalLeftNameColor = leftNameText.color;

        if (rightNameText != null)
            originalRightNameColor = rightNameText.color;

        // �C�x���g���X�i�[��o�^
        RegisterEventListeners();
    }

    private void ValidateComponents()
    {
        // �����̖��O�e�L�X�g�̊m�F
        if (leftNameText == null)
        {
            // �V�[��������T��
            GameObject leftCharacter = GameObject.Find("LeftCharacter");
            if (leftCharacter != null)
            {
                Transform nameArea = leftCharacter.transform.Find("LeftNameArea");
                if (nameArea != null)
                {
                    leftNameText = nameArea.GetComponentInChildren<TextMeshProUGUI>();
                }
            }

            if (leftNameText == null)
                Debug.LogWarning("�����L�����N�^�[�̖��O�e�L�X�g(TextMeshProUGUI)���ݒ肳��Ă��܂���B");
        }

        // �E���̖��O�e�L�X�g�̊m�F
        if (rightNameText == null)
        {
            // �V�[��������T��
            GameObject rightCharacter = GameObject.Find("RightCharacter");
            if (rightCharacter != null)
            {
                Transform nameArea = rightCharacter.transform.Find("RightNameArea");
                if (nameArea != null)
                {
                    rightNameText = nameArea.GetComponentInChildren<TextMeshProUGUI>();
                }
            }

            if (rightNameText == null)
                Debug.LogWarning("�E���L�����N�^�[�̖��O�e�L�X�g(TextMeshProUGUI)���ݒ肳��Ă��܂���B");
        }
    }

    private void RegisterEventListeners()
    {
        // �_�C�A���O�\���C�x���g�Ƀ��X�i�[��o�^
        DialogueEventNotifier.OnDialogueDisplayed += OnDialogueDisplayed;
    }

    /// <summary>
    /// �_�C�A���O���\�����ꂽ�Ƃ��̃C�x���g�n���h��
    /// </summary>
    private void OnDialogueDisplayed(DialogueEntry entry)
    {
        // �R�}���h�s�̏ꍇ�͒��ڏ���
        if (entry.isCommand && entry.type == DialogueType.Command)
        {
            ProcessSpeakerChangeCommand(entry.commandParam);
            return;
        }

        // �R�}���h�`�����ǂ������`�F�b�N�i����݊����̂��߁j
        if (entry.dialogue.StartsWith("SpeakerChange_"))
        {
            CheckForSpeakerChangeCommand(entry.dialogue);
        }
    }

    /// <summary>
    /// �b�ҕύX�R�}���h�𒼐ڏ���
    /// </summary>
    private void ProcessSpeakerChangeCommand(string commandKey)
    {
        if (string.IsNullOrEmpty(commandKey))
            return;

        foreach (var setting in transitionSettings)
        {
            if (setting.commandKey == commandKey)
            {
                StartNameTransition(setting);

                if (debugMode)
                    Debug.Log($"�b�Җ��ύX�R�}���h����: {commandKey} ({setting.beforeName} �� {setting.afterName})");

                break;
            }
        }
    }

    /// <summary>
    /// ����R�}���h�iSpeakerChange_XXX�j�̌��o�Ə����i����݊����p�j
    /// </summary>
    private void CheckForSpeakerChangeCommand(string dialogue)
    {
        const string commandPrefix = "SpeakerChange_";

        // �R�}���h�`���`�F�b�N
        if (!dialogue.Contains(commandPrefix))
            return;

        // �R�}���h�L�[�𒊏o
        string commandText = dialogue.Trim();
        if (!commandText.StartsWith(commandPrefix))
            return;

        string commandKey = commandText.Substring(commandPrefix.Length).Trim();
        ProcessSpeakerChangeCommand(commandKey);
    }

    /// <summary>
    /// �b�Җ��̕ύX�A�j���[�V�������J�n
    /// </summary>
    private void StartNameTransition(SpeakerTransitionSetting setting)
    {
        // ���E�ǂ���̃L�����N�^�[���ŏ����𕪊�
        if (setting.isLeftCharacter)
        {
            // ���Ɏ��s���̃R���[�`��������Β�~
            if (leftNameTransition != null)
                StopCoroutine(leftNameTransition);

            // �����L�����N�^�[�̖��O��ύX
            if (leftNameText != null)
            {
                leftNameTransition = StartCoroutine(AnimateNameChange(
                    leftNameText,
                    setting.beforeName,
                    setting.afterName,
                    originalLeftNameColor));
            }
        }
        else
        {
            // ���Ɏ��s���̃R���[�`��������Β�~
            if (rightNameTransition != null)
                StopCoroutine(rightNameTransition);

            // �E���L�����N�^�[�̖��O��ύX
            if (rightNameText != null)
            {
                rightNameTransition = StartCoroutine(AnimateNameChange(
                    rightNameText,
                    setting.beforeName,
                    setting.afterName,
                    originalRightNameColor));
            }
        }
    }

    /// <summary>
    /// ���O���A�j���[�V�����ňꕶ�����ω�������R���[�`��
    /// </summary>
    private IEnumerator AnimateNameChange(TextMeshProUGUI textComponent, string fromName, string toName, Color originalColor)
    {
        // �b�Җ��ύX���t���O�𗧂Ă�
        IsTransitioning = true;

        // �ύX�J�n�O�̑ҋ@
        yield return new WaitForSeconds(pauseBeforeTransition);

        // �n�C���C�g�F�ɕύX
        Color originalTextColor = textComponent.color;
        textComponent.color = transitionHighlightColor;

        // �܂����̖��O���ꕶ��������
        string currentText = fromName;
        while (currentText.Length > 0)
        {
            currentText = currentText.Substring(0, currentText.Length - 1);
            textComponent.text = currentText;

            PlayTypeSound();
            yield return new WaitForSeconds(characterChangeInterval);
        }

        // �����ҋ@
        yield return new WaitForSeconds(characterChangeInterval * 2);

        // �V�������O���ꕶ�����\��
        currentText = "";
        for (int i = 0; i < toName.Length; i++)
        {
            currentText += toName[i];
            textComponent.text = currentText;

            PlayTypeSound();
            yield return new WaitForSeconds(characterChangeInterval);
        }

        // ���̐F�ɖ߂�
        textComponent.color = originalTextColor;

        if (debugMode)
            Debug.Log($"���O�ύX����: {fromName} �� {toName}");

        // �b�Җ��ύX���t���O�����낷
        IsTransitioning = false;
    }

    /// <summary>
    /// �^�C�v�����Đ�
    /// </summary>
    private void PlayTypeSound()
    {
        if (audioSource != null && typeSound != null)
        {
            audioSource.PlayOneShot(typeSound, 0.5f);
        }
    }

    /// <summary>
    /// �蓮�Řb�Җ��ύX���g���K�[����p�u���b�N���\�b�h�i�f�o�b�O�p�j
    /// </summary>
    public void TriggerNameChange(string commandKey)
    {
        ProcessSpeakerChangeCommand(commandKey);
    }

    private void OnDestroy()
    {
        // �b�Җ��ύX���t���O�����Z�b�g
        IsTransitioning = false;

        // �C�x���g���X�i�[�̓o�^����
        DialogueEventNotifier.OnDialogueDisplayed -= OnDialogueDisplayed;

        // �A�N�e�B�u�ȃR���[�`�����~
        if (leftNameTransition != null)
            StopCoroutine(leftNameTransition);

        if (rightNameTransition != null)
            StopCoroutine(rightNameTransition);
    }
}