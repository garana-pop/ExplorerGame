using System;
using UnityEngine;

/// <summary>
/// MainScene���[�h���ɃZ�[�u�f�[�^��ǂݍ��ރR���g���[���[
/// </summary>
public class MainSceneController : MonoBehaviour
{
    [Tooltip("�N�����ɃZ�[�u�f�[�^�������œǂݍ��ނ��ǂ���")]
    [SerializeField] private bool loadSaveDataOnStart = true;

    [Tooltip("�f�o�b�O���O��\�����邩�ǂ���")]
    [SerializeField] private bool debugMode = false;

    // GameSaveManager�ւ̎Q�Ɓi�I�v�V�����ŃC���X�y�N�^����ݒ�\�j
    [SerializeField] private GameSaveManager saveManager;

    /// <summary>
    /// �N�����̏���
    /// </summary>
    private void Start()
    {
        InitializeSaveManager();

        if (loadSaveDataOnStart)
        {
            LoadSaveData();
        }
    }

    /// <summary>
    /// SaveManager�̏�����
    /// </summary>
    private void InitializeSaveManager()
    {
        // ���łɐݒ肳��Ă���ꍇ�͉������Ȃ�
        if (saveManager != null) return;

        // GameSaveManager���擾
        saveManager = GameSaveManager.Instance;

        // �C���X�^���X��������Ȃ��ꍇ�͐V�K�쐬
        if (saveManager == null)
        {
            LogDebug("GameSaveManager��������܂���B�V�����쐬���܂��B");
            GameObject saveManagerObj = new GameObject("GameSaveManager");
            saveManager = saveManagerObj.AddComponent<GameSaveManager>();
        }
    }

    /// <summary>
    /// �Z�[�u�f�[�^��ǂݍ���
    /// </summary>
    private void LoadSaveData()
    {
        try
        {
            if (saveManager == null)
            {
                LogWarning("GameSaveManager��������܂���B�Z�[�u�f�[�^�̓ǂݍ��݂��X�L�b�v���܂��B");
                return;
            }

            // �Z�[�u�f�[�^��ǂݍ���œK�p
            bool loadSuccess = saveManager.LoadGameAndApply();

            if (debugMode)
            {
                if (loadSuccess)
                {
                    LogDebug($"�Z�[�u�f�[�^��ǂݍ��݂܂����B�ۑ�����: {saveManager.GetLastSaveTimestamp()}");
                }
                else
                {
                    LogDebug("�Z�[�u�f�[�^���Ȃ����A�ǂݍ��݂Ɏ��s���܂����B�V�K�Q�[���Ƃ��ĊJ�n���܂��B");
                }
            }
        }
        catch (Exception ex)
        {
            LogError($"�Z�[�u�f�[�^�̓ǂݍ��ݒ��ɃG���[���������܂���: {ex.Message}");
        }
    }

    // �f�o�b�O�p�̃��O���\�b�h
    private void LogDebug(string message)
    {
        if (debugMode) Debug.Log($"[MainSceneController] {message}");
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[MainSceneController] {message}");
    }

    private void LogError(string message)
    {
        Debug.LogError($"[MainSceneController] {message}");
    }
}