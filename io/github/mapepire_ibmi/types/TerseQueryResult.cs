using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types { 


/**
 * Represents a standard query result.
 */
public class TerseQueryResult : ServerResponse {
    /**
     * The metadata about the query results.
     */
    [JsonPropertyName("metadata")]
    public QueryMetadata? Metadata  {get; set; }

    /**
     * Whether the query execution is complete.
     */
    [JsonPropertyName("is_done")]
    public bool IsDone  {get; set; }

    /**
     * Whether there are results.
     */
    [JsonPropertyName("has_results")]
    public bool HasResults {get; set; }

    /**
     * The number of rows affected by the query.
     */
    [JsonPropertyName("update_count")]
    public int UpdateCount {get; set; }

    /**
     * The data returned from the query.
     */
    [JsonPropertyName("data")]
    public List<List<Object>>? Data {get; set; }

    /**
     * The number of parameters in the prepared query.
     */
    [JsonPropertyName("parameter_count")]
    public int ParameterCount {get; set; }

    /**
     * The output parameters returned from the query.
     */
    [JsonPropertyName("output_parms")]
    public List<ParameterResult>? OutputParms {get; set; }

    /**
     * Construct a new QueryResult instance.
     */
    public TerseQueryResult() : base() {
        
    }

    /**
     * Construct a new QueryResult instance.
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
     */
    public TerseQueryResult(String id, bool success, String error, int sqlRc, String sqlState, QueryMetadata metadata,
            bool isDone, bool hasResults, int updateCount, List<List<Object>> data, int parameterCount,
            List<ParameterResult> outputParms) : base(id, success, error, sqlRc, sqlState) {
        this.Metadata = metadata;
        this.IsDone = isDone;
        this.HasResults = hasResults;
        this.UpdateCount = updateCount;
        this.Data = data;
        this.ParameterCount = parameterCount;
        this.OutputParms = outputParms;
    }

    public QueryResult ConvertToQueryResult() {
        List<Dictionary<String,Object>>? queryResultData = null; 
        if (Data != null) { 
            queryResultData = []; 
            foreach ( List<Object> row in Data) {
               Dictionary<String,Object> newRow = []; 
               for (int i = 0 ; i < row.Count; i++) {
                newRow.Add(Metadata.Columns[i].Name, row[i]);
               }
               queryResultData.Add(newRow); 
            }
        }
        return new QueryResult(Id,Success,Error,SqlRc,SqlState,Metadata,IsDone,HasResults,UpdateCount, queryResultData, 
        ParameterCount,OutputParms);
        


    }
}






}