namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "metadata source" JDBC
 * option.
 */
public class MetadataSource {
    /**
     * ROI access.
     */
public static readonly String SOURCE_0 = "0";

    /**
     * SQL stored procedures.
     */
public static readonly String SOURCE_1 = "1";

    /**
     * The "metadata source" value.
     */
    private String value;

    /**
     * Construct a new MetadataSource instance.
     * 
     * @param value The "metadata source" value.
     */
    MetadataSource(String value) {
        this.value = value;
    }

    /**
     * Get the "metadata source" value.
     * 
     * @return The "metadata source" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "metadata source" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static MetadataSource FromValue(String value) {
                              if (SOURCE_0.Equals(value) || 
       SOURCE_1.Equals(value) ) { 
        return new MetadataSource(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}
