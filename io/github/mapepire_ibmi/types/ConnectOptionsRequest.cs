using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types {



/**
 * Represents the parameter result for a query.
 */
public class ConnectOptionsRequest
 {
 
    [JsonPropertyName("id")]
    public  String Id { get; set; }

    [JsonPropertyName("type")]
    public  String Type { get; set; }

    [JsonPropertyName("technique")]
    public  String Technique { get; set; }

    [JsonPropertyName("application")]
    public  String Application { get; set; }

    /// <summary>The JDBC properties, or null to use the server defaults.</summary>
    [JsonPropertyName("props")]
    public String? JdbcProperties { get; set; }

public ConnectOptionsRequest
(String id, String type, String technique, String application, String? props) {
    this.Id = id; 
    this.Type = type;
    this.Technique = technique;
    this.Application = application; 
    this.JdbcProperties = props;
}


}
}