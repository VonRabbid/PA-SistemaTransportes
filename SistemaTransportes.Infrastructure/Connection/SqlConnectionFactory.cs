using Microsoft.Data.SqlClient;

namespace SistemaTransportes.Infrastructure.Connection;

public class SqlConnectionFactory : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    public SqlConnection CreateConnection() => new(_connectionString);
}
