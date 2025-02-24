
using System.Runtime.ConstrainedExecution;

namespace io.github.mapepire_ibmi.types {

/**
 * Enum representing the possible server trace destinations.
 */
public class ServerTraceDest {
    /**
     * Trace data is saved to a file.
     */
    public static readonly ServerTraceDest FILE =  new ServerTraceDest("FILE");

    /**
     * Trace data is kept in memory.
     */
    public static readonly ServerTraceDest IN_MEM =  new ServerTraceDest("IN_MEM");

    /**
     * The server trace destination.
     */
    public string Value;

    /**
     * Construct a new ServerTraceDest instance.
     * 
     * @param value The server trace destination.
     */
    ServerTraceDest(String value) {
        this.Value = value;
    }

    /**
     * Get the server trace destination.
     * 
     * @return The server trace destination.
     */
    public String GetValue() {
        return Value;
    }

 

}
}



