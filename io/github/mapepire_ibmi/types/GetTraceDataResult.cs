

using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types { 


/**
 * Represents the result of a connection request.
 */
public class GetTraceDataResult : ServerResponse {
   
   /**
     * The trace data.
     */
    [JsonPropertyName("tracedata")]
    public String? TraceData { get; set; }

    /**
     * The JTOpen trace data.
     */
    [JsonPropertyName("jtopentracedata")]
    public String? JtOpenTraceData { get; set; }

  
    /**
     * Construct a new GetTraceDataResult instance.
     * 
     * @param id              The unique identifier for the request.
     * @param success         Whether the request was successful.
     * @param error           The error message, if any.
     * @param sqlRc           The SQL return code.
     * @param sqlState        The SQL state code.
     * @param traceData       The trace data.
     * @param jtOpenTraceData The JTOpen trace data.
     */
    public GetTraceDataResult(String id, bool success, String error, int sqlRc, String sqlState,
            String traceData, String jtOpenTraceData) : base (id, success, error, sqlRc, sqlState) {
        this.TraceData = traceData;
        this.JtOpenTraceData = jtOpenTraceData;
    }


}
}