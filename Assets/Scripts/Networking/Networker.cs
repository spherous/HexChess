using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Extensions;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using Open.Nat;
using System.Threading.Tasks;
using System.Threading;

public class Networker : MonoBehaviour
{
    [ReadOnly, ShowInInspector] public bool isHost {get; private set;} = false;
    [ReadOnly, ShowInInspector] public string ip;
    [ShowInInspector] public int port;

    Lobby lobby;
    Multiplayer multiplayer;
    SceneTransition sceneTransition;
    Latency latency;
    [ReadOnly, ShowInInspector] public Player host;
    [ReadOnly, ShowInInspector] public Player? player;

    TcpListener server;
    TcpClient client;
    NetworkStream stream;
    public bool connected => stream != null;

    const int messageMaxSize = 4096;
    public float pingDelay = 2f;
    float pingAtTime;
    float pingedAtTime;
    bool pongReceived = true;

    byte[] readBuffer;
    int readBufferStart;
    int readBufferEnd;
    
    ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();

    public bool clientIsReady {get; private set;}
    GameParams gameParams;
    public bool attemptingConnection {get; private set;} = false;

    Action<string> onConnectCallback;
    
    private void Awake()
    {
        lobby = GameObject.FindObjectOfType<Lobby>();
        sceneTransition = GameObject.FindObjectOfType<SceneTransition>();
        List<Networker> networkers = GameObject.FindObjectsOfType<Networker>().ToList();
        networkers = networkers.Where(networker => networker != this).ToList();
        for(int i = networkers.Count() - 1; i >= 0; i--)
            Destroy(networkers[i].gameObject);
            
        DontDestroyOnLoad(gameObject);
    }

    private void Update() {
        while(mainThreadActions.TryDequeue(out Action a))
            a.Invoke();
        
        // To track latency, every pingDelay seconds we ping the socket, we expect back a pong in a timely fashion
        if(Time.realtimeSinceStartup >= pingAtTime && connected && pongReceived)
        {
            SendMessage(new Message(MessageType.Ping));
            pingedAtTime = Time.realtimeSinceStartup;
            pingAtTime = Time.realtimeSinceStartup + pingDelay;
            pongReceived = false;
        }
    }

    public void Shutdown()
    {
        if(sceneTransition != null)
            sceneTransition.Transition("MainMenu");
        else
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        Destroy(gameObject);
    }

    private void OnDestroy() => Disconnect();

    private void Disconnect()
    {
        if(isHost)
            server?.Stop();
        else if(client != null && client.Connected)
            SendMessage(new Message(MessageType.Disconnect));

        stream?.Close();
        client?.Close();

        NatDiscoverer.ReleaseAll();

        lobby?.DisconnectRecieved();

        Debug.Log($"Disconnected.");
    }

    private void LoadLobby(Lobby.Type lobbyType)
    {
        lobby?.Show(lobbyType);
        lobby?.SetIP(isHost ? GetPublicIPAddress() : $"{ip}");
    }

    public static string GetPublicIPAddress()
    {
        String address = "";
        WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
        
        try {
            using(WebResponse response = request.GetResponse())
            using(StreamReader stream = new StreamReader(response.GetResponseStream()))
            {
                address = stream.ReadToEnd();
            }
        } catch (Exception e)
        {
            Debug.LogWarning($"Failed to fetch IP with error: {e}");
            return "Failed to fetch IP.";
        }

        //Search for the ip in the html
        int first = address.IndexOf("Address: ") + 9;
        int last = address.LastIndexOf("</body></html>");
        address = address.Substring(first, last - first);

        return address;
    }

