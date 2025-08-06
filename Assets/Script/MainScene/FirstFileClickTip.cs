using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// MainScene�ڍs��A�ŏ��ɃN���b�N���ׂ��t�@�C���Ɏ��o�I�q���g��񋟂���R���|�[�l���g
/// �t�@�C���ɓ_�Ō��ʂ�O���[���ʂ�ǉ����A�_�u���N���b�N��Ɏ����I�Ɍ��ʂ��I��
/// </summary>
public class FirstFileClickTip : MonoBehaviour
{
    // �C���X�y�N�^�[�ݒ�p�t�B�[���h
    [Header("�q���g�\���ݒ�")]
    [SerializeField] private bool enableTip = true; // �q���g�\���̗L��/����
    [SerializeField] private float blinkInterval = 2.5f; // �_�ŊԊu�i�b�j
    [SerializeField] private float blinkDuration = 1.0f; // �_�ŃA�j���[�V�������ԁi�b�j

    [Header("���o���ʐݒ�")]
    [SerializeField] private Color glowColor = new Color(1f, 1f, 0.7f, 1f); // �O���[�F�i�������F�j
    [SerializeField] private float minAlpha = 0.3f; // �ŏ������x
    [SerializeField] private float maxAlpha = 0.8f; // �ő哧���x
    [SerializeField] private AnimationCurve blinkCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // �_�ŃJ�[�u

    [Header("�O���[���ʐݒ�")]
    [SerializeField] private bool useOutlineEffect = true; // �A�E�g���C�����ʂ̎g�p
    [SerializeField] private float outlineWidth = 3f; // �A�E�g���C����

    // ������ԊǗ�
    private bool isEffectActive = false; // �G�t�F�N�g���A�N�e�B�u��
    private bool hasBeenClicked_FirstFileClickTip = false; // ��x�ł��_�u���N���b�N���ꂽ��
    private Coroutine blinkCoroutine; // �_�ŃR���[�`���̎Q��
    private Image targetImage; // �Ώۂ̉摜�R���|�[�l���g
    private Outline outlineComponent; // �A�E�g���C���R���|�[�l���g
    private Color originalColor; // ���̐F��ۑ�
    private FileOpen fileOpenComponent; // FileOpen�R���|�[�l���g

    // �N���b�N���o�p
    private float lastClickTime = 0f; // �Ō�̃N���b�N����
    private const float DOUBLE_CLICK_TIME = 0.3f; // �_�u���N���b�N���莞��

    // �萔��`
    private const string FIRST_FILE_NAME = "���߂Č���������.txt"; // �ŏ��ɃN���b�N���ׂ��t�@�C����

    /// <summary>
    /// ����������
    /// </summary>
    private void Awake()
    {
        // �K�v�ȃR���|�[�l���g���擾
        targetImage = GetComponent<Image>();
        fileOpenComponent = GetComponent<FileOpen>();

        if (targetImage == null)
        {
            Debug.LogWarning($"{nameof(FirstFileClickTip)}: Image �R���|�[�l���g��������܂���");
            enabled = false;
            return;
        }

        // ���̐F��ۑ�
        originalColor = targetImage.color;

        // �A�E�g���C���R���|�[�l���g�̐ݒ�
        if (useOutlineEffect)
        {
            SetupOutlineEffect();
        }
    }

    /// <summary>
    /// �J�n����
    /// </summary>
    private void Start()
    {
        // �q���g�\���������`�F�b�N
        if (!ShouldShowTip())
        {
            enabled = false;
            return;
        }

        // ���ʂ��J�n
        StartEffect();
    }

    /// <summary>
    /// �X�V�����i�N���b�N���o�p�j
    /// </summary>
    private void Update()
    {
        if (!isEffectActive || hasBeenClicked_FirstFileClickTip) return;

        // �}�E�X�N���b�N�����o
        if (Input.GetMouseButtonDown(0))
        {
            // ���̃I�u�W�F�N�g��ŃN���b�N���ꂽ���`�F�b�N
            if (IsPointerOverGameObject())
            {
                CheckForDoubleClick();
            }
        }
    }

