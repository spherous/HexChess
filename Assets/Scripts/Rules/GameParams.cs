using Newtonsoft.Json;
using System.Text;

[System.Serializable]
public struct GameParams
{
    public Team localTeam;
    public bool showMovePreviews;
    public float timerDuration;
    public bool showClock;
    

    public GameParams(Team localTeam, bool showMovePreviews, float timerDuration = 0, bool showClock = false)
    {
        this.localTeam = localTeam;
        this.showMovePreviews = showMovePreviews;
        this.timerDuration = timerDuration;
        this.showClock = timerDuration > 0 ? false : showClock;
    }

    public byte[] Serialize() => Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(this));

    public static GameParams Deserialize(byte[] data) => JsonConvert.DeserializeObject<GameParams>(Encoding.ASCII.GetString(data));
}