static class DbNamer
{
    #region DeriveName
    public static string DeriveDbName(string databaseSuffix, string memberName, string testClass)
    {
        if (databaseSuffix == null)
        {
            return $"{testClass}_{memberName}";
        }
        return $"{testClass}_{memberName}_{databaseSuffix}";
    }
    #endregion
}