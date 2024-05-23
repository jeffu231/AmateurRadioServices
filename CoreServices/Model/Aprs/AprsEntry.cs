namespace CoreServices.Model.Aprs;

public class AprsEntry
{
    public string Name { get; set; } = String.Empty;

    public string SrcCall { get; set; } = String.Empty;

    public string DstCall { get; set; } = String.Empty;

    public double Lat { get; set; }

    public double Lng { get; set; }

    public string Comment { get; set; } = String.Empty;

    public string Path { get; set; } = String.Empty;

    public string Type { get; set; } = String.Empty;

    public long Time { get; set; }

    public long LastTime { get; set; }
}