using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Extensions;
using System;
using UnityEngine.SceneManagement;

public class Lobby : MonoBehaviour
{
    public enum Type {None = 0, Host = 1, Client = 2, AI = 3}
    [SerializeField] private GroupFader fader;
    [SerializeField] private GroupFader connectionChoiceFader;
    [SerializeField] private GroupFader whiteLocalIconFader;
    [SerializeField] private GroupFader blackLocalIconFader;
    [SerializeField] private GroupFader opponentSearchingPanel;
    [SerializeField] private GroupFader aiSettingsFader;
    [SerializeField] private TextMeshProUGUI opponentSearchingText;
    [SerializeField] public IPPanel ipPanel;
    [SerializeField] public ReadyButton readyToggle;
    [SerializeField] public StartMatchButton startButton;
    [SerializeField] private TextMeshProUGUI readyButtonContextText;
    [SerializeField] private TextMeshProUGUI proposeTeamChangeText;
    [SerializeField] private TextMeshProUGUI youPlayTeamText;

    [SerializeField] private GroupFader opponentLoadingFader;
    [SerializeField] private GroupFader opponentNameFader;
    [SerializeField] private GroupFader blackOpponentIconFader;
    [SerializeField] private GroupFader whiteOpponentIconFader;
    [SerializeField] private GroupFader readyFader;
    [SerializeField] private GroupFader aiWarningText;

    [SerializeField] private TextMeshProUGUI opponentName;

    public Toggle clockToggle;
    [SerializeField] private GameObject clockObj;
    [SerializeField] private TextMeshProUGUI clockText;

    public Toggle timerToggle;
    [SerializeField] private GameObject timerObj;
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private TMP_InputField timerInputField;

    [SerializeField] private QueryTeamChangePanel teamChangePanel;
    [SerializeField] private HandicapOverlayToggle handicapOverlayToggle;

    [SerializeField] private AIDifficultySlider aiDifficultySlider;
    [SerializeField] private SliderTicks sliderTicks;

    private bool opponentSearching = true;

    public Type lobbyType {get; private set;} = Type.None;

    private Team AITeam = Team.None;

    private void Awake()
    {
        timerInputField.gameObject.SetActive(false);

        ListenForClock();
        ListenForTimer();
    }

    private void ListenForClock()
    {
        clockToggle.onValueChanged.AddListener((isOn) =>
        {
            if(isOn)
            {
                timerToggle.isOn = false;
                clockText.text = "Toggle Clock (On)";
            }
            else
                clockText.text = "Toggle Clock (Off)";

            EventSystem.current.Deselect();
        });
    }

    private void ListenForTimer()
    {
        timerToggle.onValueChanged.AddListener((isOn) =>
        {
            if(isOn)
            {
                clockToggle.isOn = false;
                timerText.rectTransform.sizeDelta = new Vector2(150, timerText.rectTransform.sizeDelta.y);
                timerText.text = "Timer (mins)";
                timerInputField.gameObject.SetActive(true);
                timerInputField.text = "20";
            }
            else
            {
                timerText.rectTransform.sizeDelta = new Vector2(223, timerText.rectTransform.sizeDelta.y);
                timerText.text = "Toggle Timer (off)";
                timerInputField.gameObject.SetActive(false);
            }

            EventSystem.current.Deselect();
        });
    }

    public void Show(Type lobbyType)
    {
        this.lobbyType = lobbyType;

        timerObj.SetActive(lobbyType == Type.Host || lobbyType == Type.AI);
        clockObj.SetActive(lobbyType == Type.Host || lobbyType == Type.AI);

        Action lobbyAction = lobbyType switch {
            Type.Host => OpponentSearching,
            Type.Client => () => {
                opponentSearching = true;
                SetSearchingText();
            },
            Type.AI => () => {
                SetAIPanel();
                startButton.ShowEnabledButton();
            },
            _ => null
        };
        lobbyAction?.Invoke();

        connectionChoiceFader?.FadeOut();
        fader.FadeIn();
    }

    public void Hide()
    {
        lobbyType = Type.None;
        connectionChoiceFader?.FadeIn();
        fader.FadeOut();
    } 

    public float GetTimeInSeconds() => int.Parse(timerInputField.text) * 60;

    public void RemovePlayer(Player player)
    {
        if(!player.isHost)
            opponentSearchingPanel.FadeIn();

        opponentSearching = true;

        SetSearchingText();
        opponentLoadingFader.FadeIn();

        opponentNameFader.FadeOut();
        blackOpponentIconFader.FadeOut();

        opponentName.text = "";
    }

    public void DisconnectRecieved()
    {
        if(teamChangePanel != null && teamChangePanel.isOpen)
            teamChangePanel.Close();

        opponentSearching = true;

        readyButtonContextText.text = "";
        ResetToSearchingPanel();

        if(lobbyType == Type.Host)
            startButton.HideButton();
        else if(lobbyType == Type.Client)
        {
            readyToggle?.Hide();
            readyToggle?.gameObject.SetActive(false);
        }
    }

    public void LoadAIGame()
    {
        SceneManager.activeSceneChanged += StartAIGame;
        SceneTransition transition = GameObject.FindObjectOfType<SceneTransition>();
        if(transition != null)
            transition.Transition("SandboxMode");
        else
            SceneManager.LoadScene("SandboxMode");
    }

    public void StartAIGame(Scene arg0, Scene arg1)
    {
        AIBattleController aiController = GameObject.FindObjectOfType<AIBattleController>();
        
        // 0 is None, meaning the player can play that team
        if(AITeam == Team.White)
            aiController.SetAI(aiDifficultySlider.AILevel, 0);
        else if(AITeam == Team.Black)
            aiController.SetAI(0, aiDifficultySlider.AILevel);
        else // If AITeam somehow comes in as Team.None, there is nothing to do here.
            return;

        aiController.StartGame();

        SceneManager.activeSceneChanged -= StartAIGame;
    }

