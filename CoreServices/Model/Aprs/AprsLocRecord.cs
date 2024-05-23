using Newtonsoft.Json;

namespace CoreServices.Model.Aprs;

public class AprsLocRecord
{
    public string Command { get; set; } = String.Empty;

    public string Result { get; set; } = String.Empty;

    public string What { get; set; } = String.Empty;

    public int Found { get; set; }

    public List<AprsEntry> Entries { get; set; } = new();

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore)]
    public string Description { get; set; } = String.Empty;
}