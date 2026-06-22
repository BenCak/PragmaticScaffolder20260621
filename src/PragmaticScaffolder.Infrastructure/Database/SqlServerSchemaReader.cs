using Microsoft.Data.SqlClient;
using PragmaticScaffolder.Application.Abstractions;
using PragmaticScaffolder.Domain.Models;

namespace PragmaticScaffolder.Infrastructure.Database;

/// <summary>Reads database schema from SQL Server via information_schema and sys views.</summary>
public sealed class SqlServerSchemaReader : ISchemaReader
{
    public async Task<bool> TestConnectionAsync(string connectionString, CancellationToken ct = default)
    {
        try
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<DatabaseMetadata> ReadDatabaseAsync(string connectionString, CancellationToken ct = default)
    {
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);

        var db = new DatabaseMetadata
        {
            DatabaseName = conn.Database,
            ServerVersion = conn.ServerVersion ?? string.Empty
        };

        var schemas = await ReadSchemasAsync(conn, ct);
        foreach (var schema in schemas)
        {
            schema.Tables           = await ReadTablesAsync(conn, schema.Name, ct);
            schema.StoredProcedures = await ReadStoredProceduresAsync(conn, schema.Name, ct);
        }

        db.Schemas = schemas;
        return db;
    }

    private static async Task<List<SchemaMetadata>> ReadSchemasAsync(SqlConnection conn, CancellationToken ct)
    {
        const string sql = """
            SELECT SCHEMA_NAME FROM INFORMATION_SCHEMA.SCHEMATA
            WHERE SCHEMA_NAME NOT IN ('information_schema','guest','sys')
            ORDER BY SCHEMA_NAME
            """;

        var schemas = new List<SchemaMetadata>();
        await using var cmd = new SqlCommand(sql, conn);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            schemas.Add(new SchemaMetadata { Name = reader.GetString(0) });
        return schemas;
    }

    private static async Task<List<TableMetadata>> ReadTablesAsync(
        SqlConnection conn, string schema, CancellationToken ct)
    {
        const string sql = """
            SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @schema AND TABLE_TYPE = 'BASE TABLE'
              AND TABLE_NAME NOT LIKE '\_%' ESCAPE '\'
            ORDER BY TABLE_NAME
            """;

        var tables = new List<TableMetadata>();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);

        await using (var reader = await cmd.ExecuteReaderAsync(ct))
            while (await reader.ReadAsync(ct))
                tables.Add(new TableMetadata { Schema = schema, Name = reader.GetString(0) });

        foreach (var table in tables)
        {
            table.Columns = await ReadColumnsAsync(conn, schema, table.Name, ct);
            table.ForeignKeys = await ReadForeignKeysAsync(conn, schema, table.Name, ct);
        }

