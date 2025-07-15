using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpeningScene;

/// <summary>
/// OpeningScene�ł̃L�����N�^�[�\���𐧌䂷��N���X
/// �Z���t�ɉ����đΉ�����L�����N�^�[���n�C���C�g�\������
/// </summary>
public class CharacterDisplayController : MonoBehaviour
{
    [System.Serializable]
    public class CharacterSettings
    {
        public string characterName;        // �L�����N�^�[��
        public GameObject characterObject;  // �L�����N�^�[��GameObject
        public Image characterImage;        // �L�����N�^�[�̃C���[�W
        public TextMeshProUGUI nameText;    // ���O�\���p�e�L�X�g
        public Color highlightColor = Color.white;             // �n�C���C�g���̐F
        public Color normalColor = new Color(0.6f, 0.6f, 0.6f, 0.8f); // ��n�C���C�g���̐F
    }

    [System.Serializable]
    public class NameDisplaySettings
    {
        [Header("�e�L�X�g�ݒ�")]
        public Color nameTextColor = Color.white;
        public float nameFontSize = 24f;

        [Header("�w�i�ݒ�")]
        public Color leftNameBgColor = new Color(0.2f, 0.2f, 0.6f, 0.8f);
        public Color rightNameBgColor = new Color(0.6f, 0.2f, 0.2f, 0.8f);

        [Header("���C�A�E�g�ݒ�")]
        public Vector2 namePanelSize = new Vector2(200, 50);
        public Vector2 namePadding = new Vector2(10, 5);
    }

    [Header("�L�����N�^�[�ݒ�")]
    [SerializeField] private List<CharacterSettings> characters = new List<CharacterSettings>();

    [Header("���O�\���ݒ�")]
    [SerializeField] private NameDisplaySettings nameDisplaySettings = new NameDisplaySettings();

    [Header("�f�t�H���g�ݒ�")]
    [SerializeField] private Color defaultHighlightColor = Color.white;
    [SerializeField] private Color defaultNormalColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
    [SerializeField] private float transitionSpeed = 3.0f; // �F�J�ڂ̑���

    [Header("�G�t�F�N�g�ݒ�")]
    [SerializeField] private bool useColorTransition = true; // �F�̑J�ڃG�t�F�N�g���g�p���邩
    [SerializeField] private bool enhanceNameDisplay = true; // ���O�\�����������邩

    [Header("�f�o�b�O")]
    [SerializeField] private bool debugMode = false;

    // ���݃n�C���C�g����Ă���L�����N�^�[
    private CharacterSettings currentHighlightedCharacter;
    private List<Coroutine> activeTransitions = new List<Coroutine>();
    private Dictionary<string, Image> nameBgImages = new Dictionary<string, Image>();

    private void Awake()
    {
        InitializeCharacters();

        // ���O�\���̋������L���ȏꍇ�͏�����
        if (enhanceNameDisplay)
        {
            InitializeNameDisplays();
        }
    }

    private void Start()
    {
        // �J�n���͑S�L�����N�^�[���n�C���C�g��Ԃ�
        ResetAllCharacters();
    }

    /// <summary>
    /// �L�����N�^�[�ݒ�̏�����
    /// </summary>
    private void InitializeCharacters()
    {
        foreach (var character in characters)
        {
            // �摜�R���|�[�l���g�̎擾�m�F
            if (character.characterObject != null && character.characterImage == null)
            {
                character.characterImage = character.characterObject.GetComponent<Image>();
                if (character.characterImage == null)
                {
                    Debug.LogWarning($"�L�����N�^�[ {character.characterName} �� Image �R���|�[�l���g��������܂���");
                }
            }

            // ���O�e�L�X�g�R���|�[�l���g�̎擾�m�F
            if (character.characterObject != null && character.nameText == null)
            {
                character.nameText = character.characterObject.GetComponentInChildren<TextMeshProUGUI>();
                if (character.nameText == null)
                {
                    Debug.LogWarning($"�L�����N�^�[ {character.characterName} �� TextMeshProUGUI �R���|�[�l���g��������܂���");
                }
            }
        }
    }

