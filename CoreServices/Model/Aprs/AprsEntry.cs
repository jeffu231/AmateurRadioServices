using System.Text.Json.Serialization;

namespace CoreServices.Model.Aprs;

public class AprsEntry
{
    public string Name { get; set; } = String.Empty;

    public string SrcCall { get; set; } = String.Empty;

    public string DstCall { get; set; } = String.Empty;

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double Lat { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public double Lng { get; set; }

    public string Comment { get; set; } = String.Empty;

    public string Path { get; set; } = String.Empty;

    public string Type { get; set; } = String.Empty;

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long Time { get; set; }

    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long LastTime { get; set; }
    
    public string Class { get; set; } = String.Empty;
    
    public string Symbol { get; set; } = String.Empty;
}