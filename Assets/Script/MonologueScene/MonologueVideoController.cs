using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// MonologueScene�p�̓���Đ��ƐF�����]����R���|�[�l���g
/// CLIP STUDIO���珑���o���ꂽMP4�A�j���[�V�����̍Đ�������s��
/// </summary>
public class MonologueVideoController : MonoBehaviour
{
    [Header("����Đ��ݒ�")]
    [Tooltip("VideoPlayer�R���|�[�l���g�ւ̎Q��")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Tooltip("�����\������RawImage�R���|�[�l���g")]
    [SerializeField] private RawImage videoDisplay;

    [Tooltip("�Đ����铮��N���b�v")]
    [SerializeField] private VideoClip[] videoClips;

    [Header("���[�v�ݒ�")]
    [Tooltip("���[�v�J�n���ԁi�b�j")]
    [SerializeField] private double loopStartTime = 5.0;

    [Tooltip("���[�v�I�����ԁi�b�j")]
    [SerializeField] private double loopEndTime = 21.0;

    [Tooltip("�ŏ����烋�[�v�͈͂܂ōĐ����邩")]
    [SerializeField] private bool playFromBeginning = false;

    [Header("�F�����]�ݒ�")]
    [Tooltip("�F�����]�p�̃V�F�[�_�[�}�e���A��")]
    [SerializeField] private Material invertMaterial;

    [Tooltip("�F�����]�̋��x (0:�ʏ�, 1:���S���])")]
    [Range(0f, 1f)]
    [SerializeField] private float invertAmount = 0f;

    [Tooltip("���x����")]
    [Range(0f, 2f)]
    [SerializeField] private float brightness = 1f;

    [Tooltip("�R���g���X�g����")]
    [Range(0f, 2f)]
    [SerializeField] private float contrast = 1f;

    [Header("�Đ�����")]
    [Tooltip("�V�[���J�n���Ɏ����Đ����邩")]
    [SerializeField] private bool autoPlay = true;

    [Tooltip("���[�v�Đ����邩")]
    [SerializeField] private bool loopVideo = true;

    [Tooltip("����I�����̓���")]
    [SerializeField] private VideoEndAction endAction = VideoEndAction.Loop;

    [Header("�f�o�b�O�ݒ�")]
    [SerializeField] private bool debugMode = false;

    // �v���C�x�[�g�ϐ�
    private RenderTexture renderTexture;
    private Material materialInstance;
    private int currentClipIndex = 0;
    private bool isInLoopRange = false;

    // �萔
    private const string INVERT_AMOUNT_PROPERTY = "_InvertAmount";
    private const string BRIGHTNESS_PROPERTY = "_Brightness";
    private const string CONTRAST_PROPERTY = "_Contrast";

    /// <summary>
    /// ����I�����̓�����`����񋓌^
    /// </summary>
    public enum VideoEndAction
    {
        Stop,           // ��~
        Loop,           // ���[�v
        NextClip,       // ���̃N���b�v
        HideVideo       // ������\��
    }

    #region Unity Lifecycle

    private void Awake()
    {
        // �R���|�[�l���g�̎����擾
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoDisplay == null)
            videoDisplay = GetComponent<RawImage>();

