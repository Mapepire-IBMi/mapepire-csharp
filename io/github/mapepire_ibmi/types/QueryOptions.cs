using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types { 


/**
 * Represents the options for configuring a query.
 */
public class QueryOptions {
    /**
     * Whether to return terse results.
     */
    [JsonPropertyName("isTerseResults")]
    public bool IsTerseResults { get; set; }

    /**
     * Whether the command is a CL command.
     */
    [JsonPropertyName("isClCommand")]
    public bool IsClCommand { get; set; }

    /**
     * The parameters for the query.
     */
    [JsonPropertyName("parameters")]
    public List<Object>? Parameters {get; set; }

    /**
     * Construct a new QueryOptions instance.
     */
    public QueryOptions() {

    }

    /**
     * Construct a new QueryOptions instance.
     * 
     * @param isTerseResults Whether to return terse results.
     * @param isClCommand    Whether the command is a CL command.
     * @param parameters     The parameters for the query.
     */
    public QueryOptions(bool isTerseResults, bool isClCommand, List<Object> parameters) {
        this.IsTerseResults = isTerseResults;
        this.IsClCommand = isClCommand;
        this.Parameters = parameters;
    }

}




}
