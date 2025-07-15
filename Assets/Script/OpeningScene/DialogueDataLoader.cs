using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using OpeningScene;

/// <summary>
/// テキストファイルから会話データを読み込むユーティリティクラス
/// </summary>
public class DialogueDataLoader : MonoBehaviour
{
    [SerializeField] private TextAsset dialogueTextAsset;
    [SerializeField] private bool loadOnAwake = true;
    [SerializeField] private bool convertArrowToNewline = true;

    // コマンドプレフィックスの定義
    private const string SPEAKER_CHANGE_COMMAND = "SpeakerChange_";
    // exit コマンドを追加
    private const string EXIT_COMMAND = "exit";

    private List<DialogueEntry> dialogueEntries = new List<DialogueEntry>();

    private void Awake()
    {
        if (loadOnAwake && dialogueTextAsset != null)
        {
            LoadDialogueFromTextAsset(dialogueTextAsset);
        }
    }

    /// <summary>
    /// テキストアセットから会話データを読み込む
    /// </summary>
    public List<DialogueEntry> LoadDialogueFromTextAsset(TextAsset textAsset)
    {
        if (textAsset == null)
        {
            Debug.LogError("DialogueDataLoader: テキストアセットが設定されていません。");
            return new List<DialogueEntry>();
        }

        dialogueEntries.Clear();
        string[] lines = textAsset.text.Split('\n');
        ParseDialogueLines(lines);
        return dialogueEntries;
    }

    /// <summary>
    /// リソースフォルダから指定されたテキストファイルを読み込む
    /// </summary>
    public List<DialogueEntry> LoadDialogueFromResources(string resourcePath)
    {
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        if (textAsset == null)
        {
            Debug.LogError($"DialogueDataLoader: リソースが見つかりません: {resourcePath}");
            return new List<DialogueEntry>();
        }

        return LoadDialogueFromTextAsset(textAsset);
    }

    /// <summary>
    /// 読み込んだ会話データを取得
    /// </summary>
    public List<DialogueEntry> GetDialogueEntries()
    {
        return dialogueEntries;
    }

