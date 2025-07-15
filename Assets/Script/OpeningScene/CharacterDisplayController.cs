using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OpeningScene;

/// <summary>
/// OpeningSceneでのキャラクター表示を制御するクラス
/// セリフに応じて対応するキャラクターをハイライト表示する
/// </summary>
public class CharacterDisplayController : MonoBehaviour
{
    [System.Serializable]
    public class CharacterSettings
    {
        public string characterName;        // キャラクター名
        public GameObject characterObject;  // キャラクターのGameObject
        public Image characterImage;        // キャラクターのイメージ
        public TextMeshProUGUI nameText;    // 名前表示用テキスト
        public Color highlightColor = Color.white;             // ハイライト時の色
        public Color normalColor = new Color(0.6f, 0.6f, 0.6f, 0.8f); // 非ハイライト時の色
    }

    [System.Serializable]
    public class NameDisplaySettings
    {
        [Header("テキスト設定")]
        public Color nameTextColor = Color.white;
        public float nameFontSize = 24f;

        [Header("背景設定")]
        public Color leftNameBgColor = new Color(0.2f, 0.2f, 0.6f, 0.8f);
        public Color rightNameBgColor = new Color(0.6f, 0.2f, 0.2f, 0.8f);

        [Header("レイアウト設定")]
        public Vector2 namePanelSize = new Vector2(200, 50);
        public Vector2 namePadding = new Vector2(10, 5);
    }

    [Header("キャラクター設定")]
    [SerializeField] private List<CharacterSettings> characters = new List<CharacterSettings>();

    [Header("名前表示設定")]
    [SerializeField] private NameDisplaySettings nameDisplaySettings = new NameDisplaySettings();

    [Header("デフォルト設定")]
    [SerializeField] private Color defaultHighlightColor = Color.white;
    [SerializeField] private Color defaultNormalColor = new Color(0.6f, 0.6f, 0.6f, 0.8f);
    [SerializeField] private float transitionSpeed = 3.0f; // 色遷移の速さ

    [Header("エフェクト設定")]
    [SerializeField] private bool useColorTransition = true; // 色の遷移エフェクトを使用するか
    [SerializeField] private bool enhanceNameDisplay = true; // 名前表示を強化するか

    [Header("デバッグ")]
    [SerializeField] private bool debugMode = false;

    // 現在ハイライトされているキャラクター
    private CharacterSettings currentHighlightedCharacter;
    private List<Coroutine> activeTransitions = new List<Coroutine>();
    private Dictionary<string, Image> nameBgImages = new Dictionary<string, Image>();

    private void Awake()
    {
        InitializeCharacters();

        // 名前表示の強化が有効な場合は初期化
        if (enhanceNameDisplay)
        {
            InitializeNameDisplays();
        }
    }

    private void Start()
    {
        // 開始時は全キャラクターを非ハイライト状態に
        ResetAllCharacters();
    }

    /// <summary>
    /// キャラクター設定の初期化
    /// </summary>
    private void InitializeCharacters()
    {
        foreach (var character in characters)
        {
            // 画像コンポーネントの取得確認
            if (character.characterObject != null && character.characterImage == null)
            {
                character.characterImage = character.characterObject.GetComponent<Image>();
                if (character.characterImage == null)
                {
                    Debug.LogWarning($"キャラクター {character.characterName} の Image コンポーネントが見つかりません");
                }
            }

            // 名前テキストコンポーネントの取得確認
            if (character.characterObject != null && character.nameText == null)
            {
                character.nameText = character.characterObject.GetComponentInChildren<TextMeshProUGUI>();
                if (character.nameText == null)
                {
                    Debug.LogWarning($"キャラクター {character.characterName} の TextMeshProUGUI コンポーネントが見つかりません");
                }
            }
        }
    }

    /// <summary>
    /// 名前表示の初期化
    /// </summary>
    private void InitializeNameDisplays()
    {
        foreach (var character in characters)
        {
            if (character.nameText != null)
            {
                // 名前エリアの取得
                Transform nameArea = character.nameText.transform.parent;
                if (nameArea == null) continue;

                // 背景イメージの設定
                Image nameBg = nameArea.GetComponent<Image>();
                if (nameBg == null)
                {
                    nameBg = nameArea.gameObject.AddComponent<Image>();

                    // 左右のキャラクターに応じた背景色を設定
                    if (character.characterName.Contains("父") ||
                        character.characterName.Contains("???"))
                    {
                        nameBg.color = nameDisplaySettings.leftNameBgColor;
                    }
                    else
                    {
                        nameBg.color = nameDisplaySettings.rightNameBgColor;
                    }

                    // 背景を一番後ろに
                    nameBg.transform.SetAsFirstSibling();
                }

                // 辞書に保存
                nameBgImages[character.characterName] = nameBg;

                // 名前テキストの設定
                ConfigureNameText(character.nameText);

                // 名前エリアのRectTransformを設定
                ConfigureNameAreaLayout(nameArea.GetComponent<RectTransform>());
            }
        }
    }

