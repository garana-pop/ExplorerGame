using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpeningScene;

namespace ConversationFatherAndDaughter
{
    /// <summary>
    /// キャラクターのハイライト表示を制御するクラス
    /// SpeakerNameDisplayと連携して、話者に応じてキャラクターの明度を調整する
    /// </summary>
    public class CharacterHighlightController : MonoBehaviour
    {
        [Header("基本設定")]
        [SerializeField] private float highlightTransitionSpeed = 2.0f;  // ハイライト遷移速度
        [SerializeField] private bool debugMode = false;                 // デバッグモード

        [Header("ハイライト設定")]
        [SerializeField] private Color activeCharacterColor = Color.white;            // アクティブなキャラクターの色
        [SerializeField] private Color inactiveCharacterColor = new Color(0.5f, 0.5f, 0.5f, 1.0f); // 非アクティブなキャラクターの色
        [SerializeField] private float inactiveBrightness = 0.5f;        // 非アクティブ時の明度(0.0-1.0)

        [Header("キャラクター設定")]
        [SerializeField] private GameObject leftCharacterObject;          // 左側のキャラクター（父親）
        [SerializeField] private GameObject rightCharacterObject;         // 右側のキャラクター（私）

        [Header("話者名と位置の対応")]
        [SerializeField]
        private CharacterMapping[] characterMappings = new CharacterMapping[]
        {
            new CharacterMapping { speakerNameJapanese = "父親", speakerNameEnglish = "Father", isLeftSide = true },
            new CharacterMapping { speakerNameJapanese = "私", speakerNameEnglish = "Daughter", isLeftSide = false }
        };

        // 内部変数
        private Image leftCharacterImage;
        private Image rightCharacterImage;
        private CanvasGroup leftCanvasGroup;
        private CanvasGroup rightCanvasGroup;
        private Coroutine leftHighlightCoroutine;
        private Coroutine rightHighlightCoroutine;
        private string currentActiveSpeaker = "";
        private string currentLanguageCode = "ja";

        /// <summary>
        /// キャラクターマッピング設定を定義するクラス
        /// </summary>
        [System.Serializable]
        public class CharacterMapping
        {
            [Tooltip("話者名（日本語）")]
            public string speakerNameJapanese;

            [Tooltip("話者名（英語）")]
            public string speakerNameEnglish;

            [Tooltip("左側に配置する場合はtrue、右側の場合はfalse")]
            public bool isLeftSide;

            /// <summary>
            /// 現在の言語設定に基づいて話者名を取得
            /// </summary>
            public string GetSpeakerName(string languageCode)
            {
                return languageCode == "en" ? speakerNameEnglish : speakerNameJapanese;
            }
        }

        private void Awake()
        {
            // キャラクターコンポーネントの初期化
            InitializeCharacterComponents();
        }

        private void Start()
        {
            // 言語設定の取得
            UpdateLanguageCode();

            // イベントリスナーの登録
            RegisterEventListeners();

            // 初期状態の設定（両キャラクターを暗くする）
            SetBothCharactersInactive();
        }

        private void OnDestroy()
        {
            // イベントリスナーの解除
            UnregisterEventListeners();
        }

        /// <summary>
        /// キャラクターコンポーネントを初期化
        /// </summary>
        private void InitializeCharacterComponents()
        {
            // 左側キャラクターのコンポーネント取得
            if (leftCharacterObject != null)
            {
                leftCharacterImage = leftCharacterObject.GetComponent<Image>();
                leftCanvasGroup = leftCharacterObject.GetComponent<CanvasGroup>();

                // CanvasGroupがない場合は追加
                if (leftCanvasGroup == null)
                {
                    leftCanvasGroup = leftCharacterObject.AddComponent<CanvasGroup>();
                }
            }
            else
            {
                DebugLogger.LogWarning($"{nameof(CharacterHighlightController)}: 左側キャラクターオブジェクトが設定されていません");
            }

            // 右側キャラクターのコンポーネント取得
            if (rightCharacterObject != null)
            {
                rightCharacterImage = rightCharacterObject.GetComponent<Image>();
                rightCanvasGroup = rightCharacterObject.GetComponent<CanvasGroup>();

                // CanvasGroupがない場合は追加
                if (rightCanvasGroup == null)
                {
                    rightCanvasGroup = rightCharacterObject.AddComponent<CanvasGroup>();
                }
            }
            else
            {
                DebugLogger.LogWarning($"{nameof(CharacterHighlightController)}: 右側キャラクターオブジェクトが設定されていません");
            }
        }

        /// <summary>
        /// イベントリスナーを登録
        /// </summary>
        private void RegisterEventListeners()
        {
            // ダイアログ表示イベントをリッスン
            DialogueEventNotifier.OnDialogueDisplayed += OnDialogueDisplayed;
        }

        /// <summary>
        /// イベントリスナーを解除
        /// </summary>
        private void UnregisterEventListeners()
        {
            // イベントリスナーの解除
            DialogueEventNotifier.OnDialogueDisplayed -= OnDialogueDisplayed;
        }

        /// <summary>
        /// ダイアログが表示された時のイベントハンドラ
        /// </summary>
        private void OnDialogueDisplayed(DialogueEntry entry)
        {
            // 話者名に応じてハイライトを更新
            UpdateCharacterHighlight(entry.speaker);
        }

