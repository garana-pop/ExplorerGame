using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SpeakerDropArea : MonoBehaviour, IDropHandler
{
    [SerializeField] private string expectedSpeaker; // このエリアに対応する正しい発言者
    [SerializeField] private Color correctColor = new Color(0.5f, 1f, 0.5f, 1f); // 正解時の色
    [SerializeField] private Color wrongColor = new Color(1f, 0.5f, 0.5f, 1f); // 不正解時の色
    [SerializeField] private AudioClip correctSound; // 正解時の音
    [SerializeField] private AudioClip wrongSound; // 不正解時の音


    // テキスト表示設定
    [Header("テキスト表示設定")]
    [SerializeField] private Color textColor = Color.white; // テキストの色
    [SerializeField] private int fontSize = 14; // フォントサイズ
    [SerializeField] private bool isBold = false; // 太字設定
    [SerializeField] private Font customFont; // カスタムフォント（設定時のみ使用）
    [SerializeField] private TextAnchor textAlignment = TextAnchor.MiddleCenter; // テキスト配置

    [Header("進捗表示設定")]
    [Tooltip("進捗度をログに表示するかどうか")]
    [SerializeField] private bool showProgressLog = true;

    [Header("進捗UI表示設定")]
    [Tooltip("進捗数を表示するText(UGUI)コンポーネント")]
    [SerializeField] private Text progressText;

    [Tooltip("進捗数を表示するTextMeshProUGUIコンポーネント")]
    [SerializeField] private TextMeshProUGUI progressTMPText;

    [Tooltip("進捗表示のフォーマット（{0}は現在の正解数、{1}は総数に置き換えられます）")]
    [SerializeField] private string progressFormat = "{0}/{1}";

    [Tooltip("TextMeshProUGUIのフォントサイズ")]
    [SerializeField] private int tmpFontSize = 30;

    [Tooltip("TextMeshProUGUIのフォントアセット")]
    [SerializeField] private TMPro.TMP_FontAsset tmpFontAsset;

    [Header("アイコン変更通知")]
    [Tooltip("パズル完了時に通知するFileIconChangeコンポーネント")]
    [SerializeField] private FileIconChange fileIconChange;

    // オリジナルのフォントサイズを保存する変数
    private int originalTextFontSize;
    private int originalTMPFontSize;

    // UI表示用
    private Image backgroundImage;
    private Text correctSpeakerText; // 正解時に表示するテキスト

    private AudioSource audioSource;
    private Color originalColor;

    // 状態管理
    private bool isCorrect = false;
    private bool hasBeenCorrect = false; // 一度でも正解したことがあるかのフラグ

    // パズル管理
    private TxtPuzzleManager puzzleManager;

    private void Start()
    {
        // Start時にTxtPuzzleManagerを取得
        if (puzzleManager == null)
        {
            // 親方向に探索してTxtPuzzleManagerを取得
            Transform current = transform;
            while (current != null && puzzleManager == null)
            {
                puzzleManager = current.GetComponent<TxtPuzzleManager>();
                if (puzzleManager == null)
                    current = current.parent;
                else
                    break;
            }

            // ファイルパネル経由でも検索
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

        // パズルマネージャーが見つかったら、初期状態を適用
        if (puzzleManager != null)
        {
            // 初期表示のために進捗をチェック
            CheckAndUpdateProgressUI();

            // 少し遅延させて確実に正しい状態を反映
            Invoke("DelayedProgressCheck", 0.5f);
        }
    }

    private void OnEnable()
    {
        // 表示されたときにも進捗をチェック（特にロード後）
        Invoke("DelayedProgressCheck", 0.2f);
    }

    // 進捗状態をチェックして更新するメソッド
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

            // 進捗表示を更新
            UpdateProgressUI(correctCount, totalCount);

            // パズルが完成している場合、ログにも出力
            if (correctCount == totalCount && totalCount > 0 && showProgressLog)
            {
                string fileName = puzzleManager.GetFileName();
                if (string.IsNullOrEmpty(fileName)) fileName = "テキストパズル";
                //Debug.Log($"{fileName} 進捗度 {correctCount}/{totalCount} パズル完了状態です");
            }
        }
    }

    private void DelayedProgressCheck()
    {
        CheckAndUpdateProgressUI();
    }

    private void Awake()
    {
        // 背景画像の取得
        backgroundImage = GetComponent<Image>();
        if (backgroundImage != null)
        {
            originalColor = backgroundImage.color;
        }

        // AudioSourceの取得または追加
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // すでに存在するCorrectSpeakerTextを探す
        correctSpeakerText = GetComponentInChildren<Text>(true);

        // 存在しない場合のみ新規作成
        if (correctSpeakerText == null)
        {
            // まず名前で検索して既存のオブジェクトがないか確認
            Transform existingText = transform.Find("CorrectSpeakerText");
            if (existingText != null)
            {
                correctSpeakerText = existingText.GetComponent<Text>();
            }
            else
            {
                // 本当に存在しない場合のみ新規作成
                CreateNewSpeakerText();
            }
        }

        // 初期状態では空文字
        if (correctSpeakerText != null)
        {
            correctSpeakerText.text = "";
        }

        // オリジナルのフォントサイズを保存
        if (progressText != null)
        {
            originalTextFontSize = progressText.fontSize;
        }

        // TMPテキストのフォントサイズを設定
        if (progressTMPText != null)
        {
            progressTMPText.fontSize = tmpFontSize;
        }

    }

    private void FindPuzzleManager()
    {
        // 新しいFindFirstObjectByTypeを使用（警告解消）
        puzzleManager = Object.FindFirstObjectByType<TxtPuzzleManager>();

        // このエリアを登録
        puzzleManager.RegisterDropArea(this);
    }

    public void OnDrop(PointerEventData eventData)
    {
        // ドロップされたオブジェクトがSpeakerDraggableを持っているか確認
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

    // 既存のOnSpeakerDroppedメソッド内で、UpdateProgressUIの代わりにCheckAndUpdateProgressUIを呼び出し
    public bool OnSpeakerDropped(SpeakerDraggable speaker)
    {
        if (speaker == null) return false;

        // 一度正解したエリアは状態を変更しない
        if (hasBeenCorrect)
        {

            // 正解の場合のみ音を鳴らす
            if (speaker.GetSpeakerName() == expectedSpeaker && correctSound != null)
            {
                audioSource.PlayOneShot(correctSound);
            }
            else if (wrongSound != null && speaker.GetSpeakerName() != expectedSpeaker)
            {
                audioSource.PlayOneShot(wrongSound);
            }

            // 既に正解状態なのでテキストが設定されているはず
            // 念のため、テキストが空の場合のみ再設定
            if (correctSpeakerText != null && string.IsNullOrEmpty(correctSpeakerText.text))
            {
                correctSpeakerText.text = expectedSpeaker;
            }

            return true;
        }

        string speakerName = speaker.GetSpeakerName();
        bool isCorrectSpeaker = (speakerName == expectedSpeaker);

        // 視覚的フィードバック
        if (isCorrectSpeaker)
        {
            // 正解の場合
            backgroundImage.color = correctColor;

            // 正解した場合は発言者名を表示（確実に設定するように強化）
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
            hasBeenCorrect = true; // 一度正解したフラグを立てる

        }
        else if (!hasBeenCorrect) // まだ正解したことがない場合のみ
        {
            // 不正解の場合
            backgroundImage.color = wrongColor;

            // 不正解の場合はテキストをクリア
            if (correctSpeakerText != null)
            {
                correctSpeakerText.text = "";
            }

            if (wrongSound != null)
            {
                audioSource.PlayOneShot(wrongSound);
            }

            isCorrect = false;
            //Debug.Log($"不正解: {gameObject.name}");
        }

        // 正解/不正解処理の後に進捗度を表示
        if (puzzleManager != null)
        {
            // 進捗状態をチェックして更新（CheckAndUpdateProgressUIを使う）
            CheckAndUpdateProgressUI();

            // 進捗カウントを再取得
            int correctCount = 0;
            int totalCount = 0;

            foreach (var area in puzzleManager.GetDropAreas())
            {
                if (area == null) continue;
                totalCount++;
                if (area.IsCorrect()) correctCount++;
            }

            // すべて正解になった場合、完了サウンドを再生
            if (correctCount == totalCount && totalCount > 0)
            {
                // パズル完了を通知
                if (showProgressLog)
                {
                    string fileName = puzzleManager.GetFileName();
                    if (string.IsNullOrEmpty(fileName)) fileName = "テキストパズル";
                    Debug.Log($"{fileName} 進捗度 {correctCount}/{totalCount} パズル完了！");
                }

                // 完了サウンドを再生
                if (SoundEffectManager.Instance != null)
                {
                    SoundEffectManager.Instance.PlayAllRevealedSound();
                }

                // TxtPuzzleManagerにも完了を通知
                if (!puzzleManager.IsPuzzleCompleted())
                {
                    puzzleManager.CheckPuzzleCompletion();
                }

                // FileIconChangeに通知
                if (correctCount == totalCount && totalCount > 0 && fileIconChange != null)
                {
                    string fileName = puzzleManager.GetFileName();
                    if (string.IsNullOrEmpty(fileName)) fileName = "テキストパズル";
                    fileIconChange.OnPuzzleCompleted(fileName);
                }
            }
        }

        return true;
    }

    // 進捗表示UIを更新するメソッド
    private void UpdateProgressUI(int correctCount, int totalCount)
    {
        // 進捗フォーマットを使用
        string displayText = string.Format(progressFormat, correctCount, totalCount);

        // テキストを更新
        if (progressText != null)
        {
            progressText.text = displayText;
            progressText.gameObject.SetActive(true); // 確実に表示
        }

        if (progressTMPText != null)
        {
            progressTMPText.text = displayText;
            progressTMPText.fontSize = tmpFontSize;

            // フォントアセットが設定されている場合に適用
            if (tmpFontAsset != null)
            {
                progressTMPText.font = tmpFontAsset;
            }

            // パズルが完成している場合は確実に表示状態にする
            if (correctCount == totalCount && totalCount > 0)
            {
                progressTMPText.gameObject.SetActive(true);
            }
        }
    }

    // パブリックメソッド - 外部からUIを強制的に更新する場合に使用
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

    // 発言者表示用テキストが存在するか確認し、なければ作成するヘルパーメソッド
    private void EnsureCorrectSpeakerText()
    {
        if (correctSpeakerText != null) return;

        // まず名前で検索
        Transform existingText = transform.Find("CorrectSpeakerText");
        if (existingText != null)
        {
            correctSpeakerText = existingText.GetComponent<Text>();
            return;
        }

        // 自身の子からTextコンポーネントを検索
        Text[] childTexts = GetComponentsInChildren<Text>(true);
        foreach (var text in childTexts)
        {
            if (text.gameObject.name == "CorrectSpeakerText" || text.transform.parent == transform)
            {
                correctSpeakerText = text;
                return;
            }
        }

        // それでも見つからなかった場合のみ作成する
        CreateNewSpeakerText();
    }

    public bool IsCorrect()
    {
        return isCorrect;
    }

    // プロジェクト内のフォントを検索して割り当てるヘルパーメソッド
    private void FindAndAssignFont()
    {
        // プロジェクト内のすべてのフォントを検索
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
    /// このエリアで期待される話者名を取得
    /// </summary>
    public string GetExpectedSpeaker()
    {
        return expectedSpeaker;
    }

    // 正解状態の視覚表現をセットする共通メソッド
    private void SetCorrectVisualState()
    {
        // 背景色をチェックして取得が必要なら取得
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        // 背景色を正解色に
        if (backgroundImage != null)
        {
            backgroundImage.color = correctColor;
        }

        // 正解時は発言者名を表示（重複作成防止のために修正したメソッドを使用）
        EnsureCorrectSpeakerText();

        if (correctSpeakerText != null)
        {
            // 重複表示を防ぐためにテキストが空の場合のみ設定
            if (string.IsNullOrEmpty(correctSpeakerText.text))
            {
                correctSpeakerText.text = expectedSpeaker;
            }
            correctSpeakerText.color = textColor;
        }

        // 正解サウンドを再生
        if (correctSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(correctSound);
        }
    }

    // 発言者テキスト作成専用メソッド
    private void CreateNewSpeakerText()
    {
        // 既存のオブジェクトが名前で検索して存在しないことを再確認
        Transform existingTextObj = transform.Find("CorrectSpeakerText");
        if (existingTextObj != null)
        {
            correctSpeakerText = existingTextObj.GetComponent<Text>();
            if (correctSpeakerText != null)
            {
                return;
            }
        }

        //Debug.Log($"CorrectSpeakerTextを新規作成します: {gameObject.name}");

        GameObject textObj = new GameObject("CorrectSpeakerText");
        textObj.transform.SetParent(transform, false);

        correctSpeakerText = textObj.AddComponent<Text>();

        // フォント設定 - プロジェクトに合わせて調整
        try
        {
            // 新しい標準フォントを使用
            Font systemFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (systemFont != null)
            {
                correctSpeakerText.font = systemFont;
            }
            else
            {
                // プロジェクト内のフォントを検索
                FindAndAssignFont();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"フォント読み込みエラー: {ex.Message}");
            FindAndAssignFont();
        }

        // インスペクターで設定した値を適用
        correctSpeakerText.fontSize = fontSize;
        correctSpeakerText.alignment = textAlignment;
        correctSpeakerText.color = textColor;
        correctSpeakerText.fontStyle = isBold ? FontStyle.Bold : FontStyle.Normal;

        // カスタムフォントが設定されている場合はそれを使用
        if (customFont != null)
        {
            correctSpeakerText.font = customFont;
        }

        // テキストの位置を調整
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 強制的に正解状態にするメソッド（セーブロード復元用）
    /// </summary>
    public void ForceCorrectState(SpeakerDraggable speaker)
    {
        if (speaker == null) return;

        isCorrect = true;
        hasBeenCorrect = true;

        // 視覚的状態を更新
        SetCorrectVisualState();
    }

    /// <summary>
    /// 話者なしで強制的に正解状態にするメソッド（セーブロード復元用）
    /// </summary>
    public void ForceCorrectStateWithoutSpeaker()
    {
        isCorrect = true;
        hasBeenCorrect = true;

        try
        {
            // 視覚的状態を更新
            SetCorrectVisualState();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"エリア: {gameObject.name} の正解状態設定中にエラー: {ex.Message}");
        }
    }
}