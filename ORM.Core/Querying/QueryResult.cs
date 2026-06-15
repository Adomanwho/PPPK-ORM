using Npgsql;

namespace ORM.Core.Querying;

public class QueryResult
{
    public string Sql { get; init; } = null!;
    public IReadOnlyList<NpgsqlParameter> Parameters { get; init; } = [];
}
