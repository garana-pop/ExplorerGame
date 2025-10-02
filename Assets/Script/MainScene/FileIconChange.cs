using UnityEngine;
using UnityEngine.UI;
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

    [Header("デバッグ設定")]
    [Tooltip("デバッグログを表示するかどうか")]
    [SerializeField] private bool debugMode = false;

    [Header("状態管理")]
    [Tooltip("パズル完了状態を記録するフラグ")]
    private bool isPuzzleCompleted = false;

    private void OnEnable()
    {
        // オブジェクトがアクティブになる度にパズルの状態をチェック
        // 完了済みの場合はすぐにアイコンを変更
        if (isPuzzleCompleted)
        {
            ApplyCompletedSprite();
            if (debugMode)
            {
                Debug.Log($"FileIconChange: 完了済み状態を適用 - {gameObject.name}");
            }
            return;
        }

        CheckPuzzleState();

        if (debugMode)
        {
            Debug.Log($"FileIconChange: OnEnableでパズル状態をチェックしました - {gameObject.name}");
        }
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
            if (debugMode)
            {
                Debug.LogWarning($"FileIconChange: パズルパネル内にSpeakerDropAreaが見つかりません - {gameObject.name}");
            }
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

        if (debugMode)
        {
            Debug.Log($"FileIconChange: 正解数(correctCount)={correctCount}, 総数(totalCount)={totalCount} - {gameObject.name}");
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

        if (debugMode)
        {
            Debug.Log($"FileIconChange: パズル完了状態を設定しました - {gameObject.name}");
        }
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
            if (debugMode)
            {
                Debug.Log($"カスタム設定されたパズル完了を検出しました: correctCount={correctCount}, totalCount={totalCount}");
            }
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

        if (debugMode)
        {
            Debug.Log($"FileIconChange: パズル完了通知を受け取りました - ファイル名: {fileName}, オブジェクト名: {gameObject.name}");
        }
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
}