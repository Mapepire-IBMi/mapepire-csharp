namespace io.github.mapepire_ibmi.types.jdbcOptions {
/**
 * Enum representing the possible types of values for the "block criteria" JDBC
 * option.
 */
public class BlockCriteria {
    /**
     * No record blocking.
     */
public static readonly String CRITERIA_0 = "0";

    /**
     * Block if FOR FETCH ONLY is specified.
     */
public static readonly String CRITERIA_1 = "1";

    /**
     * Block unless FOR UPDATE is specified.
     */
public static readonly String CRITERIA_2 = "2";

    /**
     * The "block criteria" value.
     */
    private readonly String value;

    /**
     * Construct a new BlockCriteria instance.
     * 
     * @param value The "block criteria" value.
     */
    BlockCriteria(String value) {
        this.value = value;
    }

    /**
     * Get the "block criteria" value.
     * 
     * @return The "block criteria" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "block criteria" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static BlockCriteria FromValue(String value) {
                  if (CRITERIA_0.Equals(value) || 
       CRITERIA_1.Equals(value) || 
       CRITERIA_2.Equals(value)) { 
        return new BlockCriteria(value); 
       }

        throw new ArgumentException("Unknown value: " + value);
    }

}
}

