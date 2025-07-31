using System.Collections;
using UnityEngine;
using TMPro;
using OpeningScene;

/// <summary>
/// オープニングシーンにおいて話者名を一文字ずつ変化させるアニメーションを制御するクラス
/// </summary>
public class SpeakerNameTransitionController : MonoBehaviour
{
    [System.Serializable]
    public class SpeakerTransitionSetting
    {
        [Tooltip("変更コマンド名（SpeakerChange_XXXの「XXX」部分）")]
        public string commandKey;

        [Tooltip("変更前の話者名")]
        public string beforeName;

        [Tooltip("変更後の話者名")]
        public string afterName;

        [Tooltip("左側キャラクターの変更ならtrue、右側ならfalse")]
        public bool isLeftCharacter;
    }

    [Header("基本設定")]
    [SerializeField] private TextMeshProUGUI leftNameText;
    [SerializeField] private TextMeshProUGUI rightNameText;

    [Header("トランジション設定")]
    [SerializeField] private float characterChangeInterval = 0.1f; // 1文字ずつ表示する間隔
    [SerializeField] private float pauseBeforeTransition = 0.5f;   // 変更開始前の待機時間
    [SerializeField] private Color transitionHighlightColor = new Color(1f, 0.8f, 0.4f); // 変更中のハイライト色
    [SerializeField] private AudioClip typeSound; // 文字変更時の効果音

    [Header("話者変更設定")]
    [SerializeField]
    private SpeakerTransitionSetting[] transitionSettings = new SpeakerTransitionSetting[]
    {
        new SpeakerTransitionSetting {
            commandKey = "father",
            beforeName = "？？？",
            afterName = "父親",
            isLeftCharacter = true
        },
        new SpeakerTransitionSetting {
            commandKey = "amnesiac",
            beforeName = "男性",
            afterName = "記憶喪失者",
            isLeftCharacter = false
        }
    };

    [Header("デバッグ")]
    [SerializeField] private bool debugMode = false;

    // 話者名変更中フラグ
    public bool IsTransitioning { get; private set; } = false;

    // 内部変数
    private AudioSource audioSource;
    private Color originalLeftNameColor;
    private Color originalRightNameColor;
    private Coroutine leftNameTransition;
    private Coroutine rightNameTransition;

    private void Awake()
    {
        // AudioSourceの取得
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // テキストコンポーネントの確認
        ValidateComponents();
    }

    private void Start()
    {
        // 元の文字色を保存
        if (leftNameText != null)
            originalLeftNameColor = leftNameText.color;

        if (rightNameText != null)
            originalRightNameColor = rightNameText.color;

        // イベントリスナーを登録
        RegisterEventListeners();
    }

    private void ValidateComponents()
    {
        // 左側の名前テキストの確認
        if (leftNameText == null)
        {
            // シーン内から探す
            GameObject leftCharacter = GameObject.Find("LeftCharacter");
            if (leftCharacter != null)
            {
                Transform nameArea = leftCharacter.transform.Find("LeftNameArea");
                if (nameArea != null)
                {
                    leftNameText = nameArea.GetComponentInChildren<TextMeshProUGUI>();
                }
            }

            if (leftNameText == null)
                Debug.LogWarning("左側キャラクターの名前テキスト(TextMeshProUGUI)が設定されていません。");
        }

        // 右側の名前テキストの確認
        if (rightNameText == null)
        {
            // シーン内から探す
            GameObject rightCharacter = GameObject.Find("RightCharacter");
            if (rightCharacter != null)
            {
                Transform nameArea = rightCharacter.transform.Find("RightNameArea");
                if (nameArea != null)
                {
                    rightNameText = nameArea.GetComponentInChildren<TextMeshProUGUI>();
                }
            }

            if (rightNameText == null)
                Debug.LogWarning("右側キャラクターの名前テキスト(TextMeshProUGUI)が設定されていません。");
        }
    }

    private void RegisterEventListeners()
    {
        // ダイアログ表示イベントにリスナーを登録
        DialogueEventNotifier.OnDialogueDisplayed += OnDialogueDisplayed;
    }

    /// <summary>
    /// ダイアログが表示されたときのイベントハンドラ
    /// </summary>
    private void OnDialogueDisplayed(DialogueEntry entry)
    {
        // コマンド行の場合は直接処理
        if (entry.isCommand && entry.type == DialogueType.Command)
        {
            ProcessSpeakerChangeCommand(entry.commandParam);
            return;
        }

        // コマンド形式かどうかをチェック（後方互換性のため）
        if (entry.dialogue.StartsWith("SpeakerChange_"))
        {
            CheckForSpeakerChangeCommand(entry.dialogue);
        }
    }

