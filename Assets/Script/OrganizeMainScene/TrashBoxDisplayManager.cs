using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TrashBoxDisplayManager : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("ゴミ箱が開いた時の画像")]
    public Sprite mouseOverSprite; // インスペクターで設定する、マウスカーソルが乗ったときの画像

    [Header("ゴミ箱が開いた時の表示領域の拡張数")]
    [Tooltip("Rect TransformコンポーネントのHeightの値")]
    [SerializeField] private int ImageDisplayHeightValue = 10;

    private Image image;
    private Sprite originalSprite;
    private RectTransform rectTransform;
    private bool FileDragging = false; //ドラッグ中か判定
    private bool TrashBoxOpen = false; //ゴミ箱の蓋が開いたか判定

    /// <summary>
    /// Startメソッド - シーン開始時の処理
    /// </summary>
    private void Start()
    {
        // 親オブジェクトのTransformを取得し、一番下に配置します。
        if (transform.parent != null)
        {
            transform.SetAsLastSibling();
        }

        // imageコンポーネントを取得します。
        image = GetComponent<Image>();

        //RectTransformコンポーネントを取得します。
        rectTransform = GetComponent<RectTransform>();

        // 元の画像を保存します。
        if (image != null)
        {
            originalSprite = image.sprite;
        }
    }

    /// <summary>
    /// マウスカーソルがオブジェクト上に入ると呼び出される
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        //imageに画像が設定されている　かつ、マウスカーソルがオブジェクト上にある　かつ、ドラック中である場合
        if (image != null && mouseOverSprite != null && FileDragging) 
        {
            //Rect TransformコンポーネントのHeightの値を+20
            if (rectTransform != null)
            {
                Vector2 size = rectTransform.sizeDelta;
                size.y += ImageDisplayHeightValue; //Heightの値を変更
                rectTransform.sizeDelta = size;
            }

            //画像を変更：ゴミ箱の蓋を開ける
            image.sprite = mouseOverSprite;

            //ゴミ箱の蓋が開いた
            TrashBoxOpen = true;

            Debug.Log("ゴミ箱の蓋を開ける");
        }
    }

    /// <summary>
    /// マウスカーソルがオブジェクト上から出ると呼び出される
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        //imageに画像が設定されている場合　かつ、ゴミの蓋が開いているか
        if (image != null && TrashBoxOpen)
        {
            //Rect TransformコンポーネントのHeightの値を-20
            if (rectTransform != null)
            {
                Vector2 size = rectTransform.sizeDelta;
                size.y -= ImageDisplayHeightValue; //Heightの値を元に戻す
                rectTransform.sizeDelta = size;
            }

            // 元の画像に戻す：ゴミ箱の蓋を閉める
            image.sprite = originalSprite;

            //ゴミ箱の蓋が閉まった
            TrashBoxOpen = false;

            Debug.Log("ゴミ箱の蓋を閉める");
        }
    }
    /// <summary>
    /// アタッチされたオブジェクトがクリックされた時に、呼び出される
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("クリックされたよ");
    }

    /// <summary>
    /// TrashBoxDisplayManager有効時にドラッグされたかを受け取る
    /// </summary>
    private void OnEnable()
    {
        DraggableFile.OnFileDragging += HandleFileDragging; // イベントに登録
    }

    /// <summary>
    /// TrashBoxDisplayManager無効時にドラッグイベントの通知OFF
    /// </summary>
    private void OnDisable()
    {
        DraggableFile.OnFileDragging -= HandleFileDragging; // イベントから解除
    }
    /// <summary>
    /// DraggableFileクラスからisDragging（ドラッグ判定フラグ）の値を取得
    /// </summary>
    /// <param name="isDragging"></param>
    private void HandleFileDragging(bool isDragging)
    {
        FileDragging = isDragging; // 状態を反映
        Debug.Log("FileDragging" + FileDragging);
    }

}
