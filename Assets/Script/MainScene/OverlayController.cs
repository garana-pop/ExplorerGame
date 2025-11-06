using UnityEngine;

/// <summary>
/// DraggingCanvasに「Overlay」以外の子オブジェクトが1つ以上ある場合に
/// Overlayオブジェクトをアクティブにするスクリプト
/// </summary>
public class OverlayController : MonoBehaviour
{
    [Tooltip("アクティブにするオーバーレイオブジェクト")]
    [SerializeField] private GameObject overlayObject;

    private void Awake()
    {
        // Overlayオブジェクトの自動検索（設定されていない場合）
        if (overlayObject == null)
        {
            overlayObject = transform.Find("Overlay")?.gameObject;
            if (overlayObject == null)
            {
                Debug.LogWarning("OverlayController: Overlayオブジェクトが見つかりません。インスペクターで設定してください。");
                enabled = false; // スクリプトを無効化
                return;
            }
        }

        // 初期状態のチェック
        CheckOverlayStatus();
    }

    private void OnTransformChildrenChanged()
    {
        // 子オブジェクトが変更されたときに呼び出される
        CheckOverlayStatus();
    }

    /// <summary>
    /// 「Overlay」以外の子オブジェクトが存在するかチェックし、
    /// Overlayの表示・非表示を更新
    /// </summary>
    private void CheckOverlayStatus()
    {
        if (overlayObject == null) return;

        // 「Overlay」以外の子オブジェクト数を計算
        int childCount = 0;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            // Overlayオブジェクト自身は除外
            if (child.gameObject != overlayObject)
            {
                childCount++;
            }
        }

        // 「Overlay」以外の子オブジェクトが1つ以上あればアクティブに
        overlayObject.SetActive(childCount > 0);
    }
}