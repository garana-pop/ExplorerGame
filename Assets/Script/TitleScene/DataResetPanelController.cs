using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// RememberButtonTextChangerForHerクラスのDataResetPanelControllerBootフラグがtrue、
/// またはgame_save.jsonのafterChangeToLastフラグがtrueの場合に
/// 「思い出す」ボタン押下後にデータ初期化確認パネルを表示するクラス
/// </summary>
public class DataResetPanelController : MonoBehaviour
{
    [Header("パネル参照")]
    [Tooltip("データ初期化確認パネル")]
    [SerializeField] private GameObject dataResetConfirmationPanel;

    [Tooltip("設定パネル")] 
    [SerializeField] private GameObject settingsPanel;

    [Header("ボタン参照")]
    [Tooltip("思い出すボタン")]
    [SerializeField] private Button rememberButton;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = true;

    private void Awake()
    {
        // パネルの初期状態を設定
        if (dataResetConfirmationPanel != null)
        {
            dataResetConfirmationPanel.SetActive(false);
        }
    }

    private void Start()
    {

        // ボタンのクリックイベントに登録
        RegisterButtonListener();

        // ゲームロード時のafterChangeToLastフラグチェック（要件2）
        CheckAfterChangeToLastFlagOnLoad();

        // 起動時のフラグ状態をログ出力
        if (debugMode)
        {
            CheckAndLogFlags();
        }
    }

    /// <summary>
    /// ゲームロード時にafterChangeToLastフラグをチェック
    /// </summary>
    private void CheckAfterChangeToLastFlagOnLoad()
    {
        // GameSaveManagerがロード完了するまで少し待機
        StartCoroutine(CheckAfterChangeToLastFlagDelayed());
    }

    /// <summary>
    /// 遅延してafterChangeToLastフラグをチェック
    /// </summary>
    private IEnumerator CheckAfterChangeToLastFlagDelayed()
    {
        // GameSaveManagerのロード完了を待つ
        yield return new WaitForSeconds(0.5f);

        if (GameSaveManager.Instance != null)
        {
            bool afterChangeToLastFlag = GameSaveManager.Instance.GetAfterChangeToLastFlag();
            if (afterChangeToLastFlag)
            {
                if (debugMode) Debug.Log("DataResetPanelController: ゲームロード時にafterChangeToLastフラグがtrueを検出");
                // フラグの状態を保持（ボタンクリック時に使用）
            }
        }
    }

    private void OnEnable()
    {

        // OnEnableでも登録を試みる（後から有効化された場合の対策）
        RegisterButtonListener();
    }

    /// <summary>
    /// ボタンリスナーの登録処理
    /// </summary>
    private void RegisterButtonListener()
    {
        if (rememberButton != null)
        {
            // 既存のリスナーを一旦削除してから追加（重複防止）
            rememberButton.onClick.RemoveListener(OnRememberButtonClicked);
            rememberButton.onClick.AddListener(OnRememberButtonClicked);
        }
        else
        {
            Debug.LogError("DataResetPanelController: 思い出すボタンが設定されていません");
        }
    }

    /// <summary>
    /// 思い出すボタンがクリックされた時の処理
    /// </summary>
    private void OnRememberButtonClicked()
    {

        bool shouldShowPanel = false;
        string reason = "";

        // RememberButtonTextChangerForHerのフラグをチェック
        RememberButtonTextChangerForHer textChanger = FindFirstObjectByType<RememberButtonTextChangerForHer>();
        if (textChanger != null && textChanger.DataResetPanelControllerBoot)
        {
            shouldShowPanel = true;
            reason = "DataResetPanelControllerBootフラグがtrue";
            if (debugMode) Debug.Log($"DataResetPanelController: {reason}");
        }

        // GameSaveManagerのafterChangeToLastフラグをチェック
        if (!shouldShowPanel && GameSaveManager.Instance != null)
        {
            bool afterChangeToLastFlag = GameSaveManager.Instance.GetAfterChangeToLastFlag();
            if (afterChangeToLastFlag)
            {
                shouldShowPanel = true;
                reason = "afterChangeToLastフラグがtrue";
                if (debugMode) Debug.Log($"DataResetPanelController: {reason}");
            }
        }

        // パネル表示判定
        if (shouldShowPanel)
        {
            if (debugMode) Debug.Log($"DataResetPanelController: {reason}のため、データ初期化確認パネルを表示します");
            ShowDataResetPanel();
        }
        else
        {
            if (debugMode) Debug.Log("DataResetPanelController: パネル表示条件を満たしていません");
        }
    }

