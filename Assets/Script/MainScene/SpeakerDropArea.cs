using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SpeakerDropArea : MonoBehaviour, IDropHandler
{
    [SerializeField] private string expectedSpeaker; // ���̃G���A�ɑΉ����鐳����������
    [SerializeField] private Color correctColor = new Color(0.5f, 1f, 0.5f, 1f); // �������̐F
    [SerializeField] private Color wrongColor = new Color(1f, 0.5f, 0.5f, 1f); // �s�������̐F
    [SerializeField] private AudioClip correctSound; // �������̉�
    [SerializeField] private AudioClip wrongSound; // �s�������̉�


    // �e�L�X�g�\���ݒ�
    [Header("�e�L�X�g�\���ݒ�")]
    [SerializeField] private Color textColor = Color.white; // �e�L�X�g�̐F
    [SerializeField] private int fontSize = 14; // �t�H���g�T�C�Y
    [SerializeField] private bool isBold = false; // �����ݒ�
    [SerializeField] private Font customFont; // �J�X�^���t�H���g�i�ݒ莞�̂ݎg�p�j
    [SerializeField] private TextAnchor textAlignment = TextAnchor.MiddleCenter; // �e�L�X�g�z�u

    [Header("�i���\���ݒ�")]
    [Tooltip("�i���x�����O�ɕ\�����邩�ǂ���")]
    [SerializeField] private bool showProgressLog = true;

    [Header("�i��UI�\���ݒ�")]
    [Tooltip("�i������\������Text(UGUI)�R���|�[�l���g")]
    [SerializeField] private Text progressText;

    [Tooltip("�i������\������TextMeshProUGUI�R���|�[�l���g")]
    [SerializeField] private TextMeshProUGUI progressTMPText;

    [Tooltip("�i���\���̃t�H�[�}�b�g�i{0}�͌��݂̐��𐔁A{1}�͑����ɒu���������܂��j")]
    [SerializeField] private string progressFormat = "{0}/{1}";

    [Tooltip("TextMeshProUGUI�̃t�H���g�T�C�Y")]
    [SerializeField] private int tmpFontSize = 30;

    [Tooltip("TextMeshProUGUI�̃t�H���g�A�Z�b�g")]
    [SerializeField] private TMPro.TMP_FontAsset tmpFontAsset;

    [Header("�A�C�R���ύX�ʒm")]
    [Tooltip("�p�Y���������ɒʒm����FileIconChange�R���|�[�l���g")]
    [SerializeField] private FileIconChange fileIconChange;

    // �I���W�i���̃t�H���g�T�C�Y��ۑ�����ϐ�
    private int originalTextFontSize;
    private int originalTMPFontSize;

    // UI�\���p
    private Image backgroundImage;
    private Text correctSpeakerText; // �������ɕ\������e�L�X�g

    private AudioSource audioSource;
    private Color originalColor;

    // ��ԊǗ�
    private bool isCorrect = false;
    private bool hasBeenCorrect = false; // ��x�ł������������Ƃ����邩�̃t���O

    // �p�Y���Ǘ�
    private TxtPuzzleManager puzzleManager;

    private void Start()
    {
        // Start����TxtPuzzleManager���擾
        if (puzzleManager == null)
        {
            // �e�����ɒT������TxtPuzzleManager���擾
            Transform current = transform;
            while (current != null && puzzleManager == null)
            {
                puzzleManager = current.GetComponent<TxtPuzzleManager>();
                if (puzzleManager == null)
                    current = current.parent;
                else
                    break;
            }

            // �t�@�C���p�l���o�R�ł�����
            if (puzzleManager == null)
            {
                Transform filePanel = transform;
                while (filePanel != null && !filePanel.name.Contains("FilePanel"))
                {
                    filePanel = filePanel.parent;
                }

                if (filePanel != null)
                {
                    puzzleManager = filePanel.GetComponentInChildren<TxtPuzzleManager>(true);
                }
            }
        }

        // �p�Y���}�l�[�W���[������������A������Ԃ�K�p
        if (puzzleManager != null)
        {
            // �����\���̂��߂ɐi�����`�F�b�N
            CheckAndUpdateProgressUI();

            // �����x�������Ċm���ɐ�������Ԃ𔽉f
            Invoke("DelayedProgressCheck", 0.5f);
        }
    }

    private void OnEnable()
    {
        // �\�����ꂽ�Ƃ��ɂ��i�����`�F�b�N�i���Ƀ��[�h��j
        Invoke("DelayedProgressCheck", 0.2f);
    }

    // �i����Ԃ��`�F�b�N���čX�V���郁�\�b�h
    public void CheckAndUpdateProgressUI()
    {
        if (puzzleManager != null)
        {
            int correctCount = 0;
            int totalCount = 0;

            foreach (var area in puzzleManager.GetDropAreas())
            {
                if (area == null) continue;
                totalCount++;
                if (area.IsCorrect()) correctCount++;
            }

            // �i���\�����X�V
            UpdateProgressUI(correctCount, totalCount);

            // �p�Y�����������Ă���ꍇ�A���O�ɂ��o��
            if (correctCount == totalCount && totalCount > 0 && showProgressLog)
            {
                string fileName = puzzleManager.GetFileName();
                if (string.IsNullOrEmpty(fileName)) fileName = "�e�L�X�g�p�Y��";
                //Debug.Log($"{fileName} �i���x {correctCount}/{totalCount} �p�Y��������Ԃł�");
            }
        }
    }

    private void DelayedProgressCheck()
    {
        CheckAndUpdateProgressUI();
    }

    private void Awake()
    {
        // �w�i�摜�̎擾
        backgroundImage = GetComponent<Image>();
        if (backgroundImage != null)
        {
            originalColor = backgroundImage.color;
        }

        // AudioSource�̎擾�܂��͒ǉ�
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // ���łɑ��݂���CorrectSpeakerText��T��
        correctSpeakerText = GetComponentInChildren<Text>(true);

        // ���݂��Ȃ��ꍇ�̂ݐV�K�쐬
        if (correctSpeakerText == null)
        {
            // �܂����O�Ō������Ċ����̃I�u�W�F�N�g���Ȃ����m�F
            Transform existingText = transform.Find("CorrectSpeakerText");
            if (existingText != null)
            {
                correctSpeakerText = existingText.GetComponent<Text>();
            }
            else
            {
                // �{���ɑ��݂��Ȃ��ꍇ�̂ݐV�K�쐬
                CreateNewSpeakerText();
            }
        }

        // ������Ԃł͋󕶎�
        if (correctSpeakerText != null)
        {
            correctSpeakerText.text = "";
        }

        // �I���W�i���̃t�H���g�T�C�Y��ۑ�
        if (progressText != null)
        {
            originalTextFontSize = progressText.fontSize;
        }

        // TMP�e�L�X�g�̃t�H���g�T�C�Y��ݒ�
        if (progressTMPText != null)
        {
            progressTMPText.fontSize = tmpFontSize;
        }

    }

    private void FindPuzzleManager()
    {
        // �V����FindFirstObjectByType���g�p�i�x�������j
        puzzleManager = Object.FindFirstObjectByType<TxtPuzzleManager>();

        // ���̃G���A��o�^
        puzzleManager.RegisterDropArea(this);
    }

    public void OnDrop(PointerEventData eventData)
    {
        // �h���b�v���ꂽ�I�u�W�F�N�g��SpeakerDraggable�������Ă��邩�m�F
        GameObject dropped = eventData.pointerDrag;
        if (dropped != null)
        {
            SpeakerDraggable draggable = dropped.GetComponent<SpeakerDraggable>();
            if (draggable != null)
            {
                OnSpeakerDropped(draggable);
            }
        }
    }

    // ������OnSpeakerDropped���\�b�h���ŁAUpdateProgressUI�̑����CheckAndUpdateProgressUI���Ăяo��
    public bool OnSpeakerDropped(SpeakerDraggable speaker)
    {
        if (speaker == null) return false;

        // ��x���������G���A�͏�Ԃ�ύX���Ȃ�
        if (hasBeenCorrect)
        {

            // �����̏ꍇ�̂݉���炷
            if (speaker.GetSpeakerName() == expectedSpeaker && correctSound != null)
            {
                audioSource.PlayOneShot(correctSound);
            }
            else if (wrongSound != null && speaker.GetSpeakerName() != expectedSpeaker)
            {
                audioSource.PlayOneShot(wrongSound);
            }

            // ���ɐ�����ԂȂ̂Ńe�L�X�g���ݒ肳��Ă���͂�
            // �O�̂��߁A�e�L�X�g����̏ꍇ�̂ݍĐݒ�
            if (correctSpeakerText != null && string.IsNullOrEmpty(correctSpeakerText.text))
            {
                correctSpeakerText.text = expectedSpeaker;
            }

            return true;
        }

        string speakerName = speaker.GetSpeakerName();
        bool isCorrectSpeaker = (speakerName == expectedSpeaker);

        // ���o�I�t�B�[�h�o�b�N
        if (isCorrectSpeaker)
        {
            // �����̏ꍇ
            backgroundImage.color = correctColor;

            // ���������ꍇ�͔����Җ���\���i�m���ɐݒ肷��悤�ɋ����j
            EnsureCorrectSpeakerText();

            if (correctSpeakerText != null)
            {
                correctSpeakerText.text = expectedSpeaker;
            }

            if (correctSound != null)
            {
                audioSource.PlayOneShot(correctSound);
            }

            isCorrect = true;
            hasBeenCorrect = true; // ��x���������t���O�𗧂Ă�

        }
        else if (!hasBeenCorrect) // �܂������������Ƃ��Ȃ��ꍇ�̂�
        {
            // �s�����̏ꍇ
            backgroundImage.color = wrongColor;

            // �s�����̏ꍇ�̓e�L�X�g���N���A
            if (correctSpeakerText != null)
            {
                correctSpeakerText.text = "";
            }

            if (wrongSound != null)
            {
                audioSource.PlayOneShot(wrongSound);
            }

            isCorrect = false;
            //Debug.Log($"�s����: {gameObject.name}");
        }

        // ����/�s���������̌�ɐi���x��\��
        if (puzzleManager != null)
        {
            // �i����Ԃ��`�F�b�N���čX�V�iCheckAndUpdateProgressUI���g���j
            CheckAndUpdateProgressUI();

            // �i���J�E���g���Ď擾
            int correctCount = 0;
            int totalCount = 0;

            foreach (var area in puzzleManager.GetDropAreas())
            {
                if (area == null) continue;
                totalCount++;
                if (area.IsCorrect()) correctCount++;
            }

            // ���ׂĐ����ɂȂ����ꍇ�A�����T�E���h���Đ�
            if (correctCount == totalCount && totalCount > 0)
            {
                // �p�Y��������ʒm
                if (showProgressLog)
                {
                    string fileName = puzzleManager.GetFileName();
                    if (string.IsNullOrEmpty(fileName)) fileName = "�e�L�X�g�p�Y��";
                    Debug.Log($"{fileName} �i���x {correctCount}/{totalCount} �p�Y�������I");
                }

                // �����T�E���h���Đ�
                if (SoundEffectManager.Instance != null)
                {
                    SoundEffectManager.Instance.PlayAllRevealedSound();
                }

                // TxtPuzzleManager�ɂ�������ʒm
                if (!puzzleManager.IsPuzzleCompleted())
                {
                    puzzleManager.CheckPuzzleCompletion();
                }

                // FileIconChange�ɒʒm
                if (correctCount == totalCount && totalCount > 0 && fileIconChange != null)
                {
                    string fileName = puzzleManager.GetFileName();
                    if (string.IsNullOrEmpty(fileName)) fileName = "�e�L�X�g�p�Y��";
                    fileIconChange.OnPuzzleCompleted(fileName);
                }
            }
        }

        return true;
    }

    // �i���\��UI���X�V���郁�\�b�h
    private void UpdateProgressUI(int correctCount, int totalCount)
    {
        // �i���t�H�[�}�b�g���g�p
        string displayText = string.Format(progressFormat, correctCount, totalCount);

        // �e�L�X�g���X�V
        if (progressText != null)
        {
            progressText.text = displayText;
            progressText.gameObject.SetActive(true); // �m���ɕ\��
        }

        if (progressTMPText != null)
        {
            progressTMPText.text = displayText;
            progressTMPText.fontSize = tmpFontSize;

            // �t�H���g�A�Z�b�g���ݒ肳��Ă���ꍇ�ɓK�p
            if (tmpFontAsset != null)
            {
                progressTMPText.font = tmpFontAsset;
            }

            // �p�Y�����������Ă���ꍇ�͊m���ɕ\����Ԃɂ���
            if (correctCount == totalCount && totalCount > 0)
            {
                progressTMPText.gameObject.SetActive(true);
            }
        }
    }

    // �p�u���b�N���\�b�h - �O������UI�������I�ɍX�V����ꍇ�Ɏg�p
    public void ForceUpdateProgressUI()
    {
        if (puzzleManager != null)
        {
            int correctCount = 0;
            int totalCount = 0;

            foreach (var area in puzzleManager.GetDropAreas())
            {
                if (area == null) continue;
                totalCount++;
                if (area.IsCorrect()) correctCount++;
            }

            UpdateProgressUI(correctCount, totalCount);
        }
    }

    // �����ҕ\���p�e�L�X�g�����݂��邩�m�F���A�Ȃ���΍쐬����w���p�[���\�b�h
    private void EnsureCorrectSpeakerText()
    {
        if (correctSpeakerText != null) return;

        // �܂����O�Ō���
        Transform existingText = transform.Find("CorrectSpeakerText");
        if (existingText != null)
        {
            correctSpeakerText = existingText.GetComponent<Text>();
            return;
        }

        // ���g�̎q����Text�R���|�[�l���g������
        Text[] childTexts = GetComponentsInChildren<Text>(true);
        foreach (var text in childTexts)
        {
            if (text.gameObject.name == "CorrectSpeakerText" || text.transform.parent == transform)
            {
                correctSpeakerText = text;
                return;
            }
        }

        // ����ł�������Ȃ������ꍇ�̂ݍ쐬����
        CreateNewSpeakerText();
    }

    public bool IsCorrect()
    {
        return isCorrect;
    }

    // �v���W�F�N�g���̃t�H���g���������Ċ��蓖�Ă�w���p�[���\�b�h
    private void FindAndAssignFont()
    {
        // �v���W�F�N�g���̂��ׂẴt�H���g������
        Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
        if (fonts.Length > 0)
        {
            correctSpeakerText.font = fonts[0];
        }
    }

    public void ResetArea()
    {
        isCorrect = false;
        hasBeenCorrect = false;

        if (backgroundImage != null)
        {
            backgroundImage.color = originalColor;
        }

        if (correctSpeakerText != null)
        {
            correctSpeakerText.text = "";
        }
    }

    /// <summary>
    /// ���̃G���A�Ŋ��҂����b�Җ����擾
    /// </summary>
    public string GetExpectedSpeaker()
    {
        return expectedSpeaker;
    }

    // ������Ԃ̎��o�\�����Z�b�g���鋤�ʃ��\�b�h
    private void SetCorrectVisualState()
    {
        // �w�i�F���`�F�b�N���Ď擾���K�v�Ȃ�擾
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        // �w�i�F�𐳉�F��
        if (backgroundImage != null)
        {
            backgroundImage.color = correctColor;
        }

        // �������͔����Җ���\���i�d���쐬�h�~�̂��߂ɏC���������\�b�h���g�p�j
        EnsureCorrectSpeakerText();

        if (correctSpeakerText != null)
        {
            // �d���\����h�����߂Ƀe�L�X�g����̏ꍇ�̂ݐݒ�
            if (string.IsNullOrEmpty(correctSpeakerText.text))
            {
                correctSpeakerText.text = expectedSpeaker;
            }
            correctSpeakerText.color = textColor;
        }

        // �����T�E���h���Đ�
        if (correctSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(correctSound);
        }
    }

    // �����҃e�L�X�g�쐬��p���\�b�h
    private void CreateNewSpeakerText()
    {
        // �����̃I�u�W�F�N�g�����O�Ō������đ��݂��Ȃ����Ƃ��Ċm�F
        Transform existingTextObj = transform.Find("CorrectSpeakerText");
        if (existingTextObj != null)
        {
            correctSpeakerText = existingTextObj.GetComponent<Text>();
            if (correctSpeakerText != null)
            {
                return;
            }
        }

        //Debug.Log($"CorrectSpeakerText��V�K�쐬���܂�: {gameObject.name}");

        GameObject textObj = new GameObject("CorrectSpeakerText");
        textObj.transform.SetParent(transform, false);

        correctSpeakerText = textObj.AddComponent<Text>();

        // �t�H���g�ݒ� - �v���W�F�N�g�ɍ��킹�Ē���
        try
        {
            // �V�����W���t�H���g���g�p
            Font systemFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (systemFont != null)
            {
                correctSpeakerText.font = systemFont;
            }
            else
            {
                // �v���W�F�N�g���̃t�H���g������
                FindAndAssignFont();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"�t�H���g�ǂݍ��݃G���[: {ex.Message}");
            FindAndAssignFont();
        }

        // �C���X�y�N�^�[�Őݒ肵���l��K�p
        correctSpeakerText.fontSize = fontSize;
        correctSpeakerText.alignment = textAlignment;
        correctSpeakerText.color = textColor;
        correctSpeakerText.fontStyle = isBold ? FontStyle.Bold : FontStyle.Normal;

        // �J�X�^���t�H���g���ݒ肳��Ă���ꍇ�͂�����g�p
        if (customFont != null)
        {
            correctSpeakerText.font = customFont;
        }

        // �e�L�X�g�̈ʒu�𒲐�
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// �����I�ɐ�����Ԃɂ��郁�\�b�h�i�Z�[�u���[�h�����p�j
    /// </summary>
    public void ForceCorrectState(SpeakerDraggable speaker)
    {
        if (speaker == null) return;

        isCorrect = true;
        hasBeenCorrect = true;

        // ���o�I��Ԃ��X�V
        SetCorrectVisualState();
    }

    /// <summary>
    /// �b�҂Ȃ��ŋ����I�ɐ�����Ԃɂ��郁�\�b�h�i�Z�[�u���[�h�����p�j
    /// </summary>
    public void ForceCorrectStateWithoutSpeaker()
    {
        isCorrect = true;
        hasBeenCorrect = true;

        try
        {
            // ���o�I��Ԃ��X�V
            SetCorrectVisualState();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"�G���A: {gameObject.name} �̐�����Ԑݒ蒆�ɃG���[: {ex.Message}");
        }
    }
}