    public void UpdatePlayerName(Player player) => opponentName.text = player.name;

    public void UpdateTeam(Player player)
    {
        // local
        if((player.isHost && lobbyType == Type.Host) || (!player.isHost && lobbyType == Type.Client))
            SetLocalTeam(player.team == Team.White);
        // opponent
        else
            SetLocalTeam(player.team == Team.Black);
    }

    public void SetLocalTeam(bool isWhite)
    {
        string team = isWhite ? "White" : "black";
        youPlayTeamText.text = $"Team: {team}";
        
        if(isWhite)
        {
            whiteLocalIconFader.FadeIn();
            blackLocalIconFader.FadeOut();

            whiteOpponentIconFader.FadeOut();
            blackOpponentIconFader.FadeIn();
        }
        else
        {
            whiteLocalIconFader.FadeOut();
            blackLocalIconFader.FadeIn();

            whiteOpponentIconFader.FadeIn();
            blackOpponentIconFader.FadeOut();
        }
    }

    public void SetIP(string ip) => ipPanel.SetIP(ip);

    public void OpponentSearching()
    {
        opponentSearching = true;
        readyToggle.Hide();
        readyToggle.gameObject.SetActive(false);
        readyButtonContextText.text = "";

        if(readyFader.visible)
            readyFader.FadeOut();
        
        ResetToSearchingPanel();

        Debug.Log("Opponent Searching...");
    }
    public void OpponentFound(Player host)
    {
        opponentSearching = false;
        if(lobbyType == Type.Client)
        {
            if(!readyToggle.gameObject.activeSelf)
                readyToggle.gameObject.SetActive(true);
            readyToggle.Show();
        }
        else
            startButton.ShowDisabledButton();

        readyButtonContextText.text = lobbyType == Type.Host ? "Waiting for opponent to ready up" : "";

        if(!opponentSearchingPanel.visible)
            opponentSearchingPanel.FadeIn();
        
        if(readyFader.visible)
            readyFader.FadeOut();

        opponentSearchingText.text = "Opponent Found!";
        opponentLoadingFader.FadeOut();

        opponentNameFader.FadeIn();
        blackOpponentIconFader.FadeIn();

        UpdateTeam(host);

        Debug.Log("Opponent Found!");
    }

    public void ReadyRecieved()
    {
        Debug.Log("Ready Recieved");
        if(lobbyType == Type.Host)
            startButton.ShowEnabledButton();
        readyButtonContextText.text = "";
        if(!readyFader.visible)
            readyFader.FadeIn();
    }
    public void UnreadyRecieved()
    {
        Debug.Log("Unready Recieved");
        if(lobbyType == Type.Host)
            startButton.ShowDisabledButton();
        readyButtonContextText.text = opponentSearching ? "" : "Waiting for opponent to ready up";
        if(readyFader.visible)
            readyFader.FadeOut();
    }

    private void ResetToSearchingPanel()
    {
        // This is going to be called when the networker recieves a disconnect or is destroyed.
        // We destroy the networker when loading an AI lobby.
        // AI is never searching for an opponent, just leave.
        if(lobbyType == Type.AI)
            return;

        if(!opponentSearchingPanel.visible)
            opponentSearchingPanel.FadeIn();
            
        SetSearchingText();

        if(!opponentLoadingFader.visible)
            opponentLoadingFader.FadeIn();

        if(opponentNameFader.visible)
            opponentNameFader.FadeOut();
        if(blackOpponentIconFader.visible)
            blackOpponentIconFader.FadeOut();
        if(whiteOpponentIconFader.visible)
            whiteOpponentIconFader.FadeOut();

        if(readyFader.visible)
            readyFader.FadeOut();
    }

    private void SetAIPanel()
    {
        
        SetSearchingText();

        if(!opponentSearchingPanel.visible)
            opponentSearchingPanel.FadeIn();
        if(!aiSettingsFader.visible)
            aiSettingsFader.FadeIn();
        if(!blackOpponentIconFader.visible)
            blackOpponentIconFader.FadeIn();
        
        sliderTicks.AddTicks();

        aiDifficultySlider.difficultySlider.onValueChanged.AddListener(newVal => {
            if(newVal > 4 && aiWarningText.group != null && aiWarningText.group.alpha < 1 && aiWarningText.sign != TriSign.Positive)
                aiWarningText.FadeIn();
            else if(newVal <= 4 && aiWarningText.group != null && aiWarningText.group.alpha > 0 && aiWarningText.sign != TriSign.Negative)
                aiWarningText.FadeOut();
        });

        ipPanel?.FadeOut();

        opponentLoadingFader.Disable();

        AITeam = Team.Black;

        readyButtonContextText.text = "";
        proposeTeamChangeText.text = "SWAP TEAMS";
    }

    private void SetSearchingText() => opponentSearchingText.text = lobbyType switch
    {
        Type.Host => "Waiting for opponent...",
        Type.Client => "Finding opponent...",
        Type.AI => "AI Settings",
        _ => ""
    };

    public void QueryTeamChange()
    {
        if(lobbyType == Type.Client)
        {
            if(readyToggle.toggle.isOn)
                readyToggle.toggle.isOn = false;
        }
        teamChangePanel?.Query();
    }

    public void SwapAITeam()
    {
        AITeam = AITeam.Enemy();
        SetLocalTeam(AITeam == Team.Black);
    }

    public void ToggleHandicapOverlay(bool isOn) => handicapOverlayToggle.toggle.isOn = isOn;
}