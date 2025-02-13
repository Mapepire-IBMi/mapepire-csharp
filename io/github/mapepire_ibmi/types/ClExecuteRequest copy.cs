using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types {


public class ClExecuteRequest
 {
 
 
    [JsonPropertyName("id")]
    public  String Id { get; set; }

    [JsonPropertyName("type")]
    public  String Type { get; set; }

    [JsonPropertyName("terse")]
    public  bool Terse { get; set; }

    [JsonPropertyName("cmd")]
    public  String Cmd { get; set; }

public ClExecuteRequest
(String id, String type, bool terse, String cmd) {
    this.Id = id; 
    this.Type = type;
    this.Terse = terse;
    this.Cmd = cmd; 
}


}
}