        /// <summary>
        /// キャラクターのハイライトを更新
        /// </summary>
        private void UpdateCharacterHighlight(string speakerName)
        {
            // 話者名が空の場合は処理しない
            if (string.IsNullOrEmpty(speakerName))
            {
                SetBothCharactersInactive();
                return;
            }

            // 同じ話者の場合は処理をスキップ
            if (speakerName == currentActiveSpeaker)
            {
                return;
            }

            currentActiveSpeaker = speakerName;

            // 話者のマッピングを取得
            CharacterMapping mapping = GetCharacterMapping(speakerName);

            if (mapping != null)
            {
                // 対応するキャラクターをハイライト
                if (mapping.isLeftSide)
                {
                    HighlightLeftCharacter();
                }
                else
                {
                    HighlightRightCharacter();
                }

                if (debugMode)
                {
                    DebugLogger.Log($"{nameof(CharacterHighlightController)}: {speakerName} をハイライト表示");
                }
            }
            else
            {
                // マッピングが見つからない場合は両方暗くする
                SetBothCharactersInactive();

                if (debugMode)
                {
                    DebugLogger.LogWarning($"{nameof(CharacterHighlightController)}: 話者 '{speakerName}' のマッピングが見つかりません");
                }
            }
        }

        /// <summary>
        /// 左側のキャラクターをハイライト
        /// </summary>
        public void HighlightLeftCharacter()
        {
            // 左側を明るく、右側を暗く
            SetCharacterBrightness(leftCharacterImage, leftCanvasGroup, true, ref leftHighlightCoroutine);
            SetCharacterBrightness(rightCharacterImage, rightCanvasGroup, false, ref rightHighlightCoroutine);
        }

        /// <summary>
        /// 右側のキャラクターをハイライト
        /// </summary>
        public void HighlightRightCharacter()
        {
            // 右側を明るく、左側を暗く
            SetCharacterBrightness(rightCharacterImage, rightCanvasGroup, true, ref rightHighlightCoroutine);
            SetCharacterBrightness(leftCharacterImage, leftCanvasGroup, false, ref leftHighlightCoroutine);
        }

        /// <summary>
        /// 両方のキャラクターを非アクティブ状態にする
        /// </summary>
        public void SetBothCharactersInactive()
        {
            SetCharacterBrightness(leftCharacterImage, leftCanvasGroup, false, ref leftHighlightCoroutine);
            SetCharacterBrightness(rightCharacterImage, rightCanvasGroup, false, ref rightHighlightCoroutine);
            currentActiveSpeaker = "";
        }

        /// <summary>
        /// キャラクターの明度を設定
        /// </summary>
        private void SetCharacterBrightness(Image characterImage, CanvasGroup canvasGroup, bool isActive, ref Coroutine coroutine)
        {
            if (characterImage == null && canvasGroup == null)
            {
                return;
            }

            // 既存のコルーチンを停止
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }

            // 新しいコルーチンを開始
            coroutine = StartCoroutine(TransitionBrightness(characterImage, canvasGroup, isActive));
        }

        /// <summary>
        /// 明度を遷移させるコルーチン
        /// </summary>
        private IEnumerator TransitionBrightness(Image characterImage, CanvasGroup canvasGroup, bool isActive)
        {
            Color targetColor = isActive ? activeCharacterColor : inactiveCharacterColor;
            float targetAlpha = isActive ? 1.0f : inactiveBrightness;

            // 現在の値を取得
            Color currentColor = characterImage != null ? characterImage.color : Color.white;
            float currentAlpha = canvasGroup != null ? canvasGroup.alpha : 1.0f;

            float elapsedTime = 0f;
            float duration = 1.0f / highlightTransitionSpeed;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;

                // Imageの色を遷移
                if (characterImage != null)
                {
                    characterImage.color = Color.Lerp(currentColor, targetColor, t);
                }

                // CanvasGroupのアルファ値を遷移
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.Lerp(currentAlpha, targetAlpha, t);
                }

                yield return null;
            }

            // 最終値を設定
            if (characterImage != null)
            {
                characterImage.color = targetColor;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = targetAlpha;
            }
        }

        /// <summary>
        /// 話者名に対応するマッピングを取得
        /// </summary>
        private CharacterMapping GetCharacterMapping(string speakerName)
        {
            foreach (var mapping in characterMappings)
            {
                // 現在の言語に応じて比較
                string mappingName = mapping.GetSpeakerName(currentLanguageCode);
                if (mappingName == speakerName)
                {
                    return mapping;
                }
            }
            // 一致しなかった場合のデバッグ表示
            if (debugMode)
            {
                DebugLogger.LogWarning($"{nameof(CharacterHighlightController)}: マッピングが見つかりません speakerName={speakerName}, languageCode={currentLanguageCode}");
            }
            return null;
        }

        /// <summary>
        /// 現在の言語コードを更新
        /// </summary>
        private void UpdateLanguageCode()
        {
            // LocalizationManagerから言語設定を取得
            if (ExplorerGame.Localization.LocalizationManager.Instance != null)
            {
                currentLanguageCode = ExplorerGame.Localization.LocalizationManager.Instance.GetCurrentLanguageCode();
            }
            else
            {
                // デフォルトは日本語
                currentLanguageCode = "ja";
            }
        }

        /// <summary>
        /// 言語が変更された時に呼び出されるメソッド（必要に応じて外部から呼び出し）
        /// </summary>
        public void OnLanguageChanged(string newLanguageCode)
        {
            currentLanguageCode = newLanguageCode;

            if (debugMode)
            {
                DebugLogger.Log($"{nameof(CharacterHighlightController)}: 言語が {newLanguageCode} に変更されました");
            }
        }
    }
}