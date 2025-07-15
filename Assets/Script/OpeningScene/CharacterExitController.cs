using UnityEngine;
using OpeningScene;

/// <summary>
/// �L�����N�^�[�ޏ�𐧌䂷�邽�߂̃R���|�[�l���g
/// dialogueTextFile���� "exit" �R�}���h�����o�����Ƃ��ɃL�����N�^�[���\���ɂ��܂�
/// </summary>
public class CharacterExitController : MonoBehaviour
{
    [Header("����Ώ�")]
    [SerializeField] private GameObject leftCharacter;
    [SerializeField] private float fadeOutDuration = 1.0f;
    [SerializeField] private bool useAnimation = true;

    [Header("�f�o�b�O")]
    [SerializeField] private bool debugMode = false;

    private void Start()
    {
        // �K�v�ȃI�u�W�F�N�g�̎擾
        if (leftCharacter == null)
        {
            leftCharacter = GameObject.Find("LeftCharacter");
            if (leftCharacter == null)
            {
                Debug.LogWarning("CharacterExitController: LeftCharacter��������܂���B");
            }
        }

        // �C�x���g���X�i�[��o�^
        RegisterEventListeners();
    }

    private void RegisterEventListeners()
    {
        // �_�C�A���O�\���C�x���g�����b�X��
        DialogueEventNotifier.OnDialogueDisplayed += OnDialogueDisplayed;
    }

    /// <summary>
    /// �_�C�A���O���\�����ꂽ�Ƃ��̃C�x���g�n���h��
    /// </summary>
    private void OnDialogueDisplayed(DialogueEntry entry)
    {
        // �R�}���h�ł���Aexit�R�}���h�̏ꍇ��������
        if (entry.isCommand && entry.commandParam == "exit")
        {
            if (debugMode)
            {
                Debug.Log("CharacterExitController: exit�R�}���h�����o���܂����BLeftCharacter���\���ɂ��܂��B");
            }

            // �L�����N�^�[���\��
            HideLeftCharacter();
        }
    }

    /// <summary>
    /// LeftCharacter���\���ɂ���
    /// </summary>
    private void HideLeftCharacter()
    {
        if (leftCharacter == null)
            return;

        if (useAnimation && fadeOutDuration > 0)
        {
            // �t�F�[�h�A�E�g�A�j���[�V�������g�p
            StartCoroutine(FadeOutCharacter());
        }
        else
        {
            // ������\��
            leftCharacter.SetActive(false);
        }
    }

    /// <summary>
    /// �L�����N�^�[���t�F�[�h�A�E�g������
    /// </summary>
    private System.Collections.IEnumerator FadeOutCharacter()
    {
        CanvasGroup canvasGroup = leftCharacter.GetComponent<CanvasGroup>();

        // CanvasGroup���Ȃ��ꍇ�͒ǉ�
        if (canvasGroup == null)
        {
            canvasGroup = leftCharacter.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1.0f;
        }

        float startTime = Time.time;
        float startAlpha = canvasGroup.alpha;

        // �t�F�[�h�A�E�g
        while (Time.time < startTime + fadeOutDuration)
        {
            float elapsedTime = Time.time - startTime;
            float normalizedTime = elapsedTime / fadeOutDuration;

            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, normalizedTime);
            yield return null;
        }

        // �ŏI�I�ɓ��������m����
        canvasGroup.alpha = 0f;

        // �����������������\��
        leftCharacter.SetActive(false);

        if (debugMode)
        {
            Debug.Log("CharacterExitController: �L�����N�^�[���t�F�[�h�A�E�g���܂���");
        }
    }

    /// <summary>
    /// �J���Ҍ���: �蓮�ŃL�����N�^�[��ޏꂳ����
    /// </summary>
    public void ExitCharacterManually()
    {
        HideLeftCharacter();
    }

    private void OnDestroy()
    {
        // �C�x���g���X�i�[�̓o�^����
        DialogueEventNotifier.OnDialogueDisplayed -= OnDialogueDisplayed;
    }
}