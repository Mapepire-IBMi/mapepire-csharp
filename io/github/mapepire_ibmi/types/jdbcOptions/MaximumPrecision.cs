namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "maximum precision"
 * JDBC option.
 */
public class MaximumPrecision {
    /**
     * 31 maximum decimal precision.
     */
public static readonly String PRECISION_31 = "31";

    /**
     * 63 maximum decimal precision.
     */
public static readonly String PRECISION_63 = "63";

    /**
     * The "maximum precision" value.
     */
    private String value;

    /**
     * Construct a new MaximumPrecision instance.
     * 
     * @param value The "maximum precision" value.
     */
    MaximumPrecision(String value) {
        this.value = value;
    }

    /**
     * Get the "maximum precision" value.
     * 
     * @return The "maximum precision" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "maximum precision" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static MaximumPrecision FromValue(String value) {
                             if (PRECISION_31.Equals(value) || 
       PRECISION_63.Equals(value) ) { 
        return new MaximumPrecision(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}
