using UnityEngine;

/// <summary>
/// ゴミ箱の効果音設定を管理するクラス
/// ファイルドロップ時とゴミ箱クリック時の効果音を制御します
/// </summary>
public class TrashBoxSoundSetting : MonoBehaviour
{
    // SoundEffectManagerの参照
    private SoundEffectManager soundEffectManager;

    // TrashBoxDisplayManagerの参照
    private TrashBoxDisplayManager trashBoxDisplayManager;

    /// <summary>
    /// Startメソッド - シーン開始後の処理
    /// </summary>
    private void Start()
    {
        // SoundEffectManagerの参照を取得
        soundEffectManager = FindFirstObjectByType<SoundEffectManager>();

        // TrashBoxDisplayManagerの参照を取得
        trashBoxDisplayManager = GetComponent<TrashBoxDisplayManager>();

        if (trashBoxDisplayManager != null)
        {
            //イベントを購読（受け取る）
            trashBoxDisplayManager.OnTrashBoxOpenedAndMouseReleased += HandleOpenedAndReleased;
        }
    }

    /// <summary>
    /// シーン遷移やゲーム終了、手動で Destroy() した時に呼ばれます
    /// 参照切れエラーを防ぐため、HandleOpenedAndReleased メソッドを解除
    /// </summary>
    private void OnDestroy()
    {
        if (trashBoxDisplayManager != null)
        {
            //イベントを解除
            trashBoxDisplayManager.OnTrashBoxOpenedAndMouseReleased -= HandleOpenedAndReleased;

        }
    }

    /// <summary>
    /// イベントが発生した時に実行される処理
    /// ゴミ箱にファイルを入れる効果音を再生
    /// </summary>
    private void HandleOpenedAndReleased()
    {
        SoundEffectManager.Instance.PlayCategorySound("TrashDestroySound", 0);
    }

}