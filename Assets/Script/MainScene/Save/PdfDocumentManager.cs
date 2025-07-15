using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PDFドキュメント内の隠しキーワードを管理するクラス
/// </summary>
public class PdfDocumentManager : MonoBehaviour
{
    [Header("キーワード設定")]
    [Tooltip("このPDFに含まれる隠しキーワード")]
    [SerializeField] private List<HiddenKeyword> hiddenKeywords = new List<HiddenKeyword>();

    [Header("次のフォルダー/ファイル")]
    [Tooltip("すべてのキーワードが表示されたときにアクティブにするオブジェクト")]
    [SerializeField] private GameObject nextFolderOrFile;

    [Header("保存設定")]
    [Tooltip("このPDFファイルの識別名（必須）")]
    [SerializeField] private string fileName = "document.pdf";

    [Header("効果音")]
    [Tooltip("すべて表示されたときの効果音")]
    [SerializeField] private AudioClip completionSound;

    [Header("UIコントロール")]
    [Tooltip("閉じるボタン")]
    [SerializeField] private Button closeButton;

    [Header("アイコン連携")]
    [Tooltip("完了状態変更時に更新するPdfFileIconChangeコンポーネントリスト")]
    [SerializeField] private List<PdfFileIconChange> linkedIconChangers = new List<PdfFileIconChange>();

    [Tooltip("設定されていない場合に動的検索も行うかどうか")]
    [SerializeField] private bool searchForAdditionalIcons = false;

    [Header("セーブ設定")]
    [Tooltip("完了状態設定時に自動セーブするか")]
    [SerializeField] private bool autoSaveOnCompletion = true;

    // 内部状態
    private bool isDocumentCompleted = false;
    private int revealedKeywordsCount = 0;

    private Dictionary<string, bool> completionStateCache = new Dictionary<string, bool>();
    private static Dictionary<string, bool> globalCompletionStateCache = new Dictionary<string, bool>();

    private void Awake()
    {
        // HiddenKeywordが設定されていない場合は自動取得
        if (hiddenKeywords == null || hiddenKeywords.Count == 0)
        {
            hiddenKeywords = new List<HiddenKeyword>(GetComponentsInChildren<HiddenKeyword>(true));
        }
    }

    private void Start()
    {
        // グローバルキャッシュからの状態復元を最初に試みる
        if (globalCompletionStateCache.TryGetValue(fileName, out bool cachedState) && cachedState)
        {
            isDocumentCompleted = true;
        }

        // セーブデータからも状態を復元
        RestoreStateFromSave();

        // 状態をグローバルキャッシュに保存
        UpdateGlobalCompletionState();
    }

    // 新メソッド - グローバル状態の更新
    private void UpdateGlobalCompletionState()
    {
        // 完了状態をグローバルキャッシュに保存
        if (isDocumentCompleted)
        {
            globalCompletionStateCache[fileName] = true;
            //Debug.Log($"PDF '{fileName}': 完了状態をグローバルキャッシュに保存しました");
        }
    }

    private void OnEnable()
    {
        // 表示されたときに状態を再確認
        if (isDocumentCompleted)
        {
            ForceRevealAllKeywords();
            EnsureNextFolderActive();
        }
    }

    /// <summary>
    /// キーワードが表示されたときの処理
    /// </summary>
    public void OnKeywordRevealed(HiddenKeyword keyword)
    {
        // 既に完了している場合は何もしない
        if (isDocumentCompleted) return;

        // 表示済みキーワード数を更新
        UpdateRevealedKeywordCount();

        // すべてのキーワードが表示されたかチェック
        CheckCompletion();
    }

    /// <summary>
    /// 表示済みキーワード数を更新
    /// </summary>
    private void UpdateRevealedKeywordCount()
    {
        revealedKeywordsCount = 0;
        foreach (var keyword in hiddenKeywords)
        {
            if (keyword != null && keyword.IsRevealed())
            {
                revealedKeywordsCount++;
            }
        }
    }

    /// <summary>
    /// すべてのキーワードが表示されたかチェックし、完了処理を行う
    /// </summary>
    private void CheckCompletion()
    {
        // すべてが表示されているか確認
        bool allRevealed = (revealedKeywordsCount >= hiddenKeywords.Count) && (hiddenKeywords.Count > 0);

        if (allRevealed && !isDocumentCompleted)
        {
            CompleteDocument();
        }
    }

    /// <summary>
    /// ドキュメント完了時の処理
    /// </summary>
    private void CompleteDocument()
    {
        isDocumentCompleted = true;

        // 完了効果音を再生
        PlayCompletionSound();

        // 次のフォルダー/ファイルをアクティブ化
        EnsureNextFolderActive();

        // インスペクターで設定されたアイコンを更新
        UpdateLinkedIcons();

        // ゲーム状態を保存
        SaveGameState();

        Debug.Log($"PDF '{fileName}' のすべてのキーワードが表示されました。完了状態を保存します。");
    }

