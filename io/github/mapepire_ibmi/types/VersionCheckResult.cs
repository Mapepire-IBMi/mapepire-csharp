using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types {

/**
 * Represents a standard server response.
 */
public class VersionCheckResult : ServerResponse {


    /**
     * The build date of the version.
     */
    [JsonPropertyName("buildDate")]
    public String? BuildDate {get; set;}


    /**
     * The version string.
     */
    [JsonPropertyName("version")]
    public String? Version {get; set; }


    /**
     * Construct a new ServerResponse instance.
     */
    public VersionCheckResult() {

    }

   /**
     * Construct a new VersionCheckResult instance.
     * 
     * @param id        The unique identifier for the request.
     * @param success   Whether the request was successful.
     * @param error     The error message, if any.
     * @param sqlRc     The SQL return code.
     * @param sqlState  The SQL state code.
     * @param buildDate The build date of the version.
     * @param version   The version string.
     */
    public VersionCheckResult(String id, bool success, String error, int sqlRc, String sqlState,
            String buildDate, String version) :base (id, success, error, sqlRc, sqlState) {
       
        this.BuildDate = buildDate;
        this.Version = version;
    }


}
}