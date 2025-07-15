using UnityEngine;

/// <summary>
/// ContinueIndicatorの簡単なアニメーション
/// </summary>
public class ContinueIndicatorAnimation : MonoBehaviour
{
    [Header("アニメーション設定")]
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
        // 上下に浮遊するアニメーション
        timeCounter += Time.deltaTime * animationSpeed;
        float yOffset = Mathf.Sin(timeCounter) * animationAmount;
        transform.localPosition = originalPosition + new Vector3(0, yOffset, 0);
    }

    private void OnEnable()
    {
        // 表示されるたびに位置をリセット
        if (originalPosition != Vector3.zero)
        {
            transform.localPosition = originalPosition;
        }
        timeCounter = 0f;
    }
}