using System;
using UnityEngine;
using OpeningScene;

/// <summary>
/// �_�C�A���O�C�x���g�̒ʒm���s���N���X
/// </summary>
public class DialogueEventNotifier : MonoBehaviour
{
    // �_�C�A���O�G���g���\�����̃C�x���g
    public static event Action<DialogueEntry> OnDialogueDisplayed;

    // �_�C�A���O�^�C�s���O�������̃C�x���g
    public static event Action<DialogueEntry> OnDialogueCompleted;

    // �V�[���I�����̃C�x���g
    public static event Action OnSceneEnding;

    /// <summary>
    /// �_�C�A���O�G���g�����\�����ꂽ���Ƃ�ʒm
    /// </summary>
    public static void NotifyDialogueDisplayed(DialogueEntry entry)
    {
        OnDialogueDisplayed?.Invoke(entry);
    }

    /// <summary>
    /// �_�C�A���O�G���g���̃^�C�s���O�������������Ƃ�ʒm
    /// </summary>
    public static void NotifyDialogueCompleted(DialogueEntry entry)
    {
        OnDialogueCompleted?.Invoke(entry);
    }

    /// <summary>
    /// �V�[�����I�����邱�Ƃ�ʒm
    /// </summary>
    public static void NotifySceneEnding()
    {
        OnSceneEnding?.Invoke();
    }

    // �V�[���؂�ւ����ɃC�x���g���N���A
    private void OnDestroy()
    {
        OnDialogueDisplayed = null;
        OnDialogueCompleted = null;
        OnSceneEnding = null;
    }
}