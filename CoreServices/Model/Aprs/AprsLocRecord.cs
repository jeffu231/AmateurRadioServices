using System.ComponentModel;
using System.Text.Json.Serialization;

namespace CoreServices.Model.Aprs;

public class AprsLocRecord
{
    public string Command { get; set; } = String.Empty;

    public string Result { get; set; } = String.Empty;

    public string What { get; set; } = String.Empty;

    public int Found { get; set; }

    public List<AprsEntry> Entries { get; set; } = new();

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Description { get; set; } = String.Empty;
}