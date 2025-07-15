using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// FileIconChange.csのアイコン変化に連動して自身のアイコンを変更するコンポーネント
/// </summary>
public class PngFileIconChange : MonoBehaviour
{
    [Header("アイコン設定")]
    [Tooltip("変更前のアイコンスプライト")]
    [SerializeField] private Sprite defaultSprite;

    [Tooltip("変更後のアイコンスプライト")]
    [SerializeField] private Sprite completedSprite;

    [Tooltip("監視対象のFileIconChangeコンポーネント（未設定の場合は自動検索）")]
    [SerializeField] private FileIconChange targetFileIconChange;

    [Tooltip("変更対象のImageコンポーネント（未設定の場合は自身のImageを使用）")]
    [SerializeField] private Image iconImage;

    [Header("デバッグ設定")]
    [Tooltip("デバッグログを表示するかどうか")]
    [SerializeField] private bool debugMode = false;

    // 監視対象のスプライト
    private Sprite initialSprite;
    private bool hasCompletedState = false;

    private void Awake()
    {
        // Imageコンポーネントが設定されていなければ自身から取得
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
            if (iconImage == null)
            {
                Debug.LogError("PngFileIconChange: Imageコンポーネントが見つかりません。このスクリプトはImageコンポーネントがアタッチされたオブジェクトに追加してください。");
                enabled = false;
                return;
            }
        }

        // デフォルトスプライトを設定
        if (defaultSprite != null)
        {
            iconImage.sprite = defaultSprite;
        }
    }

    private void Start()
    {
        // 監視対象のFileIconChangeが設定されていなければ自動検索
        if (targetFileIconChange == null)
        {
            FindTargetFileIconChange();
        }

        // 初期状態の記録
        if (targetFileIconChange != null)
        {
            Image targetImage = targetFileIconChange.GetComponent<Image>();
            if (targetImage != null && targetImage.sprite != null)
            {
                initialSprite = targetImage.sprite;
            }
        }

        // 初期状態の確認
        CheckIconState();
    }

    private void OnEnable()
    {
        // オブジェクトが有効になるたびに状態をチェック
        if (targetFileIconChange != null)
        {
            // 監視対象のimageの名前が"txtファイルアイコン_0"かチェック
            Image targetImage = targetFileIconChange.GetComponent<Image>();
            if (targetImage != null && targetImage.sprite != null)
            {
                // スプライト名が"txtファイルアイコン_0"だった場合
                if (targetImage.sprite.name == "txtファイルアイコン_0")
                {
                    // completedSpriteに変更
                    if (completedSprite != null && iconImage != null)
                    {
                        iconImage.sprite = completedSprite;
                        hasCompletedState = true;

                        if (debugMode)
                        {
                            Debug.Log($"PngFileIconChange(OnEnable): txtファイルアイコン_0を検出したため、完了状態に設定しました - {gameObject.name}");
                        }
                    }
                }
            }
        }
    }

    private void Update()
    {
        // FileIconChangeの状態をチェック
        CheckIconState();
    }

    /// <summary>
    /// 監視対象のFileIconChangeを検索
    /// </summary>
    private void FindTargetFileIconChange()
    {
        // 親階層をたどってFileIconChangeを検索
        Transform current = transform.parent;
        while (current != null)
        {
            FileIconChange fileIconChange = current.GetComponent<FileIconChange>();
            if (fileIconChange != null)
            {
                targetFileIconChange = fileIconChange;
                if (debugMode)
                {
                    Debug.Log($"PngFileIconChange: 親階層からFileIconChangeを自動検出しました: {current.name}");
                }
                return;
            }
            current = current.parent;
        }

        // 親階層に見つからない場合はシーン内から名前の類似性で検索
        string myName = gameObject.name;
        string baseName = ExtractBaseName(myName);

        if (!string.IsNullOrEmpty(baseName))
        {
            FileIconChange[] allFileIconChanges = FindObjectsByType<FileIconChange>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var fileIconChange in allFileIconChanges)
            {
                string otherName = fileIconChange.gameObject.name;
                if (otherName.Contains(baseName) || baseName.Contains(ExtractBaseName(otherName)))
                {
                    targetFileIconChange = fileIconChange;
                    if (debugMode)
                    {
                        Debug.Log($"PngFileIconChange: 名前の類似性からFileIconChangeを自動検出しました: {otherName}");
                    }
                    return;
                }
            }
        }

        // それでも見つからない場合は警告
        Debug.LogWarning("PngFileIconChange: 監視対象のFileIconChangeが見つかりませんでした。インスペクターで手動設定してください。");
    }

    /// <summary>
    /// 名前のベース部分を抽出（拡張子や数字を除去）
    /// </summary>
    private string ExtractBaseName(string fullName)
    {
        // 拡張子を取り除く
        int dotIndex = fullName.LastIndexOf('.');
        if (dotIndex > 0)
        {
            fullName = fullName.Substring(0, dotIndex);
        }

        return fullName;
    }

    /// <summary>
    /// FileIconChangeの状態を確認し、必要に応じてアイコンを変更
    /// </summary>
    private void CheckIconState()
    {
        if (targetFileIconChange == null || iconImage == null) return;

        // FileIconChangeのImageコンポーネントを取得
        Image targetImage = targetFileIconChange.GetComponent<Image>();
        if (targetImage == null || targetImage.sprite == null) return;

        // 初期値が未設定なら設定
        if (initialSprite == null)
        {
            initialSprite = targetImage.sprite;
            return;
        }

        // スプライトが初期値から変更されたかをチェック
        bool isChanged = (initialSprite != targetImage.sprite);

        // 状態が変化した場合のみ処理
        if (isChanged != hasCompletedState)
        {
            hasCompletedState = isChanged;

            if (isChanged)
            {
                // 完了状態のスプライトに変更
                if (completedSprite != null)
                {
                    iconImage.sprite = completedSprite;
                    if (debugMode)
                    {
                        Debug.Log($"PngFileIconChange: アイコンを完了状態に変更しました - {gameObject.name}");
                    }
                }
            }
        }
    }

    /// <summary>
    /// 監視対象のFileIconChangeを手動で設定
    /// </summary>
    public void SetTargetFileIconChange(FileIconChange target)
    {
        targetFileIconChange = target;

        // 初期スプライトをリセット
        initialSprite = null;

        // 前のスプライトを更新
        if (target != null)
        {
            Image targetImage = target.GetComponent<Image>();
            if (targetImage != null)
            {
                initialSprite = targetImage.sprite;
            }
        }

        // 設定後にすぐに状態をチェック
        CheckIconState();
    }

    /// <summary>
    /// 手動でアイコンを完了状態に設定
    /// </summary>
    public void SetCompleted(bool completed)
    {
        if (iconImage == null) return;

        if (completed && completedSprite != null)
        {
            iconImage.sprite = completedSprite;
            hasCompletedState = true;
        }
        else if (defaultSprite != null)
        {
            iconImage.sprite = defaultSprite;
            hasCompletedState = false;
        }
    }
}