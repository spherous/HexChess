using UnityEngine;
using UnityEngine.UI;

public class SliderTicks : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private GameObject tickPrefab;
    [SerializeField] private Transform tickContainer;

    public void AddTicks()
    {
        for(int i = 0; i <= slider.maxValue; i++)
        {
            GameObject newTick = Instantiate(tickPrefab, tickContainer);
            RectTransform newTickRect = (RectTransform)newTick.transform;
            newTickRect.anchorMin = new Vector2((float)i/(float)slider.maxValue, 0);
            newTickRect.anchorMax = new Vector2((float)i/(float)slider.maxValue, 1);
        }
    }
}