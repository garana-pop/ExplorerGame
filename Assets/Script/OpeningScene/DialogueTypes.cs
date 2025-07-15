namespace OpeningScene
{
    /// <summary>
    /// �_�C�A���O�\���^�C�v
    /// </summary>
    public enum DialogueType
    {
        Normal,     // �ʏ�̉�b
        Narration,  // �i���[�V����
        Command     // �R�}���h�s�i�\�����������̂ݍs���j
    }

    /// <summary>
    /// �_�C�A���O�G���g���[�̃f�[�^�N���X
    /// </summary>
    public class DialogueEntry
    {
        public string speaker;        // �����Җ�
        public string dialogue;       // �Z���t���e
        public DialogueType type;     // �Z���t�^�C�v

        // �R�}���h�֘A�̃v���p�e�B
        public bool isCommand;        // �R�}���h�s���ǂ���
        public string commandParam;   // �R�}���h�p�����[�^

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