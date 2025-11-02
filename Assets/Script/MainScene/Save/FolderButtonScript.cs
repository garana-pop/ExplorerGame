using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class FolderButtonScript : MonoBehaviour
{
    public GameObject filePanel; // このフォルダーに紐づくファイルパネル

    [Header("フォルダー情報")]
    [SerializeField] private string folderName = "";
    [SerializeField] private bool isInitialActiveFolder = false; // 初期状態でアクティブにするかどうか
    [SerializeField] private bool isAvailableByDefault = true;   // 初期状態で有効かどうか

    [Header("表示設定")]
    [SerializeField] private Image folderIcon;
    [SerializeField] private TextMeshProUGUI folderLabel;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = true; // デバッグモード

    private bool hasBeenActivated = false; // フォルダが一度でもアクティブにされたかを記録
    private bool isActive = false;

    // 静的な解放履歴管理（セッション中に一度でも解放されたフォルダーを記録）
    private static HashSet<string> unlockedFoldersThisSession = new HashSet<string>();


    private void Awake()
    {
        // フォルダー名のTextコンポーネントを取得
        if (folderLabel == null)
            folderLabel = GetComponentInChildren<TextMeshProUGUI>();

        // TextコンポーネントにフォルダーNameプロパティがあれば設定する
        if (folderLabel != null && string.IsNullOrEmpty(folderName))
        {
            folderName = folderLabel.text;
        }

        // フォルダー名がまだ空の場合はゲームオブジェクト名から取得を試みる
        if (string.IsNullOrEmpty(folderName) && gameObject.name.Contains("FolderButton"))
        {
            string[] parts = gameObject.name.Split(new char[] { '(', ')' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                folderName = parts[1].Trim();
                DebugLogger.Log($"フォルダー名をゲームオブジェクト名から自動設定: {folderName}");

                // フォルダーラベルにも設定
                if (folderLabel != null)
                    folderLabel.text = folderName;
            }
        }

        // 思い出フォルダーは初期状態でアクティブに
        if (folderName == "思い出" && !isInitialActiveFolder)
        {
            isInitialActiveFolder = true;
        }

        // 初期状態で無効の場合は非表示に
        if (!isAvailableByDefault)
        {
            gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (filePanel != null)
        {
            filePanel.SetActive(false); // 初期状態で非表示
        }

        // 初期アクティブフォルダーの場合は開く
        if (isInitialActiveFolder)
        {
            // 少し遅延させて他のフォルダー初期化後に開く
            Invoke("ToggleFolder", 0.1f);
        }
    }

    /// <summary>
    /// フォルダーをトグル（クリック時）
    /// </summary>
    public void ToggleFolder()
    {
        if (filePanel != null)
        {
            // すべてのfilePanelを非表示にする
            foreach (Transform child in transform.parent)
            {
                var folderScript = child.GetComponent<FolderButtonScript>();
                if (folderScript != null && folderScript.filePanel != null)
                {
                    folderScript.filePanel.SetActive(false);
                    folderScript.UpdateVisualState(false);
                }
            }

            filePanel.SetActive(true); // クリックしたフォルダのfilePanelを表示
            filePanel.transform.SetAsLastSibling(); // ヒエラルキーの一番下に移動
            UpdateVisualState(true);

            // このフォルダが一度アクティブになったことを記録
            hasBeenActivated = true;
        }
        else
        {
            DebugLogger.LogWarning($"フォルダー「{folderName}」にはfilePanel設定がありません");
        }
    }

    /// <summary>
    /// フォルダーの視覚状態を更新（選択/非選択）
    /// </summary>
    /// <param name="selected">選択されているかどうか</param>
    private void UpdateVisualState(bool selected)
    {
        isActive = selected;
    }

    /// <summary>
    /// フォルダー名を取得
    /// </summary>
    /// <returns>フォルダー名</returns>
    public string GetFolderName()
    {
        return folderName;
    }

    /// <summary>
    /// アクティブ状態を取得
    /// </summary>
    /// <returns>アクティブ状態</returns>
    public bool IsActive()
    {
        return isActive && filePanel != null && filePanel.activeSelf;
    }

    /// <summary>
    /// アクティブ化状態を設定
    /// </summary>
    /// <param name="activated">アクティブ化するかどうか</param>
    public void SetActivatedState(bool activated)
    {
        // アクティブ化された場合は表示を確実に有効に
        if (activated)
        {
            // フォルダーが初めて解放される場合（セッション中に一度も解放されていない場合）
            bool isFirstActivation = !unlockedFoldersThisSession.Contains(folderName);

            hasBeenActivated = true;

            // セッション履歴に追加
            if (isFirstActivation && !string.IsNullOrEmpty(folderName))
            {
                unlockedFoldersThisSession.Add(folderName);
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                if (debugMode)
                {
                    DebugLogger.Log($"{nameof(FolderButtonScript)}: フォルダー {folderName} を強制的にアクティブ化");
                }
            }

            // FolderActivationGuardにも状態を反映
            FolderActivationGuard guard = GetComponent<FolderActivationGuard>();
            if (guard != null)
            {
                guard.SetActivated(true);
            }

            // 初期アクティブフォルダーの場合はファイルパネルも表示
            if (filePanel != null && isInitialActiveFolder)
            {
                filePanel.SetActive(true);
            }

            // 初めて解放された場合はSteam実績を解除
            if (isFirstActivation)
            {
                UnlockFolderAchievement();
            }
        }
    }

    /// <summary>
    /// フォルダーの表示状態を設定
    /// </summary>
    /// <param name="visible">表示するかどうか</param>
    public void SetVisible(bool visible)
    {
        if (visible)
        {
            // フォルダーが初めて解放される場合（セッション中に一度も解放されていない場合）
            bool isFirstActivation = !unlockedFoldersThisSession.Contains(folderName);

            // 表示する場合は常に有効にし、アクティブ化された状態にする
            gameObject.SetActive(true);
            hasBeenActivated = true;

            // セッション履歴に追加
            if (isFirstActivation && !string.IsNullOrEmpty(folderName))
            {
                unlockedFoldersThisSession.Add(folderName);
            }

            // FolderActivationGuardがあれば活性化
            FolderActivationGuard guard = GetComponent<FolderActivationGuard>();
            if (guard != null)
            {
                guard.SetActivated(true);
            }

            // 初めて解放された場合はSteam実績を解除
            if (isFirstActivation)
            {
                UnlockFolderAchievement();
            }
        }
        else if (!hasBeenActivated)
        {
            // 非表示にする場合は、まだアクティブ化されていない場合のみ非表示にする
            gameObject.SetActive(false);
        }
        else
        {
            // 既にアクティブ化されたフォルダは非表示にしない
            if (debugMode)
            {
                DebugLogger.Log($"{nameof(FolderButtonScript)}: フォルダー {folderName} は既にアクティブ化されているため、非表示にしません");
            }
        }
    }

    /// <summary>
    /// フォルダが一度でもアクティブになったかを取得
    /// </summary>
    /// <returns>アクティブ化されたことがある場合true</returns>
    public bool HasBeenActivated()
    {
        return hasBeenActivated;
    }

    #region Steam実績管理

    /// <summary>
    /// フォルダー解放時のSteam実績を解除
    /// </summary>
    private void UnlockFolderAchievement()
    {
        // SteamAchievementManagerの存在確認
        if (SteamAchievementManager.Instance == null)
        {
            if (debugMode)
            {
                DebugLogger.LogWarning($"{nameof(FolderButtonScript)}: SteamAchievementManagerが存在しません。実績解除できませんでした。");
            }
            return;
        }

        // フォルダー名に対応する実績API名を取得
        string achievementApiName = GetFolderAchievementApiName(folderName);

        // 対応する実績がない場合はスキップ
        if (string.IsNullOrEmpty(achievementApiName))
        {
            if (debugMode)
            {
                DebugLogger.Log($"{nameof(FolderButtonScript)}: フォルダー「{folderName}」に対応するSteam実績はありません。");
            }
            return;
        }

        // 既に解除済みかチェック
        if (SteamAchievementManager.Instance.IsAchievementUnlocked(achievementApiName))
        {
            if (debugMode)
            {
                DebugLogger.Log($"{nameof(FolderButtonScript)}: フォルダー「{folderName}」の実績は既に解除済みです。");
            }
            return;
        }

        // 実績を解除
        bool success = SteamAchievementManager.Instance.UnlockAchievement(achievementApiName);

        if (success)
        {
            DebugLogger.Log($"{nameof(FolderButtonScript)}: フォルダー「{folderName}」のSteam実績を解除しました（API: {achievementApiName}）");
        }
        else
        {
            DebugLogger.LogError($"{nameof(FolderButtonScript)}: フォルダー「{folderName}」のSteam実績解除に失敗しました（API: {achievementApiName}）");
        }
    }

    /// <summary>
    /// フォルダー名から対応するSteam実績API名を取得
    /// </summary>
    /// <param name="folder">フォルダー名</param>
    /// <returns>対応するAPI名。該当なしの場合はnull</returns>
    private string GetFolderAchievementApiName(string folder)
    {
        switch (folder)
        {
            case "恋人":
                return "MemoryOfPain_3_ACHIEVEMENTS_UNLOCK";
            case "友人":
                return "Rejection_4_ACHIEVEMENTS_UNLOCK";
            case "記録":
                return "ConfrontingReality_5_ACHIEVEMENTS_UNLOCK";
            case "願い":
                return "Acceptance_6_ACHIEVEMENTS_UNLOCK";
            default:
                return null;
        }
    }

    #endregion
}