    /// <summary>
    /// ���O�\���̏�����
    /// </summary>
    private void InitializeNameDisplays()
    {
        foreach (var character in characters)
        {
            if (character.nameText != null)
            {
                // ���O�G���A�̎擾
                Transform nameArea = character.nameText.transform.parent;
                if (nameArea == null) continue;

                // �w�i�C���[�W�̐ݒ�
                Image nameBg = nameArea.GetComponent<Image>();
                if (nameBg == null)
                {
                    nameBg = nameArea.gameObject.AddComponent<Image>();

                    // ���E�̃L�����N�^�[�ɉ������w�i�F��ݒ�
                    if (character.characterName.Contains("��") ||
                        character.characterName.Contains("???"))
                    {
                        nameBg.color = nameDisplaySettings.leftNameBgColor;
                    }
                    else
                    {
                        nameBg.color = nameDisplaySettings.rightNameBgColor;
                    }

                    // �w�i����Ԍ���
                    nameBg.transform.SetAsFirstSibling();
                }

                // �����ɕۑ�
                nameBgImages[character.characterName] = nameBg;

                // ���O�e�L�X�g�̐ݒ�
                ConfigureNameText(character.nameText);

                // ���O�G���A��RectTransform��ݒ�
                ConfigureNameAreaLayout(nameArea.GetComponent<RectTransform>());
            }
        }
    }

    /// <summary>
    /// ���O�e�L�X�g�̐ݒ�
    /// </summary>
    private void ConfigureNameText(TextMeshProUGUI nameText)
    {
        if (nameText == null) return;

        // �t�H���g�ݒ�
        nameText.fontSize = nameDisplaySettings.nameFontSize;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = nameDisplaySettings.nameTextColor;

        // �e�L�X�g�z�u
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode = TextOverflowModes.Overflow;
    }

    /// <summary>
    /// ���O�G���A�̃��C�A�E�g�ݒ�
    /// </summary>
    private void ConfigureNameAreaLayout(RectTransform nameAreaRect)
    {
        if (nameAreaRect == null) return;

        // �T�C�Y�ݒ�
        nameAreaRect.sizeDelta = nameDisplaySettings.namePanelSize;
    }

    /// <summary>
    /// ���ׂẴL�����N�^�[���n�C���C�g��ԂɃ��Z�b�g
    /// </summary>
    public void ResetAllCharacters()
    {
        StopAllTransitions();

        foreach (var character in characters)
        {
            if (character.characterImage != null)
            {
                if (useColorTransition)
                {
                    StartColorTransition(character.characterImage, character.normalColor, transitionSpeed);
                }
                else
                {
                    character.characterImage.color = character.normalColor;
                }
            }

            if (character.nameText != null)
            {
                character.nameText.text = "";

                // ���O�w�i���\��
                if (enhanceNameDisplay && nameBgImages.ContainsKey(character.characterName))
                {
                    Image nameBg = nameBgImages[character.characterName];
                    if (nameBg != null)
                    {
                        Color bgColor = nameBg.color;
                        bgColor.a = 0f;
                        nameBg.color = bgColor;
                    }
                }
            }
        }

        currentHighlightedCharacter = null;
    }

