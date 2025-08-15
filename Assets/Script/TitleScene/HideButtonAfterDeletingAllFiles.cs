using UnityEngine;

/// <summary>
/// OrganizeMainSceneで全ファイル削除後のみ、
/// TitleSceneの”思い出すボタン”を非表示にするクラス
/// </summary>
public class HideButtonAfterDeletingAllFiles : MonoBehaviour
{
    private bool allFilesDeleted = false; // game_save.jsonのallFilesCompletelyDeletedフラグをチェック

    /// <summary>
    /// Startメソッド - シーン開始時の処理
    /// </summary>
    private void Start()
    {
        // GameSaveManagerから全ファイル完全削除フラグを取得
        allFilesDeleted = GameSaveManager.Instance.GetAllFilesCompletelyDeleted();
        //Debug.Log("HideButtonAfterDeletingAllFiles : allFilesDeletedフラグ" + allFilesDeleted);

        // OrganizeMainSceneで全ファイル削除後、確認ダイアログで"はい"押下時
        // または、game_save.jsonのallFilesCompletelyDeletedフラグがtrueの場合
        if (OrganizeMainSceneController.returnScene == true || allFilesDeleted == true)
        {
            //Debug.Log("HideButtonAfterDeletingAllFiles : Buttonを非表示にする");

            // このスクリプトがアタッチされているButtonを非表示にする
            gameObject.SetActive(false);
        }

    }

}
