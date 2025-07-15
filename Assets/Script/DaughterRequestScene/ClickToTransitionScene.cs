using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TextTyper������ɃN���b�N�ŃV�[���J�ڂ��s���R���|�[�l���g
/// </summary>
public class ClickToTransitionScene : MonoBehaviour, IPointerClickHandler
{
    [Header("�V�[���J�ڐݒ�")]
    [Tooltip("�J�ڐ�̃V�[����")]
    [SerializeField] private string targetSceneName = "TitleScene";

    [Header("�Q�Ɛݒ�")]
    [Tooltip("�Ď�����TextTyper�R���|�[�l���g�i���ݒ�̏ꍇ�͎��������j")]
    [SerializeField] private TextTyper textTyper;

    [Header("�N���b�N�ҋ@UI�ݒ�")]
    [Tooltip("�^�C�s���O������ɕ\������N���b�N�ҋ@UI")]
    [SerializeField] private GameObject clickPromptUI;

    [Tooltip("�N���b�N�ҋ@���ɕ\������e�L�X�g")]
    [SerializeField] private string promptText = "��ʂ��N���b�N���đ��s...";

    [Tooltip("�v�����v�g�e�L�X�g��\������TextMeshPro�R���|�[�l���g")]
    [SerializeField] private TMP_Text promptTextComponent;

    [Header("���ʉ��ݒ�")]
    [Tooltip("�N���b�N���Ɍ��ʉ����Đ����邩")]
    [SerializeField] private bool playClickSound = true;

    [Tooltip("�J�X�^�����ʉ��i���ݒ莞�̓f�t�H���g�����g�p�j")]
    [SerializeField] private AudioClip customClickSound;

    [Header("�x���ݒ�")]
    [Tooltip("�^�C�s���O��������N���b�N��t�J�n�܂ł̒x�����ԁi�b�j")]
    [SerializeField] private float clickDelayAfterTyping = 0.5f;

    [Header("�f�o�b�O�ݒ�")]
    [Tooltip("�f�o�b�O���O��\�����邩")]
    [SerializeField] private bool debugMode = false;

    [Header("����J�ڐݒ�")]
    [Tooltip("DaughterRequestScene����TitleScene�ւ̑J�ڎ���TitleTextChanger�����s���邩")]
    [SerializeField] private bool triggerTitleTextChange = true;

    // �������
    private bool isTypingCompleted = false;
    private bool canClick = false;
    private AudioSource audioSource;

    private void Awake()
    {
        // TextTyper�R���|�[�l���g�̎�������
        if (textTyper == null)
        {
            textTyper = FindFirstObjectByType<TextTyper>();
            if (textTyper == null)
            {
                textTyper = GetComponent<TextTyper>();
            }
        }

        // AudioSource�R���|�[�l���g�̎擾/�ǉ�
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (playClickSound || customClickSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // �v�����v�gUI�̏�����Ԑݒ�
        if (clickPromptUI != null)
        {
            clickPromptUI.SetActive(false);
        }

        // �v�����v�g�e�L�X�g�̐ݒ�
        if (promptTextComponent != null && !string.IsNullOrEmpty(promptText))
        {
            promptTextComponent.text = promptText;
        }
    }

    private void Start()
    {
        // TextTyper��������Ȃ��ꍇ�̌x��
        if (textTyper == null)
        {
            Debug.LogError("ClickToTransitionScene: TextTyper�R���|�[�l���g��������܂���B�C���X�y�N�^�[�Őݒ肷�邩�A����GameObject�ɃA�^�b�`���Ă��������B");
            enabled = false;
            return;
        }

        // �J�ڐ�V�[�����̌���
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("ClickToTransitionScene: �J�ڐ�V�[�������ݒ肳��Ă��܂���B");
        }

        // TextTyper�̊����C�x���g�ɓo�^
        textTyper.OnTypingCompleted += OnTypingCompleted;

        if (debugMode)
        {
            Debug.Log("ClickToTransitionScene: �����������BTextTyper�̊�����ҋ@��...");
        }
    }

    private void OnDestroy()
    {
        // �C�x���g���X�i�[�̉���
        if (textTyper != null)
        {
            textTyper.OnTypingCompleted -= OnTypingCompleted;
        }
    }

    /// <summary>
    /// TextTyper�������ɌĂ΂��R�[���o�b�N
    /// </summary>
    private void OnTypingCompleted()
    {
        if (debugMode)
        {
            Debug.Log("ClickToTransitionScene: �^�C�s���O���������m���܂����B");
        }

        isTypingCompleted = true;

        // �x����ɃN���b�N��t���J�n
        StartCoroutine(EnableClickAfterDelay());
    }

    /// <summary>
    /// �x����ɃN���b�N��t��L���ɂ���R���[�`��
    /// </summary>
    private IEnumerator EnableClickAfterDelay()
    {
        if (clickDelayAfterTyping > 0)
        {
            yield return new WaitForSeconds(clickDelayAfterTyping);
        }

        canClick = true;

        // �N���b�N�ҋ@UI��\��
        if (clickPromptUI != null)
        {
            clickPromptUI.SetActive(true);
        }

        if (debugMode)
        {
            Debug.Log("ClickToTransitionScene: �N���b�N��t�J�n�B��ʃN���b�N�ŃV�[���J�ڂ��܂��B");
        }
    }

    /// <summary>
    /// �N���b�N���m����
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!canClick || !isTypingCompleted)
        {
            if (debugMode)
            {
                Debug.Log("ClickToTransitionScene: �܂��N���b�N��t���Ă��܂���B");
            }
            return;
        }

        if (debugMode)
        {
            Debug.Log("ClickToTransitionScene: �N���b�N�����m�B�V�[���J�ڂ��J�n���܂��B");
        }

        // �d���N���b�N�h�~
        canClick = false;

        // �N���b�N�����Đ�
        if (playClickSound)
        {
            PlayClickSound();
        }

        // �v�����v�gUI���\��
        if (clickPromptUI != null)
        {
            clickPromptUI.SetActive(false);
        }

        // �V�[���J�ڂ����s
        TransitionToScene();
    }

    /// <summary>
    /// �N���b�N�����Đ�
    /// </summary>
    private void PlayClickSound()
    {
        // SoundEffectManager��D��g�p
        if (SoundEffectManager.Instance != null)
        {
            if (customClickSound != null)
            {
                SoundEffectManager.Instance.PlaySound(customClickSound);
            }
            else
            {
                SoundEffectManager.Instance.PlayClickSound();
            }
        }
        else if (customClickSound != null && audioSource != null)
        {
            // SoundEffectManager���Ȃ��ꍇ�͒��ڍĐ�
            audioSource.PlayOneShot(customClickSound);
        }
    }

    /// <summary>
    /// �V�[���J�ڂ����s
    /// </summary>
    private void TransitionToScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("ClickToTransitionScene: �J�ڐ�V�[�������ݒ肳��Ă��܂���B");
            return;
        }

        // DaughterRequestScene����TitleScene�ւ̑J�ڂ̏ꍇ�̂ݏ���
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == "DaughterRequest" && targetSceneName == "TitleScene")
        {
            // �J�ڃt���O��ݒ�
            TitleTextChanger.SetExecuteOnNextLoad();

            if (debugMode)
            {
                Debug.Log("ClickToTransitionScene: DaughterRequest����TitleScene�ւ̑J�ڃt���O��ݒ肵�܂����B");
            }
        }

        Debug.Log(targetSceneName + "�Ɉڍs���܂��B");
        LoadSceneDirectly();
    }

    /// <summary>
    /// ���ڃV�[���J��
    /// </summary>
    private void LoadSceneDirectly()
    {
        try
        {
            // �Z�[�u�f�[�^�̕ۑ�
            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.SaveGame();
            }

            // �V�[���J��
            SceneManager.LoadScene(targetSceneName);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ClickToTransitionScene: �V�[���J�ڒ��ɃG���[���������܂���: {ex.Message}");
        }
    }

    /// <summary>
    /// �O������N���b�N��t��Ԃ������ݒ�i�f�o�b�O�p�j
    /// </summary>
    public void ForceEnableClick()
    {
        isTypingCompleted = true;
        canClick = true;

        if (clickPromptUI != null)
        {
            clickPromptUI.SetActive(true);
        }

        if (debugMode)
        {
            Debug.Log("ClickToTransitionScene: �N���b�N��t�������L�������܂����B");
        }
    }

    /// <summary>
    /// ���݂̃N���b�N��t��Ԃ��擾
    /// </summary>
    public bool CanClick => canClick && isTypingCompleted;

    /// <summary>
    /// �J�ڐ�V�[�����𓮓I�ɕύX
    /// </summary>
    public void SetTargetScene(string sceneName)
    {
        targetSceneName = sceneName;
        if (debugMode)
        {
            Debug.Log($"ClickToTransitionScene: �J�ڐ�V�[���� '{sceneName}' �ɕύX���܂����B");
        }
    }
}