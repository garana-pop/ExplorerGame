using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MonologueScene�̃Z���t�f�[�^��ǂݍ��ރN���X
/// </summary>
public class MonologueDataLoader : MonoBehaviour
{
    [Header("�t�@�C���ݒ�")]
    [SerializeField] private string fileName = "MonologueScene_�Z���t";

    [Header("�f�o�b�O")]
    [SerializeField] private bool debugMode = false;

    /// <summary>
    /// �Z���t�f�[�^��ǂݍ���
    /// </summary>
    /// <returns>�Z���t�̃��X�g</returns>
    public List<string> LoadDialogueData()
    {
        List<string> dialogues = new List<string>();

        try
        {
            // Resources�t�H���_����e�L�X�g�t�@�C����ǂݍ���
            TextAsset textAsset = Resources.Load<TextAsset>(fileName);

            if (textAsset == null)
            {
                Debug.LogError($"�Z���t�t�@�C�� '{fileName}' ��������܂���B");
                return dialogues;
            }

            // ���s�ŕ������ă��X�g�ɒǉ�
            string[] lines = textAsset.text.Split('\n');

            foreach (string line in lines)
            {
                // ��s�����O���Ȃ��i�u�E�E�E�v���L���ȃZ���t�Ƃ��Ĉ����j
                string trimmedLine = line.Trim();
                if (!string.IsNullOrEmpty(trimmedLine))
                {
                    dialogues.Add(trimmedLine);

                    if (debugMode)
                    {
                        Debug.Log($"�ǂݍ��񂾃Z���t: {trimmedLine}");
                    }
                }
            }

            if (debugMode)
            {
                Debug.Log($"���v {dialogues.Count} �̃Z���t��ǂݍ��݂܂����B");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"�Z���t�f�[�^�̓ǂݍ��ݒ��ɃG���[���������܂���: {e.Message}");
        }

        return dialogues;
    }
}