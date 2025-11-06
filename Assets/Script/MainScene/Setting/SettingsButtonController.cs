using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 設定ボタンのクリックを検知して、設定画面を表示するスクリプト
/// ボタンにアタッチして使用します
/// </summary>
public class SettingsButtonController : MonoBehaviour
{
    [Header("参照設定")]
    [Tooltip("MainSceneSettingsManagerへの参照")]
    [SerializeField] private MainSceneSettingsManager settingsManager;

    // ボタンコンポーネント
    private Button button;

    private void Awake()
    {
        // ボタンコンポーネントを取得
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError("SettingsButtonControllerはButtonコンポーネントがアタッチされているGameObjectに追加してください。");
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        // settingsManagerが設定されていない場合は自動検索
        if (settingsManager == null)
        {
            settingsManager = FindAnyObjectByType<MainSceneSettingsManager>();
            if (settingsManager == null)
            {
                Debug.LogError("MainSceneSettingsManagerが見つかりません。シーン内にMainSceneSettingsManagerを追加してください。");
                return;
            }
        }

        // ボタンクリックイベントを設定
        button.onClick.AddListener(OnSettingsButtonClicked);
    }

    /// <summary>
    /// 設定ボタンがクリックされたときの処理
    /// </summary>
    private void OnSettingsButtonClicked()
    {
        // 設定マネージャーを通して設定画面を表示
        if (settingsManager != null)
        {
            settingsManager.ToggleSettings();
        }
    }

    private void OnDestroy()
    {
        // イベントリスナーの登録解除
        if (button != null)
        {
            button.onClick.RemoveListener(OnSettingsButtonClicked);
        }
    }
}