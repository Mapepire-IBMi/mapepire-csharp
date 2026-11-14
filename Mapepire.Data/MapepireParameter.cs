using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Mapepire.Data;

/// <summary>A parameter of a <see cref="MapepireCommand"/>.</summary>
public sealed class MapepireParameter : DbParameter
{
    private string _parameterName = string.Empty;
    private DbType? _dbType;

    /// <summary>Creates an unnamed parameter with no value.</summary>
    public MapepireParameter()
    {
    }

    /// <summary>Creates a parameter.</summary>
    /// <param name="parameterName">The name, with or without a leading <c>@</c>.</param>
    /// <param name="value">The value; null or <see cref="DBNull"/> sends SQL NULL.</param>
    public MapepireParameter(string? parameterName, object? value)
    {
        ParameterName = parameterName;
        Value = value;
    }

    /// <inheritdoc />
    public override DbType DbType
    {
        get => _dbType ?? InferDbType(Value);
        set => _dbType = value;
    }

    /// <inheritdoc />
    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;

    /// <inheritdoc />
    public override bool IsNullable { get; set; }

    /// <inheritdoc />
    [AllowNull]
    public override string ParameterName
    {
        get => _parameterName;
        set => _parameterName = value ?? string.Empty;
    }

    /// <inheritdoc />
    [AllowNull]
    public override string SourceColumn { get; set; } = string.Empty;

    /// <inheritdoc />
    public override bool SourceColumnNullMapping { get; set; }

    /// <inheritdoc />
    public override object? Value { get; set; }

    /// <inheritdoc />
    public override int Size { get; set; }

    /// <inheritdoc />
    public override byte Precision { get; set; }

    /// <inheritdoc />
    public override byte Scale { get; set; }

    /// <summary>The <see cref="DbType"/> set by the caller, or null if it is inferred from the value.</summary>
    internal DbType? ExplicitDbType => _dbType;

    /// <summary>The parameter name without a leading '@', ':' or '?'.</summary>
    internal string NormalizedName => Normalize(_parameterName);

    /// <inheritdoc />
    public override void ResetDbType() => _dbType = null;

    /// <summary>Removes a leading '@', ':' or '?' from a parameter name.</summary>
    internal static string Normalize(string name)
        => name.Length > 0 && name[0] is '@' or ':' or '?' ? name[1..] : name;

    /// <summary>Infers the <see cref="DbType"/> reported for a parameter whose type was not set explicitly.</summary>
    private static DbType InferDbType(object? value) => value switch
    {
        null or DBNull => DbType.Object,
        string => DbType.String,
        bool => DbType.Boolean,
        byte => DbType.Byte,
        short => DbType.Int16,
        int => DbType.Int32,
        long => DbType.Int64,
        float => DbType.Single,
        double => DbType.Double,
        decimal => DbType.Decimal,
        DateTime => DbType.DateTime2,
        DateTimeOffset => DbType.DateTimeOffset,
        DateOnly => DbType.Date,
        TimeOnly or TimeSpan => DbType.Time,
        Guid => DbType.Guid,
        byte[] => DbType.Binary,
        _ => DbType.Object,
    };
}

/// <summary>The parameters of a <see cref="MapepireCommand"/>.</summary>
public sealed class MapepireParameterCollection : DbParameterCollection
{
    private readonly List<MapepireParameter> _items = [];

    internal MapepireParameterCollection()
    {
    }

    /// <inheritdoc />
    public override int Count => _items.Count;

    /// <inheritdoc />
    public override object SyncRoot => ((ICollection)_items).SyncRoot;

    /// <summary>Gets or sets the parameter at an index.</summary>
    public new MapepireParameter this[int index]
    {
        get => _items[index];
        set => _items[index] = value;
    }

    /// <summary>Gets or sets a parameter by name; a leading <c>@</c>, <c>:</c> or <c>?</c> is ignored.</summary>
    public new MapepireParameter this[string parameterName]
    {
        get => _items[IndexOfChecked(parameterName)];
        set => _items[IndexOfChecked(parameterName)] = value;
    }

    /// <summary>Adds a parameter with the given name and value.</summary>
    /// <returns>The parameter that was added.</returns>
    public MapepireParameter AddWithValue(string parameterName, object? value)
    {
        var parameter = new MapepireParameter(parameterName, value);
        _items.Add(parameter);
        return parameter;
    }

    /// <inheritdoc />
    public override int Add(object value)
    {
        _items.Add(Cast(value));
        return _items.Count - 1;
    }

    /// <summary>Adds a parameter.</summary>
    /// <returns>The parameter that was added.</returns>
    public MapepireParameter Add(MapepireParameter parameter)
    {
        _items.Add(parameter);
        return parameter;
    }

    /// <inheritdoc />
    public override void AddRange(Array values)
    {
        foreach (var value in values)
        {
            Add(value ?? throw new ArgumentException("The array contains a null element.", nameof(values)));
        }
    }

    /// <inheritdoc />
    public override void Clear() => _items.Clear();

    /// <inheritdoc />
    public override bool Contains(object value) => value is MapepireParameter parameter && _items.Contains(parameter);

    /// <inheritdoc />
    public override bool Contains(string value) => IndexOf(value) >= 0;

    /// <inheritdoc />
    public override void CopyTo(Array array, int index) => ((ICollection)_items).CopyTo(array, index);

    /// <inheritdoc />
    public override IEnumerator GetEnumerator() => _items.GetEnumerator();

    /// <inheritdoc />
    public override int IndexOf(object value) => value is MapepireParameter parameter ? _items.IndexOf(parameter) : -1;

    /// <inheritdoc />
    public override int IndexOf(string parameterName)
    {
        var name = MapepireParameter.Normalize(parameterName);
        for (var i = 0; i < _items.Count; i++)
        {
            if (string.Equals(_items[i].NormalizedName, name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    /// <inheritdoc />
    public override void Insert(int index, object value) => _items.Insert(index, Cast(value));

    /// <inheritdoc />
    public override void Remove(object value) => _items.Remove(Cast(value));

    /// <inheritdoc />
    public override void RemoveAt(int index) => _items.RemoveAt(index);

    /// <inheritdoc />
    public override void RemoveAt(string parameterName) => _items.RemoveAt(IndexOfChecked(parameterName));

    /// <inheritdoc />
    protected override DbParameter GetParameter(int index) => _items[index];

    /// <inheritdoc />
    protected override DbParameter GetParameter(string parameterName) => _items[IndexOfChecked(parameterName)];

    /// <inheritdoc />
    protected override void SetParameter(int index, DbParameter value) => _items[index] = Cast(value);

    /// <inheritdoc />
    protected override void SetParameter(string parameterName, DbParameter value) => _items[IndexOfChecked(parameterName)] = Cast(value);

    private static MapepireParameter Cast(object value)
        => value as MapepireParameter
            ?? throw new InvalidCastException($"Only {nameof(MapepireParameter)} objects can be added to the collection.");

    private int IndexOfChecked(string parameterName)
    {
        var index = IndexOf(parameterName);
        return index >= 0 ? index : throw new IndexOutOfRangeException($"Parameter '{parameterName}' not found.");
    }
}