    // Server
    public void Host()
    {
        isHost = true;
        host = new Player(PlayerPrefs.GetString("PlayerName", "GUEST"), Team.White, isHost);

        Task t = Task.Run(async () => {
            NatDiscoverer discoverer = new NatDiscoverer();
            CancellationTokenSource cts = new CancellationTokenSource(5000);
            NatDevice device = await discoverer.DiscoverDeviceAsync(PortMapper.Upnp, cts);
            await device.CreatePortMapAsync(new Mapping(Protocol.Tcp, port, port, "Hexachessagon (Session lifetime)"));
        });

        try {
            t.Wait();
        } catch (Exception e) {
            Debug.LogWarning(e);
        }

        server = TcpListener.Create(port);

        try {
            server.Start();
            server.BeginAcceptTcpClient(new AsyncCallback(AcceptClientCallback), server);
        }
        catch (Exception e) {
            Debug.LogWarning($"Failed to host on {ip}:{port} with error:\n{e}");
        }

        LoadLobby(Lobby.Type.Host);
    }

    private void AcceptClientCallback(IAsyncResult ar)
    {
        try {
            TcpClient incomingClient = server.EndAcceptTcpClient(ar);
            if(client == null)
            {
                client = incomingClient;
                stream = client.GetStream();
            }
            // In chess, there is only ever 2 players, (1 host, 1 player), so reject any connection trying to come in if the player slot is already full
            else
            {
                incomingClient.Close();
                try {
                    server.BeginAcceptTcpClient(new AsyncCallback(AcceptClientCallback), server);
                }
                catch (Exception e) {
                    Debug.LogWarning($"Failed to connect to incoming client:\n{e}");
                }
                return;
            }
        } catch (Exception e) {
            Debug.LogWarning($"Failed to connect to incoming client:\n{e}");
            return;
        }

        Debug.Log($"Connected to incoming client: {client.IP()}.");
        
        mainThreadActions.Enqueue(() => {
            player = new Player($"{client.IP()}", Team.Black, false);
            
            lobby?.OpponentFound(host);
            lobby?.UpdateTeam(player.Value);
        });

        SendMessage(new Message(MessageType.Connect, Encoding.UTF8.GetBytes(host.name)));
        SendMessage(new Message(MessageType.OpponentFound));

        readBuffer = new byte[messageMaxSize];
        readBufferStart = 0;
        readBufferEnd = 0;

        try {
            stream.BeginRead(readBuffer, readBufferEnd, readBuffer.Length - readBufferEnd, new AsyncCallback(ReceiveMessage), this);
        } catch(Exception e) {
            Debug.LogWarning($"Failed to read from socket:\n{e}");
        }
   
        try {
            server.BeginAcceptTcpClient(new AsyncCallback(AcceptClientCallback), server);
        } catch(Exception e) {
            Debug.LogWarning($"Failed to connect to incoming client:\n{e}");
        }
    }

    // Client
    public void TryConnectClient(string ip, int port, bool dns = false, Action<string> onConnectCallback = null)
    {
        attemptingConnection = true;
        this.ip = ip;
        this.port = port;
        this.onConnectCallback = onConnectCallback;

        try {
            IPAddress addy = dns ? Dns.GetHostAddresses(ip).First() : IPAddress.Parse(ip);
            Debug.Log($"Attempting to connect to {ip}:{port}.");
            client = new TcpClient(addy.AddressFamily);
            client.BeginConnect(addy, port, new AsyncCallback(ClientConnectCallback), this);
        } catch(Exception e) {
            Debug.LogWarning($"Failed to connect to {ip}:{port} with error:\n{e}");
            attemptingConnection = false;
        }
    }

    private void ClientConnectCallback(IAsyncResult ar)
    {
        try { 
            client.EndConnect(ar);
            stream = client.GetStream();
        } catch(Exception e) {
            Debug.LogWarning($"Failed to connect with error:\n{e}");
            onConnectCallback?.Invoke(e.Message);
            onConnectCallback = null;
            attemptingConnection = false;
            return;
        }
        Debug.Log("Sucessfully connected.");
        onConnectCallback?.Invoke("");
        onConnectCallback = null;
        attemptingConnection = false;
        readBuffer = new byte[messageMaxSize];
        readBufferStart = 0;
        readBufferEnd = 0;
        try {
            stream.BeginRead(readBuffer, readBufferEnd, readBuffer.Length - readBufferEnd, new AsyncCallback(ReceiveMessage), this);
        } catch(Exception e) {
            Debug.LogWarning($"Failed to read from socket:\n{e}");
        }
    }

