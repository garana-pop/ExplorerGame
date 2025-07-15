using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// TitleTextChanger��hasChanged��true�ɂȂ����Ƃ���
/// �u�v���o���{�^���v��Alpha�l�𓮓I�ɕϓ�������R���|�[�l���g
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class RememberButtonAlphaAnimator : MonoBehaviour
{
    [Header("�Q�Ɛݒ�")]
    [Tooltip("�Ď�����TitleTextChanger�R���|�[�l���g")]
    [SerializeField] private TitleTextChanger titleTextChanger;

    [Tooltip("�A�j���[�V�����Ώۂ�CanvasGroup�i���ݒ�̏ꍇ�͎����擾�j")]
    [SerializeField] private CanvasGroup targetCanvasGroup;

    [Header("�A�j���[�V�����ݒ�")]
    [Tooltip("�A�j���[�V�����̎��")]
    [SerializeField] private AnimationType animationType = AnimationType.Pulse;

    [Tooltip("�ŏ�Alpha�l")]
    [SerializeField][Range(0f, 1f)] private float minAlpha = 0.3f;

    [Tooltip("�ő�Alpha�l")]
    [SerializeField][Range(0f, 1f)] private float maxAlpha = 1.0f;

    [Tooltip("�A�j���[�V�������x")]
    [SerializeField] private float animationSpeed = 2.0f;

    [Tooltip("�p���X�A�j���[�V�����̊��炩��")]
    [SerializeField] private AnimationCurve pulseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("�t�F�[�h�C���ݒ�")]
    [Tooltip("�t�F�[�h�C�����g�p���邩")]
    [SerializeField] private bool useFadeIn = true;

    [Tooltip("�t�F�[�h�C�����ԁi�b�j")]
    [SerializeField] private float fadeInDuration = 1.0f;

    [Tooltip("�t�F�[�h�C���J�n�O�̒x���i�b�j")]
    [SerializeField] private float fadeInDelay = 0.5f;

    [Header("�t���O�Ď��ݒ�")]
    [Tooltip("�t���O�ύX���`�F�b�N����Ԋu�i�b�j")]
    [SerializeField] private float flagCheckInterval = 0.1f;

    [Tooltip("TitleTextChanger�̕ύX�������Ď����邩")]
    [SerializeField] private bool monitorTextChangerCompletion = true;

    [Tooltip("�V�[���J�ڎ��̓��ʏ�����L���ɂ��邩")]
    [SerializeField] private bool enableSceneTransitionDetection = true;

    [Header("�����_���ݒ�")]
    [Tooltip("�����_���ȕϓ���ǉ����邩")]
    [SerializeField] private bool useRandomVariation = false;

    [Tooltip("�����_���ϓ��̋��x")]
    [SerializeField][Range(0f, 0.5f)] private float randomVariationStrength = 0.1f;

    [Header("�G�t�F�N�g�ݒ�")]
    [Tooltip("�O���[���ʂ�ǉ����邩")]
    [SerializeField] private bool useGlowEffect = false;

    [Tooltip("�O���[���ʂ̑Ώ�Image")]
    [SerializeField] private Image glowImage;

    [Tooltip("�O���[���ʂ̐F")]
    [SerializeField] private Color glowColor = new Color(1f, 1f, 1f, 0.5f);

    [Header("�f�o�b�O�ݒ�")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceStart = false; // �e�X�g�p�̋����J�n

    // �A�j���[�V�����^�C�v
    public enum AnimationType
    {
        Pulse,          // ����
        Breathe,        // �ċz�̂悤�ȓ���
        Flicker,        // �_��
        Wave,           // �g�̂悤�ȓ���
        Heartbeat       // �S���̂悤�ȓ���
    }

    // �v���C�x�[�g�ϐ�
    private bool isAnimating = false;
    private Coroutine animationCoroutine;
    private Coroutine flagMonitorCoroutine;
    private float currentAlpha;
    private Button targetButton;
    private TMP_Text buttonText;

    // �t���O�Ď��p
    private bool lastAfterChangeFlag = false;
    private bool lastTextChangerFlag = false;
    private bool hasStartedAnimation = false;

    private void Awake()
    {
        // CanvasGroup�̎擾�܂��͍쐬
        if (targetCanvasGroup == null)
        {
            targetCanvasGroup = GetComponent<CanvasGroup>();
            if (targetCanvasGroup == null)
            {
                targetCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        // TitleTextChanger�̎�������
        if (titleTextChanger == null)
        {
            titleTextChanger = FindFirstObjectByType<TitleTextChanger>();
            if (titleTextChanger == null && !forceStart)
            {
                Debug.LogError("RememberButtonAlphaAnimator: TitleTextChanger��������܂���B");
                enabled = false;
                return;
            }
        }

        // Button�R���|�[�l���g�̎擾
        targetButton = GetComponent<Button>();

        // TextMeshPro�R���|�[�l���g�̎擾
        buttonText = GetComponentInChildren<TMP_Text>();

        // ����Alpha�l��ݒ�
        currentAlpha = targetCanvasGroup.alpha;

        if (debugMode)
        {
            Debug.Log("RememberButtonAlphaAnimator: ����������");
        }
    }

    private void Start()
    {
        // �����t���O��Ԃ��L�^
        lastAfterChangeFlag = GetAfterChangeToHerMemoryFlag();
        lastTextChangerFlag = GetTitleTextChangerFlag();

        if (debugMode)
        {
            Debug.Log($"RememberButtonAlphaAnimator: �����t���O��� - AfterChange: {lastAfterChangeFlag}, TextChanger: {lastTextChangerFlag}");
        }

        // �����J�n�܂��͂��łɃt���O��true�̏ꍇ�̓A�j���[�V�����J�n
        if (forceStart || lastAfterChangeFlag || lastTextChangerFlag)
        {
            if (debugMode)
            {
                string reason = forceStart ? "�����J�n" :
                               lastAfterChangeFlag ? "AfterChange�t���O��true" : "TextChanger�t���O��true";
                Debug.Log($"RememberButtonAlphaAnimator: {reason}�̂��߃A�j���[�V�����J�n");
            }

            StartAnimation();
        }

        // �t���O�Ď����J�n
        StartFlagMonitoring();
    }

    /// <summary>
    /// �t���O�̕ύX���p���I�ɊĎ�����
    /// </summary>
    private void StartFlagMonitoring()
    {
        if (flagMonitorCoroutine != null)
        {
            StopCoroutine(flagMonitorCoroutine);
        }

        flagMonitorCoroutine = StartCoroutine(MonitorFlags());
    }

    /// <summary>
    /// �t���O�Ď��p�R���[�`��
    /// </summary>
    private IEnumerator MonitorFlags()
    {
        while (enabled)
        {
            // AfterChangeToHerMemory�t���O���`�F�b�N
            bool currentAfterChangeFlag = GetAfterChangeToHerMemoryFlag();
            bool currentTextChangerFlag = GetTitleTextChangerFlag();

            // �t���O�� false ���� true �ɕς�����ꍇ
            if (!hasStartedAnimation && !isAnimating)
            {
                bool shouldStart = false;
                string reason = "";

                if (!lastAfterChangeFlag && currentAfterChangeFlag)
                {
                    shouldStart = true;
                    reason = "AfterChange�t���O��false����true�ɕύX";
                }
                else if (monitorTextChangerCompletion && !lastTextChangerFlag && currentTextChangerFlag)
                {
                    shouldStart = true;
                    reason = "TextChanger�t���O��false����true�ɕύX";
                }
                else if (currentAfterChangeFlag || currentTextChangerFlag)
                {
                    shouldStart = true;
                    reason = "�t���O��true�̏�Ԃ����o";
                }

                if (shouldStart)
                {
                    if (debugMode)
                    {
                        Debug.Log($"RememberButtonAlphaAnimator: {reason} - �A�j���[�V�����J�n");
                    }

                    hasStartedAnimation = true;
                    StartAnimation();
                }
            }

            // �O��̏�Ԃ��X�V
            lastAfterChangeFlag = currentAfterChangeFlag;
            lastTextChangerFlag = currentTextChangerFlag;

            yield return new WaitForSeconds(flagCheckInterval);
        }
    }

    /// <summary>
    /// AfterChangeToHerMemory�t���O�̏�Ԃ��擾
    /// </summary>
    private bool GetAfterChangeToHerMemoryFlag()
    {
        // �����J�n�t���O�i�e�X�g�p�j
        if (forceStart)
        {
            return true;
        }

        // GameSaveManager����擾�i�ŗD��j
        if (GameSaveManager.Instance != null)
        {
            return GameSaveManager.Instance.GetAfterChangeToHerMemoryFlag();
        }

        // GameSaveManager�����݂��Ȃ��ꍇ��false
        return false;
    }

    /// <summary>
    /// TitleTextChanger��hasChanged�t���O���擾
    /// </summary>
    private bool GetTitleTextChangerFlag()
    {
        if (!monitorTextChangerCompletion || titleTextChanger == null)
        {
            return false;
        }

        return titleTextChanger.HasChanged;
    }

    /// <summary>
    /// �A�j���[�V�������J�n
    /// </summary>
    public void StartAnimation()
    {
        if (isAnimating) return;

        if (debugMode)
        {
            Debug.Log("RememberButtonAlphaAnimator: �A�j���[�V�����J�n");
        }

        isAnimating = true;
        hasStartedAnimation = true;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(AnimateAlpha());
    }

    /// <summary>
    /// �A�j���[�V�������~
    /// </summary>
    public void StopAnimation()
    {
        if (!isAnimating) return;

        if (debugMode)
        {
            Debug.Log("RememberButtonAlphaAnimator: �A�j���[�V������~");
        }

        isAnimating = false;

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }

        // Alpha�l���ő�l�ɖ߂�
        targetCanvasGroup.alpha = maxAlpha;
    }

    /// <summary>
    /// Alpha�l�A�j���[�V�����R���[�`��
    /// </summary>
    private IEnumerator AnimateAlpha()
    {
        // �t�F�[�h�C������
        if (useFadeIn)
        {
            yield return StartCoroutine(FadeIn());
        }

        // ���C���A�j���[�V�������[�v
        float time = 0f;

        while (isAnimating)
        {
            time += Time.deltaTime * animationSpeed;

            // �A�j���[�V�����^�C�v�ɉ�����Alpha�l���v�Z
            float alpha = CalculateAlpha(time);

            // �����_���ϓ���ǉ�
            if (useRandomVariation)
            {
                alpha += Random.Range(-randomVariationStrength, randomVariationStrength);
                alpha = Mathf.Clamp(alpha, minAlpha, maxAlpha);
            }

            // Alpha�l��K�p
            targetCanvasGroup.alpha = alpha;
            currentAlpha = alpha;

            // �O���[���ʂ̍X�V
            if (useGlowEffect && glowImage != null)
            {
                UpdateGlowEffect(alpha);
            }

            yield return null;
        }
    }

    /// <summary>
    /// �t�F�[�h�C������
    /// </summary>
    private IEnumerator FadeIn()
    {
        if (fadeInDelay > 0)
        {
            yield return new WaitForSeconds(fadeInDelay);
        }

        targetCanvasGroup.alpha = 0f;
        float elapsedTime = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeInDuration;
            targetCanvasGroup.alpha = Mathf.Lerp(0f, maxAlpha, t);

            yield return null;
        }

        targetCanvasGroup.alpha = maxAlpha;
    }

    /// <summary>
    /// �A�j���[�V�����^�C�v�ɉ�����Alpha�l���v�Z
    /// </summary>
    private float CalculateAlpha(float time)
    {
        float normalizedValue = 0f;

        switch (animationType)
        {
            case AnimationType.Pulse:
                // �T�C���g���g��������
                normalizedValue = (Mathf.Sin(time) + 1f) * 0.5f;
                normalizedValue = pulseCurve.Evaluate(normalizedValue);
                break;

            case AnimationType.Breathe:
                // ��莩�R�Ȍċz�̂悤�ȓ���
                normalizedValue = Mathf.Sin(time * 0.5f) * 0.5f + 0.5f;
                normalizedValue = Mathf.Pow(normalizedValue, 2.2f); // �K���}�␳��
                break;

            case AnimationType.Flicker:
                // �_�Ō���
                normalizedValue = Mathf.PingPong(time * 2f, 1f);
                if (normalizedValue > 0.9f) normalizedValue = 1f;
                else if (normalizedValue < 0.1f) normalizedValue = 0f;
                break;

            case AnimationType.Wave:
                // �g�̂悤�ȓ���
                float wave1 = Mathf.Sin(time) * 0.5f;
                float wave2 = Mathf.Sin(time * 1.5f) * 0.3f;
                float wave3 = Mathf.Sin(time * 2.1f) * 0.2f;
                normalizedValue = (wave1 + wave2 + wave3 + 1f) * 0.5f;
                break;

            case AnimationType.Heartbeat:
                // �S���̂悤�ȓ���
                float beat = time % 2f;
                if (beat < 0.1f)
                    normalizedValue = 1f;
                else if (beat < 0.3f)
                    normalizedValue = 0.7f;
                else if (beat < 0.4f)
                    normalizedValue = 1f;
                else
                    normalizedValue = 0.5f;
                break;
        }

        // �ŏ��l�ƍő�l�̊Ԃŕ��
        return Mathf.Lerp(minAlpha, maxAlpha, normalizedValue);
    }

    /// <summary>
    /// �O���[���ʂ��X�V
    /// </summary>
    private void UpdateGlowEffect(float alpha)
    {
        if (glowImage == null) return;

        // Alpha�l�ɉ����ăO���[�̋��x��ύX
        Color color = glowColor;
        color.a = glowColor.a * (alpha - minAlpha) / (maxAlpha - minAlpha);
        glowImage.color = color;
    }

    /// <summary>
    /// �O������A�j���[�V�����J�n����������i�f�o�b�O�p�j
    /// </summary>
    [ContextMenu("Debug: Force Start Animation")]
    public void ForceStartAnimation()
    {
        hasStartedAnimation = false;
        StartAnimation();
    }

    /// <summary>
    /// ���݂̃t���O��Ԃ�\���i�f�o�b�O�p�j
    /// </summary>
    [ContextMenu("Debug: Show Flag Status")]
    public void ShowFlagStatus()
    {
        bool afterChangeFlag = GetAfterChangeToHerMemoryFlag();
        bool textChangerFlag = GetTitleTextChangerFlag();

        Debug.Log($"=== RememberButtonAlphaAnimator �t���O��� ===");
        Debug.Log($"AfterChangeToHerMemory: {afterChangeFlag}");
        Debug.Log($"TitleTextChanger HasChanged: {textChangerFlag}");
        Debug.Log($"�A�j���[�V������: {isAnimating}");
        Debug.Log($"�J�n�ς�: {hasStartedAnimation}");
        Debug.Log($"=====================================");
    }

    /// <summary>
    /// �G�f�B�^�p�F�A�j���[�V�����^�C�v��ύX
    /// </summary>
    public void SetAnimationType(AnimationType type)
    {
        animationType = type;
    }

    /// <summary>
    /// �G�f�B�^�p�F���x��ύX
    /// </summary>
    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = Mathf.Max(0.1f, speed);
    }

    /// <summary>
    /// ���݂̃A�j���[�V������Ԃ��擾
    /// </summary>
    public bool IsAnimating => isAnimating;

    private void OnDestroy()
    {
        StopAnimation();
        if (flagMonitorCoroutine != null)
        {
            StopCoroutine(flagMonitorCoroutine);
        }
    }

    private void OnDisable()
    {
        StopAnimation();
        if (flagMonitorCoroutine != null)
        {
            StopCoroutine(flagMonitorCoroutine);
        }
    }

    private void OnEnable()
    {
        // �L�������Ƀt���O���`�F�b�N
        if (!hasStartedAnimation)
        {
            bool shouldAnimate = GetAfterChangeToHerMemoryFlag() || GetTitleTextChangerFlag();

            if (shouldAnimate && !isAnimating)
            {
                if (debugMode)
                    Debug.Log("RememberButtonAlphaAnimator: OnEnable - �t���O��true�̂��߃A�j���[�V�����J�n");

                StartAnimation();
            }
        }

        // �t���O�Ď����ĊJ
        StartFlagMonitoring();
    }
}