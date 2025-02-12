namespace io.github.mapepire_ibmi.types.jdbcOptions
{

    /**
     * Enum representing the possible types of values for the "driver" JDBC option.
     */
    public class Driver
    {
        /**
         * Use only the IBM Toolbox for Java JDBC driver.
         */
        public static readonly String TOOLBOX = "toolbox";

        /**
         * Use the IBM Developer Kit for Java JDBC driver if running on the server,
         * otherwise use the IBM Toolbox for Java JDBC driver.
         */
        public static readonly String NATIVE = "native";

        /**
         * The "driver" value.
         */
        private String value;

        /**
         * Construct a new Driver instance.
         * 
         * @param value The "driver" value.
         */
        Driver(String value)
        {
            this.value = value;
        }

        /**
         * Get the "driver" value.
         * 
         * @return The "driver" value.
         */
        public String getValue()
        {
            return value;
        }

        /**
         * Get the enum "driver" value representation of a string.
         * 
         * @param value The string representation of the option.
         * @return The enum representation of the option.
         */
        public static Driver FromValue(String value)
        {

            if (TOOLBOX.Equals(value) ||
NATIVE.Equals(value))
            {
                return new Driver(value);
            }

            throw new ArgumentException("Unknown value: " + value);
        }
    }

}
