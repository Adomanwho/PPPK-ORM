using Npgsql;

namespace ORM.Core.Migrations;

public class DbColumnInfo
{
    public string TableName { get; init; } = null!;
    public string ColumnName { get; init; } = null!;
    public string DataType { get; init; } = null!;
    public bool IsNullable { get; init; }
    public string? ColumnDefault { get; init; }
    public bool IsIdentity { get; init; }
}

public class DbConstraintInfo
{
    public string TableName { get; init; } = null!;
    public string ConstraintName { get; init; } = null!;
    public string ConstraintType { get; init; } = null!; // PRIMARY KEY, UNIQUE, FOREIGN KEY
    public string ColumnName { get; init; } = null!;
    public string? ForeignTableName { get; init; }
    public string? ForeignColumnName { get; init; }
}

/// <summary>
/// Čita trenutno stanje sheme Postgres baze putem information_schema.
/// Koristi se u MigrationGenerator-u za diff s EntityMetadata.
/// </summary>
public class SchemaInspector
{
    private readonly NpgsqlConnection _connection;

    public SchemaInspector(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    /// <summary>Vraća lista svih tablica u public shemi.</summary>
    public List<string> GetTableNames()
    {
        const string sql = """
            SELECT table_name
            FROM information_schema.tables
            WHERE table_schema = 'public'
              AND table_type = 'BASE TABLE';
            """;

        var tables = new List<string>();
        using var cmd    = new NpgsqlCommand(sql, _connection);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
            tables.Add(reader.GetString(0));

        return tables;
    }

    /// <summary>Vraća sve stupce za danu tablicu.</summary>
    public List<DbColumnInfo> GetColumns(string tableName)
    {
        const string sql = """
            SELECT
                column_name,
                data_type,
                is_nullable,
                column_default,
                is_identity
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name   = @table
            ORDER BY ordinal_position;
            """;

        var columns = new List<DbColumnInfo>();
        using var cmd = new NpgsqlCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@table", tableName);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            columns.Add(new DbColumnInfo
            {
                TableName     = tableName,
                ColumnName    = reader.GetString(0),
                DataType      = reader.GetString(1),
                IsNullable    = reader.GetString(2) == "YES",
                ColumnDefault = reader.IsDBNull(3) ? null : reader.GetString(3),
                IsIdentity    = reader.GetString(4) == "YES"
            });
        }

        return columns;
    }

    /// <summary>Vraća sve constraint-e (PK, UNIQUE, FK) za danu tablicu.</summary>
    public List<DbConstraintInfo> GetConstraints(string tableName)
    {
        const string sql = """
            SELECT
                tc.constraint_name,
                tc.constraint_type,
                kcu.column_name,
                ccu.table_name  AS foreign_table,
                ccu.column_name AS foreign_column
            FROM information_schema.table_constraints tc
            JOIN information_schema.key_column_usage kcu
                ON tc.constraint_name = kcu.constraint_name
               AND tc.table_schema    = kcu.table_schema
            LEFT JOIN information_schema.referential_constraints rc
                ON tc.constraint_name = rc.constraint_name
            LEFT JOIN information_schema.constraint_column_usage ccu
                ON rc.unique_constraint_name = ccu.constraint_name
            WHERE tc.table_schema = 'public'
              AND tc.table_name   = @table;
            """;

        var constraints = new List<DbConstraintInfo>();
        using var cmd = new NpgsqlCommand(sql, _connection);
        cmd.Parameters.AddWithValue("@table", tableName);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            constraints.Add(new DbConstraintInfo
            {
                TableName       = tableName,
                ConstraintName  = reader.GetString(0),
                ConstraintType  = reader.GetString(1),
                ColumnName      = reader.GetString(2),
                ForeignTableName  = reader.IsDBNull(3) ? null : reader.GetString(3),
                ForeignColumnName = reader.IsDBNull(4) ? null : reader.GetString(4)
            });
        }

        return constraints;
    }

    /// <summary>Vraća sve stupce za sve tablice — ključ je ime tablice.</summary>
    public Dictionary<string, List<DbColumnInfo>> GetAllColumns()
    {
        var tables = GetTableNames();
        return tables.ToDictionary(t => t, GetColumns);
    }
}
