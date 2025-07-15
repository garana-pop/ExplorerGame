namespace OpeningScene
{
    /// <summary>
    /// ダイアログ表示タイプ
    /// </summary>
    public enum DialogueType
    {
        Normal,     // 通常の会話
        Narration,  // ナレーション
        Command     // コマンド行（表示せず処理のみ行う）
    }

    /// <summary>
    /// ダイアログエントリーのデータクラス
    /// </summary>
    public class DialogueEntry
    {
        public string speaker;        // 発言者名
        public string dialogue;       // セリフ内容
        public DialogueType type;     // セリフタイプ

        // コマンド関連のプロパティ
        public bool isCommand;        // コマンド行かどうか
        public string commandParam;   // コマンドパラメータ

        public DialogueEntry(string speaker, string dialogue, DialogueType type = DialogueType.Normal)
        {
            this.speaker = speaker;
            this.dialogue = dialogue;
            this.type = type;
            this.isCommand = false;
            this.commandParam = "";
        }
    }
}