using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// TitleSceneのタイトルテキストを一文字ずつ変更するコンポーネント
/// DaughterRequestSceneから遷移してきた場合のみ動作
/// </summary>
public class TitleTextChanger : MonoBehaviour
{
    [Header("テキスト設定")]
    [Tooltip("変更対象のTextMeshProコンポーネント")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("変更後のテキスト")]
    [SerializeField] private string newTitleText = "「彼女」の記憶";

    [Tooltip("元のテキスト（復元用）")]
    [SerializeField] private string originalTitleText = "「願い」の記憶";

    [Header("アニメーション設定")]
    [Tooltip("1文字変更にかかる時間（秒）")]
    [SerializeField] private float changeInterval = 0.3f;

    [Tooltip("変更開始までの遅延時間（秒）")]
    [SerializeField] private float startDelay = 1.0f;

    [Tooltip("文字変更時のエフェクト（フェード、グリッチなど）")]
    [SerializeField] private bool useGlitchEffect = true;

    [Tooltip("グリッチエフェクトの持続時間（秒）")]
    [SerializeField] private float glitchDuration = 0.1f;

    [Header("効果音設定")]
    [Tooltip("文字変更時の効果音")]
    [SerializeField] private AudioClip changeSound;

    [Tooltip("完了時の効果音")]
    [SerializeField] private AudioClip completeSound;

    [Header("シーン遷移チェック")]
    [Tooltip("特定のシーンから遷移した場合のみ動作するか")]
    [SerializeField] private bool checkPreviousScene = true;

    [Tooltip("前のシーン名（この名前から遷移した場合のみ動作）")]
    [SerializeField] private string previousSceneName = "DaughterRequest";

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceExecute = false; // デバッグ用：強制実行

    [Header("フラグ管理設定")]
    [Tooltip("hasChangedがtrueの場合のみAfterChangeToHerMemoryフラグを設定するか")]
    [SerializeField] private bool onlySetFlagWhenChanged = true;

    [Tooltip("手動でフラグをリセットする（デバッグ用）")]
    [SerializeField] private bool manualResetFlag = false;

    [Header("セーブデータ検証設定")]
    [Tooltip("セーブデータが存在しない場合はhasChangedをfalseにするか")]
    [SerializeField] private bool requireSaveDataForChange = true;

    [Header("遷移後の自動実行設定")]
    [Tooltip("DaughterRequestSceneから遷移した場合に自動実行するかどうか")]
    [SerializeField] private bool autoExecuteOnTransition = true;

    [Header("ボタン制御設定")]
    [Tooltip("タイトル変更中にMenuContainerのボタンを無効化するか")]
    [SerializeField] private bool disableButtonsDuringChange = true;

    [Tooltip("MenuContainerへの参照（未設定の場合は自動検索）")]
    [SerializeField] private GameObject menuContainer;

    [Tooltip("遷移フラグを保持する時間（秒）")]
    [SerializeField] private float transitionFlagDuration = 1.0f;

    // プライベート変数
    private AudioSource audioSource;
    private string currentText;
    private bool isChanging = false;
    private bool hasChanged = false;
    private static bool shouldExecuteOnNextLoad = false;
    private float transitionFlagTimer = 0f;
    private bool soundEnabled = true;

    // グリッチ用の文字
    private readonly string glitchChars = "!#$%&'()*+,-./0123456789:;<=>?@[]^_`{|}~";

    private void Awake()
    {
        // TextMeshProコンポーネントの取得
        if (titleText == null)
        {
            titleText = GetComponent<TMP_Text>();
            if (titleText == null)
            {
                titleText = GetComponentInChildren<TMP_Text>();
            }
        }

        if (titleText == null)
        {
            Debug.LogError("TitleTextChanger: TextMeshProコンポーネントが見つかりません。");
            enabled = false;
            return;
        }

        // AudioSourceの取得または作成
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && (changeSound != null || completeSound != null))
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        // 現在のテキストを保存
        currentText = titleText.text;
        if (string.IsNullOrEmpty(originalTitleText))
        {
            originalTitleText = currentText;
        }

