using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// ѓNѓЉѓbѓN‚ЕѓVЃ[ѓ“‘J€Ъ‚·‚йѓRѓ“ѓ|Ѓ[ѓlѓ“ѓg
/// ѓCѓ“ѓXѓyѓNѓ^Ѓ[‚Е‘J€ЪђжѓVЃ[ѓ“‚рђЭ’и‰В”\
/// </summary>
public class SceneTransition : MonoBehaviour, IPointerClickHandler
{
    [Header("ѓVЃ[ѓ“‘J€ЪђЭ’и")]
    [Tooltip("‘J€Ъђж‚МѓVЃ[ѓ“–ј")]
    [SerializeField] private string targetSceneName;

    [Header("Њш‰К‰№ђЭ’и")]
    [Tooltip("ѓNѓЉѓbѓNЋћ‚ЙЊш‰К‰№‚рЌДђ¶‚·‚й‚©")]
    [SerializeField] private bool playSound = true;

    [Tooltip("ѓJѓXѓ^ѓЂЊш‰К‰№Ѓi–ўђЭ’иЋћ‚НѓfѓtѓHѓ‹ѓg‰№‚рЋg—pЃj")]
    [SerializeField] private AudioClip customClickSound;

    [Header("‘J€ЪЊш‰К")]
    [Tooltip("ѓtѓFЃ[ѓhЊш‰К‚рЋg—p‚·‚й‚©")]
    [SerializeField] private bool useFadeEffect = false;

    [Tooltip("ѓtѓFЃ[ѓhѓpѓlѓ‹ЃiЋg—pЋћ‚М‚ЭђЭ’иЃj")]
    [SerializeField] private CanvasGroup fadePanel;

    [Tooltip("ѓtѓFЃ[ѓhЋћЉФЃi•bЃj")]
    [SerializeField] private float fadeDuration = 0.5f;

    // ѓNѓЉѓbѓNЋћ‚МЏ€—ќ
    public void OnPointerClick(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("‘J€ЪђжѓVЃ[ѓ“–ј‚ЄђЭ’и‚і‚к‚Д‚ў‚Ь‚№‚сЃBѓCѓ“ѓXѓyѓNѓ^Ѓ[‚ЕђЭ’и‚µ‚Д‚­‚ѕ‚і‚ўЃB");
            return;
        }

        // ѓNѓЉѓbѓNЊш‰К‰№‚рЌДђ¶
        if (playSound)
        {
            PlayClickSound();
        }

        // ’јђЪѓVЃ[ѓ“‘J€Ъ
        TransitionToScene();
    }

    // Њш‰К‰№ЌДђ¶
    private void PlayClickSound()
    {
        // SoundEffectManager‚Є‚ ‚йЏкЌ‡‚Н‚»‚ї‚з‚р—Dђж
        if (SoundEffectManager.Instance != null)
        {
            // ѓJѓXѓ^ѓЂЊш‰К‰№‚Є‚ ‚к‚О‚»‚к‚рЌДђ¶ЃA‚И‚Ї‚к‚ОѓfѓtѓHѓ‹ѓg
            if (customClickSound != null)
            {
                SoundEffectManager.Instance.PlaySound(customClickSound);
            }
            else
            {
                SoundEffectManager.Instance.PlayClickSound();
            }
        }
        else if (customClickSound != null)
        {
            // SoundEffectManager‚Є‚И‚­ЃAѓJѓXѓ^ѓЂЊш‰К‰№‚Є‚ ‚йЏкЌ‡
            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
            audioSource.PlayOneShot(customClickSound);
        }
    }

    // ѓVЃ[ѓ“‘J€Ъ‚рЋАЌsЃiЊцЉJѓЃѓ\ѓbѓh‚ЕЉO•”‚©‚з‚аЊД‚СЏo‚µ‰В”\Ѓj
    public void TransitionToScene()
    {
        if (useFadeEffect && fadePanel != null)
        {
            // ѓtѓFЃ[ѓhЊш‰К‚ ‚и
            StartCoroutine(FadeAndLoadScene());
        }
        else
        {
            // ѓtѓFЃ[ѓhЊш‰К‚И‚µ
            LoadSceneDirectly();
        }
    }

    // ѓtѓFЃ[ѓhЊш‰К•t‚«ѓVЃ[ѓ“‘J€Ъ
    private System.Collections.IEnumerator FadeAndLoadScene()
    {
        // ѓtѓFЃ[ѓhѓpѓlѓ‹‚р—LЊш‰»
        fadePanel.gameObject.SetActive(true);
        fadePanel.alpha = 0f;

        // ѓtѓFЃ[ѓhѓCѓ“
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            fadePanel.alpha = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        fadePanel.alpha = 1f;

        // ѓVЃ[ѓ““З‚ЭЌћ‚Э
        LoadSceneDirectly();
    }

    // ’јђЪѓVЃ[ѓ“‘J€Ъ
    private void LoadSceneDirectly()
    {
        try
        {
            // ѓZЃ[ѓuѓfЃ[ѓ^‚М•Ы‘¶
            if (GameSaveManager.Instance != null)
            {
                GameSaveManager.Instance.SaveGame();
            }

            // ѓVЃ[ѓ“‘J€Ъ
            SceneManager.LoadScene(targetSceneName);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"ѓVЃ[ѓ“‘J€Ъ’†‚ЙѓGѓ‰Ѓ[‚Є”­ђ¶‚µ‚Ь‚µ‚Ѕ: {ex.Message}");
        }
    }

    // ѓGѓfѓBѓ^Љg’Ј—pЃFѓVЃ[ѓ“‚Є‘¶ЌЭ‚·‚й‚©Љm”F‚·‚йѓЃѓ\ѓbѓh
    public bool IsSceneValid()
    {
        if (string.IsNullOrEmpty(targetSceneName))
            return false;

        // ѓVЃ[ѓ“‚М‘¶ЌЭЉm”FѓЌѓWѓbѓN
        // ѓGѓfѓBѓ^Љg’Ј‚ЕSceneManager‚рЋg—p‚µ‚ДЊџЏШ
        return true;
    }
}