using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpeningScene;

/// <summary>
/// OpeningSceneController�ƃL�����N�^�[�\����A�g������g���N���X
/// </summary>
public class OpeningSceneManagerExtension : MonoBehaviour
{
    [Header("�L�����N�^�[����")]
    [SerializeField] private CharacterDisplayController characterController;

    [Header("�L�����N�^�[��GameObject")]
    [SerializeField] private GameObject leftCharacter;
    [SerializeField] private GameObject rightCharacter;

    [Header("�L�����N�^�[�ݒ�")]
    [SerializeField] private string leftCharacterName = "���e";    // �����L�����N�^�[��
    [SerializeField] private string rightCharacterName = "��"; // �E���L�����N�^�[��

    [Header("�f�o�b�O")]
    [SerializeField] private bool debugMode = false;

    private void Awake()
    {
        // CharacterDisplayController���ݒ肳��Ă��Ȃ��ꍇ�͎����擾
        if (characterController == null)
        {
            characterController = GetComponent<CharacterDisplayController>();

            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterDisplayController>();
                Debug.Log("CharacterDisplayController��AddComponent�Œǉ����܂���");
            }
        }
    }

    private void Start()
    {
        // �C�x���g���X�i�[��o�^
        DialogueEventNotifier.OnDialogueDisplayed += OnDialogueDisplayed;

        // �L�����N�^�[�̏����ݒ�
        SetupCharacters();
    }

    /// <summary>
    /// �L�����N�^�[�̏����ݒ�
    /// </summary>
    private void SetupCharacters()
    {
        if (characterController == null) return;

        // �����L�����N�^�[�̐ݒ�
        if (leftCharacter != null)
        {
            // �ʏ�̍��L�����N�^�[����o�^
            RegisterCharacter(leftCharacter, leftCharacterName);

            // "???"�����L�����N�^�[(���e)�Ƃ��ēo�^
            RegisterCharacter(leftCharacter, "�H�H�H");
        }

        // �E���L�����N�^�[�̐ݒ�
        if (rightCharacter != null)
        {
            // �ʏ�̉E�L�����N�^�[����o�^
            RegisterCharacter(rightCharacter, rightCharacterName);

            // "�L���r����"���E�L�����N�^�[�Ƃ��ēo�^
            RegisterCharacter(rightCharacter, "�L���r����");
        }

        if (debugMode)
        {
            Debug.Log("�L�����N�^�[�ݒ���������܂���");
        }
    }

    /// <summary>
    /// �L�����N�^�[��o�^�i�w���p�[���\�b�h�j
    /// </summary>
    private void RegisterCharacter(GameObject characterObject, string characterName)
    {
        if (characterController == null || characterObject == null) return;

        // ���ɑΉ�����L�����N�^�[���ݒ肳��Ă��Ȃ��ꍇ�̂ݓo�^
        if (!characterController.HasCharacterForSpeaker(characterName))
        {
            // Image �� TextMeshProUGUI �R���|�[�l���g�̎擾
            var characterImage = characterObject.GetComponent<UnityEngine.UI.Image>();

            // ���O�\���pTextMeshPro���擾
            var nameArea = characterObject.transform.Find("LeftNameArea") ??
                           characterObject.transform.Find("RightNameArea");

            TMPro.TextMeshProUGUI nameText = null;
            if (nameArea != null)
            {
                nameText = nameArea.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            }

            // �L�����N�^�[��ǉ�
            characterController.AddCharacter(characterName, characterObject, characterImage, nameText);
        }
    }

    /// <summary>
    /// �_�C�A���O�\�����̃C�x���g�n���h��
    /// </summary>
    private void OnDialogueDisplayed(DialogueEntry entry)
    {
        if (characterController == null || entry == null) return;

        // DialogueType.Narration �̏ꍇ�̓L�����N�^�[���n�C���C�g���Ȃ�
        if (entry.type == DialogueType.Narration)
        {
            characterController.ResetAllCharacters();
            return;
        }

        // �L�����N�^�[�̃n�C���C�g�\�����X�V
        characterController.HighlightCharacter(entry.speaker);

        if (debugMode)
        {
            Debug.Log($"�_�C�A���O�C�x���g: '{entry.speaker}' ���Z���t��b���Ă��܂�: '{entry.dialogue.Substring(0, Mathf.Min(20, entry.dialogue.Length))}...'");
        }
    }

    private void OnDestroy()
    {
        // �C�x���g���X�i�[�̓o�^����
        DialogueEventNotifier.OnDialogueDisplayed -= OnDialogueDisplayed;
    }
}