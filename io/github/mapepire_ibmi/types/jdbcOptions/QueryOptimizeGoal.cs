namespace io.github.mapepire_ibmi.types.jdbcOptions {

/**
 * Enum representing the possible types of values for the "query optimize goal"
 * JDBC option.
 */
public class QueryOptimizeGoal {
    /**
     * Optimize query for first block of data (*FIRSTIO) when extended dynamic
     * packages are used; Optimize query for entire result set (*ALLIO) when
     * packages are not used.
     */
public static readonly String GOAL_0 = "0";

    /**
     * Optimize query for first block of data (*FIRSTIO).
     */
public static readonly String GOAL_1 = "1";

    /**
     * Optimize query for entire result set (*ALLIO).
     */
public static readonly String GOAL_2 = "2";

    /**
     * The "query optimize goal" value.
     */
    private String value;

    /**
     * Construct a new QueryOptimizeGoal instance.
     * 
     * @param value The "query optimize goal" value.
     */
    QueryOptimizeGoal(String value) {
        this.value = value;
    }

    /**
     * Get the "query optimize goal" value.
     * 
     * @return The "query optimize goal" value.
     */
    public String getValue() {
        return value;
    }

    /**
     * Get the enum "query optimize goal" value representation of a string.
     * 
     * @param value The string representation of the option.
     * @return The enum representation of the option.
     */
    public static QueryOptimizeGoal FromValue(String value) {
                               if (GOAL_0.Equals(value) || 
       GOAL_1.Equals(value) || 
       GOAL_2.Equals(value) ) { 
        return new QueryOptimizeGoal(value); 
       }


        throw new ArgumentException("Unknown value: " + value);
    }
}
}

