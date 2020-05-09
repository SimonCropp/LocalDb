using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using EfLocalDb;

namespace EfClassicLocalDb.Tests.Snippets
{
    static class SuppliedTemplate
    {
        static SqlInstance<MyDbContext> sqlInstance;

        static SuppliedTemplate()
        {
            var baseDir = Path.Join(AppDomain.CurrentDomain.BaseDirectory, "Test Data");
            var templatePath = Path.Join(baseDir, "template.mdf");
            var logPath = Path.Join(baseDir, "template_log.ldf");

            sqlInstance = new SqlInstance<MyDbContext>(
                connection => new MyDbContext(connection),
                templatePath: templatePath,
                logPath: logPath);
        }
    }
}
