using UnityEngine;

/// <summary>
/// OrganizeMainSceneで全ファイル削除後のみ、
/// TitleSceneの”思い出すボタン”を非表示にするクラス
/// </summary>
public class HideButtonAfterDeletingAllFiles : MonoBehaviour
{

    /// <summary>
    /// Startメソッド - シーン開始時の処理
    /// </summary>
    private void Start()
    {
        // OrganizeMainSceneで全ファイル削除後、確認ダイアログで”はい”押下時
        if (OrganizeMainSceneController.returnScene == true)
        {
            // Debug.Log("HideButtonAfterDeletingAllFiles : Buttonを非表示にする");

            // このスクリプトがアタッチされているButtonを非表示にする
            gameObject.SetActive(false);
        }

    }

}
