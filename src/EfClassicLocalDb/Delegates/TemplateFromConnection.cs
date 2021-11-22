using Microsoft.Data.SqlClient;

namespace EfLocalDb;

public delegate Task TemplateFromConnection(SqlConnection connection);