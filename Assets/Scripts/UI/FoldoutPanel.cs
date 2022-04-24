using UnityEngine;
using UnityEngine.EventSystems;

public class FoldoutPanel : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private RectTransform arrow;
    public bool isOpen => panel != null && panel.sizeDelta.y > collapsedHeight;
    public bool startOpen = false;
    private TriSign changing = TriSign.Zero;
    public float collapsedHeight = 35f;
    public float openHeight = 350f;
    public float foldoutTime = 0.15f;
    private float? ellapsed = 0f;
    private void Awake()
    {
        if((isOpen && !startOpen) || (!isOpen && startOpen))
            InstantToggle();
    }

    private void Update()
    {
        if(changing == TriSign.Zero || !ellapsed.HasValue)
            return;
        
        ellapsed = Mathf.Clamp(ellapsed.Value + Time.deltaTime, 0f, foldoutTime);
        float goal = changing == TriSign.Positive ? openHeight : collapsedHeight;
        float newY = Mathf.Lerp(panel.sizeDelta.y, goal, ellapsed.Value / foldoutTime);
        panel.sizeDelta = new Vector2(panel.sizeDelta.x, newY);
        if(changing != TriSign.Zero && Mathf.Abs(panel.sizeDelta.y - goal) < .5f)
        {
            changing = TriSign.Zero;
            ellapsed = null;
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, goal);
        }
    }

    public void InstantToggle()
    {
        if(!isOpen)
        {
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, openHeight);
            arrow.localScale = new Vector3(1, arrow.localScale.y * -1, 1);
        }
        else
        {
            panel.sizeDelta = new Vector2(panel.sizeDelta.x, collapsedHeight);
            arrow.localScale = new Vector3(1, arrow.localScale.y * -1, 1);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        arrow.localScale = new Vector3(1, arrow.localScale.y * -1, 1);
        changing = isOpen ? TriSign.Negative : TriSign.Positive;
        ellapsed = 0f;
    }
}