    /// <summary>
    /// テキスト行を解析して会話データに変換
    /// </summary>
    private void ParseDialogueLines(string[] lines)
    {
        // 話者名と会話内容を抽出するための正規表現パターン
        Regex dialoguePattern = new Regex(@"^(?:([^:：\[\]]+)[:：]|「(.+)」|.*「(.+)」.*|^\[(.+)\])", RegexOptions.Compiled);
        string currentSpeaker = "";

        foreach (string line in lines)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//"))
                continue;

            // SpeakerChange_コマンドの処理
            if (trimmedLine.StartsWith(SPEAKER_CHANGE_COMMAND))
            {
                string commandParam = trimmedLine.Substring(SPEAKER_CHANGE_COMMAND.Length).Trim();
                // コマンド専用のDialogueEntryを作成（isCommandフラグをtrueに設定）
                DialogueEntry commandEntry = new DialogueEntry("", trimmedLine, DialogueType.Command);
                commandEntry.isCommand = true;
                commandEntry.commandParam = commandParam;
                dialogueEntries.Add(commandEntry);
                continue;
            }

            // exitコマンドの処理を追加
            if (trimmedLine.Equals(EXIT_COMMAND, System.StringComparison.OrdinalIgnoreCase))
            {
                DialogueEntry exitCommandEntry = new DialogueEntry("", EXIT_COMMAND, DialogueType.Command);
                exitCommandEntry.isCommand = true;
                exitCommandEntry.commandParam = EXIT_COMMAND;
                dialogueEntries.Add(exitCommandEntry);
                continue;
            }

            // ナレーション形式 [説明文] の処理
            if (trimmedLine.StartsWith("[") && trimmedLine.EndsWith("]"))
            {
                string narration = convertArrowToNewline ? trimmedLine.Replace("←", "\n") : trimmedLine;
                dialogueEntries.Add(new DialogueEntry("", narration, DialogueType.Narration));
                continue;
            }

            // 話者と会話内容を抽出
            Match match = dialoguePattern.Match(trimmedLine);
            if (match.Success)
            {
                ProcessMatchedLine(match, trimmedLine, ref currentSpeaker);
            }
            else if (trimmedLine.Contains(":") || trimmedLine.Contains("："))
            {
                ProcessColonSeparatedLine(trimmedLine, ref currentSpeaker);
            }
            else
            {
                // パターンに一致しない場合は、そのまま会話として追加
                string dialogue = convertArrowToNewline ? trimmedLine.Replace("←", "\n") : trimmedLine;
                dialogueEntries.Add(new DialogueEntry(currentSpeaker, dialogue));
            }
        }
    }

    private void ProcessMatchedLine(Match match, string line, ref string currentSpeaker)
    {
        // 話者名がある場合 (「名前:」または「名前：」の形式)
        if (!string.IsNullOrEmpty(match.Groups[1].Value))
        {
            currentSpeaker = match.Groups[1].Value.Trim();
            string dialogue = ExtractDialogueAfterColon(line);

            string processedDialogue = convertArrowToNewline ? dialogue.Replace("←", "\n") : dialogue;
            dialogueEntries.Add(new DialogueEntry(currentSpeaker, processedDialogue));
        }
        // 引用符がある場合（「セリフ」の形式）
        else if (!string.IsNullOrEmpty(match.Groups[2].Value) || !string.IsNullOrEmpty(match.Groups[3].Value))
        {
            string dialogue = !string.IsNullOrEmpty(match.Groups[2].Value)
                ? match.Groups[2].Value
                : match.Groups[3].Value;

            // 話者名を抽出
            string speakerName = ExtractSpeakerName(line);
            if (!string.IsNullOrEmpty(speakerName))
            {
                currentSpeaker = speakerName.Trim();
            }

            string processedDialogue = convertArrowToNewline ? dialogue.Replace("←", "\n") : dialogue;
            dialogueEntries.Add(new DialogueEntry(currentSpeaker, processedDialogue));
        }
        // ナレーションの場合
        else if (!string.IsNullOrEmpty(match.Groups[4].Value))
        {
            string narration = match.Groups[4].Value;
            string processedNarration = convertArrowToNewline ? narration.Replace("←", "\n") : narration;
            dialogueEntries.Add(new DialogueEntry("", processedNarration, DialogueType.Narration));
        }
    }

    private void ProcessColonSeparatedLine(string line, ref string currentSpeaker)
    {
        // コロンを含む場合は話者とセリフを分離
        int colonIndex = line.IndexOf(':');
        if (colonIndex == -1) colonIndex = line.IndexOf('：');

        string speaker = line.Substring(0, colonIndex).Trim();
        string dialogue = line.Substring(colonIndex + 1).Trim();

        // ←記号を改行に変換
        if (convertArrowToNewline)
        {
            dialogue = dialogue.Replace("←", "\n");
        }

        currentSpeaker = speaker;
        dialogueEntries.Add(new DialogueEntry(speaker, dialogue));
    }

    /// <summary>
    /// 「」形式の会話から話者名を抽出
    /// </summary>
    private string ExtractSpeakerName(string line)
    {
        int quoteIndex = line.IndexOf('「');
        if (quoteIndex > 0)
        {
            return line.Substring(0, quoteIndex).Trim();
        }
        return "";
    }

    /// <summary>
    /// コロン以降のテキストを抽出
    /// </summary>
    private string ExtractDialogueAfterColon(string line)
    {
        int colonIndex = line.IndexOf(':');
        if (colonIndex == -1) colonIndex = line.IndexOf('：');

        string dialogue = line.Substring(colonIndex + 1).Trim();
        if (dialogue.StartsWith("：")) dialogue = dialogue.Substring(1).Trim();

        return dialogue;
    }
}