    /// <summary>
    /// 設定されたPdfFileIconChangeコンポーネントを更新
    /// </summary>
    private void UpdateLinkedIcons()
    {
        // インスペクターで設定されたアイコンを更新
        foreach (var iconChanger in linkedIconChangers)
        {
            if (iconChanger != null)
            {
                iconChanger.CheckCompletionState();
            }
        }
    }

    /// <summary>
    /// 完了効果音の再生
    /// </summary>
    private void PlayCompletionSound()
    {
        if (SoundEffectManager.Instance != null)
        {
            SoundEffectManager.Instance.PlayCompletionSound();
        }
        else if (completionSound != null)
        {
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.PlayOneShot(completionSound);
        }
    }

    /// <summary>
    /// 次のフォルダー/ファイルをアクティブにする
    /// </summary>
    public void EnsureNextFolderActive()
    {
        // PdfDocumentLinkManagerからマッピングを優先的に取得
        GameObject targetObject = PdfDocumentLinkManager.Instance.GetNextObjectForPdf(fileName);

        // マッピングがない場合のみインスペクターで設定されたnextFolderOrFileを使用
        if (targetObject == null)
        {
            targetObject = nextFolderOrFile;
            if (targetObject == null)
            {
                Debug.Log($"PDF '{fileName}': 次のフォルダー/ファイルが設定されていません");
                return;
            }
        }

        // 対象オブジェクトをアクティブに
        targetObject.SetActive(true);

        // FolderButtonScriptの設定
        FolderButtonScript folderScript = targetObject.GetComponent<FolderButtonScript>();
        if (folderScript == null)
        {
            folderScript = targetObject.GetComponentInParent<FolderButtonScript>();
        }

        if (folderScript != null)
        {
            folderScript.SetActivatedState(true);
            folderScript.SetVisible(true);

            // ファイルパネルもアクティブに
            if (folderScript.filePanel != null)
            {
                folderScript.filePanel.SetActive(true);
            }
        }

        // FolderActivationGuardの設定
        FolderActivationGuard guard = targetObject.GetComponent<FolderActivationGuard>();
        if (guard != null)
        {
            guard.SetActivated(true);
        }

    }

    /// <summary>
    /// すべてのキーワードを強制的に表示状態にする
    /// </summary>
    private void ForceRevealAllKeywords()
    {
        if (hiddenKeywords == null || hiddenKeywords.Count == 0)
        {
            // 自動的にHiddenKeywordsを探す
            hiddenKeywords = new List<HiddenKeyword>(GetComponentsInChildren<HiddenKeyword>(true));
            Debug.Log($"PDF '{fileName}': {hiddenKeywords.Count}個のHiddenKeywordを自動検出しました");
        }

        // キーワードが見つからない場合はさらに広く検索
        if (hiddenKeywords.Count == 0)
        {
            // LineTextオブジェクトをすべて探す
            Transform[] textObjects = GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.Contains("Line") && t.name.Contains("Text"))
                .ToArray();

            foreach (Transform textObject in textObjects)
            {
                // 子オブジェクトとしてHiddenKeywordがあるか確認
                HiddenKeyword[] keywords = textObject.GetComponentsInChildren<HiddenKeyword>(true);
                if (keywords.Length > 0)
                {
                    hiddenKeywords.AddRange(keywords);
                }
            }

            Debug.Log($"PDF '{fileName}': LineTextから{hiddenKeywords.Count}個のHiddenKeywordを検出しました");
        }

        foreach (var keyword in hiddenKeywords)
        {
            if (keyword != null)
            {
                // 表示状態を強制的に設定（すでに表示されていても再度適用）
                keyword.ForceReveal();
                // Debug.Log($"PDF '{fileName}': キーワード '{keyword.GetHiddenWord()}' を強制表示しました");
            }
        }

