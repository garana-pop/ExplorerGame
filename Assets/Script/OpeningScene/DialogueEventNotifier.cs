using System;
using UnityEngine;
using OpeningScene;

/// <summary>
/// ダイアログイベントの通知を行うクラス
/// </summary>
public class DialogueEventNotifier : MonoBehaviour
{
    // ダイアログエントリ表示時のイベント
    public static event Action<DialogueEntry> OnDialogueDisplayed;

    // ダイアログタイピング完了時のイベント
    public static event Action<DialogueEntry> OnDialogueCompleted;

    // シーン終了時のイベント
    public static event Action OnSceneEnding;

    /// <summary>
    /// ダイアログエントリが表示されたことを通知
    /// </summary>
    public static void NotifyDialogueDisplayed(DialogueEntry entry)
    {
        OnDialogueDisplayed?.Invoke(entry);
    }

    /// <summary>
    /// ダイアログエントリのタイピングが完了したことを通知
    /// </summary>
    public static void NotifyDialogueCompleted(DialogueEntry entry)
    {
        OnDialogueCompleted?.Invoke(entry);
    }

    /// <summary>
    /// シーンが終了することを通知
    /// </summary>
    public static void NotifySceneEnding()
    {
        OnSceneEnding?.Invoke();
    }

    // シーン切り替え時にイベントをクリア
    private void OnDestroy()
    {
        OnDialogueDisplayed = null;
        OnDialogueCompleted = null;
        OnSceneEnding = null;
    }
}