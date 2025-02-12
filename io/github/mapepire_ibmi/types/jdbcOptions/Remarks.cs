namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "remarks" JDBC option.
 */
public class Remarks {
    /**
     * SQL object comment.
     */
public static readonly String SQL = "sql";

    /**
     * IBM i object description.
     */
public static readonly String SYSTEM = "system";

    /**
     * The "remarks" value.
     */
    private String value;

    /**
     * Construct a new Remarks instance.
     * 
     * @param value The "remarks" value.
     */
    Remarks(String value) {
        this.value = value;
    }

    /**
     * Get the "remarks" value.
     * 
     * @return The "remarks" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "remarks" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static Remarks FromValue(String value) {
                               if (SQL.Equals(value) || 
       SYSTEM.Equals(value) ) { 
        return new Remarks(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}
