using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types {



/**
 * Represents a simple request with ID and TYPE.
 */
public class IdTypeRequest(String id, String type)
    {
 
    [JsonPropertyName("id")]
        public String Id { get; set; } = id;

        [JsonPropertyName("type")]
        public String Type { get; set; } = type;

        
    }
}