using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

/// <summary>
/// ゴミ箱の表示管理を行うクラス
/// ドラッグ&ドロップ時の表示切り替えとクリック時のメッセージ表示を制御
/// </summary>
public class TrashBoxDisplayManager : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IDropHandler
{
    #region インスペクター設定

    [Header("ゴミ箱が開いた時の画像")]
    [Tooltip("マウスカーソルが乗ったときの画像")]
    [SerializeField] private Sprite mouseOverSprite;

    [Header("ゴミ箱が開いた時の表示領域の拡張分")]
    [Tooltip("Rect Transformコンポーネントの Height の値")]
    [SerializeField] private int imageDisplayHeightValue = 10;

    [Header("デバッグ設定")]
    [Tooltip("デバッグログを表示するか")]
    [SerializeField] private bool debugMode = false;

    #endregion

    #region プライベート変数

    // UIコンポーネント
    private Image image;
    private Sprite originalSprite;
    private RectTransform rectTransform;

    // 状態管理
    private bool fileDragging = false;        // ドラッグ中か判定
    private bool trashBoxOpen = false;        // ゴミ箱の蓋が開いたか判定
    private bool waitingForMouseUp = false;   // 開いていた後にマウスアップを検知する用

    // ゴミ箱上でマウスアップした際に発火するイベントを宣言
    public event Action OnTrashBoxOpenedAndMouseReleased;

    // 他のコンポーネント参照
    private TrashBoxSoundSetting soundSetting;
    private TrashBoxTips tips;
    private TrashBoxDeletionManagement deletionManagement;

    #endregion

    #region Unity ライフサイクル

    /// <summary>
    /// Startメソッド - シーン開始時の処理
    /// </summary>
    private void Start()
    {
        // imageコンポーネントを取得します
        image = GetComponent<Image>();

        // RectTransformコンポーネントを取得します
        rectTransform = GetComponent<RectTransform>();

        // 元の画像を保存します
        if (image != null)
        {
            originalSprite = image.sprite;
        }

        // 他のコンポーネントを取得
        soundSetting = GetComponent<TrashBoxSoundSetting>();
        tips = GetComponent<TrashBoxTips>();
        deletionManagement = GetComponent<TrashBoxDeletionManagement>();

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDisplayManager)}: 初期化完了");
        }
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

    #endregion

    #region イベントハンドラー

    /// <summary>
    /// アタッチされたオブジェクトがクリックされた時に呼び出される
    /// </summary>
    /// <param name="eventData">ポインタイベントデータ</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        // ヒントメッセージ表示
        if (tips != null)
        {
            tips.ShowTrashMessage();

            if (debugMode)
            {
                Debug.Log($"{nameof(TrashBoxDisplayManager)}: ゴミ箱がクリックされました - メッセージを表示");
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning($"{nameof(TrashBoxDisplayManager)}: TrashBoxTipsコンポーネントが見つかりません");
        }
    }

    /// <summary>
    /// マウスカーソルがオブジェクト上に入ると呼び出される
    /// </summary>
    /// <param name="eventData">ポインタイベントデータ</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // imageに画像が設定されている かつ マウスカーソルがオブジェクト上にある かつ ドラック中である場合
        if (image != null && mouseOverSprite != null && fileDragging)
        {
            // Rect Transformコンポーネントの Height の値を拡張
            if (rectTransform != null)
            {
                Vector2 size = rectTransform.sizeDelta;
                size.y += imageDisplayHeightValue; // Height の値を変更
                rectTransform.sizeDelta = size;
            }

            // 画像を変更：ゴミ箱の蓋を開ける
            image.sprite = mouseOverSprite;

            // ゴミ箱の蓋が開いた
            trashBoxOpen = true;

            // マウスアップ待機開始
            waitingForMouseUp = true;

            if (debugMode)
            {
                Debug.Log($"{nameof(TrashBoxDisplayManager)}: ゴミ箱が開きました");
            }
        }
    }

    /// <summary>
    /// ドラッグアイテムがゴミ箱上でドロップされたときに呼ばれる（IDropHandler）
    /// </summary>
    /// <param name="eventData">ポインタイベントデータ</param>
    public void OnDrop(PointerEventData eventData)
    {
        if (waitingForMouseUp && trashBoxOpen)
        {
            // イベントを発火
            OnTrashBoxOpenedAndMouseReleased?.Invoke();

            if (debugMode)
            {
                Debug.Log($"{nameof(TrashBoxDisplayManager)}: ファイルがドロップされました");
            }
        }
        waitingForMouseUp = false;
    }

    /// <summary>
    /// マウスカーソルがオブジェクト上から出ると呼び出される
    /// </summary>
    /// <param name="eventData">ポインタイベントデータ</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        // imageに画像が設定されている場合 かつ ゴミ箱の蓋が開いているか
        if (image != null && trashBoxOpen)
        {
            // Rect Transformコンポーネントの Height の値を元に戻す
            if (rectTransform != null)
            {
                Vector2 size = rectTransform.sizeDelta;
                size.y -= imageDisplayHeightValue; // Height の値を元に戻す
                rectTransform.sizeDelta = size;
            }

            // 元の画像に戻す：ゴミ箱の蓋を閉める
            image.sprite = originalSprite;

            // ゴミ箱の蓋が閉まった
            trashBoxOpen = false;

            if (debugMode)
            {
                Debug.Log($"{nameof(TrashBoxDisplayManager)}: ゴミ箱が閉じました");
            }
        }
    }

    /// <summary>
    /// DraggableFileクラスからisDragging（ドラッグ判定フラグ）の値を取得
    /// </summary>
    /// <param name="isDragging">ドラッグ中かどうか</param>
    private void HandleFileDragging(bool isDragging)
    {
        fileDragging = isDragging; // 状態を反映

        if (debugMode && isDragging)
        {
            Debug.Log($"{nameof(TrashBoxDisplayManager)}: ファイルのドラッグが開始されました");
        }
    }

    #endregion

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

    /// <summary>
    /// ゴミ箱のスプライトを設定
    /// </summary>
    /// <param name="newMouseOverSprite">新しいマウスオーバー時のスプライト</param>
    public void SetMouseOverSprite(Sprite newMouseOverSprite)
    {
        mouseOverSprite = newMouseOverSprite;

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDisplayManager)}: マウスオーバースプライトを更新しました");
        }
    }

    /// <summary>
    /// 画像表示領域の拡張値を設定
    /// </summary>
    /// <param name="newHeightValue">新しい高さの拡張値</param>
    public void SetImageDisplayHeightValue(int newHeightValue)
    {
        imageDisplayHeightValue = Mathf.Max(0, newHeightValue);

        if (debugMode)
        {
            Debug.Log($"{nameof(TrashBoxDisplayManager)}: 表示領域拡張値を更新 - {imageDisplayHeightValue}");
        }
    }

    #endregion
}