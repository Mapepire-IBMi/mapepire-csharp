namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "errors" JDBC option.
 */
public class Errors {
    /**
     * Full error detail.
     */
public static readonly String FULL = "full";

    /**
     * Basic error detail.
     */
public static readonly String BASIC = "basic";

    /**
     * The "errors" value.
     */
    private String value;

    /**
     * Construct a new Errors instance.
     * 
     * @param value The "errors" value.
     */
    Errors(String value) {
        this.value = value;
    }

    /**
     * Get the "errors" value.
     * 
     * @return The "errors" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "errors" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static Errors FromValue(String value) {
                           if (FULL.Equals(value) || 
       BASIC.Equals(value) ) { 
        return new Errors(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}

