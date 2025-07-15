using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// afterChangeToHerMemoryフラグがtrueの場合のみ、
/// 「思い出す」ボタン押下後にConversationFatherAndDaughterSceneに移行するコントローラー
/// MainMenuControllerと併合して動作します
/// </summary>
public class ConversationTransitionController : MonoBehaviour
{
    [Header("シーン遷移設定")]
    [Tooltip("会話シーンの名前")]
    [SerializeField] private string conversationSceneName = "ConversationFatherAndDaughterScene";

    [Tooltip("通常のゲームシーンの名前")]
    [SerializeField] private string normalGameSceneName = "MainScene";

    [Tooltip("デフォルトのオープニングシーン名")]
    [SerializeField] private string defaultOpeningSceneName = "OpeningScene";

    [Header("ボタン参照")]
    [Tooltip("「思い出す」ボタンへの参照（自動取得可能）")]
    [SerializeField] private Button startButton;

    [Header("フェード設定")]
    [Tooltip("シーン遷移時にフェード効果を使用するか")]
    [SerializeField] private bool useFadeTransition = true;

    [Tooltip("フェード時間（秒）")]
    [SerializeField] private float fadeDuration = 1.0f;

    [Tooltip("フェード用パネル（自動取得可能）")]
    [SerializeField] private CanvasGroup fadePanel;

    [Header("音響設定")]
    [Tooltip("シーン遷移時の効果音")]
    [SerializeField] private AudioClip transitionSound;

    [Tooltip("音響再生用AudioSource（自動取得可能）")]
    [SerializeField] private AudioSource audioSource;

    [Header("セーブデータ検証設定")]
    [Tooltip("セーブデータが存在しない場合は通常の遷移ロジックを使用するか")]
    [SerializeField] private bool requireSaveDataForConversation = true;

    [Header("デバッグ設定")]
    [Tooltip("デバッグログを表示するか")]
    [SerializeField] private bool debugMode = false;

    [Tooltip("強制的に会話シーンに遷移（テスト用）")]
    [SerializeField] private bool forceConversationScene = false;

    [Header("フラグ制御設定")]
    [Tooltip("afterChangeToHerMemoryフラグのみで会話シーン判定するか")]
    [SerializeField] private bool useAfterChangeFlagOnly = true;

    [Tooltip("デバッグ用：フラグ状態をログに出力")]
    [SerializeField] private bool logFlagStatus = false;

    // 内部変数
    private GameSaveManager gameSaveManager;
    private MainMenuController mainMenuController;
    private bool isTransitioning = false;

    private void Awake()
    {
        // 必要なコンポーネントの自動取得
        InitializeComponents();
    }

    private void Start()
    {
        // GameSaveManagerとMainMenuControllerの参照を取得
        InitializeManagers();

        // MainMenuControllerのStartボタンイベントを拡張
        OverrideStartButtonBehavior();
    }

    //先頭に重複防止処理を追加
    public void StartTransition()
    {
        // 既に遷移中の場合は何もしない
        if (isTransitioning)
        {
            if (debugMode)
                Debug.Log("ConversationTransitionController: 既に遷移処理中です。新しい遷移をスキップします。");
            return;
        }

        // 他のシーン遷移が進行中でないか確認
        if (SceneManager.GetActiveScene().name != "TitleScene")
        {
            if (debugMode)
                Debug.LogWarning("ConversationTransitionController: 既に別のシーンに遷移しています。処理を中断します。");
            return;
        }

        // 遷移フラグを立てる
        isTransitioning = true;

        StartCoroutine(PerformSceneTransition());
    }

    /// <summary>
    /// 必要なコンポーネントを初期化
    /// </summary>
    private void InitializeComponents()
    {
        // 「思い出す」ボタンの自動取得
        if (startButton == null)
        {
            startButton = GameObject.Find("思い出すボタン")?.GetComponent<Button>();

            if (startButton == null)
            {
                // 英語名での検索も実行
                startButton = GameObject.Find("StartButton")?.GetComponent<Button>();
            }
        }

        // AudioSourceの自動取得または作成
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        // フェード用パネルの自動取得
        if (fadePanel == null && useFadeTransition)
        {
            GameObject fadeObj = GameObject.Find("FadePanel");
            if (fadeObj != null)
            {
                fadePanel = fadeObj.GetComponent<CanvasGroup>();
                if (fadePanel == null)
                {
                    fadePanel = fadeObj.AddComponent<CanvasGroup>();
                }
            }
            else
            {
                // フェードパネルが見つからない場合は作成
                CreateFadePanel();
            }
        }
    }

