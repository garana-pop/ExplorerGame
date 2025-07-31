using UnityEngine;
using System.Runtime.InteropServices;
using System;

/// <summary>
/// �E�B���h�E�̃T�C�Y�ύX�𐧌䂷��}�l�[�W���[
/// </summary>
public class WindowResizeManager : MonoBehaviour
{
    private static WindowResizeManager instance;

    // Windows API
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    // �E�B���h�E�X�^�C���̒萔
    private const int GWL_STYLE = -16;
    private const int WS_SIZEBOX = 0x00040000;
    private const int WS_MAXIMIZEBOX = 0x00010000;
    private const int WS_MINIMIZEBOX = 0x00020000;
    private const int WS_CAPTION = 0x00C00000;

    // ���T�C�Y�������t���O�i���false�j
    private const bool IS_RESIZABLE = false;

    public static WindowResizeManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("WindowResizeManager");
                instance = go.AddComponent<WindowResizeManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // �N�����Ƀ��T�C�Y�𖳌���
            DisableWindowResize();
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // �G�f�B�^�łȂ��ꍇ�̂ݎ��s
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        DisableWindowResize();
#endif
    }

    /// <summary>
    /// �E�B���h�E�̃��T�C�Y�𖳌���
    /// </summary>
    private void DisableWindowResize()
    {
#if !UNITY_EDITOR && UNITY_STANDALONE_WIN
        IntPtr windowHandle = GetActiveWindow();
        if (windowHandle != IntPtr.Zero)
        {
            int style = GetWindowLong(windowHandle, GWL_STYLE);
            
            // ���T�C�Y�{�[�_�[�ƍő剻�{�^�����폜
            style &= ~(WS_SIZEBOX | WS_MAXIMIZEBOX);
            
            // �ŏ����{�^���ƃ^�C�g���o�[�͈ێ�
            style |= WS_MINIMIZEBOX | WS_CAPTION;
            
            SetWindowLong(windowHandle, GWL_STYLE, style);
            
            Debug.Log("�E�B���h�E�̃��T�C�Y�𖳌������܂���");
        }
#endif
    }

    /// <summary>
    /// �E�B���h�E�T�C�Y��ݒ�i���T�C�Y�s�̏�Ԃł��ύX�\�j
    /// </summary>
    /// <param name="width">��</param>
    /// <param name="height">����</param>
    public void SetWindowSize(int width, int height)
    {
        Screen.SetResolution(width, height, false);
        Debug.Log($"�E�B���h�E�T�C�Y�� {width}x{height} �ɕύX���܂���");

        // �𑜓x�ύX��Ƀ��T�C�Y���������ēK�p
        StartCoroutine(ReapplyResizeDisableAfterDelay());
    }

    /// <summary>
    /// �𑜓x�ύX��A�����x�����Ă��烊�T�C�Y���������ēK�p
    /// </summary>
    private System.Collections.IEnumerator ReapplyResizeDisableAfterDelay()
    {
        // �𑜓x�ύX����������܂ŏ����ҋ@
        yield return new WaitForSeconds(0.1f);

        // ���T�C�Y���������ēK�p
        DisableWindowResize();
    }

    /// <summary>
    /// ���݂̃��T�C�Y�\��Ԃ��擾�i���false��Ԃ��j
    /// </summary>
    public bool IsResizable()
    {
        return false;
    }

    // �ȉ��̃��\�b�h�͌݊����̂��߂Ɏc�����A���ۂɂ͉������Ȃ�
    /// <summary>
    /// �E�B���h�E�̃��T�C�Y�ۂ�ݒ�i���̃o�[�W�����ł͖����j
    /// </summary>
    /// <param name="resizable">���T�C�Y�\�ɂ���ꍇ��true�i���������j</param>
    [System.Obsolete("���̃��\�b�h�͎g�p����܂���B�E�B���h�E���T�C�Y�͏�ɖ����ł��B")]
    public void SetWindowResizable(bool resizable)
    {
        Debug.LogWarning("SetWindowResizable �͖���������Ă��܂��B�E�B���h�E���T�C�Y�͏�ɖ����ł��B");
    }
}