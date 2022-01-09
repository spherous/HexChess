using UnityEngine;
using UnityEngine.EventSystems;

public class HistoryTeamPanel : MonoBehaviour, IPointerDownHandler
{
    [SerializeField] private MovePanel movePanel;
    public Team team;

    public void OnPointerDown(PointerEventData eventData) => movePanel.SetHistory(team);
}