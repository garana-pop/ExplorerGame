using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ClickSound : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        SoundEffectManager.Instance.PlayClickSound();
    }

}
