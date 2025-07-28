using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// RememberButtonTextChangerForHer�N���X��DataResetPanelControllerBoot�t���O��true�A
/// �܂���game_save.json��afterChangeToLast�t���O��true�̏ꍇ��
/// �u�v���o���v�{�^��������Ƀf�[�^�������m�F�p�l����\������N���X
/// </summary>
public class DataResetPanelController : MonoBehaviour
{
    [Header("�p�l���Q��")]
    [Tooltip("�f�[�^�������m�F�p�l��")]
    [SerializeField] private GameObject dataResetConfirmationPanel;

    [Tooltip("�ݒ�p�l��")] 
    [SerializeField] private GameObject settingsPanel;

    [Header("�{�^���Q��")]
    [Tooltip("�v���o���{�^��")]
    [SerializeField] private Button rememberButton;

    [Header("�f�o�b�O�ݒ�")]
    [SerializeField] private bool debugMode = true;

    private void Awake()
    {
        // �p�l���̏�����Ԃ�ݒ�
        if (dataResetConfirmationPanel != null)
        {
            dataResetConfirmationPanel.SetActive(false);
        }
    }

    private void Start()
    {

        // �{�^���̃N���b�N�C�x���g�ɓo�^
        RegisterButtonListener();

        // �Q�[�����[�h����afterChangeToLast�t���O�`�F�b�N�i�v��2�j
        CheckAfterChangeToLastFlagOnLoad();

        // �N�����̃t���O��Ԃ����O�o��
        if (debugMode)
        {
            CheckAndLogFlags();
        }
    }

    /// <summary>
    /// �Q�[�����[�h����afterChangeToLast�t���O���`�F�b�N
    /// </summary>
    private void CheckAfterChangeToLastFlagOnLoad()
    {
        // GameSaveManager�����[�h��������܂ŏ����ҋ@
        StartCoroutine(CheckAfterChangeToLastFlagDelayed());
    }

    /// <summary>
    /// �x������afterChangeToLast�t���O���`�F�b�N
    /// </summary>
    private IEnumerator CheckAfterChangeToLastFlagDelayed()
    {
        // GameSaveManager�̃��[�h������҂�
        yield return new WaitForSeconds(0.5f);

        if (GameSaveManager.Instance != null)
        {
            bool afterChangeToLastFlag = GameSaveManager.Instance.GetAfterChangeToLastFlag();
            if (afterChangeToLastFlag)
            {
                if (debugMode) Debug.Log("DataResetPanelController: �Q�[�����[�h����afterChangeToLast�t���O��true�����o");
                // �t���O�̏�Ԃ�ێ��i�{�^���N���b�N���Ɏg�p�j
            }
        }
    }

    private void OnEnable()
    {

        // OnEnable�ł��o�^�����݂�i�ォ��L�������ꂽ�ꍇ�̑΍�j
        RegisterButtonListener();
    }

    /// <summary>
    /// �{�^�����X�i�[�̓o�^����
    /// </summary>
    private void RegisterButtonListener()
    {
        if (rememberButton != null)
        {
            // �����̃��X�i�[����U�폜���Ă���ǉ��i�d���h�~�j
            rememberButton.onClick.RemoveListener(OnRememberButtonClicked);
            rememberButton.onClick.AddListener(OnRememberButtonClicked);
        }
        else
        {
            Debug.LogError("DataResetPanelController: �v���o���{�^�����ݒ肳��Ă��܂���");
        }
    }

    /// <summary>
    /// �v���o���{�^�����N���b�N���ꂽ���̏���
    /// </summary>
    private void OnRememberButtonClicked()
    {

        bool shouldShowPanel = false;
        string reason = "";

        // RememberButtonTextChangerForHer�̃t���O���`�F�b�N
        RememberButtonTextChangerForHer textChanger = FindFirstObjectByType<RememberButtonTextChangerForHer>();
        if (textChanger != null && textChanger.DataResetPanelControllerBoot)
        {
            shouldShowPanel = true;
            reason = "DataResetPanelControllerBoot�t���O��true";
            if (debugMode) Debug.Log($"DataResetPanelController: {reason}");
        }

        // GameSaveManager��afterChangeToLast�t���O���`�F�b�N
        if (!shouldShowPanel && GameSaveManager.Instance != null)
        {
            bool afterChangeToLastFlag = GameSaveManager.Instance.GetAfterChangeToLastFlag();
            if (afterChangeToLastFlag)
            {
                shouldShowPanel = true;
                reason = "afterChangeToLast�t���O��true";
                if (debugMode) Debug.Log($"DataResetPanelController: {reason}");
            }
        }

        // �p�l���\������
        if (shouldShowPanel)
        {
            if (debugMode) Debug.Log($"DataResetPanelController: {reason}�̂��߁A�f�[�^�������m�F�p�l����\�����܂�");
            ShowDataResetPanel();
        }
        else
        {
            if (debugMode) Debug.Log("DataResetPanelController: �p�l���\�������𖞂����Ă��܂���");
        }
    }