    /// <summary>
    /// �}�E�X�|�C���^�����̃I�u�W�F�N�g��ɂ��邩�`�F�b�N
    /// </summary>
    private bool IsPointerOverGameObject()
    {
        // EventSystem���g�p���ă��C�L���X�g�����s
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject == gameObject)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// �_�u���N���b�N���`�F�b�N
    /// </summary>
    private void CheckForDoubleClick()
    {
        float currentTime = Time.time;

        // �O��̃N���b�N����̌o�ߎ��Ԃ��`�F�b�N
        if (currentTime - lastClickTime < DOUBLE_CLICK_TIME)
        {
            // �_�u���N���b�N���o
            OnFileDoubleClicked();
        }

        lastClickTime = currentTime;
    }

    /// <summary>
    /// �q���g��\�����ׂ�������
    /// </summary>
    private bool ShouldShowTip()
    {
        // �q���g�������̏ꍇ
        if (!enableTip) return false;

        // �Ώۃt�@�C�����łȂ��ꍇ
        if (!gameObject.name.Contains(FIRST_FILE_NAME)) return false;

        // ���łɃq���g��\���ς݂̏ꍇ�i�Z�[�u�f�[�^�ŊǗ��j
        if (GameSaveManager.Instance != null)
        {
            var saveData = GameSaveManager.Instance.GetCurrentSaveData();
            if (saveData != null)
            {
                // firstFileTipShown�t�B�[���h�����݂���ꍇ�̃`�F�b�N
                // ��: GameSaveData�N���X�ɂ��̃t�B�[���h��ǉ�����K�v������܂�
                try
                {
                    var field = saveData.GetType().GetField("firstFileTipShown");
                    if (field != null && field.GetValue(saveData) is bool shown && shown)
                    {
                        return false;
                    }
                }
                catch
                {
                    // �t�B�[���h�����݂��Ȃ��ꍇ�͕\������
                }
            }
        }

        return true;
    }

    /// <summary>
    /// �A�E�g���C�����ʂ̐ݒ�
    /// </summary>
    private void SetupOutlineEffect()
    {
        // Outline�R���|�[�l���g��ǉ��܂��͎擾
        outlineComponent = GetComponent<Outline>();
        if (outlineComponent == null)
        {
            outlineComponent = gameObject.AddComponent<Outline>();
        }

        // �A�E�g���C���̐ݒ�
        outlineComponent.effectColor = glowColor;
        outlineComponent.effectDistance = new Vector2(outlineWidth, outlineWidth);
        outlineComponent.useGraphicAlpha = false;
        outlineComponent.enabled = false; // ������Ԃł͖���
    }

    /// <summary>
    /// ���ʂ��J�n
    /// </summary>
    private void StartEffect()
    {
        if (isEffectActive) return;

        isEffectActive = true;
        blinkCoroutine = StartCoroutine(BlinkEffect());

        Debug.Log($"{nameof(FirstFileClickTip)}: �q���g���ʂ��J�n���܂��� - {gameObject.name}");
    }

    /// <summary>
    /// ���ʂ��~
    /// </summary>
    private void StopEffect()
    {
        if (!isEffectActive) return;

        isEffectActive = false;

        // �R���[�`�����~
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        // ���̏�Ԃɖ߂�
        if (targetImage != null)
        {
            targetImage.color = originalColor;
        }

        // �A�E�g���C���𖳌���
        if (outlineComponent != null)
        {
            outlineComponent.enabled = false;
        }

        // �Z�[�u�f�[�^�ɋL�^
        if (GameSaveManager.Instance != null)
        {
            var saveData = GameSaveManager.Instance.GetCurrentSaveData();
            if (saveData != null)
            {
                // firstFileTipShown�t�B�[���h�ɒl��ݒ�
                // ��: GameSaveData�N���X�ɂ��̃t�B�[���h��ǉ�����K�v������܂�
                try
                {
                    var field = saveData.GetType().GetField("firstFileTipShown");
                    if (field != null)
                    {
                        field.SetValue(saveData, true);
                        GameSaveManager.Instance.SaveGame();
                    }
                }
                catch
                {
                    // �t�B�[���h�����݂��Ȃ��ꍇ�͉������Ȃ�
                }
            }
        }

        Debug.Log($"{nameof(FirstFileClickTip)}: �q���g���ʂ��~���܂��� - {gameObject.name}");

        // �R���|�[�l���g�𖳌���
        enabled = false;
    }

