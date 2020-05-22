using System.Data.Common;
using System.Threading.Tasks;

namespace EfLocalDb
{
    public delegate Task TemplateFromConnection(DbConnection connection);
}