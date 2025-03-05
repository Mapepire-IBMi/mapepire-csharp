namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "naming" JDBC option.
 */
public class Naming {
    /**
     * As in schema.table.
     */
public static readonly Naming SQL = new Naming("sql");

    /**
     * As in schema/table.
     */
public static readonly Naming SYSTEM = new Naming("system");

    /**
     * The "naming" value.
     */
    private String value;

    /**
     * Construct a new Naming instance.
     * 
     * @param value The "naming" value.
     */
    Naming(String value) {
        this.value = value;
    }

    /**
     * Get the "naming" value.
     * 
     * @return The "naming" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "naming" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static Naming FromValue(String value) {
                               if (SQL.Equals(value) || 
       SYSTEM.Equals(value) ) { 
        return new Naming(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}

