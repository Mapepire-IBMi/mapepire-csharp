namespace io.github.mapepire_ibmi.types { 





/**
 * Enum representing the possible states of a query execution.
 */
public enum QueryState {
    /**
     * Indicates that the query has not yet been run.
     */
    NOT_YET_RUN = 1,

    /**
     * Indicates that the query has been executed, and more data is available for
     * retrieval.
     */
    RUN_MORE_DATA_AVAILABLE = 2,

    /**
     * Indicates that the query has been successfully executed and all data has been
     * retrieved.
     */
    RUN_DONE = 3,

    /**
     * Indicates that an error occurred during the query execution.
     */
    ERROR = 4

    }












}