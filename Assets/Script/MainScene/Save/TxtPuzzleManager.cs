using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ExplorerGame.Localization;

public class TxtPuzzleManager : MonoBehaviour
{
    [Header("ドロップエリア設定")]
    [Tooltip("インスペクターで全てのSpeakerDropAreaを手動で設定してください")]
    [SerializeField] private List<SpeakerDropArea> dropAreas = new List<SpeakerDropArea>();

    [Header("完了表示設定")]
    [Tooltip("パズル完了時に表示するオリジナル画像")]
    [SerializeField] private GameObject originalImage; // 完了時に表示する画像

    [Tooltip("パズル完了時に非表示にするモザイクコンテナ")]
    [SerializeField] private GameObject mosaicContainer; // 完了時に非表示にするモザイク

    [SerializeField] private GameObject nextFolderOrFile; // 解放されるフォルダーまたはファイル
    [SerializeField] private FolderButtonScript nextFolderScript; // 直接FolderButtonScriptへの参照
    [SerializeField] private AudioClip completionSound; // 完了時のサウンド
    [SerializeField] private TxtPuzzleConnector puzzleConnector;
    [SerializeField] private Button closeButton; // 閉じるボタンへの参照

    [Header("完了演出設定")]
    [SerializeField] private float completionDuration = 1.0f; // 完了演出の所要時間
    [SerializeField] private float unlockDelay = 20.0f; // フォルダー解放後、ボタンをアンロックするまでの遅延

    [Header("セーブ用設定")]
    [Tooltip("このTXTファイルの識別名（必ず設定してください）")]
    [SerializeField] private string fileName = "dialogue.txt"; // このパズルに関連するファイル名

    [Header("オーバーレイ設定")]
    [SerializeField] private GameObject overlayPanel; // 操作をブロックするオーバーレイパネル

