namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "time separator" JDBC
 * option.
 */
public class TimeSeparator {
    /**
     * Colon time separator.
     */
public static readonly String COLON = ":";

    /**
     * period time separator.
     */
public static readonly String DOT = ".";

    /**
     * Comma time separator.
     */
public static readonly String COMMA = ",";

    /**
     * Space time separator.
     */
public static readonly String B = "b";

    /**
     * The "time separator" value.
     */
    private String value;

    /**
     * Construct a new TimeSeparator instance.
     * 
     * @param value The "time separator" value.
     */
    TimeSeparator(String value) {
        this.value = value;
    }

    /**
     * Get the "time separator" value.
     * 
     * @return The "time separator" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "time separator" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static TimeSeparator FromValue(String value) {
                              if (COLON.Equals(value) || 
       DOT.Equals(value) || 
       COMMA.Equals(value) || 
       B.Equals(value) ) { 
        return new TimeSeparator(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}
