using UnityEngine;
using UnityEngine.UI;

public class QueryTeamChangePanel : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Button approveButton;
    [SerializeField] private Button denyButton;
    Networker networker;
    public bool isOpen {get; private set;} = false;

    private void Awake() {
        Close();
        networker = GameObject.FindObjectOfType<Networker>();

        approveButton.onClick.AddListener(() => {
            networker?.RespondToTeamChange(MessageType.ApproveTeamChange);
            Close();    
        });
        denyButton.onClick.AddListener(() => {
            networker?.RespondToTeamChange(MessageType.DenyTeamChange);
            Close();    
        });
    }

    public void Close()
    {
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        isOpen = false;
    }

    public void Query()
    {
        isOpen = true;
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;
    }
}