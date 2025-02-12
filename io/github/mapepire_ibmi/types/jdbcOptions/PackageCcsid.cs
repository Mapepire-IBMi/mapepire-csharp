namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "package ccsid" JDBC
 * option.
 */
public class PackageCcsid {
    /**
     * UCS-2 encoding.
     */
public static readonly String CCSID_1200 = "1200";

    /**
     * UTF-16 encoding.
     */
public static readonly String CCSID_13488 = "13488";

    /**
     * System encoding.
     */
public static readonly String SYSTEM = "system";

    /**
     * The "package ccsid" value.
     */
    private String value;

    /**
     * Construct a new PackageCcsid instance.
     * 
     * @param value The "package ccsid" value.
     */
    PackageCcsid(String value) {
        this.value = value;
    }

    /**
     * Get the "package ccsid" value.
     * 
     * @return The "package ccsid" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "package ccsid" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static PackageCcsid FromValue(String value) {
                              if (CCSID_1200.Equals(value) || 
       CCSID_13488.Equals(value) || 
       SYSTEM.Equals(value) ) { 
        return new PackageCcsid(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}

