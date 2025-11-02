using UnityEngine;
using OpeningScene;

/// <summary>
/// セリフ・ナレーション表示時のサウンドエフェクト再生を管理
/// </summary>
public class NarrationSoundController : MonoBehaviour
{
    #region Inspector設定

    [Header("Volume Settings")]
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("効果音のボリューム倍率")]
    private float soundVolume = 1f;

    [Header("Debug")]
    [SerializeField]
    [Tooltip("デバッグログを表示するか")]
    private bool debugMode = false;

    #endregion

    #region Unity Lifecycle

    private void OnEnable()
    {
        // ダイアログ表示イベントを購読
        DialogueEventNotifier.OnDialogueDisplayed += OnDialogueDisplayed;
    }

    private void OnDisable()
    {
        // イベント購読解除
        DialogueEventNotifier.OnDialogueDisplayed -= OnDialogueDisplayed;
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// ダイアログ表示イベントのハンドラ
    /// </summary>
    private void OnDialogueDisplayed(DialogueEntry entry)
    {
        if (entry == null)
            return;

        // 効果音キーが設定されている場合のみ再生
        if (!string.IsNullOrEmpty(entry.soundEffectKey))
        {
            PlaySoundEffect(entry.soundEffectKey);
        }
    }

    #endregion

    #region Sound Playback Methods

    /// <summary>
    /// 効果音を再生
    /// </summary>
    private void PlaySoundEffect(string soundKey)
    {
        // SoundEffectManager.Instanceを直接参照
        var manager = SoundEffectManager.Instance;
        if (manager == null)
        {
            DebugLogger.LogError($"{nameof(NarrationSoundController)}: SoundEffectManagerが見つかりません");
            return;
        }

        manager.PlaySound(soundKey, soundVolume);

        if (debugMode)
        {
            DebugLogger.Log($"{nameof(NarrationSoundController)}: 効果音 '{soundKey}' を再生しました (Volume: {soundVolume})");
        }
    }

    #endregion
}