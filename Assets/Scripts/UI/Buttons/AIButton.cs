using UnityEngine;

public class AIButton : TwigglyButton
{
    [SerializeField] private Lobby lobby;
    [SerializeField] private ModeText modeText;
    [SerializeField] private Networker networker;

    private new void Awake() {
        base.Awake();
        base.onClick += Clicked;
    }

    public void Clicked()
    {
        if(networker != null)
            Destroy(networker.gameObject);
            
        modeText?.Show("Computer AI");
        lobby?.Show(Lobby.Type.AI);
    }
}