using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types {

/**
 * Represents a standard server response.
 */
public class ServerResponse {

    /**
     * The unique identifier for the request.
     */
     [JsonPropertyName("id")]
    public string? Id {get; set; }

    /**
     * Whether the request was successful.
     */
     [JsonPropertyName("success")]
    public bool Success {get; set; }

    /**
     * The error message, if any.
     */
     [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]

    public string? Error {get; set; }

    /**
     * The SQL return code.
     */
     [JsonPropertyName("sql_rc")]
    public int SqlRc  {get; set; }

    /**
     * The SQL state code.
     */
      [JsonPropertyName("sql_state")]
    
    public string? SqlState  {get; set; }

    /**
     * Construct a new ServerResponse instance.
     */
    public ServerResponse() {

    }

    /**
     * Construct a new ServerResponse instance.
     * 
     * @param id       The unique identifier for the request.
     * @param success  Whether the request was successful.
     * @param error    The error message, if any.
     * @param sqlRc    The SQL return code.
     * @param sqlState The SQL state code.
     */
    public ServerResponse(String Id, bool success, String error, int sqlRc, String sqlState) {
        this.Id = Id;
        this.Success = success;
        this.Error = error;
        this.SqlRc = sqlRc;
        this.SqlState = sqlState;
    }


}
}