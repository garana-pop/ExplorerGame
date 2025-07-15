using UnityEngine;

/// <summary>
/// ContinueIndicator�̊ȒP�ȃA�j���[�V����
/// </summary>
public class ContinueIndicatorAnimation : MonoBehaviour
{
    [Header("�A�j���[�V�����ݒ�")]
    [SerializeField] private float animationSpeed = 1f;
    [SerializeField] private float animationAmount = 10f;

    private Vector3 originalPosition;
    private float timeCounter = 0f;

    private void Start()
    {
        originalPosition = transform.localPosition;
    }

    private void Update()
    {
        // �㉺�ɕ��V����A�j���[�V����
        timeCounter += Time.deltaTime * animationSpeed;
        float yOffset = Mathf.Sin(timeCounter) * animationAmount;
        transform.localPosition = originalPosition + new Vector3(0, yOffset, 0);
    }

    private void OnEnable()
    {
        // �\������邽�тɈʒu�����Z�b�g
        if (originalPosition != Vector3.zero)
        {
            transform.localPosition = originalPosition;
        }
        timeCounter = 0f;
    }
}