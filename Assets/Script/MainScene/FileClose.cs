using UnityEngine;
using UnityEngine.UI;

public class FileClose : MonoBehaviour
{
    [SerializeField] private FileOpen fileOpen; // FileOpenへの参照
    [SerializeField] private float unlockDelay = 2.0f; // ボタンロック解除の遅延時間（秒）

    private Button closeButton; // このスクリプトが付いたButton
    private bool isLocked = false; // ボタンがロックされているかどうか

    private void Awake()
    {
        // Buttonコンポーネントを取得
        closeButton = GetComponent<Button>();
        if (closeButton == null)
        {
            Debug.LogError("Buttonコンポーネントが見つかりません");
            return;
        }

        // FileOpenが設定されているか確認
        if (fileOpen == null)
        {
            //Debug.LogError("FileOpenがインスペクターで設定されていません");
            return;
        }

        // Buttonのクリックイベントを設定
        closeButton.onClick.AddListener(ClosePanel);
    }

    // パネルを閉じる処理
    private void ClosePanel()
    {
        // ボタンがロックされている場合は何もしない
        if (isLocked)
        {
            Debug.Log("ボタンはロックされているため、閉じる操作は無視されました");
            return;
        }

        // ShockEffect中かチェック
        PdfDocumentManager pdfManager = GetComponentInParent<PdfDocumentManager>();

        // TXTパズルの完了処理中かチェック
        TxtPuzzleManager txtManager = fileOpen.GetComponentInChildren<TxtPuzzleManager>(true);
        if (txtManager != null && txtManager.IsProcessingCompletion())
        {
            Debug.Log("TXTパズル完了処理中のため、閉じる操作は無視されました");
            return;
        }

        if (fileOpen != null)
        {
            // 追加: ファイルを閉じる前にPdfManagerの状態をリセット
            PdfDocumentManager pdfDocManager = fileOpen.GetComponentInChildren<PdfDocumentManager>(true);

            // ファイルパネルを閉じる
            fileOpen.ClosePanel();
        }
    }

    // ボタンをロックする（外部から呼び出し可能）
    public void LockButton()
    {
        isLocked = true;
        closeButton.interactable = false;
    }

    // ボタンのロックを解除する（外部から呼び出し可能）
    public void UnlockButton()
    {
        isLocked = false;
        closeButton.interactable = true;
    }

    // 遅延付きでボタンのロックを解除する
    public void UnlockButtonDelayed()
    {
        Invoke("UnlockButton", unlockDelay);
    }
}