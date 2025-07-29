using UnityEngine;
using System.Collections;

/// <summary>
/// 16:9�̃A�X�y�N�g����ێ����ăE�B���h�E�T�C�Y���Ǘ�����}�l�[�W���[
/// </summary>
public class AspectRatioManager : MonoBehaviour
{
    private bool isResizing = false;
    private float resizeDelay = 0.1f; // ���T�C�Y�����̒x������
    private float lastResizeTime = 0f;

    // �V���O���g���C���X�^���X
    private static AspectRatioManager instance;
    public static AspectRatioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AspectRatioManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("AspectRatioManager");
                    instance = go.AddComponent<AspectRatioManager>();
                }
            }
            return instance;
        }
    }

    [Header("�A�X�y�N�g��ݒ�")]
    [SerializeField] private float targetAspectRatio = 16f / 9f; // 16:9�Œ�

    [Header("�f�o�b�O�ݒ�")]
    [SerializeField] private bool debugMode = false;

    // �O�t���[���̃E�B���h�E�T�C�Y
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        // �V���O���g���p�^�[���̎���
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // �����E�B���h�E�T�C�Y���L�^
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        if (debugMode)
        {
            Debug.Log($"{nameof(AspectRatioManager)}: ���������� - �^�[�Q�b�g�A�X�y�N�g��: {targetAspectRatio:F2}");
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
        return Mathf.RoundToInt(width / targetAspectRatio);
    }

    /// <summary>
    /// �w�肳�ꂽ�����ɑ΂���16:9�̕����v�Z
    /// </summary>
    public int CalculateWidthForHeight(int height)
    {
        return Mathf.RoundToInt(height * targetAspectRatio);
    }

    /// <summary>
    /// ���݂̃E�B���h�E�T�C�Y��16:9���ǂ������`�F�b�N
    /// </summary>
    public bool IsCorrectAspectRatio()
    {
        float currentRatio = GetCurrentAspectRatio();
        float difference = Mathf.Abs(currentRatio - targetAspectRatio);
        return difference < 0.01f; // ���e�덷
    }

    /// <summary>
    /// �E�B���h�E�T�C�Y�ύX�����o
    /// </summary>
    public bool HasWindowSizeChanged()
    {
        return Screen.width != lastScreenWidth || Screen.height != lastScreenHeight;
    }

    /// <summary>
    /// �Ō�̃E�B���h�E�T�C�Y���X�V
    /// </summary>
    public void UpdateLastWindowSize()
    {
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
    }

    private void Update()
    {
        // ���T�C�Y���̏ꍇ�͏������X�L�b�v
        if (isResizing)
        {
            return;
        }

        // �E�B���h�E�T�C�Y�ύX���Ď�
        if (HasWindowSizeChanged())
        {
            // �Ō�̃��T�C�Y�����莞�Ԍo�߂��Ă��Ȃ��ꍇ�̓X�L�b�v
            if (Time.time - lastResizeTime < resizeDelay)
            {
                return;
            }

            // �A�X�y�N�g�䂪16:9�łȂ��ꍇ�̂ݏC��
            if (!IsCorrectAspectRatio())
            {
                // �R���[�`���ŃA�X�y�N�g����C��
                StartCoroutine(EnforceAspectRatio());
            }
            else
            {
                // �A�X�y�N�g�䂪�������ꍇ�́A�T�C�Y�����X�V
                UpdateLastWindowSize();
            }
        }
    }

    /// <summary>
    /// 16:9�̃A�X�y�N�g��������I�Ɉێ�����
    /// </summary>
    private IEnumerator EnforceAspectRatio()
    {
        isResizing = true;
        lastResizeTime = Time.time;

        // 1�t���[���ҋ@
        yield return null;

        int currentWidth = Screen.width;
        int currentHeight = Screen.height;
        float currentRatio = GetCurrentAspectRatio();

        // ���݂̃A�X�y�N�g��ƖڕW�A�X�y�N�g��̍������v�Z
        float ratioDifference = currentRatio - targetAspectRatio;

        int newWidth = currentWidth;
        int newHeight = currentHeight;

        // �A�X�y�N�g��̕␳���@������
        if (Mathf.Abs(ratioDifference) > 0.01f)
        {
            // �ύX�ʂ����Ȃ�����I��
            int heightBasedOnWidth = CalculateHeightForWidth(currentWidth);
            int widthBasedOnHeight = CalculateWidthForHeight(currentHeight);

            // �O��̃T�C�Y�Ƃ̍������l��
            int deltaHeightFromLast = Mathf.Abs(currentHeight - lastScreenHeight);
            int deltaWidthFromLast = Mathf.Abs(currentWidth - lastScreenWidth);

            // ���ύX���傫��������������ɂ���
            if (deltaWidthFromLast > deltaHeightFromLast)
            {
                // ���̕ύX���傫���ꍇ�A������ɍ����𒲐�
                newHeight = heightBasedOnWidth;
            }
            else
            {
                // �����̕ύX���傫���ꍇ�A��������ɕ��𒲐�
                newWidth = widthBasedOnHeight;
            }

            // �ŏ��𑜓x�̊m��
            if (newWidth < 960 || newHeight < 540)
            {
                newWidth = 960;
                newHeight = 540;
            }

            // �𑜓x��ݒ�i�E�B���h�E���[�h�Œ�j
            Screen.SetResolution(newWidth, newHeight, false);

            if (debugMode)
            {
                Debug.Log($"{nameof(AspectRatioManager)}: �A�X�y�N�g����C�����܂��� - {currentWidth}�~{currentHeight} �� {newWidth}�~{newHeight}");
            }

            // �𑜓x�ύX��A�����ҋ@
            yield return new WaitForSeconds(0.1f);
        }

        // �Ō�̃E�B���h�E�T�C�Y���X�V
        UpdateLastWindowSize();

        isResizing = false;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}