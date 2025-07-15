using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpeningScene;

/// <summary>
/// OpeningSceneControllerとキャラクター表示を連携させる拡張クラス
/// </summary>
public class OpeningSceneManagerExtension : MonoBehaviour
{
    [Header("キャラクター制御")]
    [SerializeField] private CharacterDisplayController characterController;

    [Header("キャラクターのGameObject")]
    [SerializeField] private GameObject leftCharacter;
    [SerializeField] private GameObject rightCharacter;

    [Header("キャラクター設定")]
    [SerializeField] private string leftCharacterName = "父親";    // 左側キャラクター名
    [SerializeField] private string rightCharacterName = "私"; // 右側キャラクター名

    [Header("デバッグ")]
    [SerializeField] private bool debugMode = false;

    private void Awake()
    {
        // CharacterDisplayControllerが設定されていない場合は自動取得
        if (characterController == null)
        {
            characterController = GetComponent<CharacterDisplayController>();

            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterDisplayController>();
                Debug.Log("CharacterDisplayControllerをAddComponentで追加しました");
            }
        }
    }

    private void Start()
    {
        // イベントリスナーを登録
        DialogueEventNotifier.OnDialogueDisplayed += OnDialogueDisplayed;

        // キャラクターの初期設定
        SetupCharacters();
    }

    /// <summary>
    /// キャラクターの初期設定
    /// </summary>
    private void SetupCharacters()
    {
        if (characterController == null) return;

        // 左側キャラクターの設定
        if (leftCharacter != null)
        {
            // 通常の左キャラクター名を登録
            RegisterCharacter(leftCharacter, leftCharacterName);

            // "???"も左キャラクター(父親)として登録
            RegisterCharacter(leftCharacter, "？？？");
        }

        // 右側キャラクターの設定
        if (rightCharacter != null)
        {
            // 通常の右キャラクター名を登録
            RegisterCharacter(rightCharacter, rightCharacterName);

            // "記憶喪失者"も右キャラクターとして登録
            RegisterCharacter(rightCharacter, "記憶喪失者");
        }

        if (debugMode)
        {
            Debug.Log("キャラクター設定を完了しました");
        }
    }

    /// <summary>
    /// キャラクターを登録（ヘルパーメソッド）
    /// </summary>
    private void RegisterCharacter(GameObject characterObject, string characterName)
    {
        if (characterController == null || characterObject == null) return;

        // 既に対応するキャラクターが設定されていない場合のみ登録
        if (!characterController.HasCharacterForSpeaker(characterName))
        {
            // Image と TextMeshProUGUI コンポーネントの取得
            var characterImage = characterObject.GetComponent<UnityEngine.UI.Image>();

            // 名前表示用TextMeshProを取得
            var nameArea = characterObject.transform.Find("LeftNameArea") ??
                           characterObject.transform.Find("RightNameArea");

            TMPro.TextMeshProUGUI nameText = null;
            if (nameArea != null)
            {
                nameText = nameArea.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            }

            // キャラクターを追加
            characterController.AddCharacter(characterName, characterObject, characterImage, nameText);
        }
    }

    /// <summary>
    /// ダイアログ表示時のイベントハンドラ
    /// </summary>
    private void OnDialogueDisplayed(DialogueEntry entry)
    {
        if (characterController == null || entry == null) return;

        // DialogueType.Narration の場合はキャラクターをハイライトしない
        if (entry.type == DialogueType.Narration)
        {
            characterController.ResetAllCharacters();
            return;
        }

        // キャラクターのハイライト表示を更新
        characterController.HighlightCharacter(entry.speaker);

        if (debugMode)
        {
            Debug.Log($"ダイアログイベント: '{entry.speaker}' がセリフを話しています: '{entry.dialogue.Substring(0, Mathf.Min(20, entry.dialogue.Length))}...'");
        }
    }

    private void OnDestroy()
    {
        // イベントリスナーの登録解除
        DialogueEventNotifier.OnDialogueDisplayed -= OnDialogueDisplayed;
    }
}