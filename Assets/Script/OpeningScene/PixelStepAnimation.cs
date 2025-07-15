using UnityEngine;

/// <summary>
/// �J�N�J�N�ƃX�e�b�v��ɏ㉺�ړ�����A�j���[�V������񋟂���R���|�[�l���g
/// ContinueIndicator�Ȃǂ�UI�v�f�Ɏg�p���܂�
/// </summary>
[AddComponentMenu("UI/Animations/Pixel Step Animation")]
public class PixelStepAnimation : MonoBehaviour
{
    [Header("�ړ��ݒ�")]
    [Tooltip("�㉺�̈ړ��ʁi�s�N�Z���P�ʁj")]
    [SerializeField] private float amplitude = 10f;

    [Tooltip("�A�j���[�V�����̑��x")]
    [SerializeField] private float speed = 2f;

    [Header("�J�N�J�N�ݒ�")]
    [Tooltip("�ʒu�X�V�̎��ԊԊu�i�b�j- �傫���قǃJ�N�J�N")]
    [Range(0.01f, 0.5f)]
    [SerializeField] private float updateInterval = 0.1f;

    [Tooltip("�s�N�Z���P�ʂňʒu���ۂ߂�i�J�N�J�N���������܂��j")]
    [SerializeField] private bool snapToPixel = true;

    [Tooltip("�X�e�b�v�� - ���Ȃ��قǃJ�N�J�N")]
    [Range(2, 20)]
    [SerializeField] private int steps = 4;

    [Header("�ڍאݒ�")]
    [Tooltip("�A�j���[�V�������J�n���Ɏ����I�ɊJ�n���邩")]
    [SerializeField] private bool playOnStart = true;

    // �v���C�x�[�g�ϐ�
    private RectTransform rectTransform;
    private Vector2 startPosition;
    private float currentTime;
    private float lastUpdateTime;
    private bool isPlaying = false;

    private void Awake()
    {
        // RectTransform�̎擾
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogError("PixelStepAnimation: RectTransform��������܂���BUI�v�f�ɃA�^�b�`���Ă��������B");
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        // �����ʒu��ۑ�
        startPosition = rectTransform.anchoredPosition;

        // �����J�n�̏ꍇ
        if (playOnStart)
        {
            Play();
        }
    }

    private void Update()
    {
        if (!isPlaying)
            return;

        // ���Ԃ̍X�V
        currentTime += Time.deltaTime * speed;

        // �X�V�Ԋu�ɒB���Ă��Ȃ��ꍇ�̓X�L�b�v
        if (Time.time - lastUpdateTime < updateInterval)
            return;

        lastUpdateTime = Time.time;

        // �X�e�b�v��̒l���v�Z�i0����steps-1�͈̔͂ŏz�j
        int currentStep = Mathf.FloorToInt(Mathf.Repeat(currentTime, steps)) % steps;

        // �X�e�b�v�𐳋K���i0-1�͈̔́j
        float normalizedStep = (float)currentStep / (steps - 1);

        // �O�p�g�p�^�[�����쐬�i0��1��0�̃p�^�[���j
        float triangleWave;
        if (normalizedStep < 0.5f)
            triangleWave = normalizedStep * 2;
        else
            triangleWave = 1 - ((normalizedStep - 0.5f) * 2);

        // Y�����̃I�t�Z�b�g���v�Z
        float yOffset = (triangleWave * 2 - 1) * amplitude;

        // �s�N�Z���ɃX�i�b�v����ꍇ
        if (snapToPixel)
            yOffset = Mathf.Round(yOffset);

        // �ʒu���X�V
        rectTransform.anchoredPosition = new Vector2(
            startPosition.x,
            startPosition.y + yOffset
        );
    }

    /// <summary>
    /// �A�j���[�V�������J�n���܂�
    /// </summary>
    public void Play()
    {
        isPlaying = true;
        lastUpdateTime = Time.time;
    }

    /// <summary>
    /// �A�j���[�V�������ꎞ��~���܂�
    /// </summary>
    public void Pause()
    {
        isPlaying = false;
    }

    /// <summary>
    /// �A�j���[�V�������~���A�����ʒu�ɖ߂��܂�
    /// </summary>
    public void Stop()
    {
        isPlaying = false;
        rectTransform.anchoredPosition = startPosition;
    }

    /// <summary>
    /// �J�N�J�N���𒲐����܂�
    /// </summary>
    /// <param name="pixelSnap">�s�N�Z���X�i�b�v�̗L��/����</param>
    /// <param name="intervalTime">�X�V�Ԋu�i�b�j</param>
    /// <param name="stepCount">�X�e�b�v��</param>
    public void SetPixelation(bool pixelSnap, float intervalTime, int stepCount)
    {
        snapToPixel = pixelSnap;
        updateInterval = Mathf.Clamp(intervalTime, 0.01f, 0.5f);
        steps = Mathf.Clamp(stepCount, 2, 20);
    }
}