    /// <summary>
    /// 名前テキストの設定
    /// </summary>
    private void ConfigureNameText(TextMeshProUGUI nameText)
    {
        if (nameText == null) return;

        // フォント設定
        nameText.fontSize = nameDisplaySettings.nameFontSize;
        nameText.fontStyle = FontStyles.Bold;
        nameText.color = nameDisplaySettings.nameTextColor;

        // テキスト配置
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.overflowMode = TextOverflowModes.Overflow;
    }

    /// <summary>
    /// 名前エリアのレイアウト設定
    /// </summary>
    private void ConfigureNameAreaLayout(RectTransform nameAreaRect)
    {
        if (nameAreaRect == null) return;

        // サイズ設定
        nameAreaRect.sizeDelta = nameDisplaySettings.namePanelSize;
    }

    /// <summary>
    /// すべてのキャラクターを非ハイライト状態にリセット
    /// </summary>
    public void ResetAllCharacters()
    {
        StopAllTransitions();

        foreach (var character in characters)
        {
            if (character.characterImage != null)
            {
                if (useColorTransition)
                {
                    StartColorTransition(character.characterImage, character.normalColor, transitionSpeed);
                }
                else
                {
                    character.characterImage.color = character.normalColor;
                }
            }

            if (character.nameText != null)
            {
                character.nameText.text = "";

                // 名前背景を非表示
                if (enhanceNameDisplay && nameBgImages.ContainsKey(character.characterName))
                {
                    Image nameBg = nameBgImages[character.characterName];
                    if (nameBg != null)
                    {
                        Color bgColor = nameBg.color;
                        bgColor.a = 0f;
                        nameBg.color = bgColor;
                    }
                }
            }
        }

        currentHighlightedCharacter = null;
    }

    /// <summary>
    /// 話者名からキャラクターをハイライト表示
    /// </summary>
    public void HighlightCharacter(string speakerName)
    {
        if (string.IsNullOrEmpty(speakerName))
        {
            // 話者名が空の場合（ナレーションなど）は全キャラクター非ハイライト
            ResetAllCharacters();
            return;
        }

        // すべての遷移エフェクトを停止
        StopAllTransitions();

        CharacterSettings targetCharacter = null;

        // マッチするキャラクターを検索
        foreach (var character in characters)
        {
            if (character.characterName.Equals(speakerName, System.StringComparison.OrdinalIgnoreCase) ||
               (speakerName == "???" && character.characterName == "父親") ||  // "???"が左キャラクター(父親)に対応
               (speakerName == "記憶喪失者" && character.characterName == "男性")) // "記憶喪失者"が右キャラクター(男性)に対応
            {
                targetCharacter = character;
                break;
            }
        }

        // 対象キャラクターがない場合は処理しない
        if (targetCharacter == null)
        {
            if (debugMode)
            {
                Debug.LogWarning($"話者名 '{speakerName}' に一致するキャラクターが見つかりません");
            }
            return;
        }

        // 現在のハイライトキャラクターと同じなら処理しない
        if (currentHighlightedCharacter == targetCharacter)
        {
            return;
        }

        // 前回のハイライトキャラクターを非ハイライト状態に
        if (currentHighlightedCharacter != null)
        {
            if (currentHighlightedCharacter.characterImage != null)
            {
                if (useColorTransition)
                {
                    StartColorTransition(currentHighlightedCharacter.characterImage,
                                         currentHighlightedCharacter.normalColor,
                                         transitionSpeed);
                }
                else
                {
                    currentHighlightedCharacter.characterImage.color = currentHighlightedCharacter.normalColor;
                }
            }

            if (currentHighlightedCharacter.nameText != null)
            {
                // 名前表示を強化した非表示
                if (enhanceNameDisplay)
                {
                    HideEnhancedNameDisplay(currentHighlightedCharacter);
                }
                else
                {
                    currentHighlightedCharacter.nameText.text = "";
                }
            }
        }

        // 新しいキャラクターをハイライト状態に
        if (targetCharacter.characterImage != null)
        {
            if (useColorTransition)
            {
                StartColorTransition(targetCharacter.characterImage,
                                     targetCharacter.highlightColor,
                                     transitionSpeed);
            }
            else
            {
                targetCharacter.characterImage.color = targetCharacter.highlightColor;
            }
        }

        if (targetCharacter.nameText != null)
        {
            // 名前表示の強化が有効な場合
            if (enhanceNameDisplay)
            {
                ShowEnhancedNameDisplay(targetCharacter, speakerName);
            }
            else
            {
                targetCharacter.nameText.text = speakerName;
            }
        }

        currentHighlightedCharacter = targetCharacter;

        if (debugMode)
        {
            Debug.Log($"キャラクターをハイライト: {speakerName}");
        }
    }

