using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FlagTestController : MonoBehaviour
{
    [Header("�e�X�g�{�^��")]
    [SerializeField] private Button setTrueButton;
    [SerializeField] private Button setFalseButton;
    [SerializeField] private Button getFlagButton;

    [Header("���ʕ\���p�e�L�X�g")]
    [SerializeField] private TextMeshProUGUI resultText;

    [Header("�f�o�b�O���\��")]
    [SerializeField] private bool showDebugInfo = true;

    void Start()
    {
        // �{�^���C�x���g�̐ݒ�
        if (setTrueButton != null)
            setTrueButton.onClick.AddListener(() => SetFlag(true));

        if (setFalseButton != null)
            setFalseButton.onClick.AddListener(() => SetFlag(false));

        if (getFlagButton != null)
            getFlagButton.onClick.AddListener(GetFlag);

        // ������Ԃ̕\��
        UpdateResultText("�e�X�g��������");

        if (showDebugInfo)
        {
            Debug.Log("FlagTestController: �e�X�g�J�n��������");
            Debug.Log($"GameSaveManager���݊m�F: {GameSaveManager.Instance != null}");
        }
    }

    /// <summary>
    /// �t���O��ݒ肷��e�X�g
    /// </summary>
    /// <param name="value">�ݒ肷��l</param>
    public void SetFlag(bool value)
    {
        if (GameSaveManager.Instance == null)
        {
            UpdateResultText("�G���[: GameSaveManager��������܂���");
            Debug.LogError("FlagTestController: GameSaveManager�����݂��܂���");
            return;
        }

        try
        {
            GameSaveManager.Instance.SetEndOpeningSceneFlag(value);
            UpdateResultText($"�t���O��{value}�ɐݒ肵�܂���");
            Debug.Log($"FlagTestController: SetEndOpeningSceneFlag({value})�����s");
        }
        catch (System.Exception e)
        {
            UpdateResultText($"�G���[: {e.Message}");
            Debug.LogError($"FlagTestController: SetFlag({value})�ŃG���[ - {e.Message}");
        }
    }

    /// <summary>
    /// �t���O���擾����e�X�g
    /// </summary>
    public void GetFlag()
    {
        if (GameSaveManager.Instance == null)
        {
            UpdateResultText("�G���[: GameSaveManager��������܂���");
            Debug.LogError("FlagTestController: GameSaveManager�����݂��܂���");
            return;
        }

        try
        {
            bool flagValue = GameSaveManager.Instance.GetEndOpeningSceneFlag();
            UpdateResultText($"���݂̃t���O�l: {flagValue}");
            Debug.Log($"FlagTestController: GetEndOpeningSceneFlag()�̌��� = {flagValue}");
        }
        catch (System.Exception e)
        {
            UpdateResultText($"�G���[: {e.Message}");
            Debug.LogError($"FlagTestController: GetFlag()�ŃG���[ - {e.Message}");
        }
    }

    /// <summary>
    /// �Z�[�u�f�[�^�S�̂̏󋵂��m�F����e�X�g
    /// </summary>
    [ContextMenu("�Z�[�u�f�[�^�󋵊m�F")]
    public void CheckSaveDataStatus()
    {
        if (GameSaveManager.Instance == null)
        {
            Debug.LogError("FlagTestController: GameSaveManager�����݂��܂���");
            return;
        }

        Debug.Log("=== �Z�[�u�f�[�^�󋵊m�F ===");
        Debug.Log($"�Z�[�u�f�[�^����: {GameSaveManager.Instance.SaveDataExists()}");

        if (GameSaveManager.Instance.SaveDataExists())
        {
            GameSaveManager.Instance.LoadGame();
            bool flagValue = GameSaveManager.Instance.GetEndOpeningSceneFlag();
            Debug.Log($"endOpeningScene�t���O: {flagValue}");
        }
        else
        {
            Debug.Log("�Z�[�u�f�[�^�����݂��Ȃ����߁A�t���O�l�͊m�F�ł��܂���");
        }
        Debug.Log("=========================");
    }

    /// <summary>
    /// ���ʕ\���e�L�X�g���X�V
    /// </summary>
    /// <param name="message">�\�����郁�b�Z�[�W</param>
    private void UpdateResultText(string message)
    {
        if (resultText != null)
        {
            resultText.text = $"[{System.DateTime.Now:HH:mm:ss}] {message}";
        }
    }

    /// <summary>
    /// �Z�[�u�f�[�^���폜���ăe�X�g�����Z�b�g
    /// </summary>
    [ContextMenu("�Z�[�u�f�[�^�폜�i�e�X�g���Z�b�g�j")]
    public void ResetTestData()
    {
        if (GameSaveManager.Instance != null)
        {
            // �Z�[�u�t�@�C�����폜
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, "game_save.json");
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                Debug.Log("FlagTestController: �Z�[�u�t�@�C�����폜���܂���");
                UpdateResultText("�Z�[�u�f�[�^�����Z�b�g���܂���");
            }
            else
            {
                Debug.Log("FlagTestController: �Z�[�u�t�@�C�������݂��܂���ł���");
                UpdateResultText("�폜�Ώۂ̃Z�[�u�t�@�C��������܂���");
            }
        }
    }
}