    // Both client + server
    public void SendMessage(Message message)
    {
        try {
            if(connected)
            {
                byte[] messageData = message.Serialize();
                stream.Write(messageData, 0, messageData.Length);
            }
            else
                Debug.LogWarning($"No stream, cannot send message: {message}");
        } catch(Exception e) {
            Debug.LogWarning($"Failed to write to socket with error:\n{e}");
        }
    }
    
    private void ReceiveMessage(IAsyncResult ar)
    {
        try {
            int amountOfBytesRead = stream.EndRead(ar);
            readBufferEnd += amountOfBytesRead;

            if(amountOfBytesRead == 0)
            {
                if(!isHost)
                {
                    Debug.Log("The host closed the socket.");
                    if(lobby != null)
                        mainThreadActions.Enqueue(Shutdown);
                    else if(multiplayer != null)
                    {
                        mainThreadActions.Enqueue(() => multiplayer.Surrender(host.team)); 
                        Disconnect();
                    }
                }
                else
                {
                    Debug.Log("The player disconnected.");
                    mainThreadActions.Enqueue(PlayerDisconnected);
                    return;
                }
            }
            else
                CheckCompleteMessage();

            // Wait for next message
            var availableBufferBytes = readBuffer.Length - readBufferEnd;
            if(availableBufferBytes > 10)
                stream.BeginRead(readBuffer, readBufferEnd, readBuffer.Length - readBufferEnd, new AsyncCallback(ReceiveMessage), this);
            else
            {
                Debug.LogError($"Other player sent too big of a message!");
                readBufferStart = 0;
                readBufferEnd = 0;
            }

            
        } catch(IOException e) {
            Debug.Log($"The socket was closed.\n{e}");
            mainThreadActions.Enqueue(Shutdown);
        }
        catch(ObjectDisposedException) {
            // ignore object disposed exceptions, connection is in the process of being torn down
        }
        catch(Exception e) {
            Debug.LogWarning($"Failed to read from socket:\n{e}");
        }
    }

    private void CheckCompleteMessage()
    {
        while(readBufferStart < readBufferEnd)
        {
            Message? readResult;
            try {
                readResult = Message.ReadMessage(new ReadOnlySpan<byte>(readBuffer, readBufferStart, readBufferEnd - readBufferStart));
            } catch (ArgumentException err) {
                readBufferStart = readBufferEnd;
                Debug.LogError($"Error reading message: {err}");
                break;
            }
            if(!readResult.HasValue)
                break;

            Message message = readResult.Value;
            readBufferStart += message.totalLength;

            mainThreadActions.Enqueue(() => Dispatch(message));
        }

        if(readBufferStart == readBufferEnd)
        {
            readBufferStart = 0;
            readBufferEnd = 0;
        }
    }

