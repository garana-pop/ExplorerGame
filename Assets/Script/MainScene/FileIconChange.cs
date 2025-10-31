using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// パズル完了時（correctCount == totalCount）にファイルアイコンを変更するコンポーネント
/// </summary>
public class FileIconChange : MonoBehaviour
{
    [Header("アイコン設定")]
    [Tooltip("変更前のアイコンスプライト")]
    [SerializeField] private Sprite defaultSprite;

    [Tooltip("変更後のアイコンスプライト")]
    [SerializeField] private Sprite completedSprite;

    [Tooltip("変更対象のImageコンポーネント（未設定の場合は自身のImageを使用）")]
    [SerializeField] private Image iconImage;

    [Header("パズル参照")]
    [Tooltip("カスタム：インスペクターで直接設定するドロップエリア")]
    [SerializeField] private List<SpeakerDropArea> dropAreas = new List<SpeakerDropArea>();

    [Tooltip("従来互換：パズルがまとめて含まれるパネル（dropAreasが空の場合のみ使用）")]
    [SerializeField] private GameObject puzzlePanel;

    [Header("パズルマネージャー参照")]
    [Tooltip("インスペクターで設定するTxtPuzzleManager（自動検索しません）")]
    [SerializeField] private TxtPuzzleManager puzzleManager;

    [Header("イベント")]
    [Tooltip("パズル完了時に発行されるイベント")]
    public UnityEvent onPuzzleCompleted;

    [Header("状態管理")]
    [Tooltip("パズル完了状態を記録するフラグ")]
    private bool isPuzzleCompleted = false;

    /// <summary>
    /// オブジェクトがアクティブになったときの初期化処理を行う。
    /// </summary>
    /// <remarks>パズルがすでに完了している場合は、即座に完了スプライトを適用する。それ以外の場合は、現在のパズル状態をチェックする。</remarks>
    private void OnEnable()
    {
        // オブジェクトがアクティブになる度にパズルの状態をチェック
        // 完了済みの場合はすぐにアイコンを変更
        if (isPuzzleCompleted)
        {
            ApplyCompletedSprite();
            return;
        }

        CheckPuzzleState();
    }

    private void Start()
    {
        // iconImageが設定されていなければ、自身のImageコンポーネントを取得
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }

        // デフォルトスプライトを適用
        if (iconImage != null && defaultSprite != null)
        {
            iconImage.sprite = defaultSprite;
        }

        // パズルの状態をチェック（既存コード）
        CheckPuzzleState();
    }

    /// <summary>
    /// パズルの状態をチェックし、アイコンを更新
    /// </summary>
    private void CheckPuzzleState()
    {
        // 既に完了済みの場合は処理をスキップ
        if (isPuzzleCompleted)
        {
            ApplyCompletedSprite();
            return;
        }

        // インスペクターで直接設定されたドロップエリアがある場合はそれを使用
        if (dropAreas != null && dropAreas.Count > 0)
        {
            CheckCustomDropAreas();
            return;
        }

        // 従来通り: パズルパネルから自動検索
        if (puzzlePanel == null) return;

        // パネルからSpeakerDropAreaを全て取得 (非アクティブも含む)
        SpeakerDropArea[] panelDropAreas = puzzlePanel.GetComponentsInChildren<SpeakerDropArea>(true);
        if (panelDropAreas.Length == 0)
        {
            return;
        }

        // 正解数と総数カウント
        int correctCount = 0;
        int totalCount = panelDropAreas.Length;

        foreach (var area in panelDropAreas)
        {
            if (area != null && area.IsCorrect())
            {
                correctCount++;
            }
        }
        // 全て正解ならアイコンを変更
        if (correctCount == totalCount && totalCount > 0)
        {
            SetPuzzleCompleted();
        }
    }

    /// <summary>
    /// パズル完了状態を設定
    /// </summary>
    public void SetPuzzleCompleted()
    {
        if (isPuzzleCompleted)
        {
            return; // 既に完了済みなら何もしない
        }

        isPuzzleCompleted = true;
        ApplyCompletedSprite();

        // パズル完了イベントを発行：SteamAchievementsUnlock2_HintsOfTruthに通知
        onPuzzleCompleted?.Invoke();
    }

    /// <summary>
    /// インスペクターで設定されたカスタムドロップエリアをチェック
    /// </summary>
    private void CheckCustomDropAreas()
    {
        // 無効なエリアを除外
        dropAreas.RemoveAll(area => area == null);

        if (dropAreas.Count == 0) return;

        // 正解数と総数をカウント
        int correctCount = 0;
        int totalCount = dropAreas.Count;

        foreach (var area in dropAreas)
        {
            if (area != null && area.IsCorrect())
            {
                correctCount++;
            }
        }

        // 全て正解ならアイコンを変更
        if (correctCount == totalCount)
        {
            ApplyCompletedSprite();
        }
    }

    /// <summary>
    /// 完了時のスプライトを適用
    /// </summary>
    private void ApplyCompletedSprite()
    {
        if (iconImage != null && completedSprite != null)
        {
            iconImage.sprite = completedSprite;
        }
    }


    /// <summary>
    /// パズル完了通知を受け取るメソッド (SpeakerDropAreaから呼び出される)
    /// </summary>
    /// <param name="fileName">完了したファイル名</param>
    public void OnPuzzleCompleted(string fileName)
    {
        SetPuzzleCompleted();
    }

    /// <summary>
    /// カスタムドロップエリアの追加（スクリプトから動的に追加する場合）
    /// </summary>
    public void AddDropArea(SpeakerDropArea area)
    {
        if (area != null && !dropAreas.Contains(area))
        {
            dropAreas.Add(area);
            CheckPuzzleState(); // 追加後に状態を再チェック
        }
    }

    /// <summary>
    /// カスタムドロップエリアのリストをクリア
    /// </summary>
    public void ClearDropAreas()
    {
        dropAreas.Clear();
    }

    /// <summary>
    /// パズル完了状態を取得
    /// </summary>
    /// <returns>完了済みならtrue</returns>
    public bool IsPuzzleCompleted()
    {
        return isPuzzleCompleted;
    }

    /// <summary>
    /// パズル完了状態を外部から設定（セーブデータ復元用）
    /// </summary>
    /// <param name="completed">完了状態</param>
    public void SetPuzzleCompletedState(bool completed)
    {
        isPuzzleCompleted = completed;

        if (completed)
        {
            ApplyCompletedSprite();

            Debug.Log($"FileIconChange: セーブデータから完了状態を復元 - {gameObject.name}");
        }
        else
        {
            // 未完了の場合はデフォルトスプライトに戻す
            if (iconImage != null && defaultSprite != null)
            {
                iconImage.sprite = defaultSprite;
            }
        }
    }

    /// <summary>
    /// 関連するTxtPuzzleManagerの完了状態を確認して反映
    /// </summary>
    public void SyncWithPuzzleManager()
    {
        if (puzzleManager != null && puzzleManager.IsPuzzleCompleted())
        {
            SetPuzzleCompletedState(true);
        }
        else if (puzzleManager != null)
        {
            Debug.LogError("FileIconChange: TxtPuzzleManagerが設定されていません。");
        }
    }
}