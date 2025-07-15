#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// �e�X�g�v���C�p�̃t���O�ƃZ�[�u�f�[�^�����S����������G�f�B�^�[�X�N���v�g
/// </summary>
public class TestFlagResetter : EditorWindow
{
    [MenuItem("Tools/Test Flag Resetter")]
    public static void ShowWindow()
    {
        GetWindow<TestFlagResetter>("�e�X�g�t���O���Z�b�^�[");
    }

    private void OnGUI()
    {
        GUILayout.Label("�e�X�g�p�t���O�E�f�[�^������", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // ���݂̏�ԕ\��
        GUILayout.Label("���݂̏��:", EditorStyles.boldLabel);
        GUILayout.Label($"PlayerPrefs AfterChangeToHerMemory: {PlayerPrefs.GetInt("AfterChangeToHerMemory", 0)}");
        GUILayout.Label($"PlayerPrefs TitleTextChanged: {PlayerPrefs.GetInt("TitleTextChanged", 0)}");
        GUILayout.Label($"PlayerPrefs LastSceneName: {PlayerPrefs.GetString("LastSceneName", "�Ȃ�")}");

        string saveFilePath = Path.Combine(Application.persistentDataPath, "game_save.json");
        GUILayout.Label($"�Z�[�u�t�@�C������: {File.Exists(saveFilePath)}");

        GUILayout.Space(20);

        // �ʃ��Z�b�g�{�^��
        GUILayout.Label("�ʃ��Z�b�g:", EditorStyles.boldLabel);

        if (GUILayout.Button("PlayerPrefs �t���O�̂݃��Z�b�g"))
        {
            ResetPlayerPrefsFlags();
        }

        if (GUILayout.Button("�Z�[�u�f�[�^�̂ݍ폜"))
        {
            DeleteSaveData();
        }

        if (GUILayout.Button("�����^�C���t���O�̂݃��Z�b�g"))
        {
            ResetRuntimeFlags();
        }

        GUILayout.Space(10);

        // ���S���Z�b�g�{�^��
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("���S���Z�b�g�i���ׂď������j", GUILayout.Height(40)))
        {
            if (EditorUtility.DisplayDialog("�m�F",
                "���ׂẴt���O�ƃZ�[�u�f�[�^���폜���܂��B\n�{���ɂ�낵���ł����H",
                "�͂�", "�L�����Z��"))
            {
                CompleteReset();
            }
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(20);

        // ���\��
        GUILayout.Label("�g�p���@:", EditorStyles.boldLabel);
        GUILayout.Label("1. �e�X�g�O�Ɂu���S���Z�b�g�v�����s");
        GUILayout.Label("2. �܂��͌ʃ��Z�b�g�ŕK�v�ȕ����̂ݏ�����");
        GUILayout.Label("3. �v���C���[�h�J�n�ŃN���[���ȏ�ԂŃe�X�g�\");
    }

    private void ResetPlayerPrefsFlags()
    {
        PlayerPrefs.DeleteKey("AfterChangeToHerMemory");
        PlayerPrefs.DeleteKey("TitleTextChanged");
        PlayerPrefs.DeleteKey("LastSceneName");
        PlayerPrefs.Save();

        Debug.Log("TestFlagResetter: PlayerPrefs �t���O�����������܂���");
    }

    private void DeleteSaveData()
    {
        try
        {
            string saveFilePath = Path.Combine(Application.persistentDataPath, "game_save.json");
            string txtFilePath = Path.Combine(Application.persistentDataPath, "txt_progress.json");
            string pngFilePath = Path.Combine(Application.persistentDataPath, "png_progress.json");
            string pdfFilePath = Path.Combine(Application.persistentDataPath, "pdf_progress.json");

            if (File.Exists(saveFilePath)) File.Delete(saveFilePath);
            if (File.Exists(txtFilePath)) File.Delete(txtFilePath);
            if (File.Exists(pngFilePath)) File.Delete(pngFilePath);
            if (File.Exists(pdfFilePath)) File.Delete(pdfFilePath);

            Debug.Log("TestFlagResetter: �Z�[�u�f�[�^���폜���܂���");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"TestFlagResetter: �Z�[�u�f�[�^�폜�G���[: {e.Message}");
        }
    }

    private void ResetRuntimeFlags()
    {
        // �����^�C������GameSaveManager�̃t���O�����Z�b�g
        GameSaveManager saveManager = Object.FindFirstObjectByType<GameSaveManager>();
        if (saveManager != null)
        {
            // GameSaveManager�ɑ��݂���p�u���b�N���\�b�h���g�p
            if (Application.isPlaying)
            {
                // �v���C���̏ꍇ�A���ڃt���O�����Z�b�g
                saveManager.SetAfterChangeToHerMemoryFlag(false);
            }
        }

        // TitleTextChanger�̃t���O�����Z�b�g
        TitleTextChanger titleChanger = Object.FindFirstObjectByType<TitleTextChanger>();
        if (titleChanger != null)
        {
            // TitleTextChanger�ɑ��݂���p�u���b�N���\�b�h���g�p
            titleChanger.ResetText();
        }

        Debug.Log("TestFlagResetter: �����^�C���t���O�����������܂���");
    }

    private void CompleteReset()
    {
        // ���ׂĂ�������
        ResetPlayerPrefsFlags();
        DeleteSaveData();

        if (Application.isPlaying)
        {
            ResetRuntimeFlags();
        }

        Debug.Log("TestFlagResetter: ���S���������������܂���");

        // �E�B���h�E���X�V
        Repaint();
    }
}
#endif