        // VideoPlayer�̊�{�ݒ�
        SetupVideoPlayer();
    }

    private void Start()
    {
        // �}�e���A���̏�����
        InitializeMaterial();

        // �����Đ��ݒ�
        if (autoPlay && videoClips.Length > 0)
        {
            PlayVideo(0);
        }

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: ����������");
    }

    private void Update()
    {
        // �}�e���A���v���p�e�B�̍X�V
        UpdateMaterialProperties();

        // ���[�v�͈̓`�F�b�N
        if (videoPlayer != null && videoPlayer.isPlaying && loopVideo)
        {
            CheckLoopRange();
        }
    }

    private void OnDestroy()
    {
        // ���\�[�X�̃N���[���A�b�v
        CleanupResources();
    }

    #endregion

    #region ���������\�b�h

    /// <summary>
    /// VideoPlayer�̊�{�ݒ���s��
    /// </summary>
    private void SetupVideoPlayer()
    {
        if (videoPlayer == null) return;

        // �����_�[���[�h�� RenderTexture �ɐݒ�
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        // RenderTexture�̍쐬
        CreateRenderTexture();

        // �C�x���g�̓o�^
        videoPlayer.loopPointReached += OnVideoEnd;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.frameReady += OnVideoFrameReady;

        // �ʏ�̃��[�v�@�\�͖������i�J�X�^�����[�v���g�p�j
        videoPlayer.isLooping = false;

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: VideoPlayer�ݒ芮��");
    }

    /// <summary>
    /// RenderTexture���쐬����
    /// </summary>
    private void CreateRenderTexture()
    {
        // ������RenderTexture������Δj��
        if (renderTexture != null)
        {
            renderTexture.Release();
        }

        // �V����RenderTexture���쐬 (�Q�[���̊�𑜓x�ɍ��킹��)
        renderTexture = new RenderTexture(1920, 1080, 0);
        renderTexture.Create();

        // VideoPlayer��RawImage�ɐݒ�
        if (videoPlayer != null)
            videoPlayer.targetTexture = renderTexture;

        if (videoDisplay != null)
            videoDisplay.texture = renderTexture;

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: RenderTexture�쐬����");
    }

    /// <summary>
    /// �F�����]�}�e���A��������������
    /// </summary>
    private void InitializeMaterial()
    {
        if (invertMaterial == null)
        {
            Debug.LogWarning($"{nameof(MonologueVideoController)}: �F�����]�}�e���A�����ݒ肳��Ă��܂���");
            return;
        }

        // �}�e���A���̃C���X�^���X���쐬
        materialInstance = new Material(invertMaterial);

        // RawImage�Ƀ}�e���A����K�p
        if (videoDisplay != null)
        {
            videoDisplay.material = materialInstance;
        }

        // �����v���p�e�B��ݒ�
        UpdateMaterialProperties();

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: �}�e���A������������");
    }

    #endregion

    #region ���搧�䃁�\�b�h

    /// <summary>
    /// �w�肳�ꂽ�C���f�b�N�X�̓�����Đ�����
    /// </summary>
    /// <param name="clipIndex">�Đ����铮��̃C���f�b�N�X</param>
    public void PlayVideo(int clipIndex)
    {
        if (videoClips == null || clipIndex < 0 || clipIndex >= videoClips.Length)
        {
            Debug.LogError($"{nameof(MonologueVideoController)}: �����ȓ���C���f�b�N�X: {clipIndex}");
            return;
        }

        currentClipIndex = clipIndex;
        videoPlayer.clip = videoClips[clipIndex];

        // �ŏ�����Đ����邩�A���[�v�J�n�ʒu����Đ����邩������
        if (playFromBeginning)
        {
            videoPlayer.time = 0;
            isInLoopRange = false;
        }
        else
        {
            videoPlayer.time = loopStartTime;
            isInLoopRange = true;
        }

        videoPlayer.Play();

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: ����Đ��J�n - {videoClips[clipIndex].name}");
    }

    /// <summary>
    /// ���݂̓�����~����
    /// </summary>
    public void StopVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            isInLoopRange = false;
        }

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: �����~");
    }

    /// <summary>
    /// ���̓�����Đ�����
    /// </summary>
    public void PlayNextVideo()
    {
        int nextIndex = (currentClipIndex + 1) % videoClips.Length;
        PlayVideo(nextIndex);
    }

    /// <summary>
    /// ����̕\��/��\����؂�ւ���
    /// </summary>
    /// <param name="visible">�\�����邩�ǂ���</param>
    public void SetVideoVisible(bool visible)
    {
        if (videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// ���[�v�͈͂��`�F�b�N���ď�������
    /// </summary>
    private void CheckLoopRange()
    {
        // ���[�v�I���ʒu�ɓ��B�����ꍇ
        if (videoPlayer.time >= loopEndTime)
        {
            // ��x�������������s���邽�߂̃t���O�`�F�b�N
            if (isInLoopRange)
            {
                isInLoopRange = false;

                // �Đ����~���Ă���ʒu��ύX
                videoPlayer.Pause();
                videoPlayer.time = loopStartTime;
                videoPlayer.Play();

                if (debugMode)
                    Debug.Log($"{nameof(MonologueVideoController)}: ���[�v�I���ʒu�ɓ��B�B�J�n�ʒu {loopStartTime}�b �ɖ߂�܂�");
            }
        }
        else if (!isInLoopRange && videoPlayer.time >= loopStartTime && videoPlayer.time < loopEndTime)
        {
            isInLoopRange = true;

            if (debugMode)
                Debug.Log($"{nameof(MonologueVideoController)}: ���[�v�͈͂ɓ���܂���");
        }
    }

    #endregion

    #region �F�����]���䃁�\�b�h

    /// <summary>
    /// �F�����]�̋��x��ݒ肷��
    /// </summary>
    /// <param name="amount">���]���x (0-1)</param>
    public void SetInvertAmount(float amount)
    {
        invertAmount = Mathf.Clamp01(amount);
        UpdateMaterialProperties();

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: �F�����]���x�ύX - {invertAmount}");
    }

    /// <summary>
    /// ���x��ݒ肷��
    /// </summary>
    /// <param name="brightnessValue">���x�l (0-2)</param>
    public void SetBrightness(float brightnessValue)
    {
        brightness = Mathf.Clamp(brightnessValue, 0f, 2f);
        UpdateMaterialProperties();
    }

    /// <summary>
    /// �R���g���X�g��ݒ肷��
    /// </summary>
    /// <param name="contrastValue">�R���g���X�g�l (0-2)</param>
    public void SetContrast(float contrastValue)
    {
        contrast = Mathf.Clamp(contrastValue, 0f, 2f);
        UpdateMaterialProperties();
    }

    /// <summary>
    /// �F�����]���A�j���[�V�����Ő؂�ւ���
    /// </summary>
    /// <param name="targetAmount">�ڕW���]���x</param>
    /// <param name="duration">�A�j���[�V��������</param>
    public void AnimateInvert(float targetAmount, float duration = 1f)
    {
        StartCoroutine(AnimateInvertCoroutine(targetAmount, duration));
    }

    /// <summary>
    /// �F�����]�A�j���[�V�����̃R���[�`��
    /// </summary>
    private IEnumerator AnimateInvertCoroutine(float targetAmount, float duration)
    {
        float startAmount = invertAmount;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // �X���[�Y�ȕ��
            float currentAmount = Mathf.Lerp(startAmount, targetAmount, progress);
            SetInvertAmount(currentAmount);

            yield return null;
        }

        // �ŏI�l��ݒ�
        SetInvertAmount(targetAmount);

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: �F�����]�A�j���[�V��������");
    }

    /// <summary>
    /// �}�e���A���v���p�e�B���X�V����
    /// </summary>
    private void UpdateMaterialProperties()
    {
        if (materialInstance == null) return;

        materialInstance.SetFloat(INVERT_AMOUNT_PROPERTY, invertAmount);
        materialInstance.SetFloat(BRIGHTNESS_PROPERTY, brightness);
        materialInstance.SetFloat(CONTRAST_PROPERTY, contrast);
    }

    #endregion

    #region �C�x���g�n���h���[

    /// <summary>
    /// ����̏����������������̏���
    /// </summary>
    /// <param name="vp">VideoPlayer</param>
    private void OnVideoPrepared(VideoPlayer vp)
    {
        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: ���揀������ - {vp.clip.name}");

        // ���[�v�J�n�ʒu����Đ�����ꍇ�̏���
        if (!playFromBeginning)
        {
            videoPlayer.time = loopStartTime;
            isInLoopRange = true;

            if (debugMode)
                Debug.Log($"{nameof(MonologueVideoController)}: ���[�v�J�n�ʒu {loopStartTime}�b ����Đ��J�n");
        }
    }

    /// <summary>
    /// �t���[���X�V���̏���
    /// </summary>
    /// <param name="vp">VideoPlayer</param>
    /// <param name="frameIdx">�t���[���C���f�b�N�X</param>
    private void OnVideoFrameReady(VideoPlayer vp, long frameIdx)
    {
        // �K�v�ɉ����ăt���[���P�ʂ̏�����ǉ�
    }

    /// <summary>
    /// ���悪�I���������̏���
    /// </summary>
    /// <param name="vp">VideoPlayer</param>
    private void OnVideoEnd(VideoPlayer vp)
    {
        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: ����I�� - {endAction}");

        switch (endAction)
        {
            case VideoEndAction.Stop:
                StopVideo();
                break;

            case VideoEndAction.Loop:
                // �J�X�^�����[�v����
                if (loopVideo)
                {
                    videoPlayer.time = loopStartTime;
                    videoPlayer.Play();
                    isInLoopRange = true;
                }
                break;

            case VideoEndAction.NextClip:
                PlayNextVideo();
                break;

            case VideoEndAction.HideVideo:
                SetVideoVisible(false);
                break;
        }
    }

    #endregion

    #region �N���[���A�b�v

    /// <summary>
    /// ���\�[�X�̃N���[���A�b�v
    /// </summary>
    private void CleanupResources()
    {
        // �C�x���g�̓o�^����
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoEnd;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.frameReady -= OnVideoFrameReady;
        }

        // RenderTexture�̉��
        if (renderTexture != null)
        {
            renderTexture.Release();
            renderTexture = null;
        }

        // �}�e���A���C���X�^���X�̔j��
        if (materialInstance != null)
        {
            DestroyImmediate(materialInstance);
            materialInstance = null;
        }

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: ���\�[�X�N���[���A�b�v����");
    }

    #endregion

    #region ���[�v�͈͐ݒ�

    /// <summary>
    /// ���[�v�͈͂�ݒ�
    /// </summary>
    /// <param name="startTime">�J�n���ԁi�b�j</param>
    /// <param name="endTime">�I�����ԁi�b�j</param>
    public void SetLoopRange(double startTime, double endTime)
    {
        loopStartTime = Mathf.Max(0, (float)startTime);
        loopEndTime = Mathf.Max((float)loopStartTime, (float)endTime);

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: ���[�v�͈͂� {loopStartTime}�b - {loopEndTime}�b �ɐݒ�");
    }

    #endregion
}