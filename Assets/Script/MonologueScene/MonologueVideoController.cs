using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// MonologueScene用の動画再生と色調反転制御コンポーネント
/// CLIP STUDIOから書き出されたMP4アニメーションの再生制御を行う
/// </summary>
public class MonologueVideoController : MonoBehaviour
{
    [Header("動画再生設定")]
    [Tooltip("VideoPlayerコンポーネントへの参照")]
    [SerializeField] private VideoPlayer videoPlayer;

    [Tooltip("動画を表示するRawImageコンポーネント")]
    [SerializeField] private RawImage videoDisplay;

    [Tooltip("再生する動画クリップ")]
    [SerializeField] private VideoClip[] videoClips;

    [Header("ループ設定")]
    [Tooltip("ループ開始時間（秒）")]
    [SerializeField] private double loopStartTime = 5.0;

    [Tooltip("ループ終了時間（秒）")]
    [SerializeField] private double loopEndTime = 21.0;

    [Tooltip("最初からループ範囲まで再生するか")]
    [SerializeField] private bool playFromBeginning = false;

    [Header("色調反転設定")]
    [Tooltip("色調反転用のシェーダーマテリアル")]
    [SerializeField] private Material invertMaterial;

    [Tooltip("色調反転の強度 (0:通常, 1:完全反転)")]
    [Range(0f, 1f)]
    [SerializeField] private float invertAmount = 0f;

    [Tooltip("明度調整")]
    [Range(0f, 2f)]
    [SerializeField] private float brightness = 1f;

    [Tooltip("コントラスト調整")]
    [Range(0f, 2f)]
    [SerializeField] private float contrast = 1f;

    [Header("再生制御")]
    [Tooltip("シーン開始時に自動再生するか")]
    [SerializeField] private bool autoPlay = true;

    [Tooltip("ループ再生するか")]
    [SerializeField] private bool loopVideo = true;

    [Tooltip("動画終了時の動作")]
    [SerializeField] private VideoEndAction endAction = VideoEndAction.Loop;

    [Header("デバッグ設定")]
    [SerializeField] private bool debugMode = false;

    // プライベート変数
    private RenderTexture renderTexture;
    private Material materialInstance;
    private int currentClipIndex = 0;
    private bool isInLoopRange = false;

    // 定数
    private const string INVERT_AMOUNT_PROPERTY = "_InvertAmount";
    private const string BRIGHTNESS_PROPERTY = "_Brightness";
    private const string CONTRAST_PROPERTY = "_Contrast";

    /// <summary>
    /// 動画終了時の動作を定義する列挙型
    /// </summary>
    public enum VideoEndAction
    {
        Stop,           // 停止
        Loop,           // ループ
        NextClip,       // 次のクリップ
        HideVideo       // 動画を非表示
    }

    #region Unity Lifecycle

    private void Awake()
    {
        // コンポーネントの自動取得
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        if (videoDisplay == null)
            videoDisplay = GetComponent<RawImage>();

        // VideoPlayerの基本設定
        SetupVideoPlayer();
    }

    private void Start()
    {
        // マテリアルの初期化
        InitializeMaterial();

        // 自動再生設定
        if (autoPlay && videoClips.Length > 0)
        {
            PlayVideo(0);
        }

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: 初期化完了");
    }

    private void Update()
    {
        // マテリアルプロパティの更新
        UpdateMaterialProperties();

        // ループ範囲チェック
        if (videoPlayer != null && videoPlayer.isPlaying && loopVideo)
        {
            CheckLoopRange();
        }
    }

    private void OnDestroy()
    {
        // リソースのクリーンアップ
        CleanupResources();
    }

    #endregion

    #region 初期化メソッド

    /// <summary>
    /// VideoPlayerの基本設定を行う
    /// </summary>
    private void SetupVideoPlayer()
    {
        if (videoPlayer == null) return;

        // レンダーモードを RenderTexture に設定
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;

        // RenderTextureの作成
        CreateRenderTexture();

        // イベントの登録
        videoPlayer.loopPointReached += OnVideoEnd;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.frameReady += OnVideoFrameReady;

        // 通常のループ機能は無効化（カスタムループを使用）
        videoPlayer.isLooping = false;

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: VideoPlayer設定完了");
    }

