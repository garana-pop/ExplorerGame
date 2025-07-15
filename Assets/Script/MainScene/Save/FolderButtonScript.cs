using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
 
    private bool hasBeenActivated = false; //フォルダが一度でもアクティブにされたかを記録
    private bool isActive = false;
    private Image backgroundImage;


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
                Debug.Log($"フォルダー名をゲームオブジェクト名から自動設定: {folderName}");

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

        // 背景イメージの取得
        //backgroundImage = GetComponent<Image>();

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

        // 背景カラー初期化
        //UpdateVisualState(false);

        // 初期アクティブフォルダーの場合は開く
        if (isInitialActiveFolder)
        {
            // 少し遅延させて他のフォルダー初期化後に開く
            Invoke("ToggleFolder", 0.1f);
        }
    }

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

            // 追加: このフォルダが一度アクティブになったことを記録
            hasBeenActivated = true;

        }
        else
        {
            Debug.LogWarning($"フォルダー「{folderName}」にはfilePanel設定がありません");
        }
    }

    /// <summary>
    /// フォルダーの視覚状態を更新（選択/非選択）
    /// </summary>
    private void UpdateVisualState(bool selected)
    {
        isActive = selected;

        //// 背景色の更新
        //if (backgroundImage != null)
        //{
        //    backgroundImage.color = selected ? selectedColor : normalColor;
        //}

        //// フォルダーアイコンの強調表示（オプション）
        //if (folderIcon != null)
        //{
        //    folderIcon.color = selected ? Color.white : new Color(0.9f, 0.9f, 0.9f);
        //}
    }

    /// <summary>
    /// フォルダー名を取得
    /// </summary>
    public string GetFolderName()
    {
        return folderName;
    }

    /// <summary>
    /// アクティブ状態を取得
    /// </summary>
    public bool IsActive()
    {
        return isActive && filePanel != null && filePanel.activeSelf;
    }

    // SetActivatedStateメソッドの強化
    public void SetActivatedState(bool activated)
    {
        hasBeenActivated = activated;

        // アクティブ化された場合は表示を確実に有効に
        if (activated)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                Debug.Log($"フォルダー {folderName} を強制的にアクティブ化");
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
        }
    }

    // SetVisibleメソッドの強化
    public void SetVisible(bool visible)
    {
        if (visible)
        {
            // 表示する場合は常に有効にし、アクティブ化された状態にする
            gameObject.SetActive(true);
            hasBeenActivated = true;

            // FolderActivationGuardがあれば活性化
            FolderActivationGuard guard = GetComponent<FolderActivationGuard>();
            if (guard != null)
            {
                guard.SetActivated(true);
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
            Debug.Log($"フォルダー {folderName} は既にアクティブ化されているため、非表示にしません");
        }
    }

    // 追加: フォルダが一度でもアクティブになったかを取得するメソッド
    public bool HasBeenActivated()
    {
        return hasBeenActivated;
    }

}