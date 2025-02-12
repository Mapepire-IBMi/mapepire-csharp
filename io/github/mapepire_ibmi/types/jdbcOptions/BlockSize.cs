namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "block size" JDBC
 * option.
 */
public class BlockSize {
    /**
     * 0 kilobytes.
     */
public static readonly String SIZE_0 = "0";

    /**
     * 8 kilobytes.
     */
public static readonly String SIZE_8 = "8";

    /**
     * 16 kilobytes.
     */
public static readonly String SIZE_16 = "16";

    /**
     * 32 kilobytes.
     */
public static readonly String SIZE_32 = "32";

    /**
     * 64 kilobytes.
     */
public static readonly String SIZE_64 = "64";

    /**
     * 128 kilobytes.
     */
public static readonly String SIZE_128 = "128";

    /**
     * 256 kilobytes.
     */
public static readonly String SIZE_256 = "256";

    /**
     * 512 kilobytes.
     */
public static readonly String SIZE_512 = "512";

    /**
     * The "block size" value.
     */
    private String value;

    /**
     * Construct a new BlockSize instance.
     * 
     * @param value The "block size" value.
     */
    public BlockSize(String value) {
        this.value = value;
    }

    /**
     * Get the "block size" value.
     * 
     * @return The "block size" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "block size" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static BlockSize FromValue(String value) {
                          if (SIZE_0.Equals(value) || 
       SIZE_8.Equals(value) || 
       SIZE_16.Equals(value) || 
       SIZE_32.Equals(value) || 
       SIZE_64.Equals(value) || 
       SIZE_128.Equals(value) || 
       SIZE_256.Equals(value) || 
       SIZE_512.Equals(value)) { 
        return new BlockSize(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}