    private AudioSource audioSource;
    private bool isPuzzleCompleted = false;
    private bool isProcessingCompletion = false; // 完了処理中かどうか
    private bool mosaicContainerVerified = false;
    private bool forceApplyCorrectState = false;
    private bool hasBeenOpenedThisSession = false; // このセッション中に開かれたかフラグ


    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // ロード時にも話者名を更新
        if (Application.isPlaying)
        {
            // 少し遅延させて確実にデータがロードされてから実行
            Invoke(nameof(UpdateSpeakerNamesBasedOnLanguage), 0.1f);
        }
    }

    private void Start()
    {
        // 言語設定に応じてSpeakerDropAreaの話者名を更新
        UpdateSpeakerNamesBasedOnLanguage();

        // 言語変更イベントに登録
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += OnLanguageChanged;
        }

        // オーバーレイパネルを初期状態で非表示に
        if (overlayPanel != null)
            overlayPanel.SetActive(false);

        // ボタン参照の取得を試みる (自動検索部分のみ残す)
        if (closeButton == null)
        {
            Transform parent = transform.parent;
            while (parent != null)
            {
                Transform titleBar = parent.Find("TitleBar");
                if (titleBar != null)
                {
                    closeButton = titleBar.Find("CloseButton")?.GetComponent<Button>();
                    break;
                }
                parent = parent.parent;
            }
        }
        // モザイクコンテナの参照を検証
        VerifyMosaicContainer();
    }

    /// <summary>
    /// 言語設定に基づいてすべてのSpeakerDropAreaの話者名を更新
    /// </summary>
    private void UpdateSpeakerNamesBasedOnLanguage()
    {
        // GameSaveManagerから言語コードを取得
        string languageCode = GetCurrentLanguageCode();

            Debug.Log($"TxtPuzzleManager '{fileName}': 言語コード '{languageCode}' で話者名を更新中...");

        // すべてのDropAreaの話者名を更新
        foreach (var dropArea in dropAreas)
        {
            if (dropArea != null)
            {
                UpdateDropAreaSpeakerName(dropArea, languageCode);
            }
        }
            Debug.Log($"TxtPuzzleManager '{fileName}': {dropAreas.Count}個のDropAreaの話者名を更新完了");
    }

    /// <summary>
    /// 個別のSpeakerDropAreaの話者名を言語設定に応じて更新
    /// </summary>
    /// <param name="dropArea">更新対象のSpeakerDropArea</param>
    /// <param name="languageCode">言語コード（"ja" または "en"）</param>
    private void UpdateDropAreaSpeakerName(SpeakerDropArea dropArea, string languageCode)
    {
        if (dropArea == null) return;

        // リフレクションを使用してSpeakerDropAreaのexpectedSpeakerフィールドにアクセス
        var dropAreaType = dropArea.GetType();
        var expectedSpeakerField = dropAreaType.GetField("expectedSpeaker",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (expectedSpeakerField == null)
        {
            Debug.LogWarning($"TxtPuzzleManager: expectedSpeakerフィールドが見つかりません");
            return;
        }

        // 言語に応じた話者名フィールドを取得
        string fieldName = (languageCode == "en")
            ? "expectedSpeaker_English"
            : "expectedSpeaker_Japanese";

        var speakerNameField = dropAreaType.GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (speakerNameField != null)
        {
            // 言語別フィールドからexpectedSpeakerに値をコピー
            string speakerName = speakerNameField.GetValue(dropArea) as string;
            if (!string.IsNullOrEmpty(speakerName))
            {
                expectedSpeakerField.SetValue(dropArea, speakerName);

                    Debug.Log($"DropArea更新: {dropArea.gameObject.name} - 話者名を '{speakerName}' に設定（{languageCode}）");
            }
        }
        else
        {
            Debug.LogWarning($"TxtPuzzleManager: {fieldName}フィールドが見つかりません");
        }
    }

    /// <summary>
    /// 現在の言語コードを取得
    /// </summary>
    /// <returns>言語コード（"ja" または "en"）</returns>
    private string GetCurrentLanguageCode()
    {
        // LocalizationManagerが存在する場合はそこから取得
        if (LocalizationManager.Instance != null)
        {
            return LocalizationManager.Instance.GetCurrentLanguageCode();
        }

        // LocalizationManagerがない場合はGameSaveManagerから直接取得
        var saveManager = FindObjectOfType<GameSaveManager>();
        if (saveManager != null)
        {
            var saveData = saveManager.GetCurrentSaveData();
            if (saveData != null && !string.IsNullOrEmpty(saveData.languageCode))
            {
                return saveData.languageCode;
            }
        }

        // デフォルトは日本語
        return "ja";
    }

    /// <summary>
    /// 言語変更時に呼ばれるコールバック
    /// </summary>
    /// <param name="newLocale">新しいロケール</param>
    private void OnLanguageChanged(UnityEngine.Localization.Locale newLocale)
    {
        if (newLocale != null)
        {
            string languageCode = newLocale.Identifier.Code;
            UpdateSpeakerNamesBasedOnLanguage();

            Debug.Log($"TxtPuzzleManager '{fileName}': 言語が '{languageCode}' に変更されました");
        }
    }

    private void OnDestroy()
    {
        // 言語変更イベントの登録解除
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged -= OnLanguageChanged;
        }
    }


    private void VerifyMosaicContainer()
    {
        if (mosaicContainerVerified) return;
        mosaicContainerVerified = true;
    }

    // OnTransformParentChangedメソッドを削除（自動再検索しない）

    public void CheckPuzzleCompletion()
    {
        // すでに完了している場合は何もしない
        if (isPuzzleCompleted) return;

        bool allCorrect = true;
        int correctCount = 0;

        // すべてのドロップエリアが正解か確認
        foreach (var area in dropAreas)
        {
            if (area == null) continue;
            if (!area.IsCorrect())
                allCorrect = false;
            else
                correctCount++;
        }

        // デバッグログの追加
        Debug.Log($"TxtPuzzle '{fileName}' 完了チェック: {correctCount}/{dropAreas.Count} が正解");

        // ファイル名と進捗度を表示
        Debug.Log($"{fileName} 進捗度 {correctCount}/{dropAreas.Count}");

        // すべて正解の場合
        if (allCorrect && dropAreas.Count > 0)
        {
            // 進捗度100%（パズル完了）のログ
            Debug.Log($"{fileName} 進捗度 {dropAreas.Count}/{dropAreas.Count} パズル完了");

            // 完了処理中フラグを設定
            isProcessingCompletion = true;
            isPuzzleCompleted = true;

            // モザイクコンテナを非表示に
            if (mosaicContainer != null)
            {
                mosaicContainer.SetActive(false);
            }

            // 追加: オリジナル画像をアクティブに
            if (originalImage != null)
            {
                originalImage.SetActive(true);
            }


            // 閉じるボタンを無効化
            LockCloseButton();

            // 完了サウンドを再生
            if (audioSource != null && completionSound != null)
                audioSource.PlayOneShot(completionSound);
            else
                SoundEffectManager.Instance?.PlayAllRevealedSound();

            // モザイク解除通知 - この呼び出しが重要
            if (puzzleConnector != null)
            {
                puzzleConnector.OnTxtPuzzleSolved();
            }

            // 次のフォルダーまたはファイルを解放
            StartCoroutine(UnlockNextFolder());

            // 完成時にゲーム状態を保存
            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.SaveGame();
                Debug.Log($"TXTパズル完成: {fileName} - ゲーム状態を保存しました");
            }
        }
    }

    private IEnumerator UnlockNextFolder()
    {
        // オーバーレイを表示して操作をブロック
        if (overlayPanel != null)
        {
            overlayPanel.SetActive(true);
        }

        // 閉じるボタンを非アクティブ化
        if (closeButton != null && closeButton.gameObject.activeSelf)
        {
            closeButton.gameObject.SetActive(false);
        }

        // 演出のための待機
        yield return new WaitForSeconds(completionDuration);

        // 次のフォルダーを表示（強制的にアクティブに）
        if (nextFolderOrFile != null)
        {
            // FolderActivationGuardを確認
            FolderActivationGuard guard = nextFolderOrFile.GetComponent<FolderActivationGuard>();
            if (guard != null)
            {
                guard.SetActivated(true);
            }

            nextFolderOrFile.SetActive(true);

            // FolderButtonScriptが直接アタッチされている場合
            FolderButtonScript folderScript = nextFolderOrFile.GetComponent<FolderButtonScript>();
            if (folderScript != null)
            {
                // SetActivatedStateを先に呼び出し
                folderScript.SetActivatedState(true);
                folderScript.SetVisible(true);

                // filePanelが非表示の場合は表示する
                if (folderScript.filePanel != null && !folderScript.filePanel.activeSelf)
                {
                    folderScript.filePanel.SetActive(true);
                }
            }
        }

        // 直接参照されたFolderButtonScriptがある場合はその対応するファイルパネルを表示
        if (nextFolderScript != null)
        {
            // SetActivatedStateを最初に呼び出す
            nextFolderScript.SetActivatedState(true);
            nextFolderScript.gameObject.SetActive(true);

            if (nextFolderScript.filePanel != null)
            {
                nextFolderScript.filePanel.SetActive(true);
            }
        }

        Debug.Log("TXTパズルが完了しました！新しいフォルダーが解放されました。");

        // パズル完成後にゲームをセーブ
        if (GameSaveManager.Instance != null)
        {
            GameSaveManager.Instance.SaveGame();
        }

        // 完了処理の終了後、遅延してボタンを再アクティブ化
        yield return new WaitForSeconds(unlockDelay);

        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(true);
        }

        if (overlayPanel != null)
        {
            overlayPanel.SetActive(false);
        }

        UnlockCloseButton();

        // 完了処理中フラグを解除
        isProcessingCompletion = false;
    }

    // 閉じるボタンをロックする
    private void LockCloseButton()
    {
        if (closeButton != null)
        {
            FileClose fileClose = closeButton.GetComponent<FileClose>();
            if (fileClose != null)
            {
                fileClose.LockButton();
            }
            else
            {
                closeButton.interactable = false;
            }
        }
    }

    // 閉じるボタンのロックを解除する
    private void UnlockCloseButton()
    {
        if (closeButton != null)
        {
            FileClose fileClose = closeButton.GetComponent<FileClose>();
            if (fileClose != null)
            {
                fileClose.UnlockButton();
            }
            else
            {
                closeButton.interactable = true;
            }
        }
    }

    // 完了処理中かどうかを返すメソッド
    public bool IsProcessingCompletion()
    {
        return isProcessingCompletion;
    }

    // 手動でSpeakerDropAreaを登録するメソッド（インスペクターから設定しない場合のバックアップとして残す）
    public void RegisterDropArea(SpeakerDropArea area)
    {
        if (!dropAreas.Contains(area))
            dropAreas.Add(area);
    }

    /// <summary>
    /// このTXTファイルのパズル進捗状態を取得
    /// </summary>
    public TxtFileData GetTxtProgress()
    {
        // 正解数をカウント
        int solvedCount = 0;
        foreach (var area in dropAreas)
        {
            if (area != null && area.IsCorrect())
                solvedCount++;
        }

        return new TxtFileData
        {
            fileName = fileName,
            isCompleted = isPuzzleCompleted,
            solvedMatches = solvedCount,
            totalMatches = dropAreas.Count
        };
    }

    /// <summary>
    /// すべてのTXTファイルの進捗を取得
    /// </summary>
    public Dictionary<string, TxtFileData> GetAllTxtProgress()
    {
        Dictionary<string, TxtFileData> progress = new Dictionary<string, TxtFileData>();

        TxtFileData fileData = GetTxtProgress();
        if (!string.IsNullOrEmpty(fileData.fileName))
            progress.Add(fileData.fileName, fileData);

        return progress;
    }

    // 次のフォルダーを取得するメソッド
    public GameObject GetNextFolder()
    {
        // 直接参照されたnextFolderOrFileを返す
        if (nextFolderOrFile != null)
        {
            return nextFolderOrFile;
        }

        // 直接参照されたnextFolderScriptがある場合、そのゲームオブジェクトを返す
        if (nextFolderScript != null)
        {
            return nextFolderScript.gameObject;
        }

        return null;
    }

    // パズル完了状態をセーブ後に復元するためのメソッド
    public void ApplyTxtProgress(Dictionary<string, TxtFileData> progressData)
    {
        if (progressData == null || string.IsNullOrEmpty(fileName))
            return;

        // セーブデータがない場合は必ず初期状態を維持
        if (!GameSaveManager.Instance.SaveDataExists())
        {
            isPuzzleCompleted = false;
            forceApplyCorrectState = false;
            hasBeenOpenedThisSession = false;
            ResetAllAreas();
            return;
        }

        // このTXTファイルの進捗データを探す
        if (progressData.TryGetValue(fileName, out TxtFileData fileData))
        {
            // 進捗の復元
            isPuzzleCompleted = fileData.isCompleted;

            // 追加: 完了状態ならオリジナル画像を表示
            if (isPuzzleCompleted && originalImage != null)
            {
                originalImage.SetActive(true);
            }

            // 完了状態の場合
            if (isPuzzleCompleted)
            {
                // 正解状態の強制適用フラグを立てる
                forceApplyCorrectState = true;

                // 次のフォルダーを必ず表示
                if (nextFolderOrFile != null)
                {
                    nextFolderOrFile.SetActive(true);

                    FolderButtonScript folderScript = nextFolderOrFile.GetComponent<FolderButtonScript>();
                    if (folderScript != null)
                    {
                        folderScript.SetActivatedState(true);
                    }

                    FolderActivationGuard guard = nextFolderOrFile.GetComponent<FolderActivationGuard>();
                    if (guard != null)
                    {
                        guard.SetActivated(true);
                    }
                }

                if (nextFolderScript != null)
                {
                    nextFolderScript.gameObject.SetActive(true);
                    nextFolderScript.SetActivatedState(true);

                    FolderActivationGuard guard = nextFolderScript.gameObject.GetComponent<FolderActivationGuard>();
                    if (guard != null)
                    {
                        guard.SetActivated(true);
                    }
                }

                if (puzzleConnector != null)
                    puzzleConnector.OnTxtPuzzleSolved();
            }
            else
            {
                forceApplyCorrectState = false;
            }
        }
        else
        {
            // 進捗データが見つからない場合は初期化
            isPuzzleCompleted = false;
            forceApplyCorrectState = false;
            hasBeenOpenedThisSession = false; // 追加
        }
    }

    // パズルの状態取得・設定用メソッド
    public TxtPuzzleConnector GetPuzzleConnector() => puzzleConnector;

    public bool IsPuzzleCompleted()
    {
        // たとえDropAreaが変更になっていても、既にパズルが完了済みとマークされていれば、それを尊重する
        if (isPuzzleCompleted)
            return true;

        // そうでない場合は現在の状態をチェック
        bool allCorrect = true;
        foreach (var area in dropAreas)
        {
            if (area == null) continue;
            if (!area.IsCorrect())
                allCorrect = false;
        }

        // 新しく完了した場合は、フラグを永続的に設定
        if (allCorrect && dropAreas.Count > 0)
            isPuzzleCompleted = true;

        return isPuzzleCompleted;
    }

    // SetPuzzleCompletedメソッド - 完了状態は永続的
    public void SetPuzzleCompleted(bool completed)
    {
        // すでに完了していた場合は常に完了状態を維持
        if (isPuzzleCompleted)
            return;

        // 新しく完了状態に設定する場合のみ
        if (completed)
            isPuzzleCompleted = true;
    }

    /// <summary>
    /// ゲームオブジェクトがアクティブになった時に呼ばれる
    /// </summary>
    private void OnEnable()
    {
        // このファイルのセーブデータを確認
        bool hasSaveData = GameSaveManager.Instance != null &&
                           GameSaveManager.Instance.SaveDataExists();

        // セーブデータがない、またはこのセッション初回オープンの場合
        if (!hasSaveData || !hasBeenOpenedThisSession)
        {
            // このセッション初回オープンフラグを設定
            hasBeenOpenedThisSession = true;

            // 完了済みでない場合は初期状態にリセット
            if (!isPuzzleCompleted)
            {
                forceApplyCorrectState = false;
                ResetAllAreas();
                return;
            }
        }

        // 完了済みの場合のみ強制適用
        if (isPuzzleCompleted || forceApplyCorrectState)
        {
            CancelInvoke("ForceCorrectStateForAllAreas");
            Invoke("ForceCorrectStateForAllAreas", 0.1f);
            Invoke("VerifyCorrectStateForAllAreas", 0.3f);
            StartCoroutine(ImmediateStateCheck());

            if (originalImage != null)
            {
                originalImage.SetActive(true);
            }
        }
        else
        {
            ResetAllAreas();
        }
    }

    /// <summary>
    /// ゲームオブジェクトが非アクティブになったときに呼ばれる
    /// </summary>
    private void OnDisable()
    {
        // パネルが閉じられた時にセッションフラグをリセット
        hasBeenOpenedThisSession = false;
    }

    // 追加: 全てのエリアを明示的にリセットするメソッド
    private void ResetAllAreas()
    {
        foreach (var area in dropAreas)
        {
            if (area != null)
            {
                area.ResetArea();
            }
        }
    }

    // 即時チェックを行うコルーチン
    private IEnumerator ImmediateStateCheck()
    {
        // 1フレーム待機（レンダリングが確実に行われるのを待つ）
        yield return null;

        if (isPuzzleCompleted || forceApplyCorrectState)
        {
            foreach (var area in dropAreas)
            {
                if (area != null && string.IsNullOrEmpty(area.GetComponentInChildren<Text>()?.text))
                {
                    area.ForceCorrectStateWithoutSpeaker();
                }
            }
        }
    }

    private void VerifyCorrectStateForAllAreas()
    {
        if (!isPuzzleCompleted && !forceApplyCorrectState) return;

        bool allCorrect = true;
        foreach (var area in dropAreas)
        {
            if (area != null && !area.IsCorrect())
            {
                area.ForceCorrectStateWithoutSpeaker();
                allCorrect = false;
            }
            else if (area != null)
            {
                // 正解状態でも、エリア自体に強制的に正解状態を再設定
                area.ForceCorrectStateWithoutSpeaker();
            }
        }

        if (!allCorrect)
        {
            Debug.Log($"TxtPuzzle '{fileName}': 一部のエリアが正解状態でなかったため再適用しました");
        }
    }

    private void ForceCorrectStateForAllAreas()
    {
        // 各エリアについて詳細ログを出力しながら処理
        foreach (var area in dropAreas)
        {
            if (area != null)
            {
                bool wasCorrect = area.IsCorrect();
                if (!wasCorrect)
                {
                    // 発言者が見つからなくても強制的に正解状態にする
                    area.ForceCorrectStateWithoutSpeaker();
                }
            }
        }
    }

    // DropAreasリストを取得するパブリックメソッド
    public List<SpeakerDropArea> GetDropAreas()
    {
        return dropAreas;
    }

    // ファイル名を取得するパブリックメソッド
    public string GetFileName()
    {
        return fileName;
    }
}