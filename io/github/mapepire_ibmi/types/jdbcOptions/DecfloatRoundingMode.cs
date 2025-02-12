namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "decfloat rounding
 * mode" JDBC option.
 */
public class DecfloatRoundingMode {
    /**
     * Half even rounding mode.
     */
public static readonly String HALF_EVEN = "half even";

    /**
     * Half up rounding mode.
     */
public static readonly String HALF_UP = "half up";

    /**
     * Down rounding mode.
     */
public static readonly String DOWN = "down";

    /**
     * Ceiling rounding mode.
     */
public static readonly String CEILING = "ceiling";

    /**
     * Floor rounding mode.
     */
public static readonly String FLOOR = "floor";

    /**
     * Up rounding mode.
     */
public static readonly String UP = "up";

    /**
     * Half down rounding mode.
     */
public static readonly String HALF_DOWN = "half down";

    /**
     * The "decfloat rounding mode" value.
     */
    private String value;

    /**
     * Construct a new DecfloatRoundingMode instance.
     * 
     * @param value The "decfloat rounding mode" value.
     */
    DecfloatRoundingMode(String value) {
        this.value = value;
    }

    /**
     * Get the "decfloat rounding mode" value.
     * 
     * @return The "decfloat rounding mode" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "decfloat rounding mode" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static DecfloatRoundingMode FromValue(String value) {
                        if (HALF_EVEN.Equals(value) || 
       HALF_UP.Equals(value) || 
       DOWN.Equals(value) || 
       CEILING.Equals(value) || 
       FLOOR.Equals(value) || 
       UP.Equals(value) || 
       HALF_DOWN.Equals(value)  ) { 
        return new DecfloatRoundingMode(value); 
       }

        throw new ArgumentException("Unknown value: " + value);
    }
}
}

