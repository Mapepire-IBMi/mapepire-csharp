using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types { 

/**
 * Represents the metadata for a query.
 */
public class QueryMetadata {
    /**
     * The number of columns returned by the query.
     */
    [JsonPropertyName("column_count")]
    public int ColumnCount { get; set; }

    /**
     * The metadata for each column.
     */
    [JsonPropertyName("columns")]
    public List<ColumnMetadata>? Columns {  get; set; }

    /**
     * The unique job identifier for the query.
     */
    [JsonPropertyName("job")]
    private String? Job { get; set; }

    /**
     * The parameters for the query.
     */
    [JsonPropertyName("parameters")]
    private List<ParameterDetail>? Parameters { get; set; }

    /**
     * Construct a new QueryMetadata instance.
     */
    public QueryMetadata() {

    }

    /**
     * Construct a new QueryMetadata instance.
     * 
     * @param columnCount The number of columns returned by the query.
     * @param columns     The metadata for each column.
     * @param job         The unique job identifier for the query.
     * @param parameters  The parameters for the query.
     */
    public QueryMetadata(int columnCount, List<ColumnMetadata> columns, String job, List<ParameterDetail> parameters) {
        this.ColumnCount = columnCount;
        this.Columns = columns;
        this.Job = job;
        this.Parameters = parameters;
    }

    




}
}