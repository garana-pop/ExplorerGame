using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using OpeningScene;

namespace ConversationFatherAndDaughter
{
    /// <summary>
    /// DialogueDataLoaderから読み込んだセリフデータの話者名を表示するクラス
    /// </summary>
    public class SpeakerNameDisplay : MonoBehaviour
    {
        [Header("基本設定")]
        [SerializeField] private DialogueDataLoader dialogueDataLoader; // DialogueDataLoaderへの参照
        [SerializeField] private float displayDelay = 0.1f;             // 話者名表示の遅延時間

        [Header("話者名表示用UI")]
        [SerializeField] private TextMeshProUGUI leftSpeakerNameText;   // 左側の話者名表示
        [SerializeField] private TextMeshProUGUI rightSpeakerNameText;  // 右側の話者名表示

        [Header("話者の配置設定")]
        [SerializeField]
        private SpeakerPlacement[] speakerPlacements = new SpeakerPlacement[]
        {
            new SpeakerPlacement { speakerNameJapanese = "父親", speakerNameEnglish = "Father", isLeftSide = true },
            new SpeakerPlacement { speakerNameJapanese = "私", speakerNameEnglish = "Me", isLeftSide = false }
        };

        [Header("表示エフェクト設定")]
        [SerializeField] private bool useFadeEffect = true;             // フェード効果を使用するか
        [SerializeField] private float fadeSpeed = 2.0f;                // フェード速度
        [SerializeField] private Color activeNameColor = Color.white;   // アクティブな話者名の色
        [SerializeField] private Color inactiveNameColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 非アクティブな話者名の色

        // 内部変数
        private List<DialogueEntry> currentDialogueEntries;
        private int currentDialogueIndex = -1;
        private Coroutine fadeCoroutine;
        private string previousSpeaker = "";

        /// <summary>
        /// 話者の配置設定を定義する内部クラス
        /// </summary>
        [System.Serializable]
        public class SpeakerPlacement
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
            // DialogueDataLoaderが設定されていない場合は自動取得
            if (dialogueDataLoader == null)
            {
                dialogueDataLoader = FindObjectOfType<DialogueDataLoader>();
                if (dialogueDataLoader == null)
                {
                    Debug.LogError($"{nameof(SpeakerNameDisplay)}: DialogueDataLoaderが見つかりません");
                }
            }

            // UI要素の自動取得
            FindUIElements();
        }

        private void OnEnable()
        {
            // イベントリスナーの登録
            DialogueEventNotifier.OnDialogueDisplayed += OnDialogueDisplayed;
        }

        private void OnDisable()
        {
            // イベントリスナーの解除
            DialogueEventNotifier.OnDialogueDisplayed -= OnDialogueDisplayed;

            // コルーチンの停止
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
        }

        private void Start()
        {
            // セリフデータを読み込む
            LoadDialogueData();

            Debug.Log($"{nameof(SpeakerNameDisplay)}: 初期化完了");
        }

        /// <summary>
        /// UI要素を自動的に検索して取得
        /// </summary>
        private void FindUIElements()
        {
            // 左側の話者名テキストを取得
            if (leftSpeakerNameText == null)
            {
                GameObject leftCharacter = GameObject.Find("LeftCharacter");
                if (leftCharacter != null)
                {
                    Transform nameArea = leftCharacter.transform.Find("LeftNameArea");
                    if (nameArea != null)
                    {
                        leftSpeakerNameText = nameArea.GetComponentInChildren<TextMeshProUGUI>();
                    }
                }

                Debug.Log("SpeakerNameDisplay:左側の話者名" + leftSpeakerNameText);

                if (leftSpeakerNameText == null)
                {
                    Debug.LogWarning($"{nameof(SpeakerNameDisplay)}: 左側の話者名テキストが見つかりません");
                }
            }

            // 右側の話者名テキストを取得
            if (rightSpeakerNameText == null)
            {
                GameObject rightCharacter = GameObject.Find("RightCharacter");
                if (rightCharacter != null)
                {
                    Transform nameArea = rightCharacter.transform.Find("RightNameArea");
                    if (nameArea != null)
                    {
                        rightSpeakerNameText = nameArea.GetComponentInChildren<TextMeshProUGUI>();
                    }
                }

                Debug.Log("SpeakerNameDisplay:右側の話者名" + rightSpeakerNameText);

                if (rightSpeakerNameText == null)
                {
                    Debug.LogWarning($"{nameof(SpeakerNameDisplay)}: 右側の話者名テキストが見つかりません");
                }
            }
        }

