static class DbNamer
{
    public static string DeriveDbName(string databaseSuffix, string memberName, string testClass)
    {
        #region DeriveName

        var dbName = $"{testClass}_{memberName}";
        if (databaseSuffix != null)
        {
            dbName = $"{dbName}_{databaseSuffix}";
        }

        #endregion

        return dbName;
    }
}