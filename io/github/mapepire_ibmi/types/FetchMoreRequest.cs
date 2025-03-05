using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types {


public class FetchMoreRequest(String id, String? contId, String type, String sql, int rows)
    {
        [JsonPropertyName("id")]
        public String Id { get; set; } = id;

        [JsonPropertyName("cont_id")]
        public String? ContId { get; set; } = contId;

        [JsonPropertyName("type")]
        public String Type { get; set; } = type;

        [JsonPropertyName("sql")]
        public String Terse { get; set; } = sql;

        [JsonPropertyName("rows")]
        public int Rows { get; set; } = rows;
    }

                    
}