        /// <summary>
        /// DialogueDataLoaderからセリフデータを読み込む
        /// </summary>
        private void LoadDialogueData()
        {
            if (dialogueDataLoader != null)
            {
                currentDialogueEntries = dialogueDataLoader.GetDialogueEntries();
                if (currentDialogueEntries == null || currentDialogueEntries.Count == 0)
                {
                    Debug.LogWarning($"{nameof(SpeakerNameDisplay)}: セリフデータが読み込まれていません");
                }
                else
                {
                    Debug.Log($"{nameof(SpeakerNameDisplay)}: {currentDialogueEntries.Count}件のセリフデータを読み込みました");
                }
            }
        }

        /// <summary>
        /// ダイアログ表示時のイベントハンドラ
        /// </summary>
        private void OnDialogueDisplayed(DialogueEntry entry)
        {
            if (entry == null) return;

            // コマンド行の場合はスキップ
            if (entry.isCommand) return;

            // ナレーションの場合は話者名をクリア
            if (entry.type == DialogueType.Narration)
            {
                ClearBothSpeakerNames();
                return;
            }

            // 話者名を表示
            DisplaySpeakerName(entry.speaker);

            Debug.Log($"{nameof(SpeakerNameDisplay)}: 話者名を表示 - {entry.speaker}");
        }

        /// <summary>
        /// 話者名を適切な位置に表示
        /// </summary>
        private void DisplaySpeakerName(string speakerName)
        {
            if (string.IsNullOrEmpty(speakerName))
            {
                ClearBothSpeakerNames();
                return;
            }

            // 話者が変更された場合のみ処理
            if (speakerName == previousSpeaker) return;
            previousSpeaker = speakerName;

            // 現在の言語コードを取得
            string languageCode = GetCurrentLanguageCode();

            // 話者の配置を確認
            SpeakerPlacement placement = GetSpeakerPlacement(speakerName, languageCode);

            if (placement != null)
            {
                // フェードコルーチンを停止
                if (fadeCoroutine != null)
                {
                    StopCoroutine(fadeCoroutine);
                }

                if (useFadeEffect)
                {
                    // フェード効果付きで表示
                    fadeCoroutine = StartCoroutine(FadeSpeakerName(speakerName, placement.isLeftSide));
                }
                else
                {
                    // 即座に表示
                    SetSpeakerNameImmediate(speakerName, placement.isLeftSide);
                }
            }
            else
            {
                Debug.LogWarning($"{nameof(SpeakerNameDisplay)}: 話者 '{speakerName}' の配置設定が見つかりません");
                ClearBothSpeakerNames();
            }
        }

        /// <summary>
        /// 話者の配置設定を取得
        /// </summary>
        private SpeakerPlacement GetSpeakerPlacement(string speakerName, string languageCode)
        {
            foreach (var placement in speakerPlacements)
            {
                string configuredName = placement.GetSpeakerName(languageCode);
                if (configuredName == speakerName)
                {
                    return placement;
                }
            }
            return null;
        }

