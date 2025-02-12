namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "sort" JDBC option.
 */
public class Sort {
    /**
     * Base the sort on hexadecimal values.
     */
public static readonly String HEX = "hex";

    /**
     * Base the sort on the language set in the "sort language" property.
     */
public static readonly String LANGUAGE = "language";

    /**
     * Base the sort on the sort sequence table set in the "sort table" property.
     */
public static readonly String TABLE = "table";

    /**
     * The "sort" value.
     */
    private String value;

    /**
     * Construct a new Sort instance.
     * 
     * @param value The "sort" value.
     */
    Sort(String value) {
        this.value = value;
    }

    /**
     * Get the "sort" value.
     * 
     * @return The "sort" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "sort" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static Sort FromValue(String value) {
                              if (HEX.Equals(value) || 
       LANGUAGE.Equals(value) || 
       TABLE.Equals(value) ) { 
        return new Sort(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}

