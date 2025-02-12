namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "time format" JDBC
 * option.
 */
public class TimeFormat {
    /**
     * hms time format.
     */
public static readonly String HMS = "hms";

    /**
     * usa time format.
     */
public static readonly String USA = "usa";

    /**
     * iso time format.
     */
public static readonly String ISO = "iso";

    /**
     * eur time format.
     */
public static readonly String EUR = "eur";

    /**
     * jis time format.
     */
public static readonly String JIS = "jis";

    /**
     * The "time format" value.
     */
    private String value;

    /**
     * Construct a new TimeFormat instance.
     * 
     * @param value The "time format" value.
     */
    TimeFormat(String value) {
        this.value = value;
    }

    /**
     * Get the "time format" value.
     * 
     * @return The "time format" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "time format" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static TimeFormat FromValue(String value) {
                               if (HMS.Equals(value) || 
       USA.Equals(value) || 
       ISO.Equals(value) || 
       EUR.Equals(value) || 
       JIS.Equals(value) ) { 
        return new TimeFormat(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}

