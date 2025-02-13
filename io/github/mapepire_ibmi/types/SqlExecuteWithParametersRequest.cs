using System.Reflection.Metadata.Ecma335;
using System.Text.Json.Serialization;

namespace io.github.mapepire_ibmi.types {


public class SqlExecuteWithParametersRequest
 {
 
 
    [JsonPropertyName("id")]
    public  String Id { get; set; }

    [JsonPropertyName("type")]
    public  String Type { get; set; }

    [JsonPropertyName("sql")]
    public  String Sql { get; set; }

    [JsonPropertyName("terse")]
    public  bool Terse { get; set; }

    [JsonPropertyName("rows")]
    public  int Rows { get; set; }



    [JsonPropertyName("parameters")]
      public List<Object>? Parameters { get; set; }

public SqlExecuteWithParametersRequest
(String id, String type, String sql, bool terse, int rows, List<Object> parameters) {
    this.Id = id; 
    this.Type = type;
    this.Sql = sql;
    this.Terse = terse;
    this.Rows = rows; 
    this.Parameters = parameters;
}


}
}