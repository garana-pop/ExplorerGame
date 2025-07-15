using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class BlinkEffect : MonoBehaviour
{
    [Header("�܂Ԃ��̐ݒ�")]
    [SerializeField] private RectTransform topEyelid;    // ��܂Ԃ�
    [SerializeField] private RectTransform bottomEyelid; // ���܂Ԃ�
    [SerializeField] private float closedPosition = 0f;  // �ڂ�������̈ʒu�i��ʒ����j

    [Header("�܂΂����̃^�C�~���O")]
    [SerializeField] private float minBlinkInterval = 5f;    // �ŏ��܂΂����Ԋu
    [SerializeField] private float maxBlinkInterval = 10f;   // �ő�܂΂����Ԋu
    [SerializeField] private float blinkDuration = 0.2f;     // �܂΂���1��̏��v����
    [SerializeField] private float initialBlinkDelay = 1f;   // �����x��

    [Header("�N�����̐ݒ�")]
    [SerializeField] private bool startWithClosed = true;    // �ڂ������Ԃ���J�n���邩
    [SerializeField] private float openingDuration = 1.0f;   // �N�����ɖڂ��J������

    // �v���C�x�[�g�ϐ�
    private float topEyelidOpenPosition;    // ��܂Ԃ��̊J������Ԃ̈ʒu
    private float bottomEyelidOpenPosition; // ���܂Ԃ��̊J������Ԃ̈ʒu
    private bool isBlinking = false;        // �܂΂��������ǂ���

    private void Start()
    {
        // �܂Ԃ����ݒ肳��Ă��Ȃ���΃��O��\��
        if (topEyelid == null || bottomEyelid == null)
        {
            Debug.LogError("BlinkEffect: �㉺�̂܂Ԃ���RectTransform���ݒ肳��Ă��܂���B�C���X�y�N�^�[�Őݒ肵�Ă��������B");
            enabled = false;
            return;
        }

        // �J������Ԃ̈ʒu���L�^
        topEyelidOpenPosition = topEyelid.anchoredPosition.y;
        bottomEyelidOpenPosition = bottomEyelid.anchoredPosition.y;

        // �N�����ɖڂ���Ă��邩�J���Ă��邩�̐ݒ�
        if (startWithClosed)
        {
            // ���S�ɕ����ʒu�ɐݒ�
            SetEyelidsPosition(1.0f);
        }
        else
        {
            // ���S�ɊJ�����ʒu�ɐݒ�
            SetEyelidsPosition(0.0f);
        }

        // �܂΂����R���[�`�����J�n
        StartCoroutine(BlinkCoroutine());
    }

    private IEnumerator BlinkCoroutine()
    {
        // �����x��
        yield return new WaitForSeconds(initialBlinkDelay);

        // �N�����ɖڂ���Ă���ꍇ�́A���X�ɊJ��
        if (startWithClosed)
        {
            yield return StartCoroutine(OpenEyes());
        }

        // �ʏ�̂܂΂����T�C�N��
        while (true)
        {
            // ���̂܂΂����܂ł̎��Ԃ�ݒ�
            float nextBlinkTime = Random.Range(minBlinkInterval, maxBlinkInterval);
            yield return new WaitForSeconds(nextBlinkTime);

            // �܂΂������s
            if (!isBlinking)
            {
                yield return StartCoroutine(PerformBlink());
            }
        }
    }

    private IEnumerator OpenEyes()
    {
        float timer = 0;

        while (timer < openingDuration)
        {
            timer += Time.deltaTime;
            float t = timer / openingDuration;
            float openRatio = 1.0f - t; // 1.0�i�j����0.0�i�J�j��

            SetEyelidsPosition(openRatio);

            yield return null;
        }

        // ���S�ɊJ������Ԃɐݒ�
        SetEyelidsPosition(0);
    }

    private IEnumerator PerformBlink()
    {
        isBlinking = true;

        // �܂Ԃ������
        float timer = 0;
        float halfDuration = blinkDuration / 2;

        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;

            SetEyelidsPosition(t);

            yield return null;
        }

        // ���S�ɕ�����Ԃ��m��
        SetEyelidsPosition(1.0f);

        // �܂Ԃ����J��
        timer = 0;
        while (timer < halfDuration)
        {
            timer += Time.deltaTime;
            float t = timer / halfDuration;

            SetEyelidsPosition(1.0f - t);

            yield return null;
        }

        // ���S�ɊJ������Ԃ��m��
        SetEyelidsPosition(0);

        isBlinking = false;
    }

    // �܂Ԃ��̈ʒu��ݒ肷��w���p�[���\�b�h
    // blinkProgress: 0.0 = ���S�ɊJ������ԁA1.0 = ���S�ɕ������
    private void SetEyelidsPosition(float blinkProgress)
    {
        if (topEyelid == null || bottomEyelid == null) return;

        // ��܂Ԃ��͉��Ɉړ��A���܂Ԃ��͏�Ɉړ�
        Vector2 topPos = topEyelid.anchoredPosition;
        Vector2 bottomPos = bottomEyelid.anchoredPosition;

        // ��܂Ԃ��̈ʒu���v�Z�i�J�����ʒu��������ʒu�֐��`�Ɉړ��j
        topPos.y = Mathf.Lerp(topEyelidOpenPosition, closedPosition, blinkProgress);

        // ���܂Ԃ��̈ʒu���v�Z�i�J�����ʒu��������ʒu�֐��`�Ɉړ��j
        bottomPos.y = Mathf.Lerp(bottomEyelidOpenPosition, closedPosition, blinkProgress);

        // �ʒu��K�p
        topEyelid.anchoredPosition = topPos;
        bottomEyelid.anchoredPosition = bottomPos;
    }

    // �܂΂������蓮�Ńg���K�[����p�u���b�N���\�b�h
    public void TriggerBlink()
    {
        if (!isBlinking)
        {
            StartCoroutine(PerformBlink());
        }
    }
}