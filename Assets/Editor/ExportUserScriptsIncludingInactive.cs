using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class ExportUserScriptsIncludingInactive : EditorWindow
{
    [MenuItem("Tools/Export All User Scripts (Including Inactive)")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(ExportUserScriptsIncludingInactive));
    }

    void OnGUI()
    {
        if (GUILayout.Button("Export User Scripts"))
        {
            ExportScripts();
        }
    }

    /// <summary>
    /// ���݂̃V�[�����i�A�N�e�B�u�^��A�N�e�B�u��킸�j�̑S MonoBehaviour ����A���[�U�[�쐬�iAssets�t�H���_���j�X�N���v�g�̃t�@�C���������W���A
    /// �e�L�X�g�t�@�C���֏����o���܂��B
    /// </summary>
    void ExportScripts()
    {
        // Resources.FindObjectsOfTypeAll ���g���A�SMonoBehaviour�i��A�N�e�B�u�܂ށj���擾
        MonoBehaviour[] behaviours = Resources.FindObjectsOfTypeAll<MonoBehaviour>();

        // �d���r���p�̃n�b�V���Z�b�g
        HashSet<string> scriptNames = new HashSet<string>();

        foreach (var behaviour in behaviours)
        {
            // �V�[���ɏ������Ă���I�u�W�F�N�g�̂ݑΏۂƂ���
            if (!behaviour.gameObject.scene.IsValid())
                continue;

            // MonoBehaviour ����֘A�t�����Ă��� MonoScript ���擾
            MonoScript monoScript = MonoScript.FromMonoBehaviour(behaviour);
            if (monoScript == null)
                continue;

            // �擾���� MonoScript ����A�Z�b�g�p�X�𔲂��o��
            string scriptPath = AssetDatabase.GetAssetPath(monoScript);

            // ���[�U�[���쐬�����X�N���v�g�iAssets�t�H���_���̂��́j�̂ݑΏۂƂ���
            if (!scriptPath.StartsWith("Assets/"))
                continue;

            string fileName = Path.GetFileName(scriptPath);
            if (!string.IsNullOrEmpty(fileName))
            {
                scriptNames.Add(fileName);
            }
        }

        // �o�͐�� Assets �t�H���_������ "UserScripts.txt" �Ƃ��ĕۑ�
        string filePath = Path.Combine(Application.dataPath, "UserScripts.txt");
        File.WriteAllLines(filePath, scriptNames);

        EditorUtility.DisplayDialog("Export Complete", "Exported user script file names to:\n" + filePath, "OK");
    }
}
