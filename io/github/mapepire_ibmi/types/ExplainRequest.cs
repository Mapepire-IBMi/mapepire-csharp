using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types {



/**
 * Represents the a request for the version information.
 */
public class ExplainRequest(String id, String type, String sql, bool run)
    {
 
    [JsonPropertyName("id")]
        public String Id { get; set; } = id;

        [JsonPropertyName("type")]
        public String Type { get; set; } = type;

        [JsonPropertyName("sql")]
        public String Sql { get; set; } = sql;

        [JsonPropertyName("run")]
        public bool Run { get; set; } = run;
    }
}