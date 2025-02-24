using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types {


public class SetTraceConfigRequest(String id, String type)
    {


        [JsonPropertyName("id")]
        public String Id { get; set; } = id;

        [JsonPropertyName("type")]
        public String Type { get; set; } = type;

        [JsonPropertyName("tracedest")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public  String? Tracedest { get; set; }

     [JsonPropertyName("tracelevel")]
     [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public  String? Tracelevel { get; set; } 
    [JsonPropertyName("jtopentracedest")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public  String? JTOpenTraceDest { get; set; } 
    [JsonPropertyName("jtopentracelevel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public  String? JTOpenTraceLevel { get; set; }
    }
}

