namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "transaction
 * isolation" JDBC option.
 */
public class TransactionIsolation {
    /**
     * None transaction isolation.
     */
public static readonly String NONE = "none";

    /**
     * Read uncommitted transaction isolation.
     */
public static readonly String READ_UNCOMMITTED = "read uncommitted";

    /**
     * Read committed transaction isolation.
     */
public static readonly String READ_COMMITTED = "read committed";

    /**
     * Repeatable read transaction isolation.
     */
public static readonly String REPEATABLE_READ = "repeatable read";

    /**
     * Serializable transaction isolation.
     */
public static readonly String SERIALIZABLE = "serializable";

    /**
     * The "transaction isolation" value.
     */
    private String value;

    /**
     * Construct a new TransactionIsolation instance.
     * 
     * @param value The "transaction isolation" value.
     */
    TransactionIsolation(String value) {
        this.value = value;
    }

    /**
     * Get the "transaction isolation" value.
     * 
     * @return The "transaction isolation" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "transaction isolation" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static TransactionIsolation FromValue(String value) {
                               if (NONE.Equals(value) || 
       READ_COMMITTED.Equals(value) || 
       READ_UNCOMMITTED.Equals(value) || 
       REPEATABLE_READ.Equals(value) || 
       SERIALIZABLE.Equals(value) ) { 
        return new TransactionIsolation(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}
