namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "decimal separator"
 * JDBC option.
 */
public class DecimalSeparator {
    /**
     * Period decimal separator.
     */
public static readonly String DOT = ".";

    /**
     * Comma decimal separator.
     */
public static readonly String COMMA = ",";

    /**
     * The "decimal separator" value.
     */
    private String value;

    /**
     * Construct a new DecimalSeparator instance.
     * 
     * @param value The "decimal separator" value.
     */
    DecimalSeparator(String value) {
        this.value = value;
    }

    /**
     * Get the "decimal separator" value.
     * 
     * @return The "decimal separator" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "decimal separator" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static DecimalSeparator FromValue(String value) {
                        if (DOT.Equals(value) || 
       COMMA.Equals(value) ) { 
        return new DecimalSeparator(value); 
       }

        throw new ArgumentException("Unknown value: " + value);
    }
}
}

