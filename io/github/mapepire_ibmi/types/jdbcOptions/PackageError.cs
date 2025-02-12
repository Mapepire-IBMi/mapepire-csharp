namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "package error" JDBC
 * option.
 */
public class PackageError {
    /**
     * Exception action.
     */
public static readonly String EXCEPTION = "exception";

    /**
     * Warning action.
     */
public static readonly String WARNING = "warning";

    /**
     * No action.
     */
public static readonly String NONE = "none";

    /**
     * The "package error" value.
     */
    private String value;

    /**
     * Construct a new PackageError instance.
     * 
     * @param value The "package error" value.
     */
    PackageError(String value) {
        this.value = value;
    }

    /**
     * Get the "package error" value.
     * 
     * @return The "package error" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "package error" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static PackageError FromValue(String value) {
                              if (EXCEPTION.Equals(value) || 
       WARNING.Equals(value) || 
       NONE.Equals(value) ) { 
        return new PackageError(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}

