using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// �v���o���{�^���̃e�L�X�g�\�����Ǘ�����N���X
/// RememberButtonTextChangerForHer�ƘA�g���ē���
/// </summary>
public class RememberButtonTextLoaderForHer : MonoBehaviour
{
    [Header("�e�L�X�g�ݒ�")]
    [Tooltip("�\���Ώۂ�TextMeshPro�R���|�[�l���g")]
    [SerializeField] private TMP_Text buttonText;

    [Tooltip("�ʏ펞�̃{�^���e�L�X�g")]
    [SerializeField] private string normalButtonText = "�v���o��";

    [Tooltip("�ύX��̃{�^���e�L�X�g")]
    [SerializeField] private string changedButtonText = "���ӂ���";

    [Header("RememberButtonTextChangerForHer�Q��")]
    [Tooltip("RememberButtonTextChangerForHer�ւ̎Q�Ɓi�I�v�V�����j")]
    [SerializeField] private RememberButtonTextChangerForHer buttonTextChanger;

    [Header("�f�o�b�O�ݒ�")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceChangedText = false; // �e�X�g�p�̋����ύX

    private void Awake()
    {
        // TextMeshPro�R���|�[�l���g�̎����擾
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
            Debug.LogError("RememberButtonTextLoaderForHer: �v���o���{�^����TextMeshPro�R���|�[�l���g��������܂���B�C���X�y�N�^�[�Őݒ肵�Ă��������B");
            enabled = false;
            return;
        }

        // RememberButtonTextChangerForHer�̎�������
        if (buttonTextChanger == null)
        {
            buttonTextChanger = FindFirstObjectByType<RememberButtonTextChangerForHer>();
        }

        // RememberButtonTextChangerForHer����ݒ�l���擾
        if (buttonTextChanger != null)
        {
            // �ύX��e�L�X�g���擾�i���t���N�V�������g�p�j
            var newTextField = buttonTextChanger.GetType().GetField("newButtonText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (newTextField != null)
            {
                string newTextValue = newTextField.GetValue(buttonTextChanger) as string;
                if (!string.IsNullOrEmpty(newTextValue))
                {
                    changedButtonText = newTextValue;
                }
            }
        }
    }

    private void Start()
    {
        // �����x�������Ċm���ɏ�Ԃ��m�F
        StartCoroutine(LoadAndApplyTextDelayed());
    }

    /// <summary>
    /// �x����Ƀ{�^���e�L�X�g��ǂݍ���œK�p
    /// </summary>
    private IEnumerator LoadAndApplyTextDelayed()
    {
        // ��������҂�
        yield return new WaitForSeconds(0.1f);

        LoadAndApplyText();
    }

    /// <summary>
    /// afterChangeToHerMemory�t���O���擾
    /// </summary>
    private bool GetAfterChangeToHerMemoryFlag()
    {
        // GameSaveManager����擾
        if (GameSaveManager.Instance != null)
        {
            return GameSaveManager.Instance.GetAfterChangeToHerMemoryFlag();
        }

        // �t���O���擾�ł��Ȃ��ꍇ��false��Ԃ�
        if (debugMode) Debug.LogWarning("RememberButtonTextLoaderForHer: GameSaveManager�����݂��Ȃ����߁AafterChangeToHerMemory�t���O���擾�ł��܂���ł���");
        return false;
    }

    /// <summary>
    /// afterChangeToHisFuture�t���O���擾
    /// </summary>
    private bool GetAfterChangeToHisFutureFlag()
    {
        // GameSaveManager����擾
        if (GameSaveManager.Instance != null)
        {
            return GameSaveManager.Instance.GetAfterChangeToHisFutureFlag();
        }

        // �t���O���擾�ł��Ȃ��ꍇ��false��Ԃ�
        if (debugMode) Debug.LogWarning("RememberButtonTextLoaderForHer: GameSaveManager�����݂��Ȃ����߁AafterChangeToHisFuture�t���O���擾�ł��܂���ł���");
        return false;
    }

    /// <summary>
    /// ���݂̏�ԂɊ�Â��ă{�^���e�L�X�g��K�p
    /// </summary>
    private void LoadAndApplyText()
    {
        // afterChangeToLast�t���O��true�̏ꍇ�͏������X�L�b�v
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.GetAfterChangeToLastFlag())
        {
            if (debugMode) Debug.Log("RememberButtonTextLoaderForHer: afterChangeToLast��true�̂��ߏ������X�L�b�v���܂�");
            return;
        }

        bool shouldChangeText = false;

        // �f�o�b�O���[�h�ł̋����ύX
        if (debugMode && forceChangedText)
        {
            shouldChangeText = true;
            if (debugMode) Debug.Log("RememberButtonTextLoaderForHer: �f�o�b�O���[�h�ŋ����I�Ƀe�L�X�g��ύX");
        }
        else
        {
            // �����̃t���O���`�F�b�N
            bool herMemoryFlag = GetAfterChangeToHerMemoryFlag();
            bool hisFutureFlag = GetAfterChangeToHisFutureFlag();

            // �����̃t���O��true�̏ꍇ�̂݃e�L�X�g��ύX
            shouldChangeText = herMemoryFlag && hisFutureFlag;

            if (debugMode)
            {
                Debug.Log($"RememberButtonTextLoaderForHer: afterChangeToHerMemory = {herMemoryFlag}");
                Debug.Log($"RememberButtonTextLoaderForHer: afterChangeToHisFuture = {hisFutureFlag}");
                Debug.Log($"RememberButtonTextLoaderForHer: ���t���O���� = {shouldChangeText}");
            }
        }

        // �{�^���e�L�X�g��ݒ�
        string textToApply = shouldChangeText ? changedButtonText : normalButtonText;

        if (buttonText != null)
        {
            buttonText.text = textToApply;

            if (debugMode)
            {
                Debug.Log($"RememberButtonTextLoaderForHer: �{�^���e�L�X�g���u{textToApply}�v�ɐݒ肵�܂���");
            }
        }

        // �{�^���ɃV�[���J�ڋ@�\��ݒ�
        if (shouldChangeText && buttonText != null)
        {
            Button button = buttonText.GetComponentInParent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() =>
                {
                    if (debugMode) Debug.Log("RememberButtonTextLoaderForHer: MonologueScene�֑J�ڂ��܂�");
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MonologueScene");
                });
            }
        }
    }

    /// <summary>
    /// �蓮�Ńe�L�X�g���ēǂݍ��݁i�f�o�b�O�p�j
    /// </summary>
    [ContextMenu("Reload Button Text")]
    public void ReloadButtonText()
    {
        LoadAndApplyText();
    }

    /// <summary>
    /// �t���O�̏�Ԃ��蓮�Őݒ�i�f�o�b�O�p�j
    /// </summary>
    [ContextMenu("Toggle Changed Text")]
    public void ToggleChangedText()
    {
        forceChangedText = !forceChangedText;
        LoadAndApplyText();
    }
}