using System.Data.Common;
using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DataLayer.EfCore;

public sealed class SqliteFunctionsInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        if (connection is SqliteConnection sqliteConnection)
            RegisterFunctions(sqliteConnection);
    }

    internal static void RegisterFunctions(SqliteConnection connection)
    {
        connection.CreateFunction(
            "swimdoc_contains",
            static (string? source, string? value) =>
                !string.IsNullOrEmpty(source) &&
                !string.IsNullOrEmpty(value) &&
                source.Contains(value, StringComparison.CurrentCultureIgnoreCase));
    }
}
