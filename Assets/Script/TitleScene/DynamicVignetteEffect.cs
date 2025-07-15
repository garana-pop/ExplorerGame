using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// �w�i�摜�ɓ��I�ȃr�l�b�g�G�t�F�N�g��K�p����R���|�[�l���g
/// ���̉摜��ێ����Ȃ���A�����𖾂邭�E�[���Â�����G�t�F�N�g��ǉ�
/// </summary>
[RequireComponent(typeof(Image))]
public class DynamicVignetteEffect : MonoBehaviour
{
    [Header("�r�l�b�g�ݒ�")]
    [SerializeField][Range(0f, 1f)] private float intensity = 0.5f;         // �G�t�F�N�g�̋���
    [SerializeField][Range(0f, 1f)] private float smoothness = 0.2f;        // �G�b�W�̊��炩��
    [SerializeField] private Color vignetteColor = new Color(0, 0, 0, 0.7f); // �r�l�b�g�̐F

    [Header("�A�j���[�V�����ݒ�")]
    [SerializeField] private bool animateIntensity = true;                   // ���x���A�j���[�V�������邩
    [SerializeField][Range(0f, 1f)] private float minIntensity = 0.3f;      // �ŏ����x
    [SerializeField][Range(0f, 1f)] private float maxIntensity = 0.7f;      // �ő勭�x
    [SerializeField] private float animationSpeed = 1.0f;                    // �A�j���[�V�������x

    [Header("���S�_�ݒ�")]
    [SerializeField] private Vector2 centerOffset = Vector2.zero;            // ���S�_�̃I�t�Z�b�g
    [SerializeField] private bool animateCenter = true;                      // ���S�_���A�j���[�V�������邩
    [SerializeField] private float centerAnimationRadius = 0.1f;             // ���S�_�A�j���[�V�����̔��a
    [SerializeField] private float centerAnimationSpeed = 0.5f;              // ���S�_�A�j���[�V�����̑��x

    [Header("�f�o�b�O")]
    [SerializeField] private bool useSimpleVignette = true;                 // �ȈՃr�l�b�g���g�p�i�ʏ�͂�������g�p�j

    // �v���C�x�[�g�ϐ�
    private Image targetImage;
    private Image vignetteOverlay;
    private Material effectMaterial;
    private RectTransform rectTransform;

    // �A�j���[�V�����p�ϐ�
    private float animationTime = 0f;
    private float centerAnimationTime = 0f;

    private void Awake()
    {
        // �^�[�Q�b�g�C���[�W�R���|�[�l���g���擾
        targetImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        if (useSimpleVignette)
        {
            CreateSimpleVignetteOverlay();
        }
        else
        {
            SetupAdvancedVignette();
        }
    }

    private void CreateSimpleVignetteOverlay()
    {
        // �q�I�u�W�F�N�g�Ƃ��ăI�[�o�[���C���쐬
        GameObject overlayObj = new GameObject("VignetteOverlay");
        RectTransform overlayRectTransform = overlayObj.AddComponent<RectTransform>();
        vignetteOverlay = overlayObj.AddComponent<Image>();

        // �e�q�֌W�ƈʒu��ݒ�
        overlayObj.transform.SetParent(transform, false);

        // RectTransform��ݒ�i�e�Ɠ����T�C�Y�j
        overlayRectTransform.anchorMin = Vector2.zero;
        overlayRectTransform.anchorMax = Vector2.one;
        overlayRectTransform.offsetMin = Vector2.zero;
        overlayRectTransform.offsetMax = Vector2.zero;

        // �I�[�o�[���C�̃X�v���C�g�𐶐����Đݒ�
        vignetteOverlay.sprite = CreateVignetteSprite();
        vignetteOverlay.type = Image.Type.Simple;
        vignetteOverlay.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, vignetteColor.a * intensity);

        // ���̃C���[�W���O�ʂɕ\��
        vignetteOverlay.transform.SetAsLastSibling();

        // ���C�L���X�g���u���b�N���Ȃ��悤�ɐݒ�
        vignetteOverlay.raycastTarget = false;

