namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "date format" JDBC
 * option.
 */
public class DateFormat {
    /**
     * mdy date format.
     */
public static readonly String MDY = "mdy";

    /**
     * dmy date format.
     */
public static readonly String DMY = "dmy";

    /**
     * ymd date format.
     */
public static readonly String YMD = "ymd";

    /**
     * usa date format.
     */
public static readonly String USA = "usa";

    /**
     * iso date format.
     */
public static readonly String ISO = "iso";

    /**
     * eur date format.
     */
public static readonly String EUR = "eur";

    /**
     * jis date format.
     */
public static readonly String JIS = "jis";

    /**
     * julian date format.
     */
public static readonly String JULIAN = "julian";

    /**
     * The "date format" value.
     */
    private String value;

    /**
     * Construct a new DateFormat instance.
     * 
     * @param value The "date format" value.
     */
    DateFormat(String value) {
        this.value = value;
    }

    /**
     * Get the "date format" value.
     * 
     * @return The "date format" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "date format" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static DateFormat FromValue(String value) {
                        if (MDY.Equals(value) || 
       DMY.Equals(value) || 
       YMD.Equals(value) || 
       USA.Equals(value) || 
       ISO.Equals(value) || 
       EUR.Equals(value) || 
       JIS.Equals(value) || 
       JULIAN.Equals(value)  ) { 
        return new DateFormat(value); 
       }

       throw new ArgumentException("Unknown value: " + value);
    }
}
}