    /// <summary>
    /// GameSaveManagerとMainMenuControllerの参照を初期化
    /// </summary>
    private void InitializeManagers()
    {
        // GameSaveManagerの取得
        gameSaveManager = GameSaveManager.Instance;
        if (gameSaveManager == null)
        {
            Debug.LogWarning("ConversationTransitionController: GameSaveManagerが見つかりません");
        }

        // MainMenuControllerの取得
        mainMenuController = FindFirstObjectByType<MainMenuController>();
        if (mainMenuController == null)
        {
            Debug.LogWarning("ConversationTransitionController: MainMenuControllerが見つかりません");
        }
    }

    /// <summary>
    /// MainMenuControllerのスタートボタンの動作を拡張
    /// </summary>
    private void OverrideStartButtonBehavior()
    {
        if (startButton == null)
        {
            Debug.LogError("ConversationTransitionController: 「思い出す」ボタンが見つかりません");
            return;
        }

        // 自分のリスナーのみを削除（重複登録防止）
        startButton.onClick.RemoveListener(OnStartButtonClicked);

        // 新しいイベントを登録
        startButton.onClick.AddListener(OnStartButtonClicked);

        if (debugMode)
        {
            Debug.Log("ConversationTransitionController: 「思い出す」ボタンのイベントを拡張しました");
        }
    }

