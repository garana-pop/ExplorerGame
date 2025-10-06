using ExplorerGame.Localization;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 発言者をドラッグするためのスクリプト
public class SpeakerDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("話者名設定")]
    [SerializeField] private string speakerName_Japanese; // 日本語の発言者名
    [SerializeField] private string speakerName_English; // 英語の発言者名

    private string speakerName; // 現在の言語設定に応じた発言者名

    private Vector3 originalPosition;
    private Canvas draggingCanvas;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        // ドラッグ用キャンバスの取得
        draggingCanvas = GameObject.Find("DraggingCanvas").GetComponent<Canvas>();
        if (draggingCanvas == null)
        {
            Debug.LogError("DraggingCanvasが見つかりません");
        }

        // 自身のCanvasGroupの取得または追加
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void Start()
    {
        // LocalizationManagerから現在の言語設定を取得して適用
        UpdateSpeakerNameByLanguage();

        // 言語変更イベントに登録
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }
    }

    private void OnDestroy()
    {
        // イベントの登録解除
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }

    /// <summary>
    /// 言語変更時のコールバック
    /// </summary>
    private void OnLanguageChanged(UnityEngine.Localization.Locale newLocale)
    {
        UpdateSpeakerNameByLanguage();
    }

    /// <summary>
    /// 現在の言語設定に基づいてspeakerNameを更新
    /// </summary>
    private void UpdateSpeakerNameByLanguage()
    {
        if (LocalizationManager.Instance == null)
        {
            // LocalizationManagerが存在しない場合は日本語をデフォルトとする
            speakerName = speakerName_Japanese;
            Debug.LogWarning($"{nameof(SpeakerDraggable)}: LocalizationManagerが見つかりません。日本語をデフォルトとして使用します。");
            return;
        }

        // 現在の言語コードを取得
        string currentLanguageCode = LocalizationManager.Instance.GetCurrentLanguageCode();

        // 言語コードに応じて話者名を設定
        if (currentLanguageCode == "en")
        {
            speakerName = speakerName_English;
        }
        else
        {
            speakerName = speakerName_Japanese;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 元の位置を保存
        originalPosition = transform.position;

        // ドラッグ中はレイキャストを無効に
        canvasGroup.blocksRaycasts = false;

        // 半透明にして視覚的フィードバックを提供
        canvasGroup.alpha = 0.7f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // ドラッグ位置に追従
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 元の状態に戻す
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // Raycastですべてのオブジェクトを取得
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = eventData.position
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        bool droppedOnTarget = false;

        // ドロップエリアを検索
        foreach (var result in results)
        {
            SpeakerDropArea dropArea = result.gameObject.GetComponent<SpeakerDropArea>();
            if (dropArea != null)
            {
                // ドロップエリアに発言者をドロップした処理
                droppedOnTarget = dropArea.OnSpeakerDropped(this);
                break;
            }
        }

        // 常に元の位置に戻す
        transform.position = originalPosition;
    }

    /// <summary>
    /// 現在の言語設定に応じた発言者名を取得
    /// </summary>
    /// <returns>現在の言語での発言者名</returns>
    public string GetSpeakerName()
    {
        return speakerName;
    }
}