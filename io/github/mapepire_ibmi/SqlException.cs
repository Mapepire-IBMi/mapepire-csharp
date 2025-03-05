using io.github.mapepire_ibmi.types;


namespace io.github.mapepire_ibmi
{
    public class SqlException(string message, int sqlcode, string? sqlState) : Exception(message)
    {
        public int SqlCode { get; set; } = sqlcode;
        public string? SqlState { get; set; } = sqlState;
    }
}