    /// <summary>
    /// データ初期化確認パネルを表示
    /// </summary>
    //private void ShowDataResetPanel()
    //{
    //    // まず設定パネルをアクティブにする
    //    if (settingsPanel != null)
    //    {
    //        if (!settingsPanel.activeSelf)
    //        {
    //            settingsPanel.SetActive(true);
    //            if (debugMode) Debug.Log("DataResetPanelController: 設定パネルをアクティブにしました");
    //        }
    //    }
    //    else
    //    {
    //        Debug.LogWarning("DataResetPanelController: 設定パネルが設定されていません");
    //    }

    //    // その後、データ初期化確認パネルを表示
    //    if (dataResetConfirmationPanel != null)
    //    {
    //        dataResetConfirmationPanel.SetActive(true);
    //        if (debugMode) Debug.Log("DataResetPanelController: データ初期化確認パネルを表示しました");
    //    }
    //    else
    //    {
    //        Debug.LogError("DataResetPanelController: データ初期化確認パネルが設定されていません");
    //    }
    //}
    private void ShowDataResetPanel()
    {
        // コルーチンを開始して処理を行う
        StartCoroutine(ShowDataResetPanelCoroutine());
    }

    private IEnumerator ShowDataResetPanelCoroutine()
    {
        // まず設定パネルをアクティブにする
        if (settingsPanel != null)
        {
            if (!settingsPanel.activeSelf)
            {
                settingsPanel.SetActive(true);
                if (debugMode) Debug.Log("DataResetPanelController: 設定パネルをアクティブにしました");
            }
        }
        else
        {
            Debug.LogWarning("DataResetPanelController: 設定パネルが設定されていません");
        }

        // 1フレーム待機（重要）
        yield return null;

        // その後、データ初期化確認パネルを表示
        if (dataResetConfirmationPanel != null)
        {
            dataResetConfirmationPanel.SetActive(true);
            if (debugMode) Debug.Log("DataResetPanelController: データ初期化確認パネルを表示しました");
        }
        else
        {
            Debug.LogError("DataResetPanelController: データ初期化確認パネルが設定されていません");
        }
    }

    /// <summary>
    /// パネルを非表示にする（外部から呼び出し可能）
    /// </summary>
    public void HideDataResetPanel()
    {
        if (dataResetConfirmationPanel != null)
        {
            dataResetConfirmationPanel.SetActive(false);

            if (debugMode) Debug.Log("DataResetPanelController: データ初期化確認パネルを非表示にしました");
        }
    }

    /// <summary>
    /// フラグの状態をチェックしてログ出力（デバッグ用）
    /// </summary>
    private void CheckAndLogFlags()
    {
        Debug.Log("=== DataResetPanelController フラグ状態 ===");

        // RememberButtonTextChangerForHerのフラグ
        RememberButtonTextChangerForHer textChanger = FindFirstObjectByType<RememberButtonTextChangerForHer>();
        if (textChanger != null)
        {
            Debug.Log($"DataResetPanelControllerBootフラグ: {textChanger.DataResetPanelControllerBoot}");
        }
        else
        {
            Debug.Log("RememberButtonTextChangerForHerが見つかりません");
        }

        // GameSaveManagerのフラグ
        if (GameSaveManager.Instance != null)
        {
            Debug.Log($"afterChangeToLastフラグ: {GameSaveManager.Instance.GetAfterChangeToLastFlag()}");
        }
        else
        {
            Debug.Log("GameSaveManagerが見つかりません");
        }

        Debug.Log("=====================================");
    }

    private void OnDestroy()
    {
        // イベントリスナーの解除
        if (rememberButton != null)
        {
            rememberButton.onClick.RemoveListener(OnRememberButtonClicked);
        }
    }
}