    /// <summary>
    /// �b�Җ�����L�����N�^�[���n�C���C�g�\��
    /// </summary>
    public void HighlightCharacter(string speakerName)
    {
        if (string.IsNullOrEmpty(speakerName))
        {
            // �b�Җ�����̏ꍇ�i�i���[�V�����Ȃǁj�͑S�L�����N�^�[��n�C���C�g
            ResetAllCharacters();
            return;
        }

        // ���ׂĂ̑J�ڃG�t�F�N�g���~
        StopAllTransitions();

        CharacterSettings targetCharacter = null;

        // �}�b�`����L�����N�^�[������
        foreach (var character in characters)
        {
            if (character.characterName.Equals(speakerName, System.StringComparison.OrdinalIgnoreCase) ||
               (speakerName == "???" && character.characterName == "���e") ||  // "???"�����L�����N�^�[(���e)�ɑΉ�
               (speakerName == "�L���r����" && character.characterName == "�j��")) // "�L���r����"���E�L�����N�^�[(�j��)�ɑΉ�
            {
                targetCharacter = character;
                break;
            }
        }

        // �ΏۃL�����N�^�[���Ȃ��ꍇ�͏������Ȃ�
        if (targetCharacter == null)
        {
            if (debugMode)
            {
                Debug.LogWarning($"�b�Җ� '{speakerName}' �Ɉ�v����L�����N�^�[��������܂���");
            }
            return;
        }

        // ���݂̃n�C���C�g�L�����N�^�[�Ɠ����Ȃ珈�����Ȃ�
        if (currentHighlightedCharacter == targetCharacter)
        {
            return;
        }

        // �O��̃n�C���C�g�L�����N�^�[���n�C���C�g��Ԃ�
        if (currentHighlightedCharacter != null)
        {
            if (currentHighlightedCharacter.characterImage != null)
            {
                if (useColorTransition)
                {
                    StartColorTransition(currentHighlightedCharacter.characterImage,
                                         currentHighlightedCharacter.normalColor,
                                         transitionSpeed);
                }
                else
                {
                    currentHighlightedCharacter.characterImage.color = currentHighlightedCharacter.normalColor;
                }
            }

            if (currentHighlightedCharacter.nameText != null)
            {
                // ���O�\��������������\��
                if (enhanceNameDisplay)
                {
                    HideEnhancedNameDisplay(currentHighlightedCharacter);
                }
                else
                {
                    currentHighlightedCharacter.nameText.text = "";
                }
            }
        }

        // �V�����L�����N�^�[���n�C���C�g��Ԃ�
        if (targetCharacter.characterImage != null)
        {
            if (useColorTransition)
            {
                StartColorTransition(targetCharacter.characterImage,
                                     targetCharacter.highlightColor,
                                     transitionSpeed);
            }
            else
            {
                targetCharacter.characterImage.color = targetCharacter.highlightColor;
            }
        }

        if (targetCharacter.nameText != null)
        {
            // ���O�\���̋������L���ȏꍇ
            if (enhanceNameDisplay)
            {
                ShowEnhancedNameDisplay(targetCharacter, speakerName);
            }
            else
            {
                targetCharacter.nameText.text = speakerName;
            }
        }

        currentHighlightedCharacter = targetCharacter;

        if (debugMode)
        {
            Debug.Log($"�L�����N�^�[���n�C���C�g: {speakerName}");
        }
    }

    /// <summary>
    /// �������ꂽ���O�\����\��
    /// </summary>
    private void ShowEnhancedNameDisplay(CharacterSettings character, string speakerName)
    {
        if (character.nameText == null) return;

        // ���O�e�L�X�g��ݒ�
        character.nameText.text = speakerName;

        // �w�i��\��
        if (nameBgImages.ContainsKey(character.characterName))
        {
            Image nameBg = nameBgImages[character.characterName];
            if (nameBg != null)
            {
                Color bgColor = nameBg.color;
                // ���E�̃L�����N�^�[�ɉ������w�i�F��ݒ�
                if (character.characterName.Contains("��") ||
                    character.characterName.Contains("???"))
                {
                    nameBg.color = new Color(
                        nameDisplaySettings.leftNameBgColor.r,
                        nameDisplaySettings.leftNameBgColor.g,
                        nameDisplaySettings.leftNameBgColor.b,
                        0.8f);
                }
                else
                {
                    nameBg.color = new Color(
                        nameDisplaySettings.rightNameBgColor.r,
                        nameDisplaySettings.rightNameBgColor.g,
                        nameDisplaySettings.rightNameBgColor.b,
                        0.8f);
                }
            }
        }
    }

    /// <summary>
    /// �������ꂽ���O�\�����\��
    /// </summary>
    private void HideEnhancedNameDisplay(CharacterSettings character)
    {
        if (character.nameText == null) return;

        // ���O�e�L�X�g���N���A
        character.nameText.text = "";

        // �w�i���\��
        if (nameBgImages.ContainsKey(character.characterName))
        {
            Image nameBg = nameBgImages[character.characterName];
            if (nameBg != null)
            {
                Color bgColor = nameBg.color;
                bgColor.a = 0f;
                nameBg.color = bgColor;
            }
        }
    }

