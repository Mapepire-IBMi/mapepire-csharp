namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "query timeout
 * mechanism" JDBC option.
 */
public class QueryTimeoutMechanism {
    /**
     * The queryTimeout feature uses the "QQRYTIMLMT" feature of the database
     * engine.
     */
public static readonly String QQRYTIMLMT = "qqrytimlmt";

    /**
     * The queryTimeout feature uses a database CANCEL request to cancel a running
     * SQL statement after the specified timeout expires.
     */
public static readonly String CANCEL = "cancel";

    /**
     * The "query timeout mechanism" value.
     */
    private String value;

    /**
     * Construct a new QueryTimeoutMechanism instance.
     * 
     * @param value The "query timeout mechanism" value.
     */
    QueryTimeoutMechanism(String value) {
        this.value = value;
    }

    /**
     * Get the "query timeout mechanism" value.
     * 
     * @return The "query timeout mechanism" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "query timeout mechanism" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static QueryTimeoutMechanism FromValue(String value) {
                               if (QQRYTIMLMT.Equals(value) || 
       CANCEL.Equals(value) ) { 
        return new QueryTimeoutMechanism(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}
