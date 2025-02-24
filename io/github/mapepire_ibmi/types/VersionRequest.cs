using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types {



/**
 * Represents the a request for the version information.
 */
public class VersionRequest
 {
 
    [JsonPropertyName("id")]
    public  String Id { get; set; }

    [JsonPropertyName("type")]
    public  String Type { get; set; }


public VersionRequest
(String id, String type) {
    this.Id = id; 
    this.Type = type;
    
}


}
}