    /// <summary>
    /// 話者変更コマンドを直接処理
    /// </summary>
    private void ProcessSpeakerChangeCommand(string commandKey)
    {
        if (string.IsNullOrEmpty(commandKey))
            return;

        foreach (var setting in transitionSettings)
        {
            if (setting.commandKey == commandKey)
            {
                StartNameTransition(setting);

                if (debugMode)
                    Debug.Log($"話者名変更コマンド処理: {commandKey} ({setting.beforeName} → {setting.afterName})");

                break;
            }
        }
    }

    /// <summary>
    /// 特殊コマンド（SpeakerChange_XXX）の検出と処理（後方互換性用）
    /// </summary>
    private void CheckForSpeakerChangeCommand(string dialogue)
    {
        const string commandPrefix = "SpeakerChange_";

        // コマンド形式チェック
        if (!dialogue.Contains(commandPrefix))
            return;

        // コマンドキーを抽出
        string commandText = dialogue.Trim();
        if (!commandText.StartsWith(commandPrefix))
            return;

        string commandKey = commandText.Substring(commandPrefix.Length).Trim();
        ProcessSpeakerChangeCommand(commandKey);
    }

    /// <summary>
    /// 話者名の変更アニメーションを開始
    /// </summary>
    private void StartNameTransition(SpeakerTransitionSetting setting)
    {
        // 左右どちらのキャラクターかで処理を分岐
        if (setting.isLeftCharacter)
        {
            // 既に実行中のコルーチンがあれば停止
            if (leftNameTransition != null)
                StopCoroutine(leftNameTransition);

            // 左側キャラクターの名前を変更
            if (leftNameText != null)
            {
                leftNameTransition = StartCoroutine(AnimateNameChange(
                    leftNameText,
                    setting.beforeName,
                    setting.afterName,
                    originalLeftNameColor));
            }
        }
        else
        {
            // 既に実行中のコルーチンがあれば停止
            if (rightNameTransition != null)
                StopCoroutine(rightNameTransition);

            // 右側キャラクターの名前を変更
            if (rightNameText != null)
            {
                rightNameTransition = StartCoroutine(AnimateNameChange(
                    rightNameText,
                    setting.beforeName,
                    setting.afterName,
                    originalRightNameColor));
            }
        }
    }

    /// <summary>
    /// 名前をアニメーションで一文字ずつ変化させるコルーチン
    /// </summary>
    private IEnumerator AnimateNameChange(TextMeshProUGUI textComponent, string fromName, string toName, Color originalColor)
    {
        // 話者名変更中フラグを立てる
        IsTransitioning = true;

        // 変更開始前の待機
        yield return new WaitForSeconds(pauseBeforeTransition);

        // ハイライト色に変更
        Color originalTextColor = textComponent.color;
        textComponent.color = transitionHighlightColor;

        // まず元の名前を一文字ずつ消す
        string currentText = fromName;
        while (currentText.Length > 0)
        {
            currentText = currentText.Substring(0, currentText.Length - 1);
            textComponent.text = currentText;

            PlayTypeSound();
            yield return new WaitForSeconds(characterChangeInterval);
        }

        // 少し待機
        yield return new WaitForSeconds(characterChangeInterval * 2);

        // 新しい名前を一文字ずつ表示
        currentText = "";
        for (int i = 0; i < toName.Length; i++)
        {
            currentText += toName[i];
            textComponent.text = currentText;

            PlayTypeSound();
            yield return new WaitForSeconds(characterChangeInterval);
        }

        // 元の色に戻す
        textComponent.color = originalTextColor;

        if (debugMode)
            Debug.Log($"名前変更完了: {fromName} → {toName}");

        // 話者名変更中フラグを下ろす
        IsTransitioning = false;
    }

    /// <summary>
    /// タイプ音を再生
    /// </summary>
    private void PlayTypeSound()
    {
        if (audioSource != null && typeSound != null)
        {
            audioSource.PlayOneShot(typeSound, 0.5f);
        }
    }

    /// <summary>
    /// 手動で話者名変更をトリガーするパブリックメソッド（デバッグ用）
    /// </summary>
    public void TriggerNameChange(string commandKey)
    {
        ProcessSpeakerChangeCommand(commandKey);
    }

    private void OnDestroy()
    {
        // 話者名変更中フラグをリセット
        IsTransitioning = false;

        // イベントリスナーの登録解除
        DialogueEventNotifier.OnDialogueDisplayed -= OnDialogueDisplayed;

        // アクティブなコルーチンを停止
        if (leftNameTransition != null)
            StopCoroutine(leftNameTransition);

        if (rightNameTransition != null)
            StopCoroutine(rightNameTransition);
    }
}