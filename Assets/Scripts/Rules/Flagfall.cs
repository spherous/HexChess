using System.Text;
using Newtonsoft.Json;

[System.Serializable]
public struct Flagfall
{
    public Team flaggedTeam;
    public float timestamp;

    public Flagfall(Team flaggedTeam, float timestamp)
    {
        this.flaggedTeam = flaggedTeam;
        this.timestamp = timestamp;
    }

    public byte[] Serialize() => 
        Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(this));

    public static Flagfall Deserialize(byte[] data) => 
        JsonConvert.DeserializeObject<Flagfall>(Encoding.ASCII.GetString(data));
}
