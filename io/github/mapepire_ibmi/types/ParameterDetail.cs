using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types {


/**
 * Represents the parameter details for a query.
 */
public class ParameterDetail {
    /**
     * The parameter name.
     */
    [JsonPropertyName("name")]
    public String? Name { get; set; }

    /**
     * The parameter type.
     */
    [JsonPropertyName("type")]
    public String? Type { get; set; }

    /**
     * The parameter mode.
     */
    [JsonPropertyName("mode")]
    public ParameterMode? Mode { get; set; }

    /**
     * The parameter precision.
     */
    [JsonPropertyName("precision")]
    public int Precision { get; set; }

    /**
     * The parameter scale.
     */
    [JsonPropertyName("scale")]
    public int Scale { get; set; }

    /**
     * Construct a new ParameterDetail instance.
     */
    public ParameterDetail() {

    }

    /**
     * Construct a new ParameterDetail instance.
     * 
     * @param name      The parameter name.
     * @param type      The parameter type.
     * @param mode      The parameter mode.
     * @param precision The parameter precision.
     * @param scale     The parameter scale.
     */
    public ParameterDetail(String name, String type, ParameterMode mode, int precision, int scale) {
        this.Name = name;
        this.Type = type;
        this.Mode = mode;
        this.Precision = precision;
        this.Scale = scale;
    }

  }




}