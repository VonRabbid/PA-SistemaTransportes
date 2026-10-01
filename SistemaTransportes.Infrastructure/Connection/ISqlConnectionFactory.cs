using Microsoft.Data.SqlClient;

namespace SistemaTransportes.Infrastructure.Connection;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}
