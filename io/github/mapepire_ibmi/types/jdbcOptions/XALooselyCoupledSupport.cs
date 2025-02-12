namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "XA loosely coupled
 * support" JDBC option.
 */
public class XALooselyCoupledSupport {
    /**
     * Locks cannot be shared.
     */
public static readonly String SUPPORT_0 = "0";

    /**
     * Locks can be shared.
     */
public static readonly String SUPPORT_1 = "1";

    /**
     * The "XA loosely coupled support" value.
     */
    private String value;

    /**
     * Construct a new XALooselyCoupledSupport instance.
     * 
     * @param value The "XA loosely coupled support" value.
     */
    XALooselyCoupledSupport(String value) {
        this.value = value;
    }

    /**
     * Get the "XA loosely coupled support" value.
     * 
     * @return The "XA loosely coupled support" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "XA loosely coupled support" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static XALooselyCoupledSupport FromValue(String value) {
                               if (SUPPORT_0.Equals(value) || 
       SUPPORT_1.Equals(value) ) { 
        return new XALooselyCoupledSupport(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}