    private void OnDestroy()
    {
        // 自分のリスナーのみを削除
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClicked);
        }
    }

    /// <summary>
    /// スタートボタンがクリックされた時の処理
    /// </summary>
    private void OnStartButtonClicked()
    {
        if (isTransitioning)
        {
            if (debugMode)
                Debug.Log("ConversationTransitionController: 既に遷移中です");
            return;
        }

        // RememberButtonTextChangerForHerのフラグをチェック
        RememberButtonTextChangerForHer textChanger = FindFirstObjectByType<RememberButtonTextChangerForHer>();
        if (textChanger != null && textChanger.DataResetPanelControllerBoot)
        {
            if (debugMode) Debug.Log("ConversationTransitionController: DataResetPanelControllerBootフラグがtrueのため、スタートボタン処理をスキップします");
            return; // 即座リターンで遷移処理を停止
        }

        // afterChangeToLastフラグをチェック（追加）
        if (gameSaveManager != null && gameSaveManager.GetAfterChangeToLastFlag())
        {
            if (debugMode) Debug.Log("ConversationTransitionController: afterChangeToLastフラグがtrueのため、TitleTextChangerにボタン処理を委譲します");
            // TitleTextChangerに処理を委譲し、このメソッドは終了
            return;
        }

        // 遷移フラグを即座に設定して、重複実行を防ぐ
        isTransitioning = true;

        // ボタンを無効化して誤操作を防ぐ
        if (startButton != null)
        {
            startButton.interactable = false;
        }

        // 遷移処理を実行
        StartCoroutine(PerformSceneTransition());
    }


    /// <summary>
    /// 実際のシーン遷移処理を行うコルーチン（修正版）
    /// </summary>
    private IEnumerator PerformSceneTransition()
    {
        // RememberButtonTextChangerForHerのフラグを再確認
        RememberButtonTextChangerForHer textChanger = FindFirstObjectByType<RememberButtonTextChangerForHer>();
        if (textChanger != null && textChanger.DataResetPanelControllerBoot)
        {
            if (debugMode) Debug.Log("ConversationTransitionController: DataResetPanelControllerBootフラグがtrueのため、遷移処理を中止します");
            isTransitioning = false;
            if (startButton != null) startButton.interactable = true;
            yield break; // コルーチンを終了
        }

        string targetScene = defaultOpeningSceneName;
        bool transitionSuccessful = false;

        try
        {
            bool saveExists = gameSaveManager != null && gameSaveManager.SaveDataExists();

            // セーブデータ存在チェック
            if (requireSaveDataForConversation && !saveExists)
            {
                if (debugMode)
                    Debug.Log("ConversationTransitionController: セーブデータが存在しないため、通常の遷移ロジックを使用します");

                // 新規ゲームの場合、endOpeningSceneFlagをfalseに初期化
                if (gameSaveManager != null)
                {
                    gameSaveManager.InitializeSaveData();
                    gameSaveManager.SetEndOpeningSceneFlag(false);
                    // セーブは行わない（OpeningScene完了時に行うため）

                    if (debugMode)
                        Debug.Log("ConversationTransitionController: 新規ゲームとして初期化し、endOpeningSceneFlagをfalseに設定");
                }

                targetScene = defaultOpeningSceneName;
                transitionSuccessful = true;
            }
            else if (saveExists)
            {
                // セーブデータを読み込む
                bool loadSuccess = gameSaveManager.LoadGame();

                if (loadSuccess)
                {
                    // afterChangeToHerMemoryフラグを再度確認（最新の状態を取得）
                    bool afterChangeFlag = gameSaveManager.GetAfterChangeToHerMemoryFlag();

                    // endOpeningSceneフラグも取得
                    bool isOpeningCompleted = gameSaveManager.GetEndOpeningSceneFlag();

                    if (debugMode)
                    {
                        Debug.Log($"ConversationTransitionController: セーブデータ読み込み成功");
                        Debug.Log($"ConversationTransitionController: afterChangeToHerMemoryフラグ = {afterChangeFlag}");
                        Debug.Log($"ConversationTransitionController: endOpeningSceneフラグ = {isOpeningCompleted}");
                    }

                    // afterChangeToHerMemoryフラグがtrueの場合は会話シーンへ
                    if (afterChangeFlag || forceConversationScene)
                    {
                        targetScene = conversationSceneName;
                        if (debugMode)
                            Debug.Log("ConversationTransitionController: フラグがtrueのため、会話シーンに遷移します");
                    }
                    // afterChangeToHerMemoryフラグがfalseの場合
                    else
                    {
                        // endOpeningSceneフラグで判定
                        if (isOpeningCompleted)
                        {
                            // OpeningScene完了済みならMainSceneへ
                            targetScene = normalGameSceneName; // MainScene
                            if (debugMode)
                                Debug.Log("ConversationTransitionController: OpeningScene完了済み。MainSceneへ遷移");
                        }
                        else
                        {
                            // OpeningScene未完了ならOpeningSceneへ
                            targetScene = defaultOpeningSceneName;
                            if (debugMode)
                                Debug.Log("ConversationTransitionController: OpeningScene未完了。OpeningSceneへ遷移");
                        }
                    }

                    transitionSuccessful = true;
                }
                else
                {
                    if (debugMode)
                        Debug.Log("ConversationTransitionController: セーブデータの読み込みに失敗、オープニングシーンに遷移");
                    targetScene = defaultOpeningSceneName;
                    transitionSuccessful = true;
                }
            }
            else
            {
                if (debugMode)
                    Debug.Log("ConversationTransitionController: セーブデータが存在しません、オープニングシーンに遷移");
                targetScene = defaultOpeningSceneName;
                transitionSuccessful = true;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ConversationTransitionController: シーン判定中にエラー: {ex.Message}");
            targetScene = defaultOpeningSceneName;
            transitionSuccessful = false;
        }

        // フェードとシーン遷移
        if (useFadeTransition && fadePanel != null)
        {
            yield return StartCoroutine(FadeOut());
        }

        // 最終的なシーン名を再確認（念のため）
        if (debugMode)
            Debug.Log($"ConversationTransitionController: 最終的な遷移先: {targetScene}");

        yield return StartCoroutine(LoadSceneAsync(targetScene));

        isTransitioning = false;

        if (debugMode)
        {
            string status = transitionSuccessful ? "成功" : "失敗";
            Debug.Log($"ConversationTransitionController: シーン遷移完了 ({status})");
            Debug.Log($"ConversationTransitionController: 最終的な遷移先 = {targetScene}");
        }
    }


    // ConversationTransitionController - 遅延フラグチェック用コルーチン（新規追加）
    private IEnumerator CheckAfterChangeToLastFlagAndTransition()
    {
        // GameSaveManagerのロード完了を待つ
        yield return new WaitForSeconds(0.5f);

        // afterChangeToLastフラグをチェック
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.GetAfterChangeToLastFlag())
        {
            if (debugMode) Debug.Log("ConversationTransitionController: afterChangeToLastフラグがtrueのため、シーン遷移をスキップします");
            yield break; // afterChangeToLastがtrueの場合はシーン遷移を停止
        }

        // フラグがfalseまたは取得できない場合は既存のシーン遷移処理を継続
        StartCoroutine(CheckFlagAndTransition());
    }

    /// <summary>
    /// フラグをチェックして適切なシーンに遷移
    /// </summary>
    private IEnumerator CheckFlagAndTransition()
    {
        // コルーチン開始時にも再度フラグチェック
        RememberButtonTextChangerForHer textChanger = FindFirstObjectByType<RememberButtonTextChangerForHer>();
        if (textChanger != null && textChanger.DataResetPanelControllerBoot)
        {
            if (debugMode) Debug.Log("ConversationTransitionController: CheckFlagAndTransition開始時にDataResetPanelControllerBootフラグがtrueのため、処理を停止します");
            yield break; // コルーチンを終了
        }

        // afterChangeToLastフラグの再チェック
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.GetAfterChangeToLastFlag())
        {
            if (debugMode) Debug.Log("ConversationTransitionController: CheckFlagAndTransition開始時にafterChangeToLastフラグがtrueのため、処理を停止します");
            yield break; // コルーチンを終了
        }

        isTransitioning = true;
        string targetScene = defaultOpeningSceneName;
        bool transitionSuccessful = false;

        try
        {
            bool saveExists = gameSaveManager != null && gameSaveManager.SaveDataExists();

            // セーブデータ存在チェック
            if (requireSaveDataForConversation && !saveExists)
            {
                if (debugMode)
                    Debug.Log("ConversationTransitionController: セーブデータが存在しないため、通常の遷移ロジックを使用します");

                targetScene = defaultOpeningSceneName;
                transitionSuccessful = true;
            }
            else if (saveExists)
            {
                // セーブデータを読み込む
                bool loadSuccess = gameSaveManager.LoadGame();

                if (loadSuccess)
                {
                    // afterChangeToHerMemoryフラグを再度確認（最新の状態を取得）
                    bool afterChangeFlag = gameSaveManager.GetAfterChangeToHerMemoryFlag();

                    if (debugMode)
                    {
                        Debug.Log($"ConversationTransitionController: セーブデータ読み込み成功");
                        Debug.Log($"ConversationTransitionController: afterChangeToHerMemoryフラグ = {afterChangeFlag}");
                    }

                    // フラグがfalseの場合は必ず通常の遷移を行う
                    if (!afterChangeFlag && !forceConversationScene)
                    {
                        // endOpeningSceneフラグもチェック
                        bool isOpeningCompleted = gameSaveManager.GetEndOpeningSceneFlag();

                        if (isOpeningCompleted)
                        {
                            targetScene = normalGameSceneName; // MainScene
                            if (debugMode)
                                Debug.Log("ConversationTransitionController: OpeningScene完了済み。MainSceneへ移行");
                        }
                        else
                        {
                            targetScene = defaultOpeningSceneName;
                            if (debugMode)
                                Debug.Log("ConversationTransitionController: フラグがfalseのため、オープニングシーンに遷移します");
                        }
                    }
                    else if (afterChangeFlag || forceConversationScene)
                    {
                        targetScene = conversationSceneName;
                        if (debugMode)
                            Debug.Log("ConversationTransitionController: フラグがtrueのため、会話シーンに遷移します");
                    }
                    else
                    {
                        // どちらの条件にも当てはまらない場合のフォールバック
                        targetScene = defaultOpeningSceneName;
                        if (debugMode)
                            Debug.Log("ConversationTransitionController: デフォルトでオープニングシーンに遷移します");
                    }

                    transitionSuccessful = true;
                }
                else
                {
                    if (debugMode)
                        Debug.Log("ConversationTransitionController: セーブデータの読み込みに失敗、オープニングシーンに遷移");
                    targetScene = defaultOpeningSceneName;
                    transitionSuccessful = true;
                }
            }
            else
            {
                if (debugMode)
                    Debug.Log("ConversationTransitionController: セーブデータが存在しません、オープニングシーンに遷移");
                targetScene = defaultOpeningSceneName;
                transitionSuccessful = true;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ConversationTransitionController: シーン判定中にエラー: {ex.Message}");
            targetScene = defaultOpeningSceneName;
            transitionSuccessful = false;
        }

        // フェードとシーン遷移
        if (useFadeTransition && fadePanel != null)
        {
            yield return StartCoroutine(FadeOut());
        }

        yield return StartCoroutine(LoadSceneAsync(targetScene));

        isTransitioning = false;

        if (debugMode)
        {
            string status = transitionSuccessful ? "成功" : "エラー回復";
            Debug.Log($"ConversationTransitionController: シーン遷移完了 ({status})");
            Debug.Log($"ConversationTransitionController: 最終的な遷移先 = {targetScene}");
        }
    }

    /// <summary>
    /// セーブデータの存在をチェック（新規追加）
    /// </summary>
    private bool CheckSaveDataExists()
    {
        try
        {
            // GameSaveManagerからセーブデータの存在を確認
            if (gameSaveManager != null)
            {
                bool exists = gameSaveManager.SaveDataExists();
                if (debugMode)
                    Debug.Log($"ConversationTransitionController: GameSaveManagerでのセーブデータ存在確認: {exists}");
                return exists;
            }

            // GameSaveManagerが存在しない場合はfalse
            if (debugMode)
                Debug.Log("ConversationTransitionController: GameSaveManagerが存在しないため、セーブデータなしと判定");
            return false;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ConversationTransitionController: セーブデータ存在チェック中にエラー: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// フェードアウト効果
    /// </summary>
    private IEnumerator FadeOut()
    {
        if (fadePanel == null) yield break;

        fadePanel.gameObject.SetActive(true);
        fadePanel.alpha = 0f;

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadePanel.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            yield return null;
        }

        fadePanel.alpha = 1f;
    }

    /// <summary>
    /// 非同期でシーンを読み込み
    /// </summary>
    private IEnumerator LoadSceneAsync(string sceneName)
    {
        if (debugMode)
            Debug.Log($"ConversationTransitionController: シーン '{sceneName}' を読み込み中...");

        // 非同期ロードを開始
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        // asyncLoadがnullでないことを確認
        if (asyncLoad == null)
        {
            Debug.LogError($"ConversationTransitionController: シーン '{sceneName}' の読み込みに失敗しました");
            yield break;
        }

        // 自動的にシーンがアクティブにならないようにする
        asyncLoad.allowSceneActivation = false;

        // ロードが完了するまで待機
        while (!asyncLoad.isDone)
        {
            // 進捗を表示（0～0.9の範囲）
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            if (debugMode)
                Debug.Log($"読み込み進捗: {progress * 100:F0}%");

            // ロードが90%完了したら（実際には完了している）
            if (asyncLoad.progress >= 0.9f)
            {
                // フェードアウトが完了していることを確認
                if (useFadeTransition && fadePanel != null)
                {
                    // フェードアウトが完了するまで少し待機
                    yield return new WaitForSeconds(0.1f);
                }

                // シーンをアクティブ化
                asyncLoad.allowSceneActivation = true;

                if (debugMode)
                    Debug.Log($"ConversationTransitionController: シーン '{sceneName}' をアクティブ化しました");
            }

            yield return null;
        }

        if (debugMode)
            Debug.Log($"ConversationTransitionController: シーン '{sceneName}' の読み込みが完了しました");
    }

    /// <summary>
    /// 遷移時の効果音を再生
    /// </summary>
    private void PlayTransitionSound()
    {
        if (transitionSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(transitionSound);
        }
    }

    /// <summary>
    /// フェード用パネルを動的に作成
    /// </summary>
    private void CreateFadePanel()
    {
        // Canvasを探す
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("ConversationTransitionController: Canvasが見つからないため、フェードパネルを作成できません");
            return;
        }

        // フェードパネル用GameObjectを作成
        GameObject fadeObj = new GameObject("FadePanel");
        fadeObj.transform.SetParent(canvas.transform, false);

        // RectTransformを設定（全画面をカバー）
        RectTransform rectTransform = fadeObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        // Imageコンポーネントを追加（黒背景）
        Image image = fadeObj.AddComponent<Image>();
        image.color = Color.black;

        // CanvasGroupを追加
        fadePanel = fadeObj.AddComponent<CanvasGroup>();
        fadePanel.alpha = 0f;

        // 最前面に配置
        fadeObj.transform.SetAsLastSibling();

        // 初期状態では非アクティブ
        fadeObj.SetActive(false);

        if (debugMode)
            Debug.Log("ConversationTransitionController: フェードパネルを動的に作成しました");
    }

    /// <summary>
    /// 外部からafterChangeToHerMemoryフラグの状態を確認
    /// </summary>
    public bool IsConversationSceneEligible()
    {
        if (forceConversationScene) return true;

        // セーブデータ存在チェック
        if (requireSaveDataForConversation && !CheckSaveDataExists())
        {
            if (debugMode)
                Debug.Log("ConversationTransitionController: セーブデータが存在しないため、会話シーン対象外");
            return false;
        }

        if (gameSaveManager == null) return false;

        // afterChangeToHerMemoryフラグのみをチェック
        return gameSaveManager.GetAfterChangeToHerMemoryFlag();
    }

    /// <summary>
    /// テスト用：強制的に会話シーンフラグを設定
    /// </summary>
    [ContextMenu("Debug: Force Conversation Scene")]
    public void ForceConversationSceneFlag()
    {
        forceConversationScene = true;
        if (debugMode)
            Debug.Log("ConversationTransitionController: 強制会話シーンフラグを有効にしました");
    }

    /// <summary>
    /// テスト用：通常シーンフラグにリセット
    /// </summary>
    [ContextMenu("Debug: Reset to Normal Scene")]
    public void ResetToNormalScene()
    {
        forceConversationScene = false;
        if (debugMode)
            Debug.Log("ConversationTransitionController: 通常シーンフラグにリセットしました");
    }

    /// <summary>
    /// 現在の遷移状態を取得
    /// </summary>
    public bool IsTransitioning => isTransitioning;

    /// <summary>
    /// 外部からシーン名を動的に変更
    /// </summary>
    public void SetConversationSceneName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            conversationSceneName = sceneName;
            if (debugMode)
                Debug.Log($"ConversationTransitionController: 会話シーン名を '{sceneName}' に設定しました");
        }
    }

    /// <summary>
    /// 外部から通常ゲームシーン名を動的に変更
    /// </summary>
    public void SetNormalGameSceneName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            normalGameSceneName = sceneName;
            if (debugMode)
                Debug.Log($"ConversationTransitionController: 通常ゲームシーン名を '{sceneName}' に設定しました");
        }
    }

    /// <summary>
    /// デバッグ用：現在の状態を表示
    /// </summary>
    [ContextMenu("Debug: Show Current Status")]
    public void ShowCurrentStatus()
    {
        bool saveDataExists = CheckSaveDataExists();
        bool afterChangeFlag = gameSaveManager?.GetAfterChangeToHerMemoryFlag() ?? false;
        bool eligible = IsConversationSceneEligible();

        Debug.Log($"=== ConversationTransitionController 状態 ===");
        Debug.Log($"セーブデータ存在: {saveDataExists}");
        Debug.Log($"AfterChangeフラグ: {afterChangeFlag}");
        Debug.Log($"会話シーン対象: {eligible}");
        Debug.Log($"強制会話シーン: {forceConversationScene}");
        Debug.Log($"遷移中: {isTransitioning}");
        Debug.Log($"==============================");
    }
}