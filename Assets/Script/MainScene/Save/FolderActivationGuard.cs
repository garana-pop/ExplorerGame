using UnityEngine;

/// <summary>
/// フォルダーが一度アクティブになったら、以降非アクティブにならないようにするガードスクリプト
/// FolderButtonオブジェクトに直接アタッチして使用します
/// </summary>
public class FolderActivationGuard : MonoBehaviour
{
    [Tooltip("最初から有効状態として扱うかどうか")]
    [SerializeField] private bool activatedByDefault = false;

    [Tooltip("フォルダー名（自動取得）")]
    [SerializeField] private string folderName = "";

    [Tooltip("デバッグログを表示するかどうか")]
    [SerializeField] private bool debugMode = false;

    // フォルダーが一度でもアクティブにされたかを記録
    private bool hasBeenActivated = false;

    // 再アクティブ化のために使用する変数
    private bool needsReactivation = false;

    private void Awake()
    {
        // 初期値を適用
        hasBeenActivated = activatedByDefault;

        // フォルダー名を取得
        if (string.IsNullOrEmpty(folderName))
        {
            FolderButtonScript folderScript = GetComponent<FolderButtonScript>();
            if (folderScript != null)
            {
                folderName = folderScript.GetFolderName();
            }
            else
            {
                folderName = gameObject.name;
            }
        }

        // 「願い」フォルダーの場合、強制的にアクティブ状態をチェック
        if (gameObject.name.Contains("願い"))
        {
            FolderButtonScript folderScript = GetComponent<FolderButtonScript>();
            if (folderScript != null)
            {
                folderScript.SetActivatedState(true);
                if (debugMode)
                    Debug.Log($"[FolderActivationGuard] 願いフォルダーを強制的にアクティブ化状態に設定しました");
            }
        }
    }

    private void OnEnable()
    {
        // アクティブになった時点でフラグを立てる
        if (!hasBeenActivated)
        {
            hasBeenActivated = true;

            if (debugMode)
            {
                Debug.Log($"[FolderActivationGuard] {folderName} が初めてアクティブになりました");
            }
        }

        // 再アクティブ化フラグをリセット
        needsReactivation = false;
    }

    private void OnDisable()
    {
        // 一度アクティブになったフォルダは非アクティブにしない
        if (hasBeenActivated)
        {
            if (debugMode)
            {
                Debug.Log($"[FolderActivationGuard] {folderName} の非アクティブ化を防止します");
            }

            // 次のフレームで再アクティブ化するためのフラグをセット
            needsReactivation = true;

            // 重要: 即限に再アクティブ化
            Invoke("ReactivateFolder", 0.01f);
        }
    }

    private void Update()
    {
        // 再アクティブ化フラグが立っていれば実行
        if (needsReactivation && !gameObject.activeSelf)
        {
            ReactivateFolder();
        }
    }

    // フォルダーを再アクティブ化
    private void ReactivateFolder()
    {
        if (!gameObject.activeSelf && hasBeenActivated)
        {
            gameObject.SetActive(true);
            needsReactivation = false;

            if (debugMode)
            {
                Debug.Log($"[FolderActivationGuard] {folderName} を再アクティブ化しました");
            }
        }
    }

    /// <summary>
    /// フォルダーが一度でもアクティブになったかを取得
    /// </summary>
    public bool IsActivated()
    {
        return hasBeenActivated;
    }

    /// <summary>
    /// フォルダーのアクティブ化状態を強制的に設定
    /// </summary>
    public void SetActivated(bool activated)
    {
        hasBeenActivated = activated;

        // アクティブ化された場合、確実にアクティブ状態にする
        if (activated && !gameObject.activeSelf)
        {
            gameObject.SetActive(true);

            // FolderButtonScriptも連動して更新
            FolderButtonScript folderScript = GetComponent<FolderButtonScript>();
            if (folderScript != null)
            {
                folderScript.SetActivatedState(true);
            }
        }
    }
}