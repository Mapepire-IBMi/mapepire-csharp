namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "server trace" JDBC
 * option.
 */
public class ServerTrace {
    /**
     * Trace is not active.
     */
public static readonly String TRACE_0 = "0";

    /**
     * Start the database monitor on the JDBC server job.
     */
public static readonly String TRACE_2 = "2";

    /**
     * Start debug on the JDBC server job.
     */
public static readonly String TRACE_4 = "4";

    /**
     * Save the job log when the JDBC server job ends.
     */
public static readonly String TRACE_8 = "8";

    /**
     * Start job trace on the JDBC server job.
     */
public static readonly String TRACE_16 = "16";

    /**
     * Save SQL information.
     */
public static readonly String TRACE_32 = "32";

    /**
     * Supports the activation of database host server tracing.
     */
public static readonly String TRACE_64 = "64";

    /**
     * The "server trace" value.
     */
    private String value;

    /**
     * Construct a new ServerTrace instance.
     * 
     * @param value The "server trace" value.
     */
    ServerTrace(String value) {
        this.value = value;
    }

    /**
     * Get the "server trace" value.
     * 
     * @return The "server trace" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "server trace" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static ServerTrace FromValue(String value) {
                               if (TRACE_0.Equals(value) || 
       TRACE_2.Equals(value) || 
       TRACE_4.Equals(value) || 
       TRACE_8.Equals(value) || 
       TRACE_16.Equals(value) || 
       TRACE_32.Equals(value) || 
       TRACE_64.Equals(value) ) { 
        return new ServerTrace(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}

