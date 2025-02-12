namespace io.github.mapepire_ibmi.types.jdbcOptions
{

    /**
     * Enum representing the possible types of values for the "translate hex" JDBC
     * option.
     */
    public class TranslateHex
    {
        /**
         * Interpret hexadecimal literals as character data.
         */
        public static readonly String CHARACTER = "character";

        /**
         * Interpret hexadecimal literals as binary data.
         */
        public static readonly String BINARY = "binary";

        /**
         * The "translate hex" value.
         */
        private String value;

        /**
         * Construct a new TranslateHex instance.
         * 
         * @param value The "translate hex" value.
         */
        TranslateHex(String value)
        {
            this.value = value;
        }

        /**
         * Get the "translate hex" value.
         * 
         * @return The "translate hex" value.
         */
        public String getValue()
        {
            return value;
        }

        /**
         * Get the enum "translate hex" value representation of a string.
         * 
         * @param value The string representation of the option.
         * @return The enum representation of the option.
         */
        public static TranslateHex FromValue(String value)
        {
            if (CHARACTER.Equals(value) ||
BINARY.Equals(value))
            {
                return new TranslateHex(value);
            }


            throw new ArgumentException("Unknown value: " + value);
        }
    }
}

