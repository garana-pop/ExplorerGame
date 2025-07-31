using UnityEngine;

/// <summary>
/// �Q�[���N�����̏������������s���N���X
/// </summary>
[DefaultExecutionOrder(-1000)] // ���̃X�N���v�g����Ɏ��s
public class StartupInitializer : MonoBehaviour
{
    void Awake()
    {
        // WindowResizeManager�̏�����
        InitializeWindowResizeManager();
    }

    /// <summary>
    /// WindowResizeManager��������
    /// </summary>
    private void InitializeWindowResizeManager()
    {
        // WindowResizeManager�̃C���X�^���X���쐬�i���݂��Ȃ��ꍇ�j
        if (WindowResizeManager.Instance == null)
        {
            Debug.Log("WindowResizeManager�����������Ă��܂�...");
        }

        // �E�B���h�E�̃��T�C�Y�͎����I�ɖ����������
        Debug.Log("�E�B���h�E�̃��T�C�Y���������������܂���");
    }
}