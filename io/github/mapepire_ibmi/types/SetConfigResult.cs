using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types { 
  /*
 * Represents the result of a config request.
 */
public class SetConfigResult : ServerResponse {
    /**
     * The server trace data destination.
     */
    [JsonPropertyName("tracedest")]
    public String? TraceDest{ get; set; }

    /**
     * The server trace level.
     */
    [JsonPropertyName("tracelevel")]
    public String? TraceLevel{ get; set; }

    /**
     * The JTOpen trace data destination.
     */
    [JsonPropertyName("jtopentracedest")]
    public String? JtOpenTraceDest{ get; set; }

    /**
     * The JTOpen trace level.
     */
    [JsonPropertyName("jtopentracelevel")]
    public String? JtOpenTraceLevel{ get; set; }
 

    public SetConfigResult() {

    }
    /**
     * Construct a new SetConfigResult instance.
     * 
     * @param id               The unique identifier for the request.
     * @param success          Whether the request was successful.
     * @param error            The error message, if any.
     * @param sqlRc            The SQL return code.
     * @param sqlState         The SQL state code.
     * @param traceDest        The server trace data destination.
     * @param traceLevel       The server trace level.
     * @param jtOpenTraceDest  The JTOpen trace data destination.
     * @param jtOpenTraceLevel The JTOpen trace level.
     */
    public SetConfigResult(String id, bool success, String error, int sqlRc, String sqlState,
            String traceDest, ServerTraceLevel traceLevel, String jtOpenTraceDest,
            ServerTraceLevel jtOpenTraceLevel) : base(id, success, error, sqlRc, sqlState) {
        
        this.TraceDest = traceDest;
        this.TraceLevel = traceLevel.GetValue();
        this.JtOpenTraceDest = jtOpenTraceDest;
        this.JtOpenTraceLevel = jtOpenTraceLevel.GetValue();
    }

    
}
}