    /// <summary>
    /// �F�̑J�ڃG�t�F�N�g���J�n
    /// </summary>
    private void StartColorTransition(Image image, Color targetColor, float speed)
    {
        if (image == null) return;

        Coroutine transition = StartCoroutine(ColorTransition(image, targetColor, speed));
        activeTransitions.Add(transition);
    }

    /// <summary>
    /// ���ׂĂ̑J�ڃG�t�F�N�g���~
    /// </summary>
    private void StopAllTransitions()
    {
        foreach (var transition in activeTransitions)
        {
            if (transition != null)
            {
                StopCoroutine(transition);
            }
        }

        activeTransitions.Clear();
    }

    /// <summary>
    /// �F�̑J�ڃR���[�`��
    /// </summary>
    private IEnumerator ColorTransition(Image image, Color targetColor, float speed)
    {
        Color startColor = image.color;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * speed;
            image.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        image.color = targetColor;
    }

    /// <summary>
    /// �b�Җ��ɑΉ�����L�����N�^�[���ݒ肳��Ă��邩�m�F
    /// </summary>
    public bool HasCharacterForSpeaker(string speakerName)
    {
        if (string.IsNullOrEmpty(speakerName)) return false;

        foreach (var character in characters)
        {
            if (character.characterName.Equals(speakerName, System.StringComparison.OrdinalIgnoreCase) ||
                (speakerName.Contains("???") && character.characterName.Contains("???")))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// �L�����N�^�[�𖼑O�Œǉ�
    /// </summary>
    public void AddCharacter(string characterName, GameObject characterObject, Image characterImage = null, TextMeshProUGUI nameText = null)
    {
        if (string.IsNullOrEmpty(characterName) || characterObject == null)
        {
            Debug.LogError("�L�����N�^�[���ƃQ�[���I�u�W�F�N�g�͕K�{�ł�");
            return;
        }

        // ���ɓ������O�̃L�����N�^�[�����݂��邩�m�F
        foreach (var character in characters)
        {
            if (character.characterName.Equals(characterName, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"�������O�̃L�����N�^�[ '{characterName}' �͊��ɓo�^����Ă��܂�");
                return;
            }
        }

        // Image�R���|�[�l���g���w�肳��Ă��Ȃ���Ύ����擾
        if (characterImage == null)
        {
            characterImage = characterObject.GetComponent<Image>();
        }

        // TextMeshPro�R���|�[�l���g���w�肳��Ă��Ȃ���Ύ����擾
        if (nameText == null)
        {
            nameText = characterObject.GetComponentInChildren<TextMeshProUGUI>();
        }

        // �L�����N�^�[�ݒ��ǉ�
        CharacterSettings newCharacter = new CharacterSettings
        {
            characterName = characterName,
            characterObject = characterObject,
            characterImage = characterImage,
            nameText = nameText,
            highlightColor = defaultHighlightColor,
            normalColor = defaultNormalColor
        };

        characters.Add(newCharacter);

        // ���O�\���������L���Ȃ�ݒ�
        if (enhanceNameDisplay && nameText != null)
        {
            Transform nameArea = nameText.transform.parent;
            if (nameArea != null)
            {
                Image nameBg = nameArea.GetComponent<Image>();
                if (nameBg == null)
                {
                    nameBg = nameArea.gameObject.AddComponent<Image>();
                    // ���E�ňقȂ�F��ݒ�
                    if (characterName.Contains("��") || characterName.Contains("???"))
                    {
                        nameBg.color = nameDisplaySettings.leftNameBgColor;
                    }
                    else
                    {
                        nameBg.color = nameDisplaySettings.rightNameBgColor;
                    }
                    nameBg.transform.SetAsFirstSibling();
                }

                // ��\����Ԃŏ�����
                Color bgColor = nameBg.color;
                bgColor.a = 0f;
                nameBg.color = bgColor;

                // �����ɒǉ�
                nameBgImages[characterName] = nameBg;

                // �e�L�X�g�ݒ�
                ConfigureNameText(nameText);
            }
        }

        //if (debugMode)
        //{
        //    Debug.Log($"�L�����N�^�[��ǉ�: {characterName}");
        //}
    }

    private void OnDestroy()
    {
        StopAllTransitions();
    }
}