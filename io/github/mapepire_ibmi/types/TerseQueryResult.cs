using System.Text.Json;
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

    /// <summary>Converts positional rows to dictionaries using their column names.</summary>
    /// <returns>The query result with optional response fields preserved.</returns>
    /// <exception cref="JsonException">A row contains values with missing column metadata or names.</exception>
    public QueryResult ConvertToQueryResult()
    {
        List<Dictionary<string, object>>? resultData = null;
        if (Data is not null)
        {
            resultData = [];
            List<ColumnMetadata>? columns = Metadata?.Columns;
            foreach (List<object> row in Data)
            {
                if (row.Count > (columns?.Count ?? 0))
                {
                    throw new JsonException("Terse query row has more values than columns in its metadata.");
                }

                Dictionary<string, object> resultRow = [];
                for (int i = 0; i < row.Count; i++)
                {
                    string name = columns?[i]?.Name
                        ?? throw new JsonException($"Terse query metadata is missing the name for column {i + 1}.");
                    resultRow.Add(name, row[i]);
                }

                resultData.Add(resultRow);
            }
        }

        return new QueryResult
        {
            Id = Id,
            Success = Success,
            Error = Error,
            SqlRc = SqlRc,
            SqlState = SqlState,
            Metadata = Metadata,
            IsDone = IsDone,
            HasResults = HasResults,
            UpdateCount = UpdateCount,
            Data = resultData,
            ParameterCount = ParameterCount,
            OutputParms = OutputParms
        };
    }
}






}