    /// <summary>
    /// 強化された名前表示を表示
    /// </summary>
    private void ShowEnhancedNameDisplay(CharacterSettings character, string speakerName)
    {
        if (character.nameText == null) return;

        // 名前テキストを設定
        character.nameText.text = speakerName;

        // 背景を表示
        if (nameBgImages.ContainsKey(character.characterName))
        {
            Image nameBg = nameBgImages[character.characterName];
            if (nameBg != null)
            {
                Color bgColor = nameBg.color;
                // 左右のキャラクターに応じた背景色を設定
                if (character.characterName.Contains("父") ||
                    character.characterName.Contains("???"))
                {
                    nameBg.color = new Color(
                        nameDisplaySettings.leftNameBgColor.r,
                        nameDisplaySettings.leftNameBgColor.g,
                        nameDisplaySettings.leftNameBgColor.b,
                        0.8f);
                }
                else
                {
                    nameBg.color = new Color(
                        nameDisplaySettings.rightNameBgColor.r,
                        nameDisplaySettings.rightNameBgColor.g,
                        nameDisplaySettings.rightNameBgColor.b,
                        0.8f);
                }
            }
        }
    }

    /// <summary>
    /// 強化された名前表示を非表示
    /// </summary>
    private void HideEnhancedNameDisplay(CharacterSettings character)
    {
        if (character.nameText == null) return;

        // 名前テキストをクリア
        character.nameText.text = "";

        // 背景を非表示
        if (nameBgImages.ContainsKey(character.characterName))
        {
            Image nameBg = nameBgImages[character.characterName];
            if (nameBg != null)
            {
                Color bgColor = nameBg.color;
                bgColor.a = 0f;
                nameBg.color = bgColor;
            }
        }
    }

    /// <summary>
    /// 色の遷移エフェクトを開始
    /// </summary>
    private void StartColorTransition(Image image, Color targetColor, float speed)
    {
        if (image == null) return;

        Coroutine transition = StartCoroutine(ColorTransition(image, targetColor, speed));
        activeTransitions.Add(transition);
    }

    /// <summary>
    /// すべての遷移エフェクトを停止
    /// </summary>
    private void StopAllTransitions()
    {
        foreach (var transition in activeTransitions)
        {
            if (transition != null)
            {
                StopCoroutine(transition);
            }
        }

        activeTransitions.Clear();
    }

    /// <summary>
    /// 色の遷移コルーチン
    /// </summary>
    private IEnumerator ColorTransition(Image image, Color targetColor, float speed)
    {
        Color startColor = image.color;
        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * speed;
            image.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        image.color = targetColor;
    }

    /// <summary>
    /// 話者名に対応するキャラクターが設定されているか確認
    /// </summary>
    public bool HasCharacterForSpeaker(string speakerName)
    {
        if (string.IsNullOrEmpty(speakerName)) return false;

        foreach (var character in characters)
        {
            if (character.characterName.Equals(speakerName, System.StringComparison.OrdinalIgnoreCase) ||
                (speakerName.Contains("???") && character.characterName.Contains("???")))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// キャラクターを名前で追加
    /// </summary>
    public void AddCharacter(string characterName, GameObject characterObject, Image characterImage = null, TextMeshProUGUI nameText = null)
    {
        if (string.IsNullOrEmpty(characterName) || characterObject == null)
        {
            Debug.LogError("キャラクター名とゲームオブジェクトは必須です");
            return;
        }

        // 既に同じ名前のキャラクターが存在するか確認
        foreach (var character in characters)
        {
            if (character.characterName.Equals(characterName, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"同じ名前のキャラクター '{characterName}' は既に登録されています");
                return;
            }
        }

        // Imageコンポーネントが指定されていなければ自動取得
        if (characterImage == null)
        {
            characterImage = characterObject.GetComponent<Image>();
        }

        // TextMeshProコンポーネントが指定されていなければ自動取得
        if (nameText == null)
        {
            nameText = characterObject.GetComponentInChildren<TextMeshProUGUI>();
        }

        // キャラクター設定を追加
        CharacterSettings newCharacter = new CharacterSettings
        {
            characterName = characterName,
            characterObject = characterObject,
            characterImage = characterImage,
            nameText = nameText,
            highlightColor = defaultHighlightColor,
            normalColor = defaultNormalColor
        };

        characters.Add(newCharacter);

        // 名前表示強化が有効なら設定
        if (enhanceNameDisplay && nameText != null)
        {
            Transform nameArea = nameText.transform.parent;
            if (nameArea != null)
            {
                Image nameBg = nameArea.GetComponent<Image>();
                if (nameBg == null)
                {
                    nameBg = nameArea.gameObject.AddComponent<Image>();
                    // 左右で異なる色を設定
                    if (characterName.Contains("父") || characterName.Contains("???"))
                    {
                        nameBg.color = nameDisplaySettings.leftNameBgColor;
                    }
                    else
                    {
                        nameBg.color = nameDisplaySettings.rightNameBgColor;
                    }
                    nameBg.transform.SetAsFirstSibling();
                }

                // 非表示状態で初期化
                Color bgColor = nameBg.color;
                bgColor.a = 0f;
                nameBg.color = bgColor;

                // 辞書に追加
                nameBgImages[characterName] = nameBg;

                // テキスト設定
                ConfigureNameText(nameText);
            }
        }

        //if (debugMode)
        //{
        //    Debug.Log($"キャラクターを追加: {characterName}");
        //}
    }

    private void OnDestroy()
    {
        StopAllTransitions();
    }
}