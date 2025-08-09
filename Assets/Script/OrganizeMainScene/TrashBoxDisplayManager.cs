using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class TrashBoxDisplayManager : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IDropHandler
{
    [Header("ゴミ箱が開いた時の画像")]
    public Sprite mouseOverSprite; // インスペクターで設定する、マウスカーソルが乗ったときの画像

    [Header("ゴミ箱が開いた時の表示領域の拡張数")]
    [Tooltip("Rect TransformコンポーネントのHeightの値")]
    [SerializeField] private int ImageDisplayHeightValue = 10;

    // UIコンポーネント
    private Image image;
    private Sprite originalSprite;
    private RectTransform rectTransform;

    // 状態管理
    private bool fileDragging = false; //ドラッグ中か判定
    private bool trashBoxOpen = false; //ゴミ箱の蓋が開いたか判定
    private bool waitingForMouseUp = false; //「開いていた後にマウスアップを検知する」用

    // ゴミ箱上でマウスアップした際に発火するイベントを宣言
    public event Action OnTrashBoxOpenedAndMouseReleased;

    // 他のコンポーネント参照
    private TrashBoxSoundSetting soundSetting;
    //private TrashBoxTips tips;
    //private TrashBoxDeletionManagement deletionManagement;

    /// <summary>
    /// Startメソッド - シーン開始時の処理
    /// </summary>
    private void Start()
    {
        // imageコンポーネントを取得します。
        image = GetComponent<Image>();

        //RectTransformコンポーネントを取得します。
        rectTransform = GetComponent<RectTransform>();

        // 元の画像を保存します。
        if (image != null)
        {
            originalSprite = image.sprite;
        }

        // 他のコンポーネントを取得
        soundSetting = GetComponent<TrashBoxSoundSetting>();
        //tips = GetComponent<TrashBoxTips>();
        //deletionManagement = GetComponent<TrashBoxDeletionManagement>();

    }

    /// <summary>
    /// マウスカーソルがオブジェクト上に入ると呼び出される
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        //imageに画像が設定されている　かつ、マウスカーソルがオブジェクト上にある　かつ、ドラック中である場合
        if (image != null && mouseOverSprite != null && fileDragging) 
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
            trashBoxOpen = true;

            //マウスアップ待機開始
            waitingForMouseUp = true;
        }
    }

    /// <summary>
    /// ドラッグアイテムがゴミ箱上でドロップされたときに呼ばれる（IDropHandler）
    /// </summary>
    /// <param name="eventData"></param>
    public void OnDrop(PointerEventData eventData)
    {
        if (waitingForMouseUp && trashBoxOpen)
        {
            //イベントを発火
            OnTrashBoxOpenedAndMouseReleased?.Invoke();
        }
        waitingForMouseUp = false;
    }

    /// <summary>
    /// マウスカーソルがオブジェクト上から出ると呼び出される
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        //imageに画像が設定されている場合　かつ、ゴミの蓋が開いているか
        if (image != null && trashBoxOpen)
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
            trashBoxOpen = false;
        }
    }
    /// <summary>
    /// アタッチされたオブジェクトがクリックされた時に、呼び出される
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData)
    {
        // ヒントメッセージ表示
        //if (tips != null)
        //{
        //    tips.ShowClickMessage();
        //}
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
    /// <param name="isDragging">ドラッグ中かどうか</param>
    private void HandleFileDragging(bool isDragging)
    {
        fileDragging = isDragging; // 状態を反映
    }

    #region パブリックメソッド

    /// <summary>
    /// ゴミ箱が開いているかどうかを取得
    /// </summary>
    /// <returns>開いている場合はtrue</returns>
    public bool IsTrashBoxOpen()
    {
        return trashBoxOpen;
    }

    /// <summary>
    /// ファイルがドラッグ中かどうかを取得
    /// </summary>
    /// <returns>ドラッグ中の場合はtrue</returns>
    public bool IsFileDragging()
    {
        return fileDragging;
    }

    #endregion
}
