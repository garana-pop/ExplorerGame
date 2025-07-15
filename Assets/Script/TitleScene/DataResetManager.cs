using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class DataResetManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dataResetConfirmationPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Settings")]
    [SerializeField] private float messageDisplayDuration = 2.0f;
    [SerializeField] private string deletionMessage = "�ۑ�����Ă����f�[�^�͂��ׂč폜����܂���";

    [Header("Text Animation Settings")]
    [Tooltip("�����S�̂̃t�F�[�h�C�����ԁi�b�j")]
    [SerializeField] private float textFadeInDuration = 1.0f;

    [Tooltip("�����̍ŏ��̓����x�i0.0�`1.0�j")]
    [SerializeField] private float startAlpha = 0.0f;

    [Tooltip("�����̍ŏI�����x�i0.0�`1.0�j")]
    [SerializeField] private float endAlpha = 1.0f;

    private void Start()
    {
        // �{�^���C�x���g�̐ݒ�
        if (yesButton != null)
            yesButton.onClick.AddListener(OnYesButtonClicked);

        if (noButton != null)
            noButton.onClick.AddListener(OnNoButtonClicked);

        // ������ԂŃ��b�Z�[�W�p�l�����\��
        if (messagePanel != null)
            messagePanel.SetActive(false);
    }

    private void OnYesButtonClicked()
    {
        StartCoroutine(DeleteDataAndRestart());
    }

    private void OnNoButtonClicked()
    {
        // �f�[�^�������m�F�p�l�����\��
        if (dataResetConfirmationPanel != null)
            dataResetConfirmationPanel.SetActive(false);
    }

    private IEnumerator DeleteDataAndRestart()
    {
        // 1. �S�Z�[�u�f�[�^���폜
        DeleteAllSaveData();

        // 2. �폜�������b�Z�[�W���ӂ���ƕ\��
        yield return StartCoroutine(ShowDeletionMessageWithFadeEffect());

        // 3. ���b�Z�[�W�\�����Ԃ����ҋ@
        yield return new WaitForSeconds(messageDisplayDuration);

        // 4. �Q�[�����ċN��
        RestartGame();
    }

    private void DeleteAllSaveData()
    {
        // GameSaveManager���g�p���ăZ�[�u�f�[�^���폜
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.DeleteAllSaveData();
        }

        // ���̑��̃Z�[�u�f�[�^������ꍇ�͂����ō폜
        // ��: �J�X�^���Z�[�u�t�@�C���̍폜�Ȃ�
    }

    /// <summary>
    /// �폜�������b�Z�[�W���ӂ���ƕ\������
    /// </summary>
    private IEnumerator ShowDeletionMessageWithFadeEffect()
    {
        if (messagePanel == null || messageText == null)
        {
            Debug.LogWarning("DataResetManager: ���b�Z�[�W�p�l���܂��̓e�L�X�g���ݒ肳��Ă��܂���");
            yield break;
        }

        // ���b�Z�[�W�p�l����\��
        messagePanel.SetActive(true);

        // ���b�Z�[�W�e�L�X�g��ݒ�
        messageText.text = deletionMessage;

        // �����S�̂��t�F�[�h�C���ŕ\��
        yield return StartCoroutine(FadeInText());
    }

    /// <summary>
    /// �e�L�X�g�S�̂��ӂ���ƕ\������
    /// </summary>
    private IEnumerator FadeInText()
    {
        if (messageText == null) yield break;

        Color originalColor = messageText.color;
        float timer = 0f;

        // ������ԁF�����ɐݒ�
        messageText.color = new Color(originalColor.r, originalColor.g, originalColor.b, startAlpha);

        // �t�F�[�h�C���A�j���[�V����
        while (timer < textFadeInDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / textFadeInDuration;
            float currentAlpha = Mathf.Lerp(startAlpha, endAlpha, progress);

            messageText.color = new Color(originalColor.r, originalColor.g, originalColor.b, currentAlpha);

            yield return null;
        }

        // �ŏI��ԁF���S�ɕ\��
        messageText.color = new Color(originalColor.r, originalColor.g, originalColor.b, endAlpha);
    }

    private void ShowDeletionMessage()
    {
        if (messagePanel != null && messageText != null)
        {
            messageText.text = deletionMessage;
            messagePanel.SetActive(true);
        }
    }

    private void RestartGame()
    {
        // �V�[�����ēǂݍ��݂��ăQ�[�����ċN��
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}