    /// <summary>
    /// RenderTextureを作成する
    /// </summary>
    private void CreateRenderTexture()
    {
        // 既存のRenderTextureがあれば破棄
        if (renderTexture != null)
        {
            renderTexture.Release();
        }

        // 新しいRenderTextureを作成 (ゲームの基準解像度に合わせる)
        renderTexture = new RenderTexture(1920, 1080, 0);
        renderTexture.Create();

        // VideoPlayerとRawImageに設定
        if (videoPlayer != null)
            videoPlayer.targetTexture = renderTexture;

        if (videoDisplay != null)
            videoDisplay.texture = renderTexture;

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: RenderTexture作成完了");
    }

    /// <summary>
    /// 色調反転マテリアルを初期化する
    /// </summary>
    private void InitializeMaterial()
    {
        if (invertMaterial == null)
        {
            Debug.LogWarning($"{nameof(MonologueVideoController)}: 色調反転マテリアルが設定されていません");
            return;
        }

        // マテリアルのインスタンスを作成
        materialInstance = new Material(invertMaterial);

        // RawImageにマテリアルを適用
        if (videoDisplay != null)
        {
            videoDisplay.material = materialInstance;
        }

        // 初期プロパティを設定
        UpdateMaterialProperties();

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: マテリアル初期化完了");
    }

    #endregion

    #region 動画制御メソッド

    /// <summary>
    /// 指定されたインデックスの動画を再生する
    /// </summary>
    /// <param name="clipIndex">再生する動画のインデックス</param>
    public void PlayVideo(int clipIndex)
    {
        if (videoClips == null || clipIndex < 0 || clipIndex >= videoClips.Length)
        {
            Debug.LogError($"{nameof(MonologueVideoController)}: 無効な動画インデックス: {clipIndex}");
            return;
        }

        currentClipIndex = clipIndex;
        videoPlayer.clip = videoClips[clipIndex];

        // 最初から再生するか、ループ開始位置から再生するかを決定
        if (playFromBeginning)
        {
            videoPlayer.time = 0;
            isInLoopRange = false;
        }
        else
        {
            videoPlayer.time = loopStartTime;
            isInLoopRange = true;
        }

        videoPlayer.Play();

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: 動画再生開始 - {videoClips[clipIndex].name}");
    }

