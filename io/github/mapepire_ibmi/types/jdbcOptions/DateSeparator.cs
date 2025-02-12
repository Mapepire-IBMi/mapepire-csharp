namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "date separator" JDBC
 * option.
 */
public class DateSeparator {
    /**
     * Slash date separator.
     */
public static readonly String SLASH = "/";

    /**
     * Dash date separator.
     */
public static readonly String DASH = "-";

    /**
     * Dot date separator.
     */
public static readonly String DOT = ".";

    /**
     * Comma date separator.
     */
public static readonly String COMMA = ",";

    /**
     * Space date separator.
     */
public static readonly String B = "b";

    /**
     * The "date separator" value.
     */
    private String value;

    /**
     * Construct a new DateSeparator instance.
     * 
     * @param value The "date separator" value.
     */
    DateSeparator(String value) {
        this.value = value;
    }

    /**
     * Get the "date separator" value.
     * 
     * @return The "date separator" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "date separator" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static DateSeparator FromValue(String value) {
                        if (SLASH.Equals(value) || 
       DASH.Equals(value) || 
       DOT.Equals(value) || 
       COMMA.Equals(value) || 
       B.Equals(value) ) { 
        return new DateSeparator(value); 
       }

       throw new ArgumentException("Unknown value: " + value);
    }
}
}

