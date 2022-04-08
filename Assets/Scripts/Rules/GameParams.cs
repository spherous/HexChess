using Newtonsoft.Json;
using System.Text;

[System.Serializable]
public struct GameParams
{
    public Team localTeam;
    public bool showMovePreviews;
    public float timerDuration;
    

    public GameParams(Team localTeam, bool showMovePreviews, float timerDuration = 0)
    {
        this.localTeam = localTeam;
        this.showMovePreviews = showMovePreviews;
        this.timerDuration = timerDuration;
    }

    public byte[] Serialize() => Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(this));

    public static GameParams Deserialize(byte[] data) => JsonConvert.DeserializeObject<GameParams>(Encoding.ASCII.GetString(data));
}