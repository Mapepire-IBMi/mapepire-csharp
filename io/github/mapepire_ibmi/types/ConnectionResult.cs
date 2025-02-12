using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types { 


/**
 * Represents the result of a connection request.
 */
public class ConnectionResult : ServerResponse {
    /**
     * The unique job identifier for the connection.
     */
    
     [JsonPropertyName("job")]
    public string? Job { get; set; }

    /**
     * Construct a new ConnectionResult instance.
     */
    public ConnectionResult() : base() {
        
    }

    /**
     * Construct a new ConnectionResult instance.
     * 
     * @param id       The unique identifier for the request.
     * @param success  Whether the request was successful.
     * @param error    The error message, if any.
     * @param sqlRc    The SQL return code.
     * @param sqlState The SQL state code.
     * @param job      The unique job identifier for the connection.
     */
    public ConnectionResult(string id, bool success, string error, int sqlRc, 
    string sqlState, string job)  : base(id, success, error, sqlRc, sqlState){
     
        this.Job = job;
    }

   }
}