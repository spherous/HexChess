using Extensions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ProposeTeamChangeButton : MonoBehaviour
{
    [SerializeField] private QueryTeamChangePanel teamChangePanel;
    [SerializeField] private Button button;
    [SerializeField] private Lobby lobby;
    Networker networker;
    private void Awake() {
        networker = GameObject.FindObjectOfType<Networker>();
        button.onClick.AddListener(() => {
            if(teamChangePanel.isOpen)
                return;
            // This is AI mode, the AI always approves team changes
            if(networker == null)
                lobby?.SwapAITeam();
            else if(!networker.isHost)
            {
                // This is a client
                ReadyStartObjectButton ready = GameObject.FindObjectOfType<ReadyStartObjectButton>();
                if(ready != null && ready.mode == ReadyStartObjectButton.Mode.Ready)
                    ready.ToggleReady(false, true);
            }

            EventSystem.current.Deselect();
            networker?.ProposeTeamChange();
        });
    }
}