    private void Dispatch(Message completeMessage)
    {
        if(completeMessage.type != MessageType.Ping && completeMessage.type != MessageType.Pong)
            Debug.Log($"Recieved message of type {completeMessage.type}");

        Action action = completeMessage.type switch {
            MessageType.Connect when !isHost => () => {
                string hostName = Encoding.UTF8.GetString(completeMessage.data);

                host = new Player(string.IsNullOrEmpty(hostName) ? "Host" : hostName, Team.White, true);
                string localName = PlayerPrefs.GetString("PlayerName", "GUEST");
                player = new Player(localName, Team.Black, false);

                mainThreadActions.Enqueue(() => 
                {
                    LoadLobby(Lobby.Type.Client);
                    lobby?.UpdatePlayerName(host);
                });

                SendMessage(new Message(MessageType.UpdateName, System.Text.Encoding.UTF8.GetBytes(localName)));
            },
            MessageType.Disconnect when isHost => PlayerDisconnected,
            MessageType.Disconnect when !isHost => lobby == null ? (Action)Disconnect : (Action)Shutdown,
            MessageType.Ping => () => SendMessage(new Message(MessageType.Pong)),
            MessageType.Pong => ReceivePong,
            MessageType.ProposeTeamChange => ReceiveTeamChangeProposal,
            MessageType.ApproveTeamChange => () => mainThreadActions.Enqueue(SwapTeams),
            MessageType.Ready => Ready,
            MessageType.Unready => Unready,
            MessageType.HandicapOverlayOn when lobby => () => lobby?.ToggleHandicapOverlay(true),
            MessageType.HandicapOverlayOff when lobby => () => lobby?.ToggleHandicapOverlay(false),
            MessageType.StartMatch when !isHost => () => StartMatch(GameParams.Deserialize(completeMessage.data)),
            MessageType.Surrender when multiplayer => () => multiplayer.Surrender(
                surrenderingTeam: isHost ? player.Value.team : host.team,
                timestamp: JsonConvert.DeserializeObject<float>(Encoding.ASCII.GetString(completeMessage.data))
            ),
            MessageType.BoardState when multiplayer => () => multiplayer.ReceiveBoard(BoardState.Deserialize(completeMessage.data)),
            MessageType.Promotion when multiplayer => () => multiplayer.ReceivePromotion(Promotion.Deserialize(completeMessage.data)),
            MessageType.OfferDraw when multiplayer => () => mainThreadActions.Enqueue(() => GameObject.FindObjectOfType<OfferDrawPanel>()?.Open()),
            MessageType.AcceptDraw when multiplayer => () => multiplayer.Draw(JsonConvert.DeserializeObject<float>(Encoding.ASCII.GetString(completeMessage.data))),
            MessageType.UpdateName when isHost => () => UpdateClientName(completeMessage),
            MessageType.UpdateName when !isHost => () => UpdateHostName(completeMessage),
            MessageType.FlagFall when multiplayer => () => multiplayer.ReceiveFlagfall(Flagfall.Deserialize(completeMessage.data)),
            MessageType.Checkmate when multiplayer => () => multiplayer.ReceiveCheckmate(BitConverter.ToSingle(completeMessage.data, 0)),
            MessageType.Stalemate when multiplayer => () => multiplayer.ReceiveStalemate(BitConverter.ToSingle(completeMessage.data, 0)),
            MessageType.OpponentSearching when lobby && !isHost => lobby.OpponentSearching,
            MessageType.OpponentFound when lobby && !isHost => () => mainThreadActions.Enqueue(() => lobby.OpponentFound(host)),
            _ => () => Debug.LogWarning($"Ignoring unhandled message {completeMessage.type}"),
        };

        action?.Invoke();
    }

    private void ReceivePong()
    {
        // Measure and update latency when pong received
        int latencyMs = ((Time.realtimeSinceStartup - pingedAtTime) * 1000).Ceil();
        mainThreadActions.Enqueue(() =>
        {
            pongReceived = true;

            if(!latency)
                latency = GameObject.FindObjectOfType<Latency>();
            latency?.UpdateLatency(latencyMs);
        });
    }

    private void UpdateHostName(Message completeMessage)
    {
        if(lobby == null)
            return;

        host.name = System.Text.Encoding.UTF8.GetString(completeMessage.data);
        lobby.UpdatePlayerName(host);
    }

    private void UpdateClientName(Message completeMessage)
    {
        if(!player.HasValue)
            return;

        Player p = player.Value;
        p.name = System.Text.Encoding.UTF8.GetString(completeMessage.data);
        player = p;
        lobby?.UpdatePlayerName(p);
    }