    /// <summary>
    /// �f�[�^�������m�F�p�l����\��
    /// </summary>
    //private void ShowDataResetPanel()
    //{
    //    // �܂��ݒ�p�l�����A�N�e�B�u�ɂ���
    //    if (settingsPanel != null)
    //    {
    //        if (!settingsPanel.activeSelf)
    //        {
    //            settingsPanel.SetActive(true);
    //            if (debugMode) Debug.Log("DataResetPanelController: �ݒ�p�l�����A�N�e�B�u�ɂ��܂���");
    //        }
    //    }
    //    else
    //    {
    //        Debug.LogWarning("DataResetPanelController: �ݒ�p�l�����ݒ肳��Ă��܂���");
    //    }

    //    // ���̌�A�f�[�^�������m�F�p�l����\��
    //    if (dataResetConfirmationPanel != null)
    //    {
    //        dataResetConfirmationPanel.SetActive(true);
    //        if (debugMode) Debug.Log("DataResetPanelController: �f�[�^�������m�F�p�l����\�����܂���");
    //    }
    //    else
    //    {
    //        Debug.LogError("DataResetPanelController: �f�[�^�������m�F�p�l�����ݒ肳��Ă��܂���");
    //    }
    //}
    private void ShowDataResetPanel()
    {
        // �R���[�`�����J�n���ď������s��
        StartCoroutine(ShowDataResetPanelCoroutine());
    }

    private IEnumerator ShowDataResetPanelCoroutine()
    {
        // �܂��ݒ�p�l�����A�N�e�B�u�ɂ���
        if (settingsPanel != null)
        {
            if (!settingsPanel.activeSelf)
            {
                settingsPanel.SetActive(true);
                if (debugMode) Debug.Log("DataResetPanelController: �ݒ�p�l�����A�N�e�B�u�ɂ��܂���");
            }
        }
        else
        {
            Debug.LogWarning("DataResetPanelController: �ݒ�p�l�����ݒ肳��Ă��܂���");
        }

        // 1�t���[���ҋ@�i�d�v�j
        yield return null;

        // ���̌�A�f�[�^�������m�F�p�l����\��
        if (dataResetConfirmationPanel != null)
        {
            dataResetConfirmationPanel.SetActive(true);
            if (debugMode) Debug.Log("DataResetPanelController: �f�[�^�������m�F�p�l����\�����܂���");
        }
        else
        {
            Debug.LogError("DataResetPanelController: �f�[�^�������m�F�p�l�����ݒ肳��Ă��܂���");
        }
    }

    /// <summary>
    /// �p�l�����\���ɂ���i�O������Ăяo���\�j
    /// </summary>
    public void HideDataResetPanel()
    {
        if (dataResetConfirmationPanel != null)
        {
            dataResetConfirmationPanel.SetActive(false);

            if (debugMode) Debug.Log("DataResetPanelController: �f�[�^�������m�F�p�l�����\���ɂ��܂���");
        }
    }

    /// <summary>
    /// �t���O�̏�Ԃ��`�F�b�N���ă��O�o�́i�f�o�b�O�p�j
    /// </summary>
    private void CheckAndLogFlags()
    {
        Debug.Log("=== DataResetPanelController �t���O��� ===");

        // RememberButtonTextChangerForHer�̃t���O
        RememberButtonTextChangerForHer textChanger = FindFirstObjectByType<RememberButtonTextChangerForHer>();
        if (textChanger != null)
        {
            Debug.Log($"DataResetPanelControllerBoot�t���O: {textChanger.DataResetPanelControllerBoot}");
        }
        else
        {
            Debug.Log("RememberButtonTextChangerForHer��������܂���");
        }

        // GameSaveManager�̃t���O
        if (GameSaveManager.Instance != null)
        {
            Debug.Log($"afterChangeToLast�t���O: {GameSaveManager.Instance.GetAfterChangeToLastFlag()}");
        }
        else
        {
            Debug.Log("GameSaveManager��������܂���");
        }

        Debug.Log("=====================================");
    }

    private void OnDestroy()
    {
        // �C�x���g���X�i�[�̉���
        if (rememberButton != null)
        {
            rememberButton.onClick.RemoveListener(OnRememberButtonClicked);
        }
    }
}