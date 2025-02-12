using System.Windows.Markup;

namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "access" JDBC option.
 */
public class  Access {
    /**
     * All SQL statements allowed.
     */
    public static readonly String ALL ="all";

    /**
     * SELECT and CALL statements allowed.
     */
    public static readonly String READ_CALL ="read call";

    /**
     * SELECT statements only.
     */
    public static readonly String READ_ONLY = "read only";

    /**
     * The "access" value.
     */
    private String value;

    /**
     * Construct a new Access instance.
     * 
     * @param value The "access" value.
     */
    Access(String value) {
        this.value = value;
    }

    /**
     * Get the "access" value.
     * 
     * @return The "access" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "access" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static Access FromValue(String value) {
       if (ALL.Equals(value) || 
       READ_CALL.Equals(value) || 
       READ_ONLY.Equals(value)) { 
        return new Access(value); 
       }

        throw new ArgumentException("Unknown value: " + value);
    }
}
}


