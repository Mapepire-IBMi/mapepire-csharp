using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types {


public class ClExecuteRequest(String id, String type, bool terse, String cmd)
    {
        [JsonPropertyName("id")]
        public String Id { get; set; } = id;

        [JsonPropertyName("type")]
        public String Type { get; set; } = type;

        [JsonPropertyName("terse")]
        public bool Terse { get; set; } = terse;

        [JsonPropertyName("cmd")]
        public String Cmd { get; set; } = cmd;
    }
}