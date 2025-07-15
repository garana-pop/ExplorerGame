using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// ゲームロード時にafterChangeToHisFutureフラグをチェックして
/// TitleTextChangerForHimクラスのnewTitleTextの値をTitleContainerに表示するクラス
/// </summary>
public class TitleTextLoaderForHim : MonoBehaviour
{
    [Header("テキスト設定")]
    [Tooltip("表示対象のTextMeshProコンポーネント")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("通常時のタイトルテキスト")]
    [SerializeField] private string normalTitleText = "「願い」の記憶";

    [Tooltip("afterChangeToHisFuture=true時のタイトルテキスト")]
    [SerializeField] private string changedTitleText = "「彼」の未来";

    [Header("TitleTextChangerForHim参照")]
    [Tooltip("TitleTextChangerForHimへの参照（オプション）")]
    [SerializeField] private TitleTextChangerForHim titleTextChangerForHim;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool forceChangedTitle = false; // テスト用の強制変更

    private void Awake()
    {
        // TextMeshProコンポーネントの自動取得
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
            Debug.LogError("TitleTextLoaderForHim: TextMeshProコンポーネントが見つかりません。インスペクターで設定してください。");
            enabled = false;
            return;
        }

        // TitleTextChangerForHimの自動検索
        if (titleTextChangerForHim == null)
        {
            titleTextChangerForHim = FindFirstObjectByType<TitleTextChangerForHim>();
        }

        // TitleTextChangerForHimから設定値を取得
        if (titleTextChangerForHim != null)
        {
            // 変更後テキストを取得（リフレクションを使用）
            var newTitleField = titleTextChangerForHim.GetType().GetField("newTitleText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (newTitleField != null)
            {
                string newTitleValue = newTitleField.GetValue(titleTextChangerForHim) as string;
                if (!string.IsNullOrEmpty(newTitleValue))
                {
                    changedTitleText = newTitleValue;
                }
            }

            // ロード時は効果音を無効化
            titleTextChangerForHim.SetSoundEnabled(false);
        }
    }

    private void Start()
    {
        // 少し遅延させて確実にGameSaveManagerが初期化されてから実行
        StartCoroutine(LoadAndApplyTitleDelayed());
    }

    /// <summary>
    /// 遅延後にタイトルテキストを読み込んで適用
    /// </summary>
    private IEnumerator LoadAndApplyTitleDelayed()
    {
        // GameSaveManagerの初期化を待つ
        yield return new WaitForSeconds(0.1f);

        LoadAndApplyTitle();
    }

    /// <summary>
    /// セーブデータからフラグを読み込んでタイトルテキストを適用
    /// </summary>
    private void LoadAndApplyTitle()
    {
        // afterChangeToLastフラグがtrueの場合は処理をスキップ
        if (GameSaveManager.Instance != null && GameSaveManager.Instance.GetAfterChangeToLastFlag())
        {
            if (debugMode) Debug.Log("TitleTextLoaderForHim: afterChangeToLastがtrueのため処理をスキップします");
            return;
        }

        bool shouldChangeTitle = false;

        // デバッグモードでの強制変更
        if (debugMode && forceChangedTitle)
        {
            shouldChangeTitle = true;
            if (debugMode) Debug.Log("TitleTextLoaderForHim: デバッグモードで強制的にタイトルを変更");
        }
        else
        {
            // 両方のフラグをチェック
            bool herMemoryFlag = GetAfterChangeToHerMemoryFlag();
            bool hisFutureFlag = GetAfterChangeToHisFutureFlag();

            // 両方のフラグがtrueの場合のみタイトルを変更
            shouldChangeTitle = herMemoryFlag && hisFutureFlag;

            if (debugMode)
            {
                Debug.Log($"TitleTextLoaderForHim: afterChangeToHerMemory = {herMemoryFlag}");
                Debug.Log($"TitleTextLoaderForHim: afterChangeToHisFuture = {hisFutureFlag}");
                Debug.Log($"TitleTextLoaderForHim: 両フラグ条件 = {shouldChangeTitle}");
            }
        }

        // タイトルテキストを設定
        if (shouldChangeTitle)
        {
            titleText.text = changedTitleText;
            if (debugMode) Debug.Log($"TitleTextLoaderForHim: タイトルを「{changedTitleText}」に変更しました");
        }
    }

    /// <summary>
    /// afterChangeToHerMemoryフラグを取得
    /// </summary>
    private bool GetAfterChangeToHerMemoryFlag()
    {
        // GameSaveManagerから取得を試みる
        if (GameSaveManager.Instance != null)
        {
            try
            {
                // リフレクションを使用してセーブデータを取得
                var saveDataField = GameSaveManager.Instance.GetType().GetField("currentSaveData",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (saveDataField != null)
                {
                    var saveData = saveDataField.GetValue(GameSaveManager.Instance);
                    if (saveData != null)
                    {
                        var flagField = saveData.GetType().GetField("afterChangeToHerMemory");
                        if (flagField != null)
                        {
                            bool flagValue = (bool)flagField.GetValue(saveData);
                            if (debugMode) Debug.Log($"TitleTextLoaderForHim: afterChangeToHerMemoryフラグ = {flagValue}");
                            return flagValue;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                if (debugMode) Debug.LogError($"TitleTextLoaderForHim: フラグ取得エラー: {e.Message}");
            }
        }

        // フラグが取得できない場合はfalseを返す
        if (debugMode) Debug.LogWarning("TitleTextLoaderForHim: afterChangeToHerMemoryフラグを取得できませんでした");
        return false;
    }

    /// <summary>
    /// afterChangeToHisFutureフラグを取得
    /// </summary>
    private bool GetAfterChangeToHisFutureFlag()
    {
        // GameSaveManagerから取得を試みる
        if (GameSaveManager.Instance != null)
        {
            // GameSaveManagerにメソッドが存在すると仮定
            // 実際のメソッド名に合わせて変更してください
            try
            {
                // リフレクションを使用してセーブデータを取得
                var saveDataField = GameSaveManager.Instance.GetType().GetField("currentSaveData",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (saveDataField != null)
                {
                    var saveData = saveDataField.GetValue(GameSaveManager.Instance);
                    if (saveData != null)
                    {
                        var flagField = saveData.GetType().GetField("afterChangeToHisFuture");
                        if (flagField != null)
                        {
                            bool flagValue = (bool)flagField.GetValue(saveData);
                            if (debugMode) Debug.Log($"TitleTextLoaderForHim: afterChangeToHisFutureフラグ = {flagValue}");
                            return flagValue;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                if (debugMode) Debug.LogError($"TitleTextLoaderForHim: フラグ取得エラー: {e.Message}");
            }
        }

        // フラグが取得できない場合はfalseを返す
        if (debugMode) Debug.LogWarning("TitleTextLoaderForHim: afterChangeToHisFutureフラグを取得できませんでした");
        return false;
    }

    /// <summary>
    /// 手動でタイトルを再読み込み（デバッグ用）
    /// </summary>
    [ContextMenu("Reload Title")]
    public void ReloadTitle()
    {
        LoadAndApplyTitle();
    }

    /// <summary>
    /// フラグの状態を手動で設定（デバッグ用）
    /// </summary>
    [ContextMenu("Toggle Changed Title")]
    public void ToggleChangedTitle()
    {
        forceChangedTitle = !forceChangedTitle;
        LoadAndApplyTitle();
    }
}