    /// <summary>
    /// �_�Ō��ʂ̃R���[�`��
    /// </summary>
    private IEnumerator BlinkEffect()
    {
        while (isEffectActive)
        {
            // �_�ŃA�j���[�V����
            float elapsedTime = 0f;

            // �t�F�[�h�C��
            while (elapsedTime < blinkDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / blinkDuration;
                float curveValue = blinkCurve.Evaluate(normalizedTime);

                // �O���[�F�Ƃ̐��`���
                Color currentColor = Color.Lerp(originalColor, glowColor, curveValue * 0.5f);
                currentColor.a = Mathf.Lerp(minAlpha, maxAlpha, curveValue);

                if (targetImage != null)
                {
                    targetImage.color = currentColor;
                }

                // �A�E�g���C���̓����x���ύX
                if (outlineComponent != null)
                {
                    outlineComponent.enabled = true;
                    Color outlineColor = glowColor;
                    outlineColor.a = curveValue * maxAlpha;
                    outlineComponent.effectColor = outlineColor;
                }

                yield return null;
            }

            // �t�F�[�h�A�E�g
            elapsedTime = 0f;
            while (elapsedTime < blinkDuration)
            {
                elapsedTime += Time.deltaTime;
                float normalizedTime = elapsedTime / blinkDuration;
                float curveValue = blinkCurve.Evaluate(1f - normalizedTime);

                // ���̐F�ɖ߂�
                Color currentColor = Color.Lerp(originalColor, glowColor, curveValue * 0.5f);
                currentColor.a = Mathf.Lerp(minAlpha, maxAlpha, curveValue);

                if (targetImage != null)
                {
                    targetImage.color = currentColor;
                }

                // �A�E�g���C���̓����x���ύX
                if (outlineComponent != null)
                {
                    Color outlineColor = glowColor;
                    outlineColor.a = curveValue * maxAlpha;
                    outlineComponent.effectColor = outlineColor;
                }

                yield return null;
            }

            // ���̏�Ԃɖ߂�
            if (targetImage != null)
            {
                targetImage.color = originalColor;
            }

            if (outlineComponent != null)
            {
                outlineComponent.enabled = false;
            }

            // �C���^�[�o���ҋ@
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    /// <summary>
    /// �t�@�C�����_�u���N���b�N���ꂽ���̏���
    /// </summary>
    private void OnFileDoubleClicked()
    {
        if (hasBeenClicked_FirstFileClickTip) return;

        hasBeenClicked_FirstFileClickTip = true;
        Debug.Log($"{nameof(FirstFileClickTip)}: �t�@�C�����_�u���N���b�N����܂��� - {gameObject.name}");

        // ���ʂ��~
        StopEffect();
    }

    /// <summary>
    /// �R���|�[�l���g�����������ꂽ���̏���
    /// </summary>
    private void OnDisable()
    {
        // �N���[���A�b�v
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        // ���̏�Ԃɖ߂�
        if (targetImage != null)
        {
            targetImage.color = originalColor;
        }

        if (outlineComponent != null)
        {
            outlineComponent.enabled = false;
        }
    }

    /// <summary>
    /// �G�f�B�^�p�F���ʂ��e�X�g
    /// </summary>
    [ContextMenu("Test Effect")]
    private void TestEffect()
    {
        if (Application.isPlaying)
        {
            if (isEffectActive)
            {
                StopEffect();
            }
            else
            {
                StartEffect();
            }
        }
    }
}