        return tables;
    }

    private static async Task<List<ColumnMetadata>> ReadColumnsAsync(
        SqlConnection conn, string schema, string tableName, CancellationToken ct)
    {
        const string sql = """
            SELECT
                c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH,
                c.NUMERIC_PRECISION, c.NUMERIC_SCALE, c.IS_NULLABLE,
                c.COLUMN_DEFAULT, c.ORDINAL_POSITION,
                COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA+'.'+c.TABLE_NAME), c.COLUMN_NAME,'IsIdentity') AS IS_IDENTITY,
                COLUMNPROPERTY(OBJECT_ID(c.TABLE_SCHEMA+'.'+c.TABLE_NAME), c.COLUMN_NAME,'IsComputed') AS IS_COMPUTED,
                CASE WHEN kcu.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS IS_PK
            FROM INFORMATION_SCHEMA.COLUMNS c
            LEFT JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                ON kcu.TABLE_SCHEMA = c.TABLE_SCHEMA AND kcu.TABLE_NAME = c.TABLE_NAME
                AND kcu.COLUMN_NAME = c.COLUMN_NAME
                AND kcu.CONSTRAINT_NAME IN (
                    SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
                    WHERE TABLE_SCHEMA = c.TABLE_SCHEMA AND TABLE_NAME = c.TABLE_NAME
                      AND CONSTRAINT_TYPE = 'PRIMARY KEY')
            WHERE c.TABLE_SCHEMA = @schema AND c.TABLE_NAME = @table
            ORDER BY c.ORDINAL_POSITION
            """;

        var columns = new List<ColumnMetadata>();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", tableName);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var dataType = reader.GetString(1);
            var isNullable = reader.GetString(5) == "YES";
            var col = new ColumnMetadata
            {
                Name = reader.GetString(0),
                DataType = dataType,
                MaxLength = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                Precision = reader.IsDBNull(3) ? null : (int)reader.GetByte(3),
                Scale = reader.IsDBNull(4) ? null : reader.GetInt32(4),
                IsNullable = isNullable,
                DefaultValue = reader.IsDBNull(6) ? null : reader.GetString(6),
                OrdinalPosition = reader.GetInt32(7),
                IsIdentity = reader.GetInt32(8) == 1,
                IsComputed = reader.GetInt32(9) == 1,
                IsPrimaryKey = reader.GetInt32(10) == 1
            };
            (col.ClrType, col.IsNullableClrType) = MapClrType(dataType, isNullable);
            columns.Add(col);
        }
        return columns;
    }

    private static async Task<List<ForeignKeyMetadata>> ReadForeignKeysAsync(
        SqlConnection conn, string schema, string tableName, CancellationToken ct)
    {
        const string sql = """
            SELECT
                fk.name,
                COL_NAME(fkc.parent_object_id, fkc.parent_column_id),
                OBJECT_SCHEMA_NAME(fkc.referenced_object_id),
                OBJECT_NAME(fkc.referenced_object_id),
                COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id)
            FROM sys.foreign_keys fk
            INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
            WHERE OBJECT_SCHEMA_NAME(fk.parent_object_id) = @schema
              AND OBJECT_NAME(fk.parent_object_id) = @table
            ORDER BY fk.name
            """;

        var fks = new List<ForeignKeyMetadata>();
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        cmd.Parameters.AddWithValue("@table", tableName);

        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            fks.Add(new ForeignKeyMetadata
            {
                Name = reader.GetString(0),
                ColumnName = reader.GetString(1),
                ReferencedSchema = reader.GetString(2),
                ReferencedTable = reader.GetString(3),
                ReferencedColumn = reader.GetString(4)
            });
        return fks;
    }

    private static async Task<List<StoredProcedureMetadata>> ReadStoredProceduresAsync(
        SqlConnection conn, string schema, CancellationToken ct)
    {
        const string sql = """
            SELECT p.name, pp.name AS param_name, pp.parameter_id,
                   t.name AS type_name, pp.is_output,
                   pp.max_length, pp.precision, pp.scale
            FROM sys.procedures p
            JOIN sys.schemas s ON p.schema_id = s.schema_id
            LEFT JOIN sys.parameters pp
                ON pp.object_id = p.object_id AND pp.parameter_id > 0
            LEFT JOIN sys.types t ON pp.user_type_id = t.user_type_id
            WHERE s.name = @schema
            ORDER BY p.name, pp.parameter_id
            """;

        var procMap = new Dictionary<string, StoredProcedureMetadata>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@schema", schema);
        // Explicit using block so reader is fully closed before TryDescribeResultSetAsync opens another command
        await using (var reader = await cmd.ExecuteReaderAsync(ct))
        {
            while (await reader.ReadAsync(ct))
            {
                var procName = reader.GetString(0);
                if (!procMap.TryGetValue(procName, out var sp))
                {
                    sp = new StoredProcedureMetadata { Schema = schema, Name = procName };
                    procMap[procName] = sp;
                }
                if (!reader.IsDBNull(1))
                {
                    var dataType = reader.GetString(3);
                    var (clrType, _) = MapClrType(dataType, isNullable: true);
                    sp.Parameters.Add(new ProcParameterMetadata
                    {
                        ParameterName   = reader.GetString(1),
                        DataType        = dataType,
                        ClrType         = clrType,
                        IsOutput        = reader.GetBoolean(4),
                        MaxLength       = reader.IsDBNull(5) ? null : (int)reader.GetInt16(5),
                        Precision       = reader.IsDBNull(6) ? null : (int)reader.GetByte(6),
                        Scale           = reader.IsDBNull(7) ? null : (int)reader.GetByte(7),
                        OrdinalPosition = reader.GetInt32(2)
                    });
                }
            }
        } // reader disposed here — connection free for TryDescribeResultSetAsync

        foreach (var sp in procMap.Values)
            await TryDescribeResultSetAsync(conn, sp, ct);

        return [.. procMap.Values];
    }

    private static async Task TryDescribeResultSetAsync(
        SqlConnection conn, StoredProcedureMetadata sp, CancellationToken ct)
    {
        try
        {
            var paramList = string.Join(", ",
                sp.InputParameters.Select(p => $"{p.ParameterName} = NULL"));
            var execStr = $"EXEC {sp.QualifiedName}" +
                          (paramList.Length > 0 ? $" {paramList}" : "");

            const string sql = """
                SELECT name, system_type_name, is_nullable
                FROM sys.dm_exec_describe_first_result_set(@exec, NULL, 0)
                """;
            await using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@exec", execStr);
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var colName  = reader.IsDBNull(0) ? $"Col{sp.ResultColumns.Count + 1}" : reader.GetString(0);
                var sqlType  = reader.IsDBNull(1) ? "nvarchar" : reader.GetString(1).Split('(')[0].Trim();
                var nullable = !reader.IsDBNull(2) && reader.GetBoolean(2);
                var (clrType, _) = MapClrType(sqlType, isNullable: nullable);
                sp.ResultColumns.Add(new ProcResultColumnMetadata
                {
                    Name      = colName,
                    DataType  = sqlType,
                    ClrType   = clrType,
                    IsNullable = nullable
                });
            }
            sp.HasDescribableResult = sp.ResultColumns.Count > 0;
        }
        catch (Exception ex)
        {
            sp.HasDescribableResult = false;
            sp.DescribeError        = ex.Message;
        }
    }

    private static (string clrType, bool isNullable) MapClrType(string sqlType, bool isNullable)
    {
        var (clr, _) = sqlType.ToLowerInvariant() switch
        {
            "bigint"                                                         => ("long", true),
            "int"                                                            => ("int", true),
            "smallint"                                                       => ("short", true),
            "tinyint"                                                        => ("byte", true),
            "bit"                                                            => ("bool", true),
            "decimal" or "numeric" or "money" or "smallmoney"               => ("decimal", true),
            "float"                                                          => ("double", true),
            "real"                                                           => ("float", true),
            "datetime" or "smalldatetime" or "datetime2"                    => ("DateTime", true),
            "date"                                                           => ("DateOnly", true),
            "time"                                                           => ("TimeOnly", true),
            "datetimeoffset"                                                 => ("DateTimeOffset", true),
            "uniqueidentifier"                                               => ("Guid", true),
            "binary" or "varbinary" or "image" or "timestamp" or "rowversion" => ("byte[]", false),
            "char" or "nchar" or "varchar" or "nvarchar" or "text" or "ntext" or "xml" => ("string", false),
            _                                                                => ("object", false)
        };
        var fullClr = isNullable ? $"{clr}?" : clr;
        return (fullClr, isNullable);
    }
}
