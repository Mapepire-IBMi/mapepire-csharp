namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "package criteria"
 * JDBC option.
 */
public class PackageCriteria {
    /**
     * Only store SQL statements with parameter markers in the package.
     */
public static readonly String DEFAULT = "default";

    /**
     * Store all SQL SELECT statements in the package.
     */
public static readonly String SELECT = "select";

    /**
     * The "package criteria" value.
     */
    private String value;

    /**
     * Construct a new PackageCriteria instance.
     * 
     * @param value The "package criteria" value.
     */
    PackageCriteria(String value) {
        this.value = value;
    }

    /**
     * Get the "package criteria" value.
     * 
     * @return The "package criteria" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "package criteria" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static PackageCriteria FromValue(String value) {
                              if (DEFAULT.Equals(value) || 
       SELECT.Equals(value) ) { 
        return new PackageCriteria(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}