        // 隠しキーワードのカウントを更新
        UpdateRevealedKeywordCount();
    }

    /// <summary>
    /// セーブデータから状態を復元
    /// </summary>
    private void RestoreStateFromSave()
    {
        if (GameSaveManager.Instance == null) return;

        Dictionary<string, PdfFileData> pdfData = GameSaveManager.Instance.GetAllPdfProgress();
        if (pdfData != null && pdfData.TryGetValue(fileName, out PdfFileData fileData))
        {
            // 完了状態を復元（修正：一度完了したら恒久的に完了状態とする）
            isDocumentCompleted = fileData.isCompleted;

            // キーワードを表示状態に
            if (isDocumentCompleted)
            {
                // すべて表示する前に確実にHiddenKeywordを収集
                if (hiddenKeywords == null || hiddenKeywords.Count == 0)
                {
                    hiddenKeywords = new List<HiddenKeyword>(GetComponentsInChildren<HiddenKeyword>(true));
                }

                // すべてのキーワードを強制表示
                ForceRevealAllKeywords();

                // 次のフォルダーを確実にアクティブ化
                EnsureNextFolderActive();

                //Debug.Log($"PDF '{fileName}': 完了状態から復元し、すべてのキーワードを表示しました");
            }
            // 個別のキーワード表示状態を復元
            else if (fileData.revealedKeywords != null && fileData.revealedKeywords.Length > 0)
            {
                HashSet<string> revealedWordsSet = new HashSet<string>(fileData.revealedKeywords);

                // キーワードリストを更新
                if (hiddenKeywords == null || hiddenKeywords.Count == 0)
                {
                    hiddenKeywords = new List<HiddenKeyword>(GetComponentsInChildren<HiddenKeyword>(true));
                }

                foreach (var keyword in hiddenKeywords)
                {
                    if (keyword != null && revealedWordsSet.Contains(keyword.GetHiddenWord()))
                    {
                        keyword.ForceReveal();
                        Debug.Log($"PDF '{fileName}': キーワード '{keyword.GetHiddenWord()}' の状態を復元しました");
                    }
                }

                // 表示済みキーワード数を更新
                UpdateRevealedKeywordCount();

                // 再チェック（ロード後に完了条件を満たす場合がある）
                CheckCompletion();
            }
        }
    }

    /// <summary>
    /// ゲーム状態を保存
    /// </summary>
    private void SaveGameState()
    {
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.SaveGame();
        }
    }

    /// <summary>
    /// このPDFの進捗状態を取得
    /// </summary>
    public PdfFileData GetPdfProgress()
    {
        // 表示されたキーワードを収集
        List<string> revealed = new List<string>();

        foreach (var keyword in hiddenKeywords)
        {
            if (keyword != null && keyword.IsRevealed())
            {
                revealed.Add(keyword.GetHiddenWord());
            }
        }

        return new PdfFileData
        {
            fileName = fileName,
            revealedKeywords = revealed.ToArray(),
            totalKeywords = hiddenKeywords.Count,
            isCompleted = isDocumentCompleted
        };
    }

    /// <summary>
    /// PDFファイル名を取得
    /// </summary>
    public string GetPdfFileName()
    {
        return fileName;
    }

    /// <summary>
    /// PDF完了状態を取得
    /// </summary>
    public bool IsDocumentCompleted()
    {
        return isDocumentCompleted;
    }

    /// <summary>
    /// 完了状態を強制設定（デバッグやロード用）
    /// </summary>
    /// <summary>
    /// 完了状態を強制設定（デバッグやロード用）
    /// </summary>
    public void SetCompletionState(bool completed, bool? autoSave = null)
    {
        // autoSaveの値を決定（引数指定 > インスペクター設定 > デフォルトtrue）
        bool shouldAutoSave = autoSave ?? autoSaveOnCompletion;

        // 既に完了状態の場合は変更不要
        if (isDocumentCompleted && !completed)
        {
            Debug.Log($"PDF '{fileName}': 既に完了しているため、非完了状態には設定しません");
            return;
        }

        // false→trueの変更のみ許可
        if (completed && !isDocumentCompleted)
        {
            isDocumentCompleted = true;
            globalCompletionStateCache[fileName] = true;

            ForceRevealAllKeywords();
            EnsureNextFolderActive();
            UpdateLinkedIcons();

            // 自動セーブが有効な場合のみセーブ
            if (shouldAutoSave)
            {
                SaveGameState();
            }
        }
    }
    //public void SetCompletionState(bool completed)
    //{
    //    // 既に完了状態の場合は変更不可
    //    if (isDocumentCompleted && !completed)
    //    {
    //        Debug.Log($"PDF '{fileName}': 既に完了しているため、非完了状態には設定しません");
    //        return;
    //    }

    //    // falseからtrueへの変更のみ許可
    //    if (completed && !isDocumentCompleted)
    //    {
    //        isDocumentCompleted = true;

    //        // グローバルキャッシュにも保存
    //        globalCompletionStateCache[fileName] = true;

    //        ForceRevealAllKeywords();
    //        EnsureNextFolderActive();

    //        // アイコン更新を追加
    //        UpdateLinkedIcons();

    //        SaveGameState();
    //    }
    //    else if (completed && isDocumentCompleted)
    //    {
    //        // 既に完了状態の場合は何もしない（ただしログは出す）
    //        //Debug.Log($"PDF '{fileName}': すでに完了状態です");
    //    }
    //}

}