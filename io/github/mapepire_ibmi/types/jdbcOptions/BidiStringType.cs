namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "bidi string type"
 * JDBC option.
 */
public class BidiStringType {
    /**
     * Bidi String Type 4.
     */
public static readonly String TYPE_4 = "4";

    /**
     * Bidi String Type 5.
     */
public static readonly String TYPE_5 = "5";

    /**
     * Bidi String Type 6.
     */
public static readonly String TYPE_6 = "6";

    /**
     * Bidi String Type 7.
     */
public static readonly String TYPE_7 = "7";

    /**
     * Bidi String Type 8.
     */
public static readonly String TYPE_8 = "8";

    /**
     * Bidi String Type 9.
     */
public static readonly String TYPE_9 = "9";

    /**
     * Bidi String Type 10.
     */
public static readonly String TYPE_10 = "10";

    /**
     * Bidi String Type 11.
     */
public static readonly String  TYPE_11 = "11"; 

    /**
     * The "bidi string type" value.
     */
    private readonly String value;

    /**
     * Construct a new BidiStringType instance.
     * 
     * @param value The "bidi string type" value.
     */
    BidiStringType(String value) {
        this.value = value;
    }

    /**
     * Get the "bidi string type" value.
     * 
     * @return The "bidi string type" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "bidi string type" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static BidiStringType FromValue(String value) {
           if (TYPE_4.Equals(value) || 
       TYPE_5.Equals(value) || 
       TYPE_6.Equals(value) || 
       TYPE_7.Equals(value) || 
       TYPE_8.Equals(value) || 
       TYPE_9.Equals(value) || 
       TYPE_10.Equals(value) || 
       TYPE_11.Equals(value)) { 
        return new BidiStringType(value); 
       }
        throw new ArgumentException("Unknown value: " + value);
    }
}
}



