using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// �t�@�C�����_�u���N���b�N�ŊJ�����Ƃ���������q���g�\���R���|�[�l���g
/// FirstFileClickTip�N���X�ƘA�g���ē���
/// </summary>
public class FirstFileOpenTip : MonoBehaviour, IPointerClickHandler
{
    [Header("�q���g�\���ݒ�")]
    [SerializeField] private bool enableTip = true; // �q���g�\���̗L��/����
    [SerializeField] private GameObject tipObject; // DraggingCanvas�Ɉړ�����q���g�I�u�W�F�N�g
    [SerializeField] private Transform draggingCanvas; // DraggingCanvas�ւ̎Q��
    [SerializeField] private Vector3 tipOffset = new Vector3(50f, -50f, 0f); // �t�@�C������̑��Έʒu

    [Header("�\���^�C�~���O�ݒ�")]
    [SerializeField] private float displayDelay = 0.5f; // �V���O���N���b�N��̕\���x��
    [SerializeField] private float autoHideTime = 5.0f; // ������\���܂ł̎��ԁi0�Ŗ����j

    [Header("���o�ݒ�")]
    [SerializeField] private bool fadeInEffect = true; // �t�F�[�h�C�����ʂ̎g�p
    [SerializeField] private float fadeInDuration = 0.3f; // �t�F�[�h�C������
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("�f�o�b�O�ݒ�")]
    [SerializeField] private bool debugMode = false; // �f�o�b�O���O�̕\��

    // ������ԊǗ�
    private bool isShowingTip = false; // �q���g���\������
    private bool hasBeenDoubleClicked = false; // �_�u���N���b�N�ς݂�
    private float lastClickTime = 0f; // �Ō�̃N���b�N����
    private Coroutine showTipCoroutine; // �q���g�\���R���[�`��
    private Coroutine autoHideCoroutine; // ������\���R���[�`��
    private FirstFileClickTip clickTipComponent; // FirstFileClickTip�R���|�[�l���g�ւ̎Q��
    private CanvasGroup tipCanvasGroup; // �q���g��CanvasGroup
    private Vector3 originalTipPosition; // �q���g�̌��̈ʒu
    private Transform originalTipParent; // �q���g�̌��̐e

    // �萔��`
    private const float DOUBLE_CLICK_TIME = 0.3f; // �_�u���N���b�N���莞��
    private const string DRAGGING_CANVAS_NAME = "DraggingCanvas"; // DraggingCanvas�̖��O

    /// <summary>
    /// ����������
    /// </summary>
    private void Awake()
    {
        // FirstFileClickTip�R���|�[�l���g�̎擾
        clickTipComponent = GetComponent<FirstFileClickTip>();

        // DraggingCanvas�̎�������
        if (draggingCanvas == null)
        {
            GameObject draggingCanvasObj = GameObject.Find(DRAGGING_CANVAS_NAME);
            if (draggingCanvasObj != null)
            {
                draggingCanvas = draggingCanvasObj.transform;
            }
            else
            {
                Debug.LogWarning($"{nameof(FirstFileOpenTip)}: DraggingCanvas��������܂���");
                enabled = false;
                return;
            }
        }

        // �q���g�I�u�W�F�N�g�̐ݒ�
        if (tipObject != null)
        {
            // ���̈ʒu�Ɛe��ۑ�
            originalTipPosition = tipObject.transform.position;
            originalTipParent = tipObject.transform.parent;

            // CanvasGroup�ݒ�
            tipCanvasGroup = tipObject.GetComponent<CanvasGroup>();
            if (tipCanvasGroup == null && fadeInEffect)
            {
                tipCanvasGroup = tipObject.AddComponent<CanvasGroup>();
            }

            // ������ԂŔ�\��
            HideTip(true);
        }
        else
        {
            Debug.LogWarning($"{nameof(FirstFileOpenTip)}: �q���g�I�u�W�F�N�g���ݒ肳��Ă��܂���");
            enabled = false;
        }
    }

    /// <summary>
    /// IPointerClickHandler�̎���
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!enableTip || hasBeenDoubleClicked) return;

        float currentTime = Time.time;

        // �_�u���N���b�N����
        if (currentTime - lastClickTime < DOUBLE_CLICK_TIME)
        {
            OnDoubleClick();
        }
        else
        {
            // �V���O���N���b�N�̏���
            OnSingleClick();
        }

        lastClickTime = currentTime;
    }

    /// <summary>
    /// �V���O���N���b�N���̏���
    /// </summary>
    private void OnSingleClick()
    {
        // FirstFileClickTip��hasBeenClicked_FirstFileClickTip�t���O���`�F�b�N
        if (clickTipComponent == null || ShouldShowTip())
        {
            // ���Ƀq���g�\�����̏ꍇ�̓L�����Z��
            if (showTipCoroutine != null)
            {
                StopCoroutine(showTipCoroutine);
            }

            // �q���g�\���R���[�`�����J�n
            showTipCoroutine = StartCoroutine(ShowTipAfterDelay());
        }
    }

    /// <summary>
    /// �_�u���N���b�N���̏���
    /// </summary>
    private void OnDoubleClick()
    {
        hasBeenDoubleClicked = true;

        // �q���g�\�����L�����Z��
        if (showTipCoroutine != null)
        {
            StopCoroutine(showTipCoroutine);
            showTipCoroutine = null;
        }

        // �q���g���\�����Ȃ��\���ɂ���
        if (isShowingTip)
        {
            HideTip(false);
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(FirstFileOpenTip)}: �_�u���N���b�N�����o - {gameObject.name}");
        }
    }

    /// <summary>
    /// �q���g�\�������̔���
    /// </summary>
    private bool ShouldShowTip()
    {
        // FirstFileClickTip��hasBeenClicked_FirstFileClickTip�t���O��false�̏ꍇ�̂ݕ\��
        if (clickTipComponent != null)
        {
            // ���t���N�V�������g�p����private�t�B�[���h�ɃA�N�Z�X
            var fieldInfo = clickTipComponent.GetType().GetField("hasBeenClicked_FirstFileClickTip",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (fieldInfo != null)
            {
                bool hasBeenClicked_FirstFileClickTip = (bool)fieldInfo.GetValue(clickTipComponent);
                return !hasBeenClicked_FirstFileClickTip;
            }
        }

        return true; // FirstFileClickTip���Ȃ��ꍇ�̓f�t�H���g�ŕ\��
    }

    /// <summary>
    /// �x����Ƀq���g��\������R���[�`��
    /// </summary>
    private IEnumerator ShowTipAfterDelay()
    {
        yield return new WaitForSeconds(displayDelay);

        // �ēx�������`�F�b�N
        if (!hasBeenDoubleClicked && ShouldShowTip())
        {
            ShowTip();
        }
    }

    /// <summary>
    /// �q���g��\��
    /// </summary>
    private void ShowTip()
    {
        if (tipObject == null || isShowingTip) return;

        isShowingTip = true;

        // DraggingCanvas�Ɉړ�
        tipObject.transform.SetParent(draggingCanvas);

        // �ʒu��ݒ�i�t�@�C���̋߂��ɔz�u�j
        Vector3 worldPos = transform.position + tipOffset;
        tipObject.transform.position = worldPos;

        // �A�N�e�B�u��
        tipObject.SetActive(true);

        // �t�F�[�h�C������
        if (fadeInEffect && tipCanvasGroup != null)
        {
            StartCoroutine(FadeIn());
        }

        // ������\���̐ݒ�
        if (autoHideTime > 0)
        {
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
            }
            autoHideCoroutine = StartCoroutine(AutoHide());
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(FirstFileOpenTip)}: �q���g��\�����܂��� - {gameObject.name}");
        }
    }

    /// <summary>
    /// �q���g���\��
    /// </summary>
    private void HideTip(bool immediate)
    {
        if (tipObject == null) return;

        isShowingTip = false;

        // �R���[�`���̒�~
        if (showTipCoroutine != null)
        {
            StopCoroutine(showTipCoroutine);
            showTipCoroutine = null;
        }

        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        if (immediate)
        {
            // �����ɔ�\��
            tipObject.SetActive(false);

            // ���̐e�ɖ߂�
            tipObject.transform.SetParent(originalTipParent);
            tipObject.transform.position = originalTipPosition;
        }
        else
        {
            // �t�F�[�h�A�E�g����
            if (fadeInEffect && tipCanvasGroup != null)
            {
                StartCoroutine(FadeOut());
            }
            else
            {
                tipObject.SetActive(false);
                tipObject.transform.SetParent(originalTipParent);
                tipObject.transform.position = originalTipPosition;
            }
        }

        if (debugMode)
        {
            Debug.Log($"{nameof(FirstFileOpenTip)}: �q���g���\���ɂ��܂��� - {gameObject.name}");
        }
    }

    /// <summary>
    /// �t�F�[�h�C������
    /// </summary>
    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        tipCanvasGroup.alpha = 0f;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / fadeInDuration;
            tipCanvasGroup.alpha = fadeInCurve.Evaluate(normalizedTime);
            yield return null;
        }

        tipCanvasGroup.alpha = 1f;
    }

    /// <summary>
    /// �t�F�[�h�A�E�g����
    /// </summary>
    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        float startAlpha = tipCanvasGroup.alpha;

        while (elapsedTime < fadeInDuration)
        {
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / fadeInDuration;
            tipCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, normalizedTime);
            yield return null;
        }

        tipCanvasGroup.alpha = 0f;
        tipObject.SetActive(false);

        // ���̐e�ɖ߂�
        tipObject.transform.SetParent(originalTipParent);
        tipObject.transform.position = originalTipPosition;
    }

    /// <summary>
    /// ������\���R���[�`��
    /// </summary>
    private IEnumerator AutoHide()
    {
        yield return new WaitForSeconds(autoHideTime);
        HideTip(false);
    }

    /// <summary>
    /// FirstFileClickTip��hasBeenClicked_FirstFileClickTip�t���O���Ď�
    /// </summary>
    private void Update()
    {
        if (!isShowingTip || clickTipComponent == null) return;

        // FirstFileClickTip��hasBeenClicked_FirstFileClickTip��true�ɂȂ������\��
        var fieldInfo = clickTipComponent.GetType().GetField("hasBeenClicked_FirstFileClickTip",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (fieldInfo != null)
        {
            bool hasBeenClicked_FirstFileClickTip = (bool)fieldInfo.GetValue(clickTipComponent);
            if (hasBeenClicked_FirstFileClickTip)
            {
                HideTip(false);
            }
        }
    }

    /// <summary>
    /// �R���|�[�l���g���������̏���
    /// </summary>
    private void OnDisable()
    {
        // �N���[���A�b�v
        if (showTipCoroutine != null)
        {
            StopCoroutine(showTipCoroutine);
            showTipCoroutine = null;
        }

        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        // �q���g���\��
        if (isShowingTip)
        {
            HideTip(true);
        }
    }

    /// <summary>
    /// �G�f�B�^�p�F�q���g�\�����e�X�g
    /// </summary>
    [ContextMenu("Test Show Tip")]
    private void TestShowTip()
    {
        if (Application.isPlaying)
        {
            if (isShowingTip)
            {
                HideTip(false);
            }
            else
            {
                ShowTip();
            }
        }
    }
}