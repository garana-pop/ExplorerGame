using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背景画像に動的なビネットエフェクトを適用するコンポーネント
/// 元の画像を保持しながら、中央を明るく・端を暗くするエフェクトを追加
/// </summary>
[RequireComponent(typeof(Image))]
public class DynamicVignetteEffect : MonoBehaviour
{
    [Header("ビネット設定")]
    [SerializeField][Range(0f, 1f)] private float intensity = 0.5f;         // エフェクトの強さ
    [SerializeField][Range(0f, 1f)] private float smoothness = 0.2f;        // エッジの滑らかさ
    [SerializeField] private Color vignetteColor = new Color(0, 0, 0, 0.7f); // ビネットの色

    [Header("アニメーション設定")]
    [SerializeField] private bool animateIntensity = true;                   // 強度をアニメーションするか
    [SerializeField][Range(0f, 1f)] private float minIntensity = 0.3f;      // 最小強度
    [SerializeField][Range(0f, 1f)] private float maxIntensity = 0.7f;      // 最大強度
    [SerializeField] private float animationSpeed = 1.0f;                    // アニメーション速度

    [Header("中心点設定")]
    [SerializeField] private Vector2 centerOffset = Vector2.zero;            // 中心点のオフセット
    [SerializeField] private bool animateCenter = true;                      // 中心点をアニメーションするか
    [SerializeField] private float centerAnimationRadius = 0.1f;             // 中心点アニメーションの半径
    [SerializeField] private float centerAnimationSpeed = 0.5f;              // 中心点アニメーションの速度

    [Header("デバッグ")]
    [SerializeField] private bool useSimpleVignette = true;                 // 簡易ビネットを使用（通常はこちらを使用）

    // プライベート変数
    private Image targetImage;
    private Image vignetteOverlay;
    private Material effectMaterial;
    private RectTransform rectTransform;

    // アニメーション用変数
    private float animationTime = 0f;
    private float centerAnimationTime = 0f;

    private void Awake()
    {
        // ターゲットイメージコンポーネントを取得
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
        // 子オブジェクトとしてオーバーレイを作成
        GameObject overlayObj = new GameObject("VignetteOverlay");
        RectTransform overlayRectTransform = overlayObj.AddComponent<RectTransform>();
        vignetteOverlay = overlayObj.AddComponent<Image>();

        // 親子関係と位置を設定
        overlayObj.transform.SetParent(transform, false);

        // RectTransformを設定（親と同じサイズ）
        overlayRectTransform.anchorMin = Vector2.zero;
        overlayRectTransform.anchorMax = Vector2.one;
        overlayRectTransform.offsetMin = Vector2.zero;
        overlayRectTransform.offsetMax = Vector2.zero;

        // オーバーレイのスプライトを生成して設定
        vignetteOverlay.sprite = CreateVignetteSprite();
        vignetteOverlay.type = Image.Type.Simple;
        vignetteOverlay.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, vignetteColor.a * intensity);

        // 後ろのイメージより前面に表示
        vignetteOverlay.transform.SetAsLastSibling();

        // レイキャストをブロックしないように設定
        vignetteOverlay.raycastTarget = false;

        Debug.Log("簡易ビネットオーバーレイを作成しました");
    }

    private void SetupAdvancedVignette()
    {
        try
        {
            // UIデフォルトシェーダーを使用するカスタムマテリアルを作成
            effectMaterial = new Material(Shader.Find("UI/Default"));
            if (effectMaterial == null)
            {
                Debug.LogError("UIシェーダーが見つかりませんでした。");
                useSimpleVignette = true;
                CreateSimpleVignetteOverlay();
                return;
            }

            // 元のスプライトとマテリアルを適用
            if (targetImage.sprite != null)
            {
                effectMaterial.mainTexture = targetImage.sprite.texture;
                targetImage.material = effectMaterial;
                Debug.Log("カスタムマテリアルを適用しました");
            }
            else
            {
                Debug.LogError("Image コンポーネントにスプライトが設定されていません。");
                useSimpleVignette = true;
                CreateSimpleVignetteOverlay();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("カスタムマテリアルの作成に失敗しました: " + e.Message);
            useSimpleVignette = true;
            CreateSimpleVignetteOverlay();
        }
    }

    private void Update()
    {
        // アニメーションの更新
        UpdateAnimation();

        // ビネット効果の更新
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
            // 時間の更新
            animationTime += Time.deltaTime * animationSpeed;

            // 強度を時間に基づいてサイン波で変化させる
            intensity = Mathf.Lerp(minIntensity, maxIntensity,
                                  (Mathf.Sin(animationTime) + 1f) * 0.5f);
        }

        if (animateCenter)
        {
            // 中心点のアニメーション
            centerAnimationTime += Time.deltaTime * centerAnimationSpeed;

            // 中心点をサイン波とコサイン波で円を描くように移動
            float xOffset = Mathf.Sin(centerAnimationTime) * centerAnimationRadius;
            float yOffset = Mathf.Cos(centerAnimationTime * 0.7f) * centerAnimationRadius;
            centerOffset = new Vector2(xOffset, yOffset);
        }
    }

    private void UpdateSimpleVignette()
    {
        if (vignetteOverlay != null)
        {
            // オーバーレイの色と不透明度を更新
            Color overlayColor = vignetteOverlay.color;
            overlayColor = new Color(
                vignetteColor.r,
                vignetteColor.g,
                vignetteColor.b,
                vignetteColor.a * intensity
            );
            vignetteOverlay.color = overlayColor;

            // 中心アニメーションの場合は、オーバーレイの位置を微調整
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
            // マテリアルプロパティを更新
            effectMaterial.SetFloat("_Intensity", intensity);
            effectMaterial.SetColor("_Color", vignetteColor);

            // パフォーマンスのため、中心座標は頻繁に更新する必要がない場合は省略可能
            if (animateCenter)
            {
                effectMaterial.SetVector("_Center", new Vector4(0.5f + centerOffset.x, 0.5f + centerOffset.y, 0, 0));
            }
        }
    }

    private Sprite CreateVignetteSprite()
    {
        // ビネットテクスチャを生成
        int textureSize = 256;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(textureSize / 2, textureSize / 2);
        float maxDistance = textureSize / 2;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                // 中心からの距離を計算 (0-1の範囲)
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;

                // ビネットの量を計算（中心が透明、端が不透明）
                float vignetteAmount = Mathf.SmoothStep(0, 1.0f, distance);

                // 透明度のみを持つテクスチャとして設定
                texture.SetPixel(x, y, new Color(1, 1, 1, vignetteAmount));
            }
        }

        texture.Apply();

        // テクスチャからスプライトを作成
        return Sprite.Create(
            texture,
            new Rect(0, 0, textureSize, textureSize),
            new Vector2(0.5f, 0.5f)
        );
    }

    // プロパティを外部から設定するためのメソッド
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
        // 生成したリソースを破棄
        if (effectMaterial != null)
        {
            Destroy(effectMaterial);
        }
    }
}