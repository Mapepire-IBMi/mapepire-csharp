
using System.Reflection.Metadata;

namespace io.github.mapepire_ibmi.types {
/**
 * Enum representing the possible server trace levels.
 */
public class ServerTraceLevel {
    /**
     * Tracing is turned off.
     */
    public readonly static  ServerTraceLevel OFF = new("OFF"); 

    /**
     * All trace data is collected except datastream.
     */
    public readonly static ServerTraceLevel ON = new("ON"); 

    /**
     * Only error trace data is collected.
     */
    public readonly static ServerTraceLevel ERRORS = new("ERRORS");

    /**
     * All trace data is collected including datastream.
     */
    public readonly static ServerTraceLevel DATASTREAM = new("DATASTREAM");

    /**
     * The server trace level.
     */
    private String Value;

    /**
     * Construct a new ServerTraceLevel instance.
     * 
     * @param value The server trace level.
     */
    ServerTraceLevel(String value) {
        this.Value = value;
    }

    /**
     * Get the server trace level.
     * 
     * @return The server trace level.
     */
    public String GetValue() {
        return Value;
    }

  
}
}
