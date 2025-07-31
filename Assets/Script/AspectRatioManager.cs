using UnityEngine;

/// <summary>
/// �A�X�y�N�g��Ǘ��N���X�i�E�B���h�E���T�C�Y�������ɂ��@�\��~�j
/// </summary>
[System.Obsolete("�E�B���h�E���T�C�Y�����������ꂽ���߁A���̃N���X�͎g�p����܂���")]
public class AspectRatioManager : MonoBehaviour
{
    private static AspectRatioManager instance;

    [Header("�f�o�b�O�ݒ�")]
    [SerializeField] private bool debugMode = false;

    // 16:9�̃A�X�y�N�g��
    private const float TARGET_ASPECT_RATIO = 16f / 9f;

    public static AspectRatioManager Instance
    {
        get { return instance; }
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (debugMode)
        {
            Debug.LogWarning($"{nameof(AspectRatioManager)}: ���̃N���X�͖���������Ă��܂�");
        }
    }

    /// <summary>
    /// ���݂̃A�X�y�N�g����擾
    /// </summary>
    public float GetCurrentAspectRatio()
    {
        return (float)Screen.width / Screen.height;
    }

    /// <summary>
    /// �w�肳�ꂽ���ɑ΂���16:9�̍������v�Z
    /// </summary>
    public int CalculateHeightForWidth(int width)
    {
        return Mathf.RoundToInt(width / TARGET_ASPECT_RATIO);
    }

    /// <summary>
    /// �w�肳�ꂽ�����ɑ΂���16:9�̕����v�Z
    /// </summary>
    public int CalculateWidthForHeight(int height)
    {
        return Mathf.RoundToInt(height * TARGET_ASPECT_RATIO);
    }

    /// <summary>
    /// ���݂̃E�B���h�E�T�C�Y��16:9���ǂ������`�F�b�N
    /// </summary>
    public bool IsCorrectAspectRatio()
    {
        float currentRatio = GetCurrentAspectRatio();
        float difference = Mathf.Abs(currentRatio - TARGET_ASPECT_RATIO);
        return difference < 0.01f;
    }

    // Update�����͖�����
    void Update()
    {
        // �������Ȃ�
    }
}