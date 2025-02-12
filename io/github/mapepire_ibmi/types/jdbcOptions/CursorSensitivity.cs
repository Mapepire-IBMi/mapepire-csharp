namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "cursor sensitivity"
 * JDBC option.
 */
public class CursorSensitivity {
    /**
     * Asensitive cursor sensitivity.
     */
public static readonly String ASENSITIVE = "asensitive";

    /**
     * Insensitive cursor sensitivity.
     */
public static readonly String INSENSITIVE = "insensitive";

    /**
     * Sensitive cursor sensitivity.
     */
public static readonly String SENSITIVE = "sensitive";

    /**
     * The "cursor sensitivity" value.
     */
    private String value;

    /**
     * Construct a new CursorSensitivity instance.
     * 
     * @param value The "cursor sensitivity" value.
     */
    CursorSensitivity(String value) {
        this.value = value;
    }

    /**
     * Get the "cursor sensitivity" value.
     * 
     * @return The "cursor sensitivity" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "cursor sensitivity" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static CursorSensitivity FromValue(String value) {
                          if (ASENSITIVE.Equals(value) || 
       INSENSITIVE.Equals(value) || 
       SENSITIVE.Equals(value) ) { 
        return new CursorSensitivity(value); 
       }

        throw new ArgumentException("Unknown value: " + value);
    }
}
}