        // 遷移フラグのチェック
        if (shouldExecuteOnNextLoad)
        {
            transitionFlagTimer = transitionFlagDuration;
            if (debugMode)
                Debug.Log("TitleTextChanger: 遷移フラグが設定されています。");
        }
    }

    private void Start()
    {
        // afterChangeToHerMemoryフラグの状態を確認
        bool afterChangeFlag = GetAfterChangeToHerMemoryFlag();

        if (debugMode)
        {
            Debug.Log($"TitleTextChanger: Start時のafterChangeToHerMemoryフラグ = {afterChangeFlag}");
        }

        // フラグがtrueの場合は何もしない（既に変更済み）
        if (afterChangeFlag)
        {
            if (debugMode)
                Debug.Log("TitleTextChanger: 既にテキスト変更済みのため、処理をスキップします");
            return;
        }

        // 遷移フラグをチェック
        if (shouldExecuteOnNextLoad)
        {
            shouldExecuteOnNextLoad = false; // フラグをリセット

            if (debugMode)
                Debug.Log("TitleTextChanger: DaughterRequestSceneからの遷移を検出。テキスト変更を開始します。");

            StartCoroutine(StartTextChange());
        }
    }

    /// <summary>
    /// 次回TitleScene読み込み時にテキスト変更を実行するフラグを設定
    /// </summary>
    public static void SetExecuteOnNextLoad()
    {
        shouldExecuteOnNextLoad = true;
        Debug.Log("TitleTextChanger: 次回読み込み時の実行フラグを設定しました。");
    }

    /// <summary>
    /// 外部から強制的にテキスト変更を開始
    /// </summary>
    public void ForceStartTextChange()
    {
        if (!isChanging && !hasChanged)
        {
            // afterChangeFlagの確認
            bool afterChangeFlag = GetAfterChangeToHerMemoryFlag();
            if (!afterChangeFlag)
            {
                if (debugMode)
                    Debug.Log("TitleTextChanger: afterChangeToHerMemoryがfalseのため、テキスト変更をスキップします");
                return;
            }

            // セーブデータの存在チェック
            bool saveDataExists = CheckSaveDataExists();
            if (requireSaveDataForChange && !saveDataExists)
            {
                if (debugMode)
                    Debug.Log("TitleTextChanger: セーブデータが存在しないため、hasChangedをfalseに設定します。");
                hasChanged = false;
                return;
            }

            if (debugMode)
                Debug.Log("TitleTextChanger: 強制的にテキスト変更を開始します。");

            StartCoroutine(StartTextChange());
        }
    }

    // Updateメソッドを追加
    private void Update()
    {
        // 遷移フラグのタイマー処理
        if (transitionFlagTimer > 0)
        {
            transitionFlagTimer -= Time.deltaTime;
            if (transitionFlagTimer <= 0)
            {
                shouldExecuteOnNextLoad = false;
                if (debugMode)
                    Debug.Log("TitleTextChanger: 遷移フラグがタイムアウトしました。");
            }
        }
    }

    /// <summary>
    /// セーブデータの存在をチェック（新規追加）
    /// </summary>
    private bool CheckSaveDataExists()
    {
        try
        {
            // GameSaveManager からセーブデータの存在を確認
            if (GameSaveManager.Instance != null)
            {
                bool exists = GameSaveManager.Instance.SaveDataExists();
                if (debugMode)
                    Debug.Log($"TitleTextChanger: GameSaveManager でのセーブデータ存在確認: {exists}");
                return exists;
            }

            if (debugMode)
                Debug.Log($"TitleTextChanger: GameSaveManagerが存在しないため、セーブデータなしと判定");

            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"TitleTextChanger: セーブデータ存在チェック中にエラー: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// テキスト変更を開始するコルーチン
    /// </summary>
    private IEnumerator StartTextChange()
    {
        // 変更開始時にボタンを無効化
        SetMenuButtonsInteractable(false);

        // 開始遅延
        if (startDelay > 0)
        {
            yield return new WaitForSeconds(startDelay);
        }

        isChanging = true;

        // 文字を一つずつ変更
        for (int i = 0; i < newTitleText.Length; i++)
        {
            if (i < currentText.Length)
            {
                // 既存の文字を置き換え
                yield return StartCoroutine(ChangeCharacterAt(i, newTitleText[i]));
            }
            else
            {
                // 新しい文字を追加
                currentText += newTitleText[i];
                titleText.text = currentText;
                PlayChangeSound();
            }

            yield return new WaitForSeconds(changeInterval);
        }

        // 元のテキストの方が長い場合、余分な文字を削除
        if (currentText.Length > newTitleText.Length)
        {
            currentText = newTitleText;
            titleText.text = currentText;
        }

        // 変更完了時にボタンを有効化
        SetMenuButtonsInteractable(true);

        // 完了処理
        OnChangeComplete();
    }

    /// <summary>
    /// 指定位置の文字を変更するコルーチン
    /// </summary>
    private IEnumerator ChangeCharacterAt(int index, char newChar)
    {
        if (useGlitchEffect)
        {
            // グリッチエフェクト
            float elapsedTime = 0;
            while (elapsedTime < glitchDuration)
            {
                // ランダムな文字に一時的に変更
                char[] chars = currentText.ToCharArray();
                chars[index] = glitchChars[Random.Range(0, glitchChars.Length)];
                titleText.text = new string(chars);

                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        // 最終的な文字に変更
        char[] finalChars = currentText.ToCharArray();
        finalChars[index] = newChar;
        currentText = new string(finalChars);
        titleText.text = currentText;

        // 効果音再生
        PlayChangeSound();
    }

    /// <summary>
    /// 文字変更時の効果音を再生
    /// </summary>
    private void PlayChangeSound()
    {
        // 効果音が無効の場合は再生しない
        if (!soundEnabled) return;

        if (changeSound != null)
        {
            // SoundEffectManagerを優先使用
            if (SoundEffectManager.Instance != null)
            {
                SoundEffectManager.Instance.PlaySound(changeSound);
            }
            else if (audioSource != null)
            {
                audioSource.PlayOneShot(changeSound);
            }
        }
    }

    /// <summary>
    /// 変更完了時の処理
    /// </summary>
    private void OnChangeComplete()
    {
        isChanging = false;
        hasChanged = true;

        // 効果音が有効な場合のみ完了音を再生
        if (soundEnabled && completeSound != null)
        {
            if (SoundEffectManager.Instance != null)
            {
                SoundEffectManager.Instance.PlaySound(completeSound);
            }
            else if (audioSource != null)
            {
                audioSource.PlayOneShot(completeSound);
            }
        }

        if (debugMode)
        {
            Debug.Log("TitleTextChanger: テキスト変更が完了しました。");
        }

        // GameSaveManagerにafterChangeToHerMemoryフラグを設定
        SetAfterChangeToHerMemoryFlag();
    }

    /// <summary>
    /// MenuContainer内のボタンの有効/無効を切り替える
    /// </summary>
    private void SetMenuButtonsInteractable(bool interactable)
    {
        if (!disableButtonsDuringChange) return;

        // MenuContainerの取得
        if (menuContainer == null)
        {
            menuContainer = GameObject.Find("MenuContainer");
        }

        if (menuContainer == null)
        {
            Debug.LogWarning($"{GetType().Name}: MenuContainerが見つかりません");
            return;
        }

        // 全てのButtonコンポーネントを取得して制御
        Button[] buttons = menuContainer.GetComponentsInChildren<Button>();
        foreach (Button button in buttons)
        {
            button.interactable = interactable;
        }

        if (debugMode)
        {
            Debug.Log($"{GetType().Name}: MenuContainerのボタンを{(interactable ? "有効" : "無効")}化しました");
        }
    }

    private void SetAfterChangeToHerMemoryFlag()
    {
        try
        {
            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.SetAfterChangeToHerMemoryFlag(true);

                if (debugMode)
                    Debug.Log("TitleTextChanger: AfterChangeToHerMemoryフラグをtrueに設定しました");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"TitleTextChanger: フラグ設定中にエラー: {ex.Message}");
        }
    }


    /// <summary>
    /// タイトルが"「彼女」の記憶"に変更完了をGameSaveManagerに通知
    /// </summary>
    private void NotifyTitleChangeCompleted()
    {
        try
        {
            if (GameSaveManager.Instance != null)
            {
                // hasChangedに関係なく、変更が完了したらafterChangeToHerMemoryフラグをtrueに設定
                GameSaveManager.Instance.SetAfterChangeToHerMemoryFlag(true);

                if (debugMode)
                    Debug.Log("TitleTextChanger: AfterChangeToHerMemoryフラグをtrueに設定しました");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"TitleTextChanger: フラグ設定中にエラー: {ex.Message}");
        }
    }



    /// <summary>
    /// テキストを元に戻す（デバッグ用）
    /// </summary>
    public void ResetText()
    {
        if (!isChanging)
        {
            titleText.text = originalTitleText;
            currentText = originalTitleText;
            hasChanged = false;

            if (debugMode)
            {
                Debug.Log("TitleTextChanger: テキストをリセットしました。");
            }
        }
    }

    /// <summary>
    /// 変更を即座に完了させる（デバッグ用）
    /// </summary>
    public void CompleteImmediately()
    {
        StopAllCoroutines();
        titleText.text = newTitleText;
        currentText = newTitleText;
        OnChangeComplete();
    }

    /// <summary>
    /// 文字変更が完了したかどうかを取得する
    /// </summary>
    public bool HasChanged
    {
        get
        {
            // afterChangeToHerMemoryフラグを優先して返す
            if (GameSaveManager.Instance != null)
            {
                bool afterChangeFlag = GameSaveManager.Instance.GetAfterChangeToHerMemoryFlag();
                if (afterChangeFlag)
                {
                    return true;
                }
            }

            // セーブデータ存在チェックが有効で、セーブデータが存在しない場合はfalse
            if (requireSaveDataForChange && !CheckSaveDataExists())
            {
                if (debugMode)
                    Debug.Log("TitleTextChanger: セーブデータが存在しないため、HasChanged = false を返します。");
                return false;
            }

            return hasChanged;
        }
    }

    /// <summary>
    /// 現在変更中かどうかを取得する
    /// </summary>
    public bool IsChanging => isChanging;

    /// <summary>
    /// AfterChangeToHerMemoryフラグの状態を取得（外部から確認用）
    /// </summary>
    public bool GetAfterChangeToHerMemoryFlag()
    {
        if (GameSaveManager.Instance != null)
        {
            return GameSaveManager.Instance.GetAfterChangeToHerMemoryFlag();
        }
        return false;
    }


    /// <summary>
    /// テスト用：タイトル関連のフラグをすべて初期化
    /// </summary>
    [ContextMenu("Debug: Reset Title Flags")]
    public void ResetAllTitleFlags()
    {
        // テキストを元に戻す
        titleText.text = originalTitleText;
        currentText = originalTitleText;
        hasChanged = false;
        isChanging = false;

        // GameSaveManagerのフラグもリセット
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.SetAfterChangeToHerMemoryFlag(false);
        }

        Debug.Log("TitleTextChanger: タイトル関連フラグを初期化しました");
    }

    /// <summary>
    /// 変更後のテキストを取得（TitleTextLoaderから参照用）
    /// </summary>
    public string NewTitleText => newTitleText;

    /// <summary>
    /// 元のテキストを取得（TitleTextLoaderから参照用）
    /// </summary>
    public string OriginalTitleText => originalTitleText;

    /// <summary>
    /// 効果音の有効/無効を設定
    /// </summary>
    /// <param name="enabled">有効にする場合はtrue</param>
    public void SetSoundEnabled(bool enabled)
    {
        soundEnabled = enabled;
        if (debugMode) Debug.Log($"TitleTextChanger: 効果音を{(enabled ? "有効" : "無効")}にしました");
    }
}