namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "sort weight" JDBC
 * option.
 */
public class SortWeight {
    /**
     * Uppercase and lowercase characters sort as the same character.
     */
public static readonly String SHARED = "shared";

    /**
     * Uppercase and lowercase characters sort as different characters.
     */
public static readonly String UNIQUE = "unique";

    /**
     * The "sort weight" value.
     */
    private String value;

    /**
     * Construct a new SortWeight instance.
     * 
     * @param value The "sort weight" value.
     */
    SortWeight(String value) {
        this.value = value;
    }

    /**
     * Get the "sort weight" value.
     * 
     * @return The "sort weight" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "sort weight" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static SortWeight FromValue(String value) {
                               if (SHARED.Equals(value) || 
       UNIQUE.Equals(value) ) { 
        return new SortWeight(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}
