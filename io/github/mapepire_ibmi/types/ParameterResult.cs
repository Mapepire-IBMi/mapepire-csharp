using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types {



/**
 * Represents the parameter result for a query.
 */
public class ParameterResult {
    /**
     * The parameter result name.
     */
    [JsonPropertyName("name")]
    public String? Name { get; set; }

    /**
     * The parameter result type.
     */
    [JsonPropertyName("type")]
    public String? Type { get; set; }

    /**
     * The parameter result index.
     */
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /**
     * The parameter result precision.
     */
    [JsonPropertyName("precision")]
    public int Precision { get; set; }

    /**
     * The parameter result scale.
     */
  [JsonPropertyName("scale")]
    public int Scale { get; set; }

    /**
     * The parameter result CCSID.
     */
    [JsonPropertyName("ccsid")]
    public int Ccsid { get; set; }

    /**
     * The parameter result value (only available for OUT/INOUT).
     */
    [JsonPropertyName("value")]
    public Object? Value { get; set; }

    /**
     * Construct a new ParameterResult instance.
     */
    public ParameterResult() {

    }

    /**
     * Construct a new ParameterResult instance.
     * 
     * @param name      The parameter result name.
     * @param type      The parameter result type.
     * @param index     The parameter result index.
     * @param precision The parameter result precision.
     * @param scale     The parameter result scale.
     * @param ccsid     The parameter result ccsid.
     * @param value     The parameter result value (only available for OUT/INOUT).
     */
    public ParameterResult(String name, String type, int index, int precision, int scale, int ccsid, Object value) {
        this.Name = name;
        this.Type = type;
        this.Index = index;
        this.Precision = precision;
        this.Scale = scale;
    }

}
  




}