using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types {


public class CloseRequest(String id, String? contId, String type)
    {
        [JsonPropertyName("id")]
        public String Id { get; set; } = id;

        [JsonPropertyName("cont_id")]
        public String? ContId { get; set; } = contId;

        [JsonPropertyName("type")]
        public String Type { get; set; } = type;

        
    }

                    
}