    /// <summary>
    /// 現在の動画を停止する
    /// </summary>
    public void StopVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            isInLoopRange = false;
        }

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: 動画停止");
    }

    /// <summary>
    /// 次の動画を再生する
    /// </summary>
    public void PlayNextVideo()
    {
        int nextIndex = (currentClipIndex + 1) % videoClips.Length;
        PlayVideo(nextIndex);
    }

    /// <summary>
    /// 動画の表示/非表示を切り替える
    /// </summary>
    /// <param name="visible">表示するかどうか</param>
    public void SetVideoVisible(bool visible)
    {
        if (videoDisplay != null)
        {
            videoDisplay.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// ループ範囲をチェックして処理する
    /// </summary>
    private void CheckLoopRange()
    {
        // ループ終了位置に到達した場合
        if (videoPlayer.time >= loopEndTime)
        {
            // 一度だけ処理を実行するためのフラグチェック
            if (isInLoopRange)
            {
                isInLoopRange = false;

                // 再生を停止してから位置を変更
                videoPlayer.Pause();
                videoPlayer.time = loopStartTime;
                videoPlayer.Play();

                if (debugMode)
                    Debug.Log($"{nameof(MonologueVideoController)}: ループ終了位置に到達。開始位置 {loopStartTime}秒 に戻ります");
            }
        }
        else if (!isInLoopRange && videoPlayer.time >= loopStartTime && videoPlayer.time < loopEndTime)
        {
            isInLoopRange = true;

            if (debugMode)
                Debug.Log($"{nameof(MonologueVideoController)}: ループ範囲に入りました");
        }
    }

    #endregion

    #region 色調反転制御メソッド

    /// <summary>
    /// 色調反転の強度を設定する
    /// </summary>
    /// <param name="amount">反転強度 (0-1)</param>
    public void SetInvertAmount(float amount)
    {
        invertAmount = Mathf.Clamp01(amount);
        UpdateMaterialProperties();

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: 色調反転強度変更 - {invertAmount}");
    }

    /// <summary>
    /// 明度を設定する
    /// </summary>
    /// <param name="brightnessValue">明度値 (0-2)</param>
    public void SetBrightness(float brightnessValue)
    {
        brightness = Mathf.Clamp(brightnessValue, 0f, 2f);
        UpdateMaterialProperties();
    }

    /// <summary>
    /// コントラストを設定する
    /// </summary>
    /// <param name="contrastValue">コントラスト値 (0-2)</param>
    public void SetContrast(float contrastValue)
    {
        contrast = Mathf.Clamp(contrastValue, 0f, 2f);
        UpdateMaterialProperties();
    }

    /// <summary>
    /// 色調反転をアニメーションで切り替える
    /// </summary>
    /// <param name="targetAmount">目標反転強度</param>
    /// <param name="duration">アニメーション時間</param>
    public void AnimateInvert(float targetAmount, float duration = 1f)
    {
        StartCoroutine(AnimateInvertCoroutine(targetAmount, duration));
    }

    /// <summary>
    /// 色調反転アニメーションのコルーチン
    /// </summary>
    private IEnumerator AnimateInvertCoroutine(float targetAmount, float duration)
    {
        float startAmount = invertAmount;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            // スムーズな補間
            float currentAmount = Mathf.Lerp(startAmount, targetAmount, progress);
            SetInvertAmount(currentAmount);

            yield return null;
        }

        // 最終値を設定
        SetInvertAmount(targetAmount);

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: 色調反転アニメーション完了");
    }

    /// <summary>
    /// マテリアルプロパティを更新する
    /// </summary>
    private void UpdateMaterialProperties()
    {
        if (materialInstance == null) return;

        materialInstance.SetFloat(INVERT_AMOUNT_PROPERTY, invertAmount);
        materialInstance.SetFloat(BRIGHTNESS_PROPERTY, brightness);
        materialInstance.SetFloat(CONTRAST_PROPERTY, contrast);
    }

    #endregion

    #region イベントハンドラー

    /// <summary>
    /// 動画の準備が完了した時の処理
    /// </summary>
    /// <param name="vp">VideoPlayer</param>
    private void OnVideoPrepared(VideoPlayer vp)
    {
        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: 動画準備完了 - {vp.clip.name}");

        // ループ開始位置から再生する場合の処理
        if (!playFromBeginning)
        {
            videoPlayer.time = loopStartTime;
            isInLoopRange = true;

            if (debugMode)
                Debug.Log($"{nameof(MonologueVideoController)}: ループ開始位置 {loopStartTime}秒 から再生開始");
        }
    }

    /// <summary>
    /// フレーム更新時の処理
    /// </summary>
    /// <param name="vp">VideoPlayer</param>
    /// <param name="frameIdx">フレームインデックス</param>
    private void OnVideoFrameReady(VideoPlayer vp, long frameIdx)
    {
        // 必要に応じてフレーム単位の処理を追加
    }

    /// <summary>
    /// 動画が終了した時の処理
    /// </summary>
    /// <param name="vp">VideoPlayer</param>
    private void OnVideoEnd(VideoPlayer vp)
    {
        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: 動画終了 - {endAction}");

        switch (endAction)
        {
            case VideoEndAction.Stop:
                StopVideo();
                break;

            case VideoEndAction.Loop:
                // カスタムループ処理
                if (loopVideo)
                {
                    videoPlayer.time = loopStartTime;
                    videoPlayer.Play();
                    isInLoopRange = true;
                }
                break;

            case VideoEndAction.NextClip:
                PlayNextVideo();
                break;

            case VideoEndAction.HideVideo:
                SetVideoVisible(false);
                break;
        }
    }

    #endregion

    #region クリーンアップ

    /// <summary>
    /// リソースのクリーンアップ
    /// </summary>
    private void CleanupResources()
    {
        // イベントの登録解除
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoEnd;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.frameReady -= OnVideoFrameReady;
        }

        // RenderTextureの解放
        if (renderTexture != null)
        {
            renderTexture.Release();
            renderTexture = null;
        }

        // マテリアルインスタンスの破棄
        if (materialInstance != null)
        {
            DestroyImmediate(materialInstance);
            materialInstance = null;
        }

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: リソースクリーンアップ完了");
    }

    #endregion

    #region ループ範囲設定

    /// <summary>
    /// ループ範囲を設定
    /// </summary>
    /// <param name="startTime">開始時間（秒）</param>
    /// <param name="endTime">終了時間（秒）</param>
    public void SetLoopRange(double startTime, double endTime)
    {
        loopStartTime = Mathf.Max(0, (float)startTime);
        loopEndTime = Mathf.Max((float)loopStartTime, (float)endTime);

        if (debugMode)
            Debug.Log($"{nameof(MonologueVideoController)}: ループ範囲を {loopStartTime}秒 - {loopEndTime}秒 に設定");
    }

    #endregion
}