    private void PlayerDisconnected()
    {
        if(lobby != null && player.HasValue)
        {
            lobby.RemovePlayer(player.Value);
            player = null;
            client = null;
            stream = null;

            if(host.team == Team.Black)
                host.team = Team.White;

            if(clientIsReady)
                Unready();
        }

        lobby?.DisconnectRecieved();
        
        // For now, assume a loss when the player disconnects, later we should wait for a potential reconnect
        multiplayer?.Surrender(player.Value.team);
    }

    public void ProposeTeamChange()
    {
        if(client == null)
            return;

        SendMessage(new Message(MessageType.ProposeTeamChange));
    }

    private void ReceiveTeamChangeProposal() => mainThreadActions.Enqueue(() => lobby?.QueryTeamChange());

    public void RespondToTeamChange(MessageType answer)
    {
        if(lobby == null)
            return;

        SendMessage(new Message(answer));
        
        if(answer == MessageType.ApproveTeamChange)
            SwapTeams();
    }

    public void SwapTeams()
    {
        if(!player.HasValue && lobby == null)
            return;

        Team hostTeam = host.team;
        Player playerModified = player.Value;
        host.team = playerModified.team;
        playerModified.team = hostTeam;
        player = playerModified;

        lobby?.UpdateTeam(player.Value);
    }

    private void Ready()
    {
        clientIsReady = true;
        lobby?.ReadyRecieved();
    }

    private void Unready()
    {
        clientIsReady = false;
        lobby?.UnreadyRecieved();
    }

    public void HostMatch()
    {
        if(!isHost)
            return;

        HandicapOverlayToggle previewToggle = GameObject.FindObjectOfType<HandicapOverlayToggle>();
        bool previewOn = previewToggle == null ? false : previewToggle.toggle.isOn;

        if(lobby.timerToggle.isOn)
            gameParams = new GameParams(host.team, previewOn, lobby.GetTimeInSeconds());
        else
            gameParams = new GameParams(host.team, previewOn);


        SendMessage(new Message(
            type: MessageType.StartMatch,
            data: new GameParams(
                host.team == Team.White ? Team.Black : Team.White, 
                gameParams.showMovePreviews, 
                gameParams.timerDuration
            ).Serialize()
        ));

        SceneManager.activeSceneChanged += SetupGame;
        if(sceneTransition != null)
            sceneTransition.Transition("VersusMode");
        else
            SceneManager.LoadScene("VersusMode");
    }

    public void StartMatch(GameParams gameParams)
    {
        if(isHost)
            return;

        this.gameParams = gameParams;

        SceneManager.activeSceneChanged += SetupGame;
        if(sceneTransition != null)
            sceneTransition.Transition("VersusMode");
        else
            SceneManager.LoadScene("VersusMode");
    }

    private void SetupGame(Scene arg0, Scene arg1)
    {
        multiplayer = GameObject.FindObjectOfType<Multiplayer>();
        lobby = null;
        pingAtTime = 0;
        multiplayer?.SetupGame(gameParams);
        SceneManager.activeSceneChanged -= SetupGame;
    }

    public void RespondToDrawOffer(MessageType answer)
    {
        if(multiplayer == null)
            return;

        Board board = GameObject.FindObjectOfType<Board>();
        float timestamp = board.currentGame.CurrentTime;

        if(answer == MessageType.AcceptDraw)
            multiplayer.Draw(timestamp);

        Message response = new Message(
            answer,
            Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(timestamp))
        );

        SendMessage(response);
    }

    public void UpdateName(string newName)
    {
        if(lobby == null)
            return;

        if(isHost)
            host.name = newName;
        else if(player.HasValue)
        {
            Player p = player.Value;
            p.name = newName;
            player = p;
        }

        if(connected)
            SendMessage(new Message(MessageType.UpdateName, System.Text.Encoding.UTF8.GetBytes(newName)));
    }
}