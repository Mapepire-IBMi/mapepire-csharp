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
     * Indicates whether the designated column is automatically numbered.
     */
    [JsonPropertyName("autoIncrement")]
    public bool AutoIncrement { get; set; }

    /**
     * Indicates the nullability of values in the designated column.
     */
    [JsonPropertyName("nullable")]
    public int Nullable { get; set; }

    /**
     * Indicates whether the designated column is definitely not writable.
     */
    [JsonPropertyName("readOnly")]
    public bool ReadOnly { get; set; }

    /**
     * Indicates whether it is possible for a write on the designated column to succeed.
     */
    [JsonPropertyName("writeable")]
    public bool Writeable { get; set; }

    /**
     * The column's table name.
     */
    [JsonPropertyName("table")]
    public String? Table { get; set; }
    
    /**
     * Construct a new ColumnMetadata instance.
     */
    public ColumnMetadata() {

    }

    /**
     * Construct a new ColumnMetadata instance.
     *
     * @param displaySize The display size of the column.
     * @param label          The label of the column.
     * @param name           The name of the column.
     * @param type           The type of the column.
     * @param precision      The precision/length of the column.
     * @param scale          The scale of the column.
     * @param autoIncrement  Indicates whether the designated column is automatically numbered.
     * @param nullable       Indicates the nullability of values in the designated column.
     * @param readOnly       Indicates whether the designated column is definitely not writable.
     * @param writeable      Indicates whether it is possible for a write on the designated column to succeed.
     * @param table          The column's table name.
     */
    public ColumnMetadata(int displaySize, String label, String name, String type, int precision, int scale, bool autoIncrement, int nullable, bool readOnly, bool writeable, String table) {
        this.DisplaySize = displaySize;
        this.Label = label;
        this.Name = name;
        this.Type = type;
        this.Precision = precision;
        this.Scale = scale;
        this.AutoIncrement = autoIncrement;
        this.Nullable = nullable;
        this.ReadOnly = readOnly;
        this.Writeable = writeable;
        this.Table = table;
    }





}
}
