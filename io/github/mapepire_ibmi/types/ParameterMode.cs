using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types {

/**
 * Enum representing the possible parameter modes.
 */

[JsonConverter(typeof(JsonStringEnumConverter<ParameterMode>))]
public enum ParameterMode {
    /**
     * The IN mode.
     */
    IN,

    /**
     * The OUT mode.
     */
    OUT,

    /**
     * The INOUT mode.
     */
    INOUT



}
}