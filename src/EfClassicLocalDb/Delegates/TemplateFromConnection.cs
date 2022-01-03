using System.Data.Common;

namespace EfLocalDb;

public delegate Task TemplateFromConnection(DbConnection connection);