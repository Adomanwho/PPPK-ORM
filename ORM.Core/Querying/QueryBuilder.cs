using Npgsql;
using ORM.Core.Metadata;
using System.Linq.Expressions;
using System.Text;

namespace ORM.Core.Querying;

/// <summary>
/// Builder pattern za SELECT upite nad entitetom T.
/// Metode se ulančavaju (fluent API), a Build() vraća finalni SQL s parametrima.
/// </summary>
public class QueryBuilder<T>
{
    private readonly EntityMetadata _metadata;
    private readonly List<NpgsqlParameter> _params = [];

    private readonly List<string> _whereClauses = [];
    private readonly List<string> _orderClauses = [];
    private readonly List<string> _includes      = [];
    private int? _skip;
    private int? _take;
    private int _paramCounter;

    public QueryBuilder(EntityMetadata metadata)
    {
        _metadata = metadata;
    }

    // ── Filtriranje ──────────────────────────────────────────────────────────

    public QueryBuilder<T> Where(Expression<Func<T, bool>> predicate)
    {
        var parser = new ExpressionParser(_metadata);
        var (sql, parameters) = parser.Parse(predicate);
        _whereClauses.Add(sql);

        // Renameaj parametre da ne dođu u konflikt s postojećima
        foreach (var p in parameters)
        {
            var renamed = new NpgsqlParameter($"@p{_paramCounter++}", p.Value);
            _params.Add(renamed);
            // Zamijeni originalno ime parametra u SQL fragmentu
            _whereClauses[^1] = _whereClauses[^1].Replace(p.ParameterName, renamed.ParameterName);
        }

        return this;
    }

    // ── Sortiranje ──────────────────────────────────────────────────────────

    public QueryBuilder<T> OrderBy<TKey>(Expression<Func<T, TKey>> keySelector)
        => AddOrder(keySelector, "ASC");

    public QueryBuilder<T> OrderByDescending<TKey>(Expression<Func<T, TKey>> keySelector)
        => AddOrder(keySelector, "DESC");

    private QueryBuilder<T> AddOrder<TKey>(Expression<Func<T, TKey>> keySelector, string direction)
    {
        var col = ResolveMemberColumn(keySelector.Body);
        _orderClauses.Add($"\"{col}\" {direction}");
        return this;
    }

    // ── Eager loading ────────────────────────────────────────────────────────

    /// <summary>
    /// Označava navigacijsko svojstvo za eager loading.
    /// DbSet koristi ovu listu pri izvršavanju upita.
    /// </summary>
    public QueryBuilder<T> Include<TNav>(Expression<Func<T, TNav>> navigation)
    {
        if (navigation.Body is MemberExpression m)
            _includes.Add(m.Member.Name);
        return this;
    }

    // ── Straničenje ──────────────────────────────────────────────────────────

    public QueryBuilder<T> Skip(int count) { _skip = count; return this; }
    public QueryBuilder<T> Take(int count) { _take = count; return this; }

    // ── Build ────────────────────────────────────────────────────────────────

    public QueryResult Build()
    {
        var sb = new StringBuilder();
        sb.Append($"SELECT * FROM {_metadata.QualifiedTableName}");

        if (_whereClauses.Count > 0)
            sb.Append($" WHERE {string.Join(" AND ", _whereClauses)}");

        if (_orderClauses.Count > 0)
            sb.Append($" ORDER BY {string.Join(", ", _orderClauses)}");

        if (_take.HasValue)
            sb.Append($" LIMIT {_take.Value}");

        if (_skip.HasValue)
            sb.Append($" OFFSET {_skip.Value}");

        return new QueryResult
        {
            Sql        = sb.ToString(),
            Parameters = _params.AsReadOnly()
        };
    }

    /// <summary>Lista navigacijskih svojstava za eager loading (čita DbSet).</summary>
    public IReadOnlyList<string> Includes => _includes.AsReadOnly();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private string ResolveMemberColumn(Expression body)
    {
        var memberName = body switch
        {
            MemberExpression m                              => m.Member.Name,
            UnaryExpression { Operand: MemberExpression m } => m.Member.Name,
            _ => throw new NotSupportedException("OrderBy podržava samo direktan pristup propertyju.")
        };

        var col = _metadata.Columns.FirstOrDefault(c => c.Property.Name == memberName);
        return col?.ColumnName ?? memberName;
    }
}