        Debug.Log("�ȈՃr�l�b�g�I�[�o�[���C���쐬���܂���");
    }

    private void SetupAdvancedVignette()
    {
        try
        {
            // UI�f�t�H���g�V�F�[�_�[���g�p����J�X�^���}�e���A�����쐬
            effectMaterial = new Material(Shader.Find("UI/Default"));
            if (effectMaterial == null)
            {
                Debug.LogError("UI�V�F�[�_�[��������܂���ł����B");
                useSimpleVignette = true;
                CreateSimpleVignetteOverlay();
                return;
            }

            // ���̃X�v���C�g�ƃ}�e���A����K�p
            if (targetImage.sprite != null)
            {
                effectMaterial.mainTexture = targetImage.sprite.texture;
                targetImage.material = effectMaterial;
                Debug.Log("�J�X�^���}�e���A����K�p���܂���");
            }
            else
            {
                Debug.LogError("Image �R���|�[�l���g�ɃX�v���C�g���ݒ肳��Ă��܂���B");
                useSimpleVignette = true;
                CreateSimpleVignetteOverlay();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("�J�X�^���}�e���A���̍쐬�Ɏ��s���܂���: " + e.Message);
            useSimpleVignette = true;
            CreateSimpleVignetteOverlay();
        }
    }

    private void Update()
    {
        // �A�j���[�V�����̍X�V
        UpdateAnimation();

        // �r�l�b�g���ʂ̍X�V
        if (useSimpleVignette)
        {
            UpdateSimpleVignette();
        }
        else
        {
            UpdateAdvancedVignette();
        }
    }

    private void UpdateAnimation()
    {
        if (animateIntensity)
        {
            // ���Ԃ̍X�V
            animationTime += Time.deltaTime * animationSpeed;

            // ���x�����ԂɊ�Â��ăT�C���g�ŕω�������
            intensity = Mathf.Lerp(minIntensity, maxIntensity,
                                  (Mathf.Sin(animationTime) + 1f) * 0.5f);
        }

        if (animateCenter)
        {
            // ���S�_�̃A�j���[�V����
            centerAnimationTime += Time.deltaTime * centerAnimationSpeed;

            // ���S�_���T�C���g�ƃR�T�C���g�ŉ~��`���悤�Ɉړ�
            float xOffset = Mathf.Sin(centerAnimationTime) * centerAnimationRadius;
            float yOffset = Mathf.Cos(centerAnimationTime * 0.7f) * centerAnimationRadius;
            centerOffset = new Vector2(xOffset, yOffset);
        }
    }

    private void UpdateSimpleVignette()
    {
        if (vignetteOverlay != null)
        {
            // �I�[�o�[���C�̐F�ƕs�����x���X�V
            Color overlayColor = vignetteOverlay.color;
            overlayColor = new Color(
                vignetteColor.r,
                vignetteColor.g,
                vignetteColor.b,
                vignetteColor.a * intensity
            );
            vignetteOverlay.color = overlayColor;

            // ���S�A�j���[�V�����̏ꍇ�́A�I�[�o�[���C�̈ʒu�������
            if (animateCenter && centerAnimationRadius > 0)
            {
                vignetteOverlay.rectTransform.anchoredPosition = new Vector2(
                    centerOffset.x * 10,
                    centerOffset.y * 10
                );
            }
        }
    }

    private void UpdateAdvancedVignette()
    {
        if (effectMaterial != null)
        {
            // �}�e���A���v���p�e�B���X�V
            effectMaterial.SetFloat("_Intensity", intensity);
            effectMaterial.SetColor("_Color", vignetteColor);

            // �p�t�H�[�}���X�̂��߁A���S���W�͕p�ɂɍX�V����K�v���Ȃ��ꍇ�͏ȗ��\
            if (animateCenter)
            {
                effectMaterial.SetVector("_Center", new Vector4(0.5f + centerOffset.x, 0.5f + centerOffset.y, 0, 0));
            }
        }
    }

    private Sprite CreateVignetteSprite()
    {
        // �r�l�b�g�e�N�X�`���𐶐�
        int textureSize = 256;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(textureSize / 2, textureSize / 2);
        float maxDistance = textureSize / 2;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                // ���S����̋������v�Z (0-1�͈̔�)
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;

                // �r�l�b�g�̗ʂ��v�Z�i���S�������A�[���s�����j
                float vignetteAmount = Mathf.SmoothStep(0, 1.0f, distance);

                // �����x�݂̂����e�N�X�`���Ƃ��Đݒ�
                texture.SetPixel(x, y, new Color(1, 1, 1, vignetteAmount));
            }
        }

        texture.Apply();

        // �e�N�X�`������X�v���C�g���쐬
        return Sprite.Create(
            texture,
            new Rect(0, 0, textureSize, textureSize),
            new Vector2(0.5f, 0.5f)
        );
    }

    // �v���p�e�B���O������ݒ肷�邽�߂̃��\�b�h
    public void SetIntensity(float newIntensity)
    {
        intensity = Mathf.Clamp01(newIntensity);
    }

    public void SetVignetteColor(Color newColor)
    {
        vignetteColor = newColor;
    }

    public void ToggleAnimation(bool enabled)
    {
        animateIntensity = enabled;
    }

    private void OnDestroy()
    {
        // �����������\�[�X��j��
        if (effectMaterial != null)
        {
            Destroy(effectMaterial);
        }
    }
}