        /// <summary>
        /// 話者名を即座に設定
        /// </summary>
        private void SetSpeakerNameImmediate(string speakerName, bool isLeftSide)
        {
            if (isLeftSide)
            {
                // 左側に話者名を表示
                if (leftSpeakerNameText != null)
                {
                    leftSpeakerNameText.text = speakerName;
                    leftSpeakerNameText.color = activeNameColor;
                }

                // 右側をクリア
                if (rightSpeakerNameText != null)
                {
                    rightSpeakerNameText.text = "";
                    rightSpeakerNameText.color = inactiveNameColor;
                }
            }
            else
            {
                // 右側に話者名を表示
                if (rightSpeakerNameText != null)
                {
                    rightSpeakerNameText.text = speakerName;
                    rightSpeakerNameText.color = activeNameColor;
                }

                // 左側をクリア
                if (leftSpeakerNameText != null)
                {
                    leftSpeakerNameText.text = "";
                    leftSpeakerNameText.color = inactiveNameColor;
                }
            }
        }

        /// <summary>
        /// 話者名をフェード効果付きで表示
        /// </summary>
        private IEnumerator FadeSpeakerName(string speakerName, bool isLeftSide)
        {
            yield return new WaitForSeconds(displayDelay);

            float elapsedTime = 0f;
            float fadeInDuration = 1f / fadeSpeed;

            TextMeshProUGUI activeText = isLeftSide ? leftSpeakerNameText : rightSpeakerNameText;
            TextMeshProUGUI inactiveText = isLeftSide ? rightSpeakerNameText : leftSpeakerNameText;

            // アクティブ側のテキストを設定
            if (activeText != null)
            {
                activeText.text = speakerName;
                Color startColor = activeText.color;
                startColor.a = 0f;
                activeText.color = startColor;
            }

            // フェードアニメーション
            while (elapsedTime < fadeInDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Clamp01(elapsedTime / fadeInDuration);

                // アクティブ側をフェードイン
                if (activeText != null)
                {
                    Color color = activeNameColor;
                    color.a = alpha;
                    activeText.color = color;
                }

                // 非アクティブ側をフェードアウト
                if (inactiveText != null)
                {
                    Color color = inactiveNameColor;
                    color.a = 1f - alpha;
                    inactiveText.color = color;
                }

                yield return null;
            }

            // 最終的な色を設定
            if (activeText != null)
            {
                activeText.color = activeNameColor;
            }

            if (inactiveText != null)
            {
                inactiveText.text = "";
                inactiveText.color = inactiveNameColor;
            }
        }

        /// <summary>
        /// 両方の話者名をクリア
        /// </summary>
        private void ClearBothSpeakerNames()
        {
            if (leftSpeakerNameText != null)
            {
                leftSpeakerNameText.text = "";
                leftSpeakerNameText.color = inactiveNameColor;
            }

            if (rightSpeakerNameText != null)
            {
                rightSpeakerNameText.text = "";
                rightSpeakerNameText.color = inactiveNameColor;
            }

            previousSpeaker = "";
        }

        /// <summary>
        /// 現在の言語コードを取得
        /// </summary>
        private string GetCurrentLanguageCode()
        {
            // LocalizationManagerが存在する場合
            var localizationManager = ExplorerGame.Localization.LocalizationManager.Instance;
            if (localizationManager != null)
            {
                return localizationManager.GetCurrentLanguageCode();
            }

            // デフォルトは日本語
            return "ja";
        }

        /// <summary>
        /// セリフデータを再読み込み（言語切り替え時など）
        /// </summary>
        public void ReloadDialogueData()
        {
            LoadDialogueData();
            ClearBothSpeakerNames();
        }

        /// <summary>
        /// 次のセリフの話者名を手動で表示（テスト用）
        /// </summary>
        public void ShowNextSpeakerName()
        {
            if (currentDialogueEntries == null || currentDialogueEntries.Count == 0) return;

            currentDialogueIndex++;
            if (currentDialogueIndex >= currentDialogueEntries.Count)
            {
                currentDialogueIndex = 0;
            }

            DialogueEntry entry = currentDialogueEntries[currentDialogueIndex];
            OnDialogueDisplayed(entry);
        }
    }
}