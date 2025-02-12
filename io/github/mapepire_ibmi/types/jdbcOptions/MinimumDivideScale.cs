namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "minimum divide scale"
 * JDBC option.
 */
public class MinimumDivideScale {
    /**
     * 0 scale value.
     */
public static readonly String SCALE_0 = "0";

    /**
     * 1 scale value.
     */
public static readonly String SCALE_1 = "1";

    /**
     * 2 scale value.
     */
public static readonly String SCALE_2 = "2";

    /**
     * 3 scale value.
     */
public static readonly String SCALE_3 = "3";

    /**
     * 4 scale value.
     */
public static readonly String SCALE_4 = "4";

    /**
     * 5 scale value.
     */
public static readonly String SCALE_5 = "5";

    /**
     * 6 scale value.
     */
public static readonly String SCALE_6 = "6";

    /**
     * 7 scale value.
     */
public static readonly String SCALE_7 = "7";

    /**
     * 8 scale value.
     */
public static readonly String SCALE_8 = "8";

    /**
     * 9 scale value.
     */
public static readonly String SCALE_9 = "9";

    /**
     * The "minimum divide scale" value.
     */
    private String value;

    /**
     * Construct a new MinimumDivideScale instance.
     * 
     * @param value The "minimum divide scale" value.
     */
    MinimumDivideScale(String value) {
        this.value = value;
    }

    /**
     * Get the "minimum divide scale" value.
     * 
     * @return The "minimum divide scale" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "minimum divide scale" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static MinimumDivideScale FromValue(String value) {
                               if (SCALE_0.Equals(value) || 
       SCALE_1.Equals(value) || 
       SCALE_2.Equals(value) || 
       SCALE_3.Equals(value) || 
       SCALE_4.Equals(value) || 
       SCALE_5.Equals(value) || 
       SCALE_6.Equals(value) || 
       SCALE_7.Equals(value) || 
       SCALE_8.Equals(value) || 
       SCALE_9.Equals(value)  ) { 
        return new MinimumDivideScale(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}

