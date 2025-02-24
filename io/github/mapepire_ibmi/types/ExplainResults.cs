using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types
{

    /* 
 * Represents the results of an explain request.
 */
    public class ExplainResults : QueryResult
    {
        /**
         * The metadata about the query execution.
         */
        [JsonPropertyName("vemetadata")]
        private QueryMetadata Vemetadata { get; set; }

        /**
         * The data returned from the explain request.
         */
        [JsonPropertyName("vedata")]
        private Object Vedata { get; set; }


        /**
         * Construct a new ExplainResults instance.
         * 
         * @param id             The unique identifier for the request.
         * @param success        Whether the request was successful.
         * @param error          The error message, if any.
         * @param sqlRc          The SQL return code.
         * @param sqlState       The SQL state code.
         * @param metadata       The metadata about the query results.
         * @param isDone         Whether the query execution is complete.
         * @param hasResults     Whether there are results.
         * @param updateCount    The number of rows affected by the query.
         * @param data           The data returned from the query.
         * @param parameterCount The number of parameters in the prepared query.
         * @param outputParms    The output parameters returned from the query.
         * @param vemetadata     The metadata about the query execution.
         * @param vedata         The data returned from the explain request.
         */
        public ExplainResults(String id, bool success, String error, int sqlRc, String sqlState, QueryMetadata metadata,
                bool isDone, bool hasResults, int updateCount, List<Dictionary<String, Object>> data, int parameterCount,
                List<ParameterResult> outputParms, QueryMetadata vemetadata, Object vedata) :
                base(id, success, error, sqlRc, sqlState, metadata, isDone, hasResults, updateCount, data, parameterCount,
                    outputParms)
        {
            ;
            this.Vemetadata = vemetadata;
            this.Vedata = vedata;
        }

        /**
         * Get the metadata about the query execution.
         * 
         * @return The metadata about the query execution.
         */
        public QueryMetadata getVemetadata()
        {
            return Vemetadata;
        }

        /**
         * Set the metadata about the query execution.
         * 
         * @param vemetadata The metadata about the query execution.
         */
        public void SetVemetadata(QueryMetadata vemetadata)
        {
            this.Vemetadata = vemetadata;
        }

        /**
         * Get the data returned from the explain request.
         * 
         * @return The data returned from the explain request.
         */
        public Object GetVedata()
        {
            return Vedata;
        }

        /**
         * Set the data returned from the explain request.
         * 
         * @param vedata The data returned from the explain request.
         */
        public void SetVedata(Object vedata)
        {
            this.Vedata = vedata;
        }
    }
}