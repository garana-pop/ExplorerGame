using UnityEngine;
using OpeningScene;

/// <summary>
/// キャラクター退場を制御するためのコンポーネント
/// dialogueTextFile内で "exit" コマンドを検出したときにキャラクターを非表示にします
/// </summary>
public class CharacterExitController : MonoBehaviour
{
    [Header("制御対象")]
    [SerializeField] private GameObject leftCharacter;
    [SerializeField] private float fadeOutDuration = 1.0f;
    [SerializeField] private bool useAnimation = true;

    [Header("デバッグ")]
    [SerializeField] private bool debugMode = false;

    private void Start()
    {
        // 必要なオブジェクトの取得
        if (leftCharacter == null)
        {
            leftCharacter = GameObject.Find("LeftCharacter");
            if (leftCharacter == null)
            {
                Debug.LogWarning("CharacterExitController: LeftCharacterが見つかりません。");
            }
        }

        // イベントリスナーを登録
        RegisterEventListeners();
    }

    private void RegisterEventListeners()
    {
        // ダイアログ表示イベントをリッスン
        DialogueEventNotifier.OnDialogueDisplayed += OnDialogueDisplayed;
    }

    /// <summary>
    /// ダイアログが表示されたときのイベントハンドラ
    /// </summary>
    private void OnDialogueDisplayed(DialogueEntry entry)
    {
        // コマンドであり、exitコマンドの場合処理する
        if (entry.isCommand && entry.commandParam == "exit")
        {
            if (debugMode)
            {
                Debug.Log("CharacterExitController: exitコマンドを検出しました。LeftCharacterを非表示にします。");
            }

            // キャラクターを非表示
            HideLeftCharacter();
        }
    }

    /// <summary>
    /// LeftCharacterを非表示にする
    /// </summary>
    private void HideLeftCharacter()
    {
        if (leftCharacter == null)
            return;

        if (useAnimation && fadeOutDuration > 0)
        {
            // フェードアウトアニメーションを使用
            StartCoroutine(FadeOutCharacter());
        }
        else
        {
            // 即時非表示
            leftCharacter.SetActive(false);
        }
    }

    /// <summary>
    /// キャラクターをフェードアウトさせる
    /// </summary>
    private System.Collections.IEnumerator FadeOutCharacter()
    {
        CanvasGroup canvasGroup = leftCharacter.GetComponent<CanvasGroup>();

        // CanvasGroupがない場合は追加
        if (canvasGroup == null)
        {
            canvasGroup = leftCharacter.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1.0f;
        }

        float startTime = Time.time;
        float startAlpha = canvasGroup.alpha;

        // フェードアウト
        while (Time.time < startTime + fadeOutDuration)
        {
            float elapsedTime = Time.time - startTime;
            float normalizedTime = elapsedTime / fadeOutDuration;

            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, normalizedTime);
            yield return null;
        }

        // 最終的に透明化を確実に
        canvasGroup.alpha = 0f;

        // 処理が完了したら非表示
        leftCharacter.SetActive(false);

        if (debugMode)
        {
            Debug.Log("CharacterExitController: キャラクターがフェードアウトしました");
        }
    }

    /// <summary>
    /// 開発者向け: 手動でキャラクターを退場させる
    /// </summary>
    public void ExitCharacterManually()
    {
        HideLeftCharacter();
    }

    private void OnDestroy()
    {
        // イベントリスナーの登録解除
        DialogueEventNotifier.OnDialogueDisplayed -= OnDialogueDisplayed;
    }
}