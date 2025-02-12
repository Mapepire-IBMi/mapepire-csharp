namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "toolbox trace" JDBC
 * option.
 */
public class ToolboxTrace {
    /**
     * Empty.
     */
   public static readonly String EMPTY= "";

    /**
     * None.
     */
public static readonly String NONE = "NONE";

    /**
     * Log data flow between the local host and the remote system.
     */
public static readonly String DATASTREAM = "datastream";

    /**
     * Log object state information.
     */
public static readonly String DIAGNOSTIC = "diagnostic";

    /**
     * Log errors that cause an exception.
     */
public static readonly String ERROR = "error";

    /**
     * Log errors that are recoverable.
     */
public static readonly String WARNING = "warning";

    /**
     * Log character set conversions between Unicode and native code pages.
     */
public static readonly String CONVERSION = "conversion";

    /**
     * Log jdbc information.
     */
public static readonly String JDBC = "jdbc";

    /**
     * Used to determine how PCML interprets the data that is sent to and from the
     * server.
     */
public static readonly String PCML = "pcml";

    /**
     * Log all categories.
     */
public static readonly String ALL = "all";

    /**
     * Log data flow between the client and the proxy server.
     */
public static readonly String PROXY = "proxy";

    /**
     * Log thread information.
     */
public static readonly String THREAD = "thread";

    /**
     * Used to track the flow of control through the code.
     */
public static readonly String INFORMATION = "information";

    /**
     * The "toolbox trace" value.
     */
    private String value;

    /**
     * Construct a new ToolboxTrace instance.
     * 
     * @param value The "toolbox trace" value.
     */
    ToolboxTrace(String value) {
        this.value = value;
    }

    /**
     * Get the "toolbox trace" value.
     * 
     * @return The "toolbox trace" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "toolbox trace" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static ToolboxTrace FromValue(String value) {
                               if (EMPTY.Equals(value) || 
       NONE.Equals(value) || 
       DATASTREAM.Equals(value) || 
       DIAGNOSTIC.Equals(value) || 
       ERROR.Equals(value) || 
       WARNING.Equals(value) || 
       CONVERSION.Equals(value) || 
       JDBC.Equals(value) || 
       PCML.Equals(value) || 
       ALL.Equals(value) || 
       PROXY.Equals(value) || 
       THREAD.Equals(value) || 
       INFORMATION.Equals(value) ) { 
        return new ToolboxTrace(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}

