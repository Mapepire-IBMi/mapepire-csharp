using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types { 

public class ColumnMetadata {
    /**
     * The display size of the column.
     */
    [JsonPropertyName("display_size")]
    public int DisplaySize { get; set; }

    /**
     * The label of the column.
     */
    [JsonPropertyName("label")]
    public String? Label { get; set; }

    /**
     * The name of the column.
     */
    [JsonPropertyName("name")]
    public String? Name { get; set; }

    /**
     * The type of the column.
     */
    [JsonPropertyName("type")]
    public String? Type { get; set; }

    /**
     * The precision/length of the column.
     */
    [JsonPropertyName("precision")]
    public int Precision { get; set; }

    /**
     * The scale of the column.
     */
    [JsonPropertyName("scale")]
    public int Scale { get; set; }

    /**
     * Construct a new ColumnMetadata instance.
     */
    public ColumnMetadata() {

    }

    /**
     * Construct a new ColumnMetadata instance.
     *
     * @param displaySize The display size of the column.
     * @param label       The label of the column.
     * @param name        The name of the column.
     * @param type        The type of the column.
     * @param precision   The precision/length of the column.
     * @param scale       The scale of the column.
     */
    public ColumnMetadata(int displaySize, String label, String name, String type, int precision, int scale) {
        this.DisplaySize = displaySize;
        this.Label = label;
        this.Name = name;
        this.Type = type;
        this.Precision = precision